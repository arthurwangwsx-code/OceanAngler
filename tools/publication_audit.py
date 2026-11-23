#!/usr/bin/env python3
"""Inspect only Git-tracked public candidates; never read local credential stores."""
import json,re,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
paths=subprocess.check_output(['git','ls-files','-z'],cwd=ROOT).decode().split('\0');issues=[];count=0
patterns=[rb'-----BEGIN (?:[A-Z]+ )?PRIVATE KEY-----',rb'gh[pousr]_[A-Za-z0-9]{30,}']
for name in filter(None,paths):
 path=ROOT/name
 if not path.is_file():continue
 count+=1
 if any(part in ('.local','Library','Temp','build') for part in path.relative_to(ROOT).parts) or path.suffix.lower() in ('.p8','.p12','.mobileprovision') or path.name.startswith('.env'):issues.append(name+': private/generated path')
 if path.stat().st_size<2_000_000:
  data=path.read_bytes()
  if any(re.search(pattern,data) for pattern in patterns):issues.append(name+': credential-shaped content')
manifest=json.loads((ROOT/'art/asset-manifest.json').read_text())
for asset in manifest['assets']:
 if not asset.get('redistribution'):issues.append(asset['id']+': redistribution not verified')
print(json.dumps(dict(status='FAIL' if issues else 'PASS',trackedFiles=count,issues=issues),indent=2));sys.exit(bool(issues))
