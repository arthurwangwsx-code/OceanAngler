#!/usr/bin/env python3
"""Prepare only OceanAngler's profile using an existing distribution private key."""
import argparse,base64,datetime,hashlib,json,plistlib,re,subprocess
from pathlib import Path
from asc import Client,BUNDLE,APP_ID
ROOT=Path(__file__).resolve().parents[1]
LOCAL=ROOT/'.local/beta';TEAM=json.loads((ROOT/'publication/release.json').read_text())['teamId']

def prepare(apply):
    client=Client();state=client.status()
    if len(state['apps'])!=1 or state['apps'][0]['id']!=APP_ID or len(state['bundles'])!=1:raise RuntimeError('App identity mismatch')
    bundle=client.request('GET','/v1/bundleIds/'+state['bundles'][0]['id'])['data']
    if bundle['attributes'].get('seedId')!=TEAM:raise RuntimeError('Team mismatch')
    raw=subprocess.check_output(['security','find-identity','-v','-p','codesigning'],text=True)
    local={fingerprint.upper() for fingerprint,name in re.findall(r'([A-Fa-f0-9]{40}) "([^"\n]+)"',raw) if 'Apple Distribution' in name and TEAM in name}
    now=datetime.datetime.now(datetime.timezone.utc)
    def valid_date(value):return datetime.datetime.fromisoformat(value.replace('Z','+00:00'))>now+datetime.timedelta(days=1)
    choices=[]
    for c in client.request('GET','/v1/certificates',{'limit':200})['data']:
        a=c['attributes']
        if a.get('certificateType') not in ('DISTRIBUTION','IOS_DISTRIBUTION') or not a.get('certificateContent') or not valid_date(a['expirationDate']):continue
        digest=hashlib.sha1(base64.b64decode(a['certificateContent'])).hexdigest().upper()
        if digest in local:choices.append((a['expirationDate'],c,digest))
    if not choices:raise RuntimeError('Existing distribution key not available')
    _,certificate,fingerprint=max(choices,key=lambda row:row[0])
    profiles=client.request('GET','/v1/bundleIds/'+bundle['id']+'/profiles',{'limit':200})['data'];profile=None
    for p in profiles:
        a=p['attributes']
        if a.get('profileType')!='IOS_APP_STORE' or a.get('profileState')!='ACTIVE' or not valid_date(a['expirationDate']):continue
        linked=client.request('GET','/v1/profiles/'+p['id']+'/certificates')['data']
        if any(c['id']==certificate['id'] for c in linked):profile=p;break
    if not apply:return dict(status='SIGNING_PREFLIGHT_PASS',existingPrivateKey=True,existingAppProfile=profile is not None)
    LOCAL.mkdir(parents=True,exist_ok=True)
    journal=LOCAL/'profile-attempt.json'
    if profile is None:
        if journal.exists():raise RuntimeError('Prior profile request requires live reconciliation')
        name='OceanAngler Internal '+now.strftime('%Y%m%dT%H%M%SZ')
        journal.write_text(json.dumps(dict(status='ATTEMPTED',name=name)))
        profile=client.request('POST','/v1/profiles',payload={'data':{'type':'profiles','attributes':{'name':name,'profileType':'IOS_APP_STORE'},'relationships':{'bundleId':{'data':{'type':'bundleIds','id':bundle['id']}},'certificates':{'data':[{'type':'certificates','id':certificate['id']}]}}}})['data']
        journal.write_text(json.dumps(dict(status='CREATED',profileId=profile['id'])))
    profile=client.request('GET','/v1/profiles/'+profile['id'])['data']
    linked=client.request('GET','/v1/profiles/'+profile['id']+'/bundleId')['data']
    if linked['id']!=bundle['id'] or profile['attributes']['profileState']!='ACTIVE':raise RuntimeError('Profile not scoped to app')
    content=base64.b64decode(profile['attributes']['profileContent'],validate=True)
    path=LOCAL/'OceanAngler.mobileprovision';path.write_bytes(content);path.chmod(0o600)
    decoded=plistlib.loads(subprocess.check_output(['security','cms','-D','-i',str(path)],stderr=subprocess.DEVNULL));entitlements=decoded['Entitlements']
    if decoded['TeamIdentifier']!=[TEAM] or entitlements.get('application-identifier')!=TEAM+'.'+BUNDLE or entitlements.get('get-task-allow') is not False or decoded.get('ProvisionedDevices') or decoded.get('ProvisionsAllDevices'):raise RuntimeError('Invalid distribution entitlements')
    if fingerprint not in {hashlib.sha1(c).hexdigest().upper() for c in decoded['DeveloperCertificates']}:raise RuntimeError('Profile certificate mismatch')
    destination=Path.home()/'Library/Developer/Xcode/UserData/Provisioning Profiles'/(decoded['UUID']+'.mobileprovision');destination.parent.mkdir(parents=True,exist_ok=True)
    if destination.exists() and destination.read_bytes()!=content:raise RuntimeError('Existing UUID content conflict')
    destination.write_bytes(content);destination.chmod(0o600)
    config=dict(teamId=TEAM,bundleId=BUNDLE,appId=APP_ID,profileUUID=decoded['UUID'],profileName=decoded['Name'],certificateSha1=fingerprint,profileSha256=hashlib.sha256(content).hexdigest())
    (LOCAL/'signing.json').write_text(json.dumps(config,indent=2))
    options=dict(method='app-store-connect',destination='export',signingStyle='manual',teamID=TEAM,signingCertificate=fingerprint,provisioningProfiles={BUNDLE:decoded['UUID']},manageAppVersionAndBuildNumber=False,testFlightInternalTestingOnly=True,uploadSymbols=True)
    (LOCAL/'ExportOptions.plist').write_bytes(plistlib.dumps(options))
    return dict(status='SIGNING_READY',appId=APP_ID,bundleId=BUNDLE,existingPrivateKeyReused=True,appProfileInstalled=True)
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--apply',action='store_true');a=p.parse_args()
    try:print(json.dumps(prepare(a.apply),indent=2))
    except Exception as e:print(json.dumps(dict(status='SIGNING_NOT_CONFIRMED',errorType=type(e).__name__)));raise SystemExit(1)
