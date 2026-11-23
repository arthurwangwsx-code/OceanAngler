#!/usr/bin/env python3
"""Frozen-source, app-scoped OceanAngler internal beta delivery."""
import argparse,hashlib,json,os,plistlib,re,subprocess,urllib.parse,zipfile
from pathlib import Path
from asc import APP_ID,BUNDLE,Client,credentials
ROOT=Path(__file__).resolve().parents[1];RELEASE=json.loads((ROOT/'publication/release.json').read_text());VERSION=RELEASE['version'];TEAM=RELEASE['teamId'];LOCAL=ROOT/'.local/beta'

def read(path):return json.loads(path.read_text())
def write(path,value):path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(value,indent=2)+'\n')
def command(args,log):
    log.parent.mkdir(parents=True,exist_ok=True)
    with log.open('w') as output:r=subprocess.run([str(x) for x in args],cwd=ROOT,stdout=output,stderr=subprocess.STDOUT)
    if r.returncode:raise RuntimeError('Command failed; inspect private '+log.name)
def hash_files(base,paths):
    h=hashlib.sha256()
    for path in sorted(paths,key=lambda p:p.relative_to(base).as_posix()):
        h.update(path.relative_to(base).as_posix().encode())
        with path.open('rb') as f:
            for chunk in iter(lambda:f.read(1<<20),b''):h.update(chunk)
    return h.hexdigest()
def source_digest():
    paths=subprocess.check_output(['git','ls-files','-z','--','game/Assets','game/Packages','game/ProjectSettings','publication/release.json','tools'],cwd=ROOT).decode().split('\0')
    return hash_files(ROOT,[ROOT/p for p in filter(None,paths) if (ROOT/p).is_file()])
def tree_digest(folder):return hash_files(folder,[p for p in folder.rglob('*') if p.is_file()])
def archive_digest(folder):
    h=hashlib.sha256()
    for path in sorted((p for p in folder.rglob('*') if p.is_file()),key=lambda p:p.relative_to(folder).as_posix()):
        h.update(path.relative_to(folder).as_posix().encode());data=path.read_bytes()
        if path==folder/'Info.plist':
            value=plistlib.loads(data);value.pop('Distributions',None);data=plistlib.dumps(value,sort_keys=True)
        h.update(data)
    return h.hexdigest()
def current_commit():return subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
def require_clean_source():
    state=subprocess.check_output(['git','status','--porcelain','--','game','publication/release.json','tools'],cwd=ROOT,text=True)
    if state.strip():raise RuntimeError('Commit the exact game/release/tool source before freezing')
def items(client,path,query=None):
    values=[]
    for _ in range(20):
        result=client.request('GET',path,query);values.extend(result['data']);nxt=result.get('links',{}).get('next')
        if not nxt:return values
        uri=urllib.parse.urlsplit(nxt)
        if uri.netloc!='api.appstoreconnect.apple.com' or uri.scheme!='https':raise RuntimeError('Unexpected pagination origin')
        path=uri.path;query=dict(urllib.parse.parse_qsl(uri.query))
    raise RuntimeError('Incomplete pagination')
def app_client():
    c=Client();s=c.status()
    if len(s['apps'])!=1 or s['apps'][0]['id']!=APP_ID or s['apps'][0]['bundleId']!=BUNDLE:raise RuntimeError('App scope mismatch')
    return c
def find_build(client,number):
    for b in items(client,'/v1/builds',{'filter[app]':APP_ID,'limit':100}):
        if b['attributes']['version']!=number:continue
        v=client.request('GET','/v1/builds/'+b['id']+'/preReleaseVersion')['data']
        if v['attributes']['version']==VERSION:return b
    return None
def status(client,number):
    result=dict(appId=APP_ID,bundleId=BUNDLE,version=VERSION,build=number,processingState=None,buildId=None,actualInstallation='NOT_RUN')
    b=find_build(client,number)
    if b:
        detail=client.request('GET','/v1/builds/'+b['id']+'/buildBetaDetail')['data']
        result.update(buildId=b['id'],processingState=b['attributes']['processingState'],internalBuildState=detail['attributes'].get('internalBuildState'))
    return result

def native(number,label):
    require_clean_source();commit=current_commit();before=source_digest();folder=LOCAL/number;receipt=folder/'native.json'
    if receipt.exists():raise RuntimeError('Native freeze already exists; verify before reusing')
    command(['python3','tools/game.py','ios','--build',number,'--label',label],folder/'native.log')
    require_clean_source()
    if before!=source_digest() or commit!=current_commit():raise RuntimeError('Source changed while exporting; native export is not frozen')
    output=ROOT/'build'/('ios-'+number+'-'+label)
    write(receipt,dict(sourceCommit=commit,sourceDigest=before,nativeDigest=tree_digest(output),nativePath=str(output.relative_to(ROOT)),version=VERSION,build=number,bundleId=BUNDLE))
    return dict(status='NATIVE_FROZEN',sourceCommit=commit,build=number)
def verify_archive(number):
    folder=ROOT/'build'/('OceanAngler-'+number+'.xcarchive');receipt=read(LOCAL/number/'archive.json')
    if archive_digest(folder)!=receipt['archiveDigest']:raise RuntimeError('Archive changed after verification')
    return folder,receipt

def archive(number):
    require_clean_source();receipt=read(LOCAL/number/'native.json');sign=read(LOCAL/'signing.json');export=ROOT/receipt['nativePath'];folder=ROOT/'build'/('OceanAngler-'+number+'.xcarchive')
    if (receipt['sourceCommit'],receipt['sourceDigest'])!=(current_commit(),source_digest()) or receipt['nativeDigest']!=tree_digest(export):raise RuntimeError('Frozen native/source identity changed')
    if sign['appId']!=APP_ID or sign['bundleId']!=BUNDLE or sign['teamId']!=TEAM:raise RuntimeError('Signing scope mismatch')
    if folder.exists():raise RuntimeError('Archive path exists; preserve and reconcile it')
    command(['xcodebuild','-project',export/'Unity-iPhone.xcodeproj','-scheme','Unity-iPhone','-configuration','Release','-destination','generic/platform=iOS','-archivePath',folder,'-derivedDataPath',ROOT/'build/DerivedData-beta','DEVELOPMENT_TEAM='+TEAM,'CODE_SIGN_IDENTITY='+sign['certificateSha1'],'OCEAN_PROFILE_NAME='+sign['profileName'],'OCEAN_PROFILE_UUID='+sign['profileUUID'],'archive'],LOCAL/number/'archive.log')
    if source_digest()!=receipt['sourceDigest']:raise RuntimeError('Game source changed during archive')
    apps=list((folder/'Products/Applications').glob('*.app'))
    if len(apps)!=1:raise RuntimeError('Unexpected archive application count')
    app=apps[0];info=plistlib.loads((app/'Info.plist').read_bytes())
    if (info['CFBundleIdentifier'],info['CFBundleShortVersionString'],info['CFBundleVersion'])!=(BUNDLE,VERSION,number):raise RuntimeError('Archive app identity mismatch')
    command(['codesign','--verify','--deep','--strict',app],LOCAL/number/'codesign.log')
    ent=plistlib.loads(subprocess.check_output(['codesign','-d','--entitlements',':-',app],stderr=subprocess.DEVNULL))
    profile=plistlib.loads(subprocess.check_output(['security','cms','-D','-i',app/'embedded.mobileprovision'],stderr=subprocess.DEVNULL))
    if ent.get('application-identifier')!=TEAM+'.'+BUNDLE or ent.get('get-task-allow',False) or profile['UUID']!=sign['profileUUID'] or profile['Entitlements'].get('get-task-allow') is not False:raise RuntimeError('Archive entitlements/profile mismatch')
    receipt.update(status='ARCHIVE_VERIFIED',archiveDigest=archive_digest(folder),executableSha256=hashlib.sha256((app/info['CFBundleExecutable']).read_bytes()).hexdigest())
    write(LOCAL/number/'archive.json',receipt);return dict(status='ARCHIVE_VERIFIED',build=number,sourceCommit=receipt['sourceCommit'])
def distribute(client,number,upload):
    folder,receipt=verify_archive(number);sign=read(LOCAL/'signing.json');options=plistlib.loads((LOCAL/'ExportOptions.plist').read_bytes());step='upload' if upload else 'export';target=ROOT/'build'/('TestFlight-'+number+'-'+step)
    if options.get('testFlightInternalTestingOnly') is not True or options.get('manageAppVersionAndBuildNumber') is not False or options.get('teamID')!=TEAM or options.get('provisioningProfiles')!={BUNDLE:sign['profileUUID']} or options.get('method')!='app-store-connect':raise RuntimeError('Internal-only export contract changed')
    if upload:
        exported=read(LOCAL/number/'export.json')
        local_ipas=list((ROOT/'build'/('TestFlight-'+number+'-export')).glob('*.ipa'))
        if len(local_ipas)!=1 or hashlib.sha256(local_ipas[0].read_bytes()).hexdigest()!=exported['ipaSha256']:raise RuntimeError('Verified local IPA required')
        existing=status(client,number)
        if existing['buildId']:return existing
        audit_access(client,None)
        journal=LOCAL/number/'upload.json'
        if journal.exists():raise RuntimeError('Prior upload attempted; reconcile exact Apple build instead of repeating')
        write(journal,dict(status='ATTEMPTED',sourceCommit=receipt['sourceCommit'],build=number))
    if target.exists():raise RuntimeError('Distribution destination exists')
    options['destination']='upload' if upload else 'export';path=LOCAL/number/(step+'-options.plist');path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(plistlib.dumps(options))
    args=['xcodebuild','-exportArchive','-archivePath',folder,'-exportOptionsPlist',path,'-exportPath',target]
    if upload:
        auth=credentials();args+=['-authenticationKeyPath',auth['ASC_KEY_PATH'],'-authenticationKeyID',auth['ASC_KEY_ID'],'-authenticationKeyIssuerID',auth['ASC_ISSUER_ID']]
    command(args,LOCAL/number/(step+'.log'))
    if archive_digest(folder)!=receipt['archiveDigest']:raise RuntimeError('Archive payload changed during distribution')
    if upload:
        write(journal,dict(status='SUBMITTED',sourceCommit=receipt['sourceCommit'],build=number));return dict(status='UPLOAD_SUBMITTED',processing='NOT_YET_VERIFIED',build=number)
    ipas=list(target.glob('*.ipa'))
    if len(ipas)!=1:raise RuntimeError('IPA count mismatch')
    with zipfile.ZipFile(ipas[0]) as ipa:
        infos=[name for name in ipa.namelist() if re.match(r'^Payload/[^/]+\.app/Info\.plist$',name)]
        if len(infos)!=1:raise RuntimeError('IPA application identity ambiguous')
        info=plistlib.loads(ipa.read(infos[0]))
        if (info['CFBundleIdentifier'],info['CFBundleShortVersionString'],info['CFBundleVersion'])!=(BUNDLE,VERSION,number):raise RuntimeError('IPA identity mismatch')
    receipt.update(ipaSha256=hashlib.sha256(ipas[0].read_bytes()).hexdigest(),ipaBytes=ipas[0].stat().st_size);write(LOCAL/number/'export.json',receipt)
    return dict(status='IPA_EXPORTED',build=number,ipaSha256=receipt['ipaSha256'])

def owner_email(client):
    owners=[u for u in items(client,'/v1/users',{'filter[roles]':'ACCOUNT_HOLDER','limit':100}) if 'ACCOUNT_HOLDER' in u['attributes']['roles']]
    if len(owners)!=1 or '@' not in owners[0]['attributes']['username']:raise RuntimeError('Owner identity ambiguous')
    return owners[0]['attributes']
def audit_access(client,build):
    email=owner_email(client)['username'].casefold();groups=items(client,'/v1/betaGroups',{'filter[app]':APP_ID,'limit':100});access=0
    if build:
        direct=items(client,'/v1/builds/'+build+'/individualTesters',{'limit':200})
        if any(t['attributes']['email'].casefold()!=email for t in direct):raise RuntimeError('Another direct tester has access')
    for g in groups:
        a=g['attributes'];relevant=a.get('hasAccessToAllBuilds') or (build and any(b['id']==build for b in items(client,'/v1/betaGroups/'+g['id']+'/builds')))
        if not relevant:continue
        members=items(client,'/v1/betaGroups/'+g['id']+'/betaTesters',{'limit':200})
        if not a.get('isInternalGroup') or a.get('publicLinkEnabled') or any(t['attributes']['email'].casefold()!=email for t in members):raise RuntimeError('Build is exposed beyond owner-only internal testing')
        access+=1
    return dict(ownerOnly=True,accessibleGroups=access)
def self_only(client,number):
    state=status(client,number)
    if state['processingState']!='VALID' or state.get('internalBuildState') not in ('READY_FOR_BETA_TESTING','IN_BETA_TESTING'):return state
    owner=owner_email(client);email=owner['username'];audit_access(client,state['buildId']);groups=items(client,'/v1/betaGroups',{'filter[app]':APP_ID,'limit':100});name='OceanAngler Personal'
    group=next((g for g in groups if g['attributes']['name']==name),None)
    if group is None:group=client.request('POST','/v1/betaGroups',payload={'data':{'type':'betaGroups','attributes':{'name':name,'isInternalGroup':True,'publicLinkEnabled':False,'hasAccessToAllBuilds':False},'relationships':{'app':{'data':{'type':'apps','id':APP_ID}}}}})['data']
    if not group['attributes']['isInternalGroup'] or group['attributes'].get('publicLinkEnabled'):raise RuntimeError('Group not internal-only')
    path='/v1/betaGroups/'+group['id'];members=items(client,path+'/betaTesters')
    if any(m['attributes']['email'].casefold()!=email.casefold() for m in members):raise RuntimeError('Personal group has another member')
    if not members:
        testers=items(client,'/v1/betaTesters',{'filter[apps]':APP_ID,'filter[email]':email})
        if len(testers)>1 or any(t['attributes']['email'].casefold()!=email.casefold() for t in testers):raise RuntimeError('Ambiguous tester')
        if testers:client.request('POST',path+'/relationships/betaTesters',payload={'data':[{'type':'betaTesters','id':testers[0]['id']}]})
        else:client.request('POST','/v1/betaTesters',payload={'data':{'type':'betaTesters','attributes':{'email':email,'firstName':owner.get('firstName'),'lastName':owner.get('lastName')},'relationships':{'betaGroups':{'data':[{'type':'betaGroups','id':group['id']}]}}}})
    if not any(b['id']==state['buildId'] for b in items(client,path+'/builds')):client.request('POST',path+'/relationships/builds',payload={'data':[{'type':'builds','id':state['buildId']}]})
    members=items(client,path+'/betaTesters');assigned=any(b['id']==state['buildId'] for b in items(client,path+'/builds'))
    if len(members)!=1 or members[0]['attributes']['email'].casefold()!=email.casefold() or not assigned:raise RuntimeError('Self-only assignment not verified')
    return dict(state,**audit_access(client,state['buildId']),status='SELF_ONLY_GROUP_READY',groupId=group['id'],testerCount=1,assignedBuild=True)
def notes(client,number):
    state=status(client,number)
    if not state['buildId']:return state
    description='海洋小钓手私人开发测试。钓鱼、积分、存档、图鉴、装备和任务已接入；模型与海域仍为样板，儿童试玩、手机触控与长时间真机性能尚未完成验收。'
    whatsnew='请测试抛竿、提竿、按住收线/松手降张力、放流、重开后的积分与记录、左右手和暂停恢复。含12鱼种数据、3海域入口及18任务；场景和鱼/钓具美术尚非最终质量。仅本人内部测试，不代表完整产品验收。'
    for resource,parent,identifier,relation,field,text in [('betaAppLocalizations','apps',APP_ID,'app','description',description),('betaBuildLocalizations','builds',state['buildId'],'build','whatsNew',whatsnew)]:
        path='/v1/'+parent+'/'+identifier+'/'+resource;existing=items(client,path);match=next((x for x in existing if x['attributes']['locale']=='zh-Hans'),None);attributes={field:text}
        if resource=='betaAppLocalizations':attributes['feedbackEmail']='arthur.wang.wsx@gmail.com'
        if match:client.request('PATCH','/v1/'+resource+'/'+match['id'],payload={'data':{'type':resource,'id':match['id'],'attributes':attributes}})
        else:client.request('POST','/v1/'+resource,payload={'data':{'type':resource,'attributes':dict(attributes,locale='zh-Hans'),'relationships':{relation:{'data':{'type':parent,'id':identifier}}}}})
        verified=next(x for x in items(client,path) if x['attributes']['locale']=='zh-Hans')
        if verified['attributes'][field]!=text:raise RuntimeError('Notes did not persist')
    return dict(state,notes='VERIFIED',feedbackEmail='arthur.wang.wsx@gmail.com')
def main():
    p=argparse.ArgumentParser();p.add_argument('action',choices=['native','archive','export','upload','status','self','notes']);p.add_argument('--build',default=RELEASE['build']);p.add_argument('--label',default='a');p.add_argument('--apply',action='store_true');a=p.parse_args()
    if a.build!=RELEASE['build'] or not a.build.isdigit() or not a.label.isalnum():raise RuntimeError('Use the committed release build and a simple export label')
    if a.action!='status' and not a.apply:print(json.dumps(dict(status='PREVIEW',action=a.action,build=a.build,requires='--apply')));return
    if a.action=='native':result=native(a.build,a.label)
    elif a.action=='archive':result=archive(a.build)
    else:
        c=app_client()
        if a.action=='status':result=status(c,a.build)
        elif a.action=='self':result=self_only(c,a.build)
        elif a.action=='notes':result=notes(c,a.build)
        else:result=distribute(c,a.build,a.action=='upload')
    print(json.dumps(result,ensure_ascii=False,indent=2))
if __name__=='__main__':
    try:main()
    except Exception as e:print(json.dumps(dict(status='BETA_STEP_NOT_CONFIRMED',errorType=type(e).__name__,detail='Inspect private step logs and live Apple state before retrying.')));raise SystemExit(1)
