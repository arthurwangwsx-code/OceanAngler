#!/usr/bin/env python3
"""ASC helper adapted from the user-owned TacticalFPS tooling; only OceanAngler is writable."""
import argparse
import json
import os
import shlex
import string
import time
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

BUNDLE = 'com.arthur.OceanAngler'
ORIGIN = 'https://api.appstoreconnect.apple.com'

def credentials():
    values = dict(os.environ)
    if not all(values.get(k) for k in ('ASC_KEY_ID', 'ASC_ISSUER_ID', 'ASC_KEY_PATH')):
        path = Path(values.get('ASC_ENV_FILE', '/Users/ai/project/mobile-ios/AiBox/scripts/.testflight.env'))
        for line in path.read_text().splitlines():
            parts = shlex.split(line, comments=True)
            if parts and parts[0] == 'export':
                parts = parts[1:]
            if len(parts) == 1 and '=' in parts[0]:
                key, value = parts[0].split('=', 1)
                if key in ('ASC_KEY_ID', 'ASC_ISSUER_ID', 'ASC_KEY_PATH'):
                    values.setdefault(key, value)
    for _ in range(3):
        for name in ('ASC_KEY_ID', 'ASC_ISSUER_ID', 'ASC_KEY_PATH'):
            if name in values:
                values[name] = string.Template(values[name]).safe_substitute(values)
    return values

class Client:
    def __init__(self):
        import jwt
        values = credentials()
        now = int(time.time())
        self.token = jwt.encode({'iss': values['ASC_ISSUER_ID'], 'iat': now,
                                 'exp': now + 600, 'aud': 'appstoreconnect-v1'},
                                Path(values['ASC_KEY_PATH']).expanduser().read_bytes(),
                                algorithm='ES256', headers={'kid': values['ASC_KEY_ID']})

    def request(self, method, path, query=None, payload=None):
        assert path.startswith('/v1/')
        url = ORIGIN + path + ('?' + urllib.parse.urlencode(query) if query else '')
        headers = {'Authorization': 'Bearer ' + self.token, 'Accept': 'application/json'}
        body = None
        if payload is not None:
            headers['Content-Type'] = 'application/json'
            body = json.dumps(payload).encode()
        req = urllib.request.Request(url, data=body, headers=headers, method=method)
        with urllib.request.urlopen(req, timeout=30) as response:
            raw = response.read()
            return json.loads(raw) if raw else {}

    def status(self):
        bundles = self.request('GET', '/v1/bundleIds', {'filter[identifier]': BUNDLE})['data']
        apps = self.request('GET', '/v1/apps', {'filter[bundleId]': BUNDLE})['data']
        return {'bundleIdentifier': BUNDLE,
                'bundles': [{'id': b['id'], 'identifier': b['attributes']['identifier']} for b in bundles],
                'apps': [{'id': a['id'], 'name': a['attributes']['name'],
                          'bundleId': a['attributes']['bundleId']} for a in apps]}

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('action', choices=['status', 'register-bundle'])
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    client = Client()
    status = client.status()
    if args.action == 'register-bundle' and not status['bundles']:
        if args.apply:
            client.request('POST', '/v1/bundleIds', payload={'data': {'type': 'bundleIds',
                'attributes': {'identifier': BUNDLE, 'name': 'OceanAngler', 'platform': 'IOS'}}})
            status = client.status()
        else:
            status['plannedAction'] = 'Register this app-specific iOS Bundle ID; requires --apply'
    print(json.dumps(status, ensure_ascii=False, indent=2))

if __name__ == '__main__':
    try:
        main()
    except urllib.error.HTTPError as error:
        print(json.dumps({'status': 'APPLE_HTTP_ERROR', 'httpCode': error.code}))
        raise SystemExit(1)
    except Exception:
        print(json.dumps({'status': 'AUTH_OR_CONNECTION_ERROR', 'detail': 'No credentials disclosed; verify state before retrying a write.'}))
        raise SystemExit(1)
