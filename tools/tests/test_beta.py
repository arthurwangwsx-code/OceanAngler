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
if __name__=='__main__':unittest.main()
