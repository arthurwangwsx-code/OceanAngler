import hashlib,importlib.util,json,plistlib,sys,tempfile,unittest,zipfile
from pathlib import Path
from unittest.mock import patch
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import beta
class DeliveryGuards(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.root=Path(self.temp.name);self.local=self.root/'.local/beta';self.local.mkdir(parents=True)
        self.patches=[patch.object(beta,'ROOT',self.root),patch.object(beta,'LOCAL',self.local)]
        for p in self.patches:p.start()
        self.sign={'teamId':beta.TEAM,'bundleId':beta.BUNDLE,'appId':beta.APP_ID,'profileUUID':'PROFILE','certificateSha1':'CERT'}
        (self.local/'signing.json').write_text(json.dumps(self.sign));self.options=dict(method='app-store-connect',destination='export',teamID=beta.TEAM,provisioningProfiles={beta.BUNDLE:'PROFILE'},manageAppVersionAndBuildNumber=False,testFlightInternalTestingOnly=True)
        (self.local/'ExportOptions.plist').write_bytes(plistlib.dumps(self.options));self.archive=self.root/'archive';self.archive.mkdir();self.receipt={'archiveDigest':'HASH','sourceCommit':'COMMIT'}
    def tearDown(self):
        for p in reversed(self.patches):p.stop()
        self.temp.cleanup()
    def ipa_baseline(self):
        path=self.root/'build/TestFlight-1-export/app.ipa';path.parent.mkdir(parents=True);path.write_bytes(b'known ipa')
        (self.local/'1').mkdir(exist_ok=True);(self.local/'1/export.json').write_text(json.dumps({'ipaSha256':hashlib.sha256(path.read_bytes()).hexdigest()}))
    def test_internal_only_option_is_required(self):
        self.options['testFlightInternalTestingOnly']=False;(self.local/'ExportOptions.plist').write_bytes(plistlib.dumps(self.options))
        with patch.object(beta,'verify_archive',return_value=(self.archive,self.receipt)),patch.object(beta,'command') as transport:
            with self.assertRaises(RuntimeError):beta.distribute(None,'1',False)
            transport.assert_not_called()
    def test_unknown_upload_attempt_never_repeats_transport(self):
        self.ipa_baseline();(self.local/'1/upload.json').write_text('{"status":"ATTEMPTED"}')
        with patch.object(beta,'verify_archive',return_value=(self.archive,self.receipt)),patch.object(beta,'status',return_value={'buildId':None}),patch.object(beta,'audit_access'),patch.object(beta,'command') as transport:
            with self.assertRaises(RuntimeError):beta.distribute(None,'1',True)
            transport.assert_not_called()
    def test_existing_apple_build_is_reconciled(self):
        self.ipa_baseline()
        with patch.object(beta,'verify_archive',return_value=(self.archive,self.receipt)),patch.object(beta,'status',return_value={'buildId':'B1','processingState':'PROCESSING'}),patch.object(beta,'command') as transport:
            self.assertEqual('B1',beta.distribute(None,'1',True)['buildId']);transport.assert_not_called()
    def test_modified_local_ipa_cannot_upload(self):
        self.ipa_baseline();(self.root/'build/TestFlight-1-export/app.ipa').write_bytes(b'modified')
        with patch.object(beta,'verify_archive',return_value=(self.archive,self.receipt)),patch.object(beta,'command') as transport:
            with self.assertRaises(RuntimeError):beta.distribute(None,'1',True)
            transport.assert_not_called()
    def test_export_checks_app_identity_inside_ipa(self):
        def export(args,log):
            path=self.root/'build/TestFlight-1-export';path.mkdir(parents=True)
            with zipfile.ZipFile(path/'wrong.ipa','w') as ipa:ipa.writestr('Payload/Game.app/Info.plist',plistlib.dumps(dict(CFBundleIdentifier='com.example.wrong',CFBundleShortVersionString=beta.VERSION,CFBundleVersion='1')))
        with patch.object(beta,'verify_archive',return_value=(self.archive,self.receipt)),patch.object(beta,'archive_digest',return_value='HASH'),patch.object(beta,'command',side_effect=export):
            with self.assertRaises(RuntimeError):beta.distribute(None,'1',False)
        self.assertFalse((self.local/'1/export.json').exists())
    def test_wrong_connected_app_is_rejected(self):
        with patch.object(beta,'Client') as client:
            client.return_value.status.return_value={'apps':[{'id':'another','bundleId':beta.BUNDLE}]}
            with self.assertRaises(RuntimeError):beta.app_client()
    def test_support_email_does_not_implicitly_authorize_testing(self):
        owner={'username':'owner@example.com'}
        with patch.object(beta,'RELEASE',{'supportEmail':'support@example.com'}),patch.object(beta,'owner_email',return_value=owner),patch.object(beta,'items') as query:
            self.assertEqual({'owner@example.com':owner},beta.approved_users(None));query.assert_not_called()
    def test_explicit_tester_requires_existing_eligible_app_access(self):
        owner={'username':'owner@example.com'};user={'id':'U','attributes':{'username':'tester@example.com','roles':['DEVELOPER'],'allAppsVisible':False}}
        with patch.object(beta,'RELEASE',{'internalTesterEmails':['tester@example.com']}),patch.object(beta,'owner_email',return_value=owner),patch.object(beta,'items',side_effect=[[user],[]]):
            with self.assertRaises(RuntimeError):beta.approved_users(None)
        user['attributes']['allAppsVisible']=True
        with patch.object(beta,'RELEASE',{'internalTesterEmails':['tester@example.com']}),patch.object(beta,'owner_email',return_value=owner),patch.object(beta,'items',return_value=[user]):
            self.assertEqual({'owner@example.com','tester@example.com'},set(beta.approved_users(None)))
    def test_missing_explicit_asc_account_cannot_be_invited_as_an_external_tester(self):
        with patch.object(beta,'RELEASE',{'internalTesterEmails':['missing@example.com']}),patch.object(beta,'owner_email',return_value={'username':'owner@example.com'}),patch.object(beta,'items',return_value=[]):
            with self.assertRaises(RuntimeError):beta.approved_users(None)
    def test_access_audit_still_rejects_an_unapproved_group_member(self):
        group={'id':'G','attributes':{'isInternalGroup':True,'publicLinkEnabled':False,'hasAccessToAllBuilds':True}}
        def query(c,path,query=None):
            return [group] if path=='/v1/betaGroups' else [{'attributes':{'email':'outsider@example.com'}}]
        with patch.object(beta,'items',side_effect=query):
            with self.assertRaises(RuntimeError):beta.audit_access(None,None,{'owner@example.com':{},'tester@example.com':{}})
    def test_explicit_personal_account_is_added_once_and_read_back(self):
        from unittest.mock import Mock
        members=[{'id':'T1','attributes':{'email':'owner@example.com'}}]
        group={'id':'G','attributes':{'name':'OceanAngler Personal','isInternalGroup':True,'publicLinkEnabled':False}}
        def query(c,path,query=None):
            if path=='/v1/betaGroups':return [group]
            if path=='/v1/betaTesters':return []
            if path.endswith('/betaTesters'):return list(members)
            if path.endswith('/builds'):return [{'id':'B'}]
            return []
        def request(method,path,**kwargs):
            self.assertEqual(('POST','/v1/betaTesters'),(method,path))
            self.assertEqual('tester@example.com',kwargs['payload']['data']['attributes']['email'])
            members.append({'id':'T2','attributes':{'email':'tester@example.com'}});return {'data':members[-1]}
        client=Mock();client.request.side_effect=request
        approved={e:{'username':e,'firstName':'Test','lastName':'User'} for e in ['owner@example.com','tester@example.com']}
        with patch.object(beta,'status',return_value={'buildId':'B','processingState':'VALID','internalBuildState':'READY_FOR_BETA_TESTING'}),patch.object(beta,'approved_users',return_value=approved),patch.object(beta,'audit_access',return_value={'authorizedAccountsOnly':True}),patch.object(beta,'items',side_effect=query):
            self.assertEqual(2,beta.self_only(client,'1')['testerCount'])
            self.assertEqual(2,beta.self_only(client,'1')['testerCount'])
        self.assertEqual(1,client.request.call_count)
if __name__=='__main__':unittest.main()
