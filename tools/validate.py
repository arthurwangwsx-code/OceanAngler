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
catalog=json.loads((r/'game/Assets/OceanAngler/Resources/ContentCatalog.json').read_text())
references=json.loads((r/'planning/species-references.json').read_text())['species']
index={s['catalogId']:s for s in references};check(len(index)==len(references),'duplicate science reference ID')
check({s['catalogId'] for s in references if s['role']=='catch-candidate'}=={s['id'] for s in catalog['fish']},'catch/science reference identity drift')
check({s['catalogId'] for s in references if s['role']=='observation-only'}=={s['id'] for s in catalog['observations']},'observation/science reference identity drift')
check(len(catalog['observations'])==3,'observation count drift')
names=[s['scientificName'].casefold() for s in catalog['fish']+catalog['observations']]
check(len(set(names))==len(names),'catch/observation scientific-name overlap')
for species in catalog['fish']+catalog['observations']:
 reference=index.get(species['id'],{})
 check(species['scientificName']==reference.get('scientificName'),'scientific name drift '+species['id'])
 check(bool(reference.get('sources')) and all(url.startswith('https://') for url in reference.get('sources',[])),'missing science source '+species['id'])
 check(bool(reference.get('recognition')) and bool(reference.get('modelChecklist')),'incomplete production reference '+species['id'])
for asset in a['assets']:
 for record in ([asset] if asset.get('path') else [])+asset.get('artifacts',[]):
  path=r/record['path'];check(path.is_file(),'missing registered asset '+record['path'])
  if path.is_file():check(hashlib.sha256(path.read_bytes()).hexdigest()==record.get('sha256'),'asset hash mismatch '+record['path'])
envelopes=json.loads((r/'game/Assets/OceanAngler/Resources/JournalEnvelopes.json').read_text())
envelope_inputs={'Assets/OceanAngler/'+p for p in ['Runtime/FishModel.cs','Runtime/FishVisual.cs','Runtime/AuthoredFishVisual.cs','Resources/ContentCatalog.json']}
for name in ['Bluefin','Halibut']:
 envelope_inputs.add('Assets/OceanAngler/Resources/Models/'+name+'.prefab')
 envelope_inputs.update('Assets/OceanAngler/Art/'+name+'/'+p for p in [name+'.fbx',name+'.fbx.meta',name+'.controller','Motions/idle.anim','Motions/swim.anim'])
inputs=envelopes.get('inputs',[])
check(len(inputs)==len(envelope_inputs) and {i['path'] for i in inputs}==envelope_inputs,'journal envelope input set drift')
for record in inputs:
 if record['path'] in envelope_inputs:
  check(hashlib.sha256((r/'game'/record['path']).read_bytes()).hexdigest()==record['sha256'],'stale journal geometry envelope '+record['path'])
unity_version=re.search(r'^m_EditorVersion: (.+)$',(r/'game/ProjectSettings/ProjectVersion.txt').read_text(),re.M).group(1)
check(envelopes.get('version')==1 and envelopes.get('unityVersion')==unity_version and envelopes.get('sections')==32 and abs(envelopes.get('padding',0)-.012)<1e-8,'journal envelope format/editor drift')
shapes=envelopes.get('shapes',[])
check(len(shapes)==len(catalog['fish']) and {s['fishId'] for s in shapes}=={s['id'] for s in catalog['fish']},'journal envelope species drift')
check(all(16<=len(s['sections'])<=32 and s['sourceVertices']>3000 and s['poses']==(130 if s['fishId'] in ['trevally','halibut'] else 65) for s in shapes),'incomplete journal envelope sampling')
for path in (r/'tools').rglob('*.p8'):
 if '.local' not in path.parts:errors.append('credential in project')
print(json.dumps(dict(status='FAIL' if errors else 'PASS',requirements=len(reqs),tasks=len(rows),errors=errors),ensure_ascii=False,indent=2));sys.exit(bool(errors))
