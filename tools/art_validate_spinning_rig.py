"""检查实际保存的钓组：权重、UV、机构中心、导环与变形后几何的对齐。"""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];failures=[];checks=[]
def check(ok,text):(checks if ok else failures).append(text)
manifest=json.loads((ROOT/'art/source/spinning-rig/build-report.json').read_text());rig=bpy.data.objects['RodRig'];bones=set(b.name for b in rig.data.bones)
report=dict(scope='saved-spinning-rig-engineering',checks=checks,failures=failures,lods={},guideErrors={},visualAcceptance='NOT_ACCEPTED')
for name,n in manifest['lods'].items():
 o=bpy.data.objects.get(name);check(o is not None,'网格存在：'+name)
 if o is None:continue
 o.data.calc_loop_triangles();bad=sum(not all(math.isfinite(v) and -.0001<=v<=1.0001 for v in u.uv) for layer in o.data.uv_layers for u in layer.data)
 check(bad==0,'UV范围有效：'+name)
 if name.startswith('Rod_'):
  indices={g.index for g in o.vertex_groups if g.name in bones};invalid=sum(abs(sum(g.weight for g in v.groups if g.group in indices)-1)>.002 for v in o.data.vertices);check(invalid==0,'竿身骨骼权重有效：'+name)
 report['lods'][name]={'triangles':len(o.data.loop_triangles),'invalidUVLoops':bad}
for stem in ['Rod','Housing','Rotor','Spool','Crank','Lure','BailWire']:
 check(all(report['lods'][stem+'_LOD'+str(i)]['triangles']>report['lods'][stem+'_LOD'+str(i+1)]['triangles'] for i in range(2)),'LOD实际递减：'+stem)
butt=bpy.data.objects['ButtAnchor'].matrix_world.translation;tip=bpy.data.objects['TipAnchor'].matrix_world.translation
check(abs((tip-butt).length-2.35)<.0001,'全竿物理长度2.35米')
for name,pos in [('RotorPivot',(0,-.145,.47)),('SpoolPivot',(0,-.145,.52)),('CrankPivot',(-.045,-.142,.38)),('BailPivot',(0,-.145,.551))]:check((bpy.data.objects[name].matrix_world.translation-Vector(pos)).length<.0001,'机构中心正确：'+name)
for label,bend in [('rest',0),('loaded',.05)]:
 for i in range(10):p=rig.pose.bones['RodBone'+str(i)];p.rotation_mode='XYZ';p.rotation_euler.x=bend*(i/9)**1.3
 bpy.context.view_layer.update();obj=bpy.data.objects['Rod_LOD0'];ev=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=ev.to_mesh();errors=[]
 for i,g in enumerate(manifest['guides']):
  vg=obj.vertex_groups['__GuideRing'+str(i)].index;ids=[v.index for v in obj.data.vertices if any(x.group==vg and x.weight>.5 for x in v.groups)]
  marker=bpy.data.objects[g['name']];points=[marker.matrix_world.inverted() @ (ev.matrix_world@mesh.vertices[j].co) for j in ids]
  center=Vector(tuple((min(p[k] for p in points)+max(p[k] for p in points))*.5 for k in range(3)));error=center.length;errors.append(error)
  check(error<.0003,'导环与变形后网格对齐：'+label+'/'+g['name'])
  if label=='rest':check((marker.matrix_world.translation-Vector(g['position'])).length<.0001,'导环初始位置正确：'+g['name'])
 ev.to_mesh_clear();report['guideErrors'][label]=errors
for im in bpy.data.images:
 if im.source=='FILE':check(Path(bpy.path.abspath(im.filepath)).is_file(),'贴图可解析：'+im.name)
detail_scope={'__file__':str(ROOT/'tools/art_validate_rig_detail.py'),'__name__':'rig_detail_module'}
exec(compile((ROOT/'tools/art_validate_rig_detail.py').read_text(),detail_scope['__file__'],'exec'),detail_scope)
report['detail']=detail_scope['inspect']();checks.extend(report['detail']['checks']);failures.extend(report['detail']['failures'])
winding_scope={'__file__':str(ROOT/'tools/art_validate_rig_winding.py'),'__name__':'rig_winding_module'}
exec(compile((ROOT/'tools/art_validate_rig_winding.py').read_text(),winding_scope['__file__'],'exec'),winding_scope)
report['winding']=winding_scope['inspect']();checks.extend(report['winding']['checks']);failures.extend(report['winding']['failures'])
shaft_scope={'__file__':str(ROOT/'tools/art_validate_rig_shaft.py'),'__name__':'rig_shaft_module'}
exec(compile((ROOT/'tools/art_validate_rig_shaft.py').read_text(),shaft_scope['__file__'],'exec'),shaft_scope)
report['shaft']=shaft_scope['inspect']();checks.extend(report['shaft']['checks']);failures.extend(report['shaft']['failures'])
report['status']='FAIL' if failures else 'ENGINEERING_CHECKS_PASS';out=ROOT/'.local/art/spinning-rig/validation.json';out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(report,ensure_ascii=False,indent=2))
if failures:raise RuntimeError('Spinning rig asset validation failed')
