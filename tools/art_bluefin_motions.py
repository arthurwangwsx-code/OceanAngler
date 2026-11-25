"""Original cyclic motion curves; update saved actions without rebuilding meshes."""
import bpy,math,json,sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
MOTIONS=[('idle',.06,1),('swim',.16,2),('turn',.14,1),('burst',.25,3),('bite',.10,1),('hooked',.30,3),('release',.18,2)]

def values(name,amplitude,cycles,u):
    phase=math.tau*cycles*u;omega=math.tau*cycles;result={}
    for i,bone in enumerate(['head','spine1','spine2','spine3','peduncle','tail']):
        scale=amplitude*(i/5)**1.8;angle=math.sin(phase-i*.65)*scale;velocity=math.cos(phase-i*.65)*omega*scale
        if name=='turn' and i>0:
            # A closed steering envelope, with zero slope at both ends.
            angle+=.12*math.sin(math.pi*u)**2;velocity+=.12*math.pi*math.sin(math.tau*u)
        result[bone]=((0,0,angle),(0,0,velocity))
    paired_omega=math.tau*((cycles+1)//2)
    for sign,label in [(1,'L'),(-1,'R')]:
        result['pectoral'+label]=((.09*math.sin(paired_omega*u),0,sign*.07*math.sin(phase)),(.09*paired_omega*math.cos(paired_omega*u),0,sign*.07*omega*math.cos(phase)))
    if name in ('bite','hooked'):
        opening=max(0,math.sin(phase));jaw=-.32*opening*opening;velocity=-.64*opening*math.cos(phase)*omega
    else:jaw=.005*math.sin(phase);velocity=.005*math.cos(phase)*omega
    result['jaw']=((jaw,0,0),(velocity,0,0))
    return result

def write_motions(rig):
    rig.animation_data_create();rig.animation_data.action=None
    for name,amplitude,cycles in MOTIONS:
        previous=bpy.data.actions.get(name)
        if previous is not None:bpy.data.actions.remove(previous)
        action=bpy.data.actions.new(name);rig.animation_data.action=action;action.use_fake_user=True
        for frame in range(1,62,3):
            for bone,(rotation,velocity) in values(name,amplitude,cycles,(frame-1)/60).items():
                pose=rig.pose.bones[bone];pose.rotation_mode='XYZ';pose.rotation_euler=rotation;pose.keyframe_insert(data_path='rotation_euler',frame=frame)
        slot=rig.animation_data.action_slot
        curves=action.layers[0].strips[0].channelbag(slot).fcurves
        for curve in curves:
            bone=curve.data_path.split('"')[1]
            for key in curve.keyframe_points:
                frame=float(key.co.x);derivative=values(name,amplitude,cycles,(frame-1)/60)[bone][1][curve.array_index]/60
                key.interpolation='BEZIER';key.handle_left_type='FREE';key.handle_right_type='FREE'
                key.handle_left=(frame-1,key.co.y-derivative);key.handle_right=(frame+1,key.co.y+derivative)
            curve.update()
        action['semantic']=name;action['motionRecipe']='periodic-analytic-tangents-v1'
    from art_bluefin_pectoral_motion import write_fin_motions
    write_fin_motions(rig)
    rig.animation_data.action=bpy.data.actions['idle'];bpy.context.scene.frame_set(1)

def update_existing():
    source=ROOT/'art/source/bluefin/Bluefin.blend';bpy.ops.wm.open_mainfile(filepath=str(source));bpy.context.preferences.filepaths.save_version=0
    rig=bpy.data.objects['BluefinRig'];lods=[bpy.data.objects['Bluefin_LOD'+str(i)] for i in range(3)]
    write_motions(rig)
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
    for obj in lods:obj.hide_set(False);obj.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=str(ROOT/'game/Assets/OceanAngler/Art/Bluefin/Bluefin.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='RELATIVE')
    for obj in lods:obj.hide_set(obj.name!='Bluefin_LOD0')
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    report=ROOT/'art/source/bluefin/build-report.json';data=json.loads(report.read_text());data['motionRecipe']='periodic-analytic-tangents-v1';data['motionUpdateScope']='Saved actions and FBX update only; no geometry, UV, weights, rig rest structure or texture regeneration';report.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')

if __name__=='__main__':
    if '--update-existing' not in sys.argv:raise RuntimeError('Use tools/art.py build --asset bluefin --motions-only')
    update_existing()
