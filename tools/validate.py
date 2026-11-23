#!/usr/bin/env python3
"""Validate scope/traceability, dependency graph and local document links."""
import csv,hashlib,json,re,sys
from pathlib import Path
r=Path(__file__).resolve().parents[1];errors=[]
def check(ok,why):
 if not ok:errors.append(why)
b=json.loads((r/'planning/baseline.json').read_text());rows=list(csv.DictReader((r/'planning/backlog.csv').open()))
ids=[t['id'] for t in rows];check(len(ids)==len(set(ids)),'duplicate task IDs')
reqs=set(re.findall(r'### Requirement: (REQ-\d+)',(r/'ispec/specs/fishing-game/spec.md').read_text()))
check(len(reqs)==12,'requirement count drift');check(reqs=={t['requirement'] for t in rows},'uncovered or unknown requirement')
graph={t['id']:list(filter(None,t['dependencies'].split(';'))) for t in rows}
for t in rows:
 check(t['stage'] in ('F0','P0','P1','P2'),'unknown stage')
 check(t['implementation'] in ('PLANNED','IN_PROGRESS','IMPLEMENTED'),'invalid implementation status')
 for dep in graph[t['id']]:check(dep in graph,'missing dependency '+dep)
def visit(x,chain):
 if x in chain:errors.append('dependency cycle '+x);return
 for dep in graph.get(x,[]):visit(dep,chain+[x])
for x in graph:visit(x,[])
check(b['stages']['P1']==dict(habitats=3,fish=12,observationCreatures=3,rods=6,reels=4,lines=4,lures=6,tasks=18),'P1 scope drift')
for path in [r/'README.md',*list((r/'docs').glob('*.md'))]:
 for target in re.findall(r'\[[^\]]*\]\(([^)]+)\)',path.read_text()):
  if target.startswith(('https:','http:','#','mailto:')):continue
  check((path.parent/target.split('#')[0]).exists(),'broken link '+str(path.relative_to(r))+' -> '+target)
a=json.loads((r/'art/asset-manifest.json').read_text());check(len({x['id'] for x in a['assets']})==len(a['assets']),'duplicate asset ID')
for asset in a['assets']:
 for record in ([asset] if asset.get('path') else [])+asset.get('artifacts',[]):
  path=r/record['path'];check(path.is_file(),'missing registered asset '+record['path'])
  if path.is_file():check(hashlib.sha256(path.read_bytes()).hexdigest()==record.get('sha256'),'asset hash mismatch '+record['path'])
for path in (r/'tools').rglob('*.p8'):
 if '.local' not in path.parts:errors.append('credential in project')
print(json.dumps(dict(status='FAIL' if errors else 'PASS',requirements=len(reqs),tasks=len(rows),errors=errors),ensure_ascii=False,indent=2));sys.exit(bool(errors))
