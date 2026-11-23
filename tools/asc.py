#!/usr/bin/env python3
"""OceanAngler-scoped App Store Connect client; credentials stay outside Git."""
import argparse,json,os,shlex,string,time,urllib.error,urllib.parse,urllib.request
from pathlib import Path
BUNDLE='com.arthur.OceanAngler'
APP_ID='6838308856'
ORIGIN='https://api.appstoreconnect.apple.com'
AUTH_FIELDS=('ASC_KEY_ID','ASC_ISSUER_ID','ASC_KEY_PATH')

def credentials():
    values=dict(os.environ)
    if not all(values.get(key) for key in AUTH_FIELDS):
        config=values.get('ASC_ENV_FILE')
        if not config:raise RuntimeError('Set ASC environment variables or ASC_ENV_FILE')
        for line in Path(config).expanduser().read_text().splitlines():
            words=shlex.split(line,comments=True)
            if words and words[0]=='export':words=words[1:]
            if len(words)==1 and '=' in words[0]:
                key,value=words[0].split('=',1)
                if key in AUTH_FIELDS and not values.get(key):values[key]=value
    for _ in range(3):
        for key in AUTH_FIELDS:
            if key in values:values[key]=string.Template(values[key]).safe_substitute(values)
    if not all(values.get(key) for key in AUTH_FIELDS):raise RuntimeError('ASC configuration incomplete')
    values['ASC_KEY_PATH']=str(Path(values['ASC_KEY_PATH']).expanduser())
    return values

class Client:
    def __init__(self):
        import jwt
        config=credentials();issued=int(time.time())
        self.token=jwt.encode(dict(iss=config['ASC_ISSUER_ID'],iat=issued,exp=issued+600,aud='appstoreconnect-v1'),Path(config['ASC_KEY_PATH']).read_bytes(),algorithm='ES256',headers=dict(kid=config['ASC_KEY_ID']))
    def request(self,method,path,query=None,payload=None):
        if not path.startswith('/v1/') or '..' in path:raise ValueError('ASC resource path required')
        url=ORIGIN+path+('?' + urllib.parse.urlencode(query) if query else '')
        body=None if payload is None else json.dumps(payload).encode()
        req=urllib.request.Request(url,data=body,method=method,headers={'Authorization':'Bearer '+self.token,'Accept':'application/json','Content-Type':'application/json'})
        with urllib.request.urlopen(req,timeout=30) as response:
            body=response.read();return json.loads(body) if body else {}
    def status(self):
        bundles=self.request('GET','/v1/bundleIds',{'filter[identifier]':BUNDLE})['data']
        apps=self.request('GET','/v1/apps',{'filter[bundleId]':BUNDLE})['data']
        return dict(bundleIdentifier=BUNDLE,bundles=[dict(id=x['id'],identifier=x['attributes']['identifier']) for x in bundles],apps=[dict(id=x['id'],name=x['attributes']['name'],bundleId=x['attributes']['bundleId']) for x in apps])

def main():
    p=argparse.ArgumentParser();p.add_argument('action',choices=['status','register-bundle']);p.add_argument('--apply',action='store_true');args=p.parse_args();client=Client();state=client.status()
    if args.action=='register-bundle' and not state['bundles']:
        if not args.apply:state['plannedAction']='Register OceanAngler iOS bundle; pass --apply to write'
        else:
            client.request('POST','/v1/bundleIds',payload={'data':{'type':'bundleIds','attributes':{'identifier':BUNDLE,'name':'OceanAngler','platform':'IOS'}}})
            state=client.status()
            if len(state['bundles'])!=1:raise RuntimeError('Bundle registration not confirmed')
    print(json.dumps(state,ensure_ascii=False,indent=2))
if __name__=='__main__':
    try:main()
    except urllib.error.HTTPError as error:print(json.dumps(dict(status='APPLE_HTTP_ERROR',httpCode=error.code)));raise SystemExit(1)
    except Exception as error:print(json.dumps(dict(status='ASC_NOT_CONFIRMED',errorType=type(error).__name__,detail='Check private authentication/configuration; credentials are never printed.')));raise SystemExit(1)
