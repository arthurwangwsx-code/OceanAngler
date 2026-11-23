"""Read the saved Blender asset and inspect real weights, UVs and motion.
Run: Blender --background art/source/bluefin/Bluefin.blend --python tools/art_validate_bluefin.py
"""
import bpy,json,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];failures=[];checks=[]
def check(condition,text):
    (checks if condition else failures).append(text)
rig=bpy.data.objects.get('BluefinRig');check(rig is not None and rig.type=='ARMATURE','可编辑骨骼存在')
report={'scope':'saved-blend-engineering','visualAcceptance':'NOT_ACCEPTED','lods':{},'motion':{},'failures':failures,'checks':checks}
lods=[bpy.data.objects.get('Bluefin_LOD'+str(i)) for i in range(3)]
check(all(o is not None for o in lods),'三个实际LOD存在')
if rig and all(lods):
    previous=10**9
    for i,obj in enumerate(lods):
        data=obj.data;data.calc_loop_triangles();count=len(data.loop_triangles);bad_weights=0
        for v in data.vertices:
            total=sum(g.weight for g in v.groups)
            if not math.isfinite(total) or abs(total-1)>.002:bad_weights+=1
        invalid_uv=sum(not all(math.isfinite(x) and -.0001<=x<=1.0001 for x in loop.uv) for layer in data.uv_layers for loop in layer.data)
        check(count<previous,'LOD'+str(i)+'面数递减');previous=count
        check(bad_weights==0,'LOD'+str(i)+'全部顶点权重归一且非空')
        check(invalid_uv==0,'LOD'+str(i)+' UV有限且位于贴图范围')
        check(all(math.isfinite(n) for v in data.vertices for n in v.co),'LOD'+str(i)+'无非法顶点')
        check(any(m.type=='ARMATURE' and m.object==rig for m in obj.modifiers),'LOD'+str(i)+'使用共同骨骼')
        report['lods'][obj.name]={'triangles':count,'invalidWeightVertices':bad_weights,'invalidUVLoops':invalid_uv}
    check(8000<=len(lods[0].data.loop_triangles)<=25000,'LOD0在候选面数预算内')
    check(set(b.name for b in rig.data.bones)>={'jaw','tail','pectoralL','pectoralR','pelvicL','pelvicR'},'颌/尾/成对鳍均有独立骨骼')
    for image in bpy.data.images:
        if image.source=='FILE':check(Path(bpy.path.abspath(image.filepath)).is_file(),'贴图文件存在：'+image.name)
    for name in ['idle','swim','turn','burst','bite','hooked','release']:
        action=bpy.data.actions.get(name);check(action is not None,'语义动作存在：'+name)
        if action is None:continue
        rig.animation_data.action=action;poses=[];jaw=[]
        for frame in [1,16,31,46,61]:
            bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
            poses.append(tuple(rig.pose.bones['tail'].rotation_euler));jaw.append(tuple(rig.pose.bones['jaw'].rotation_euler))
        spread=max(sum((a-b)**2 for a,b in zip(p,poses[0]))**.5 for p in poses)
        jaw_spread=max(sum((a-b)**2 for a,b in zip(p,jaw[0]))**.5 for p in jaw)
        check(spread>.001,'语义动作具有真实尾摆：'+name)
        if name=='bite':check(jaw_spread>.1,'咬钩动作有实际下颌运动')
        report['motion'][name]={'tailRotationRange':spread,'jawRotationRange':jaw_spread}
report['status']='FAIL' if failures else 'ENGINEERING_CHECKS_PASS'
path=ROOT/'.local/art/bluefin/validation.json';path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(report,ensure_ascii=False,indent=2))
if failures:raise RuntimeError('Saved asset checks failed; see private validation report')
