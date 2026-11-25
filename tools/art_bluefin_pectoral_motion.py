"""Original paired-fin deformation candidate; preserve the reviewed rest meshes."""
import bpy,hashlib,json,math,sys
from pathlib import Path
from statistics import median
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
RECIPE='paired-distal-curl-v3'
EXTRA={side+segment for side in ['pectoralL','pectoralR'] for segment in ['Mid','Tip']}
MOTIONS={'idle':(.00675,1),'swim':(.01275,1),'turn':(.018,1),'burst':(.01425,2),'bite':(.00975,1),'hooked':(.0165,2),'release':(.015,1)}

def fin_vertices(obj,side):
    names={side,side+'Mid',side+'Tip'}
    vertices={v.index for v in obj.data.vertices if any(obj.vertex_groups[g.group].name in names and g.weight>0 for g in v.groups)}
    if not vertices:raise RuntimeError('Missing original paired-fin weights: '+side)
    radial={i:[] for i in vertices}
    for face in obj.data.polygons:
        for li in face.loop_indices:
            index=obj.data.loops[li].vertex_index
            if index in radial:radial[index].append(obj.data.uv_layers.active.data[li].uv.y)
    return vertices,{i:max(0,min(1,median(values))) for i,values in radial.items()}

def configure_fin_rig(rig,lods):
    sys.path.insert(0,str(ROOT/'tools'))
    from art_bluefin_pectoral_surface import refine_membranes
    surfaces={obj.name:refine_membranes(obj) for obj in lods}
    names={b.name for b in rig.data.bones};existing=names&EXTRA
    if existing and existing!=EXTRA:raise RuntimeError('Incomplete existing paired-fin rig; preserve for review')
    bpy.ops.object.select_all(action='DESELECT');rig.hide_set(False);rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
    for sign,label in [(1,'L'),(-1,'R')]:
        side='pectoral'+label
        # Rest axes follow a representative authored middle ray, not a measured
        # biological joint. The original root bone/rest transform is untouched.
        origin=Vector((-.48,sign*.16,-.035));end=Vector((-.44+.77*.5**1.1,sign*(.16+.17*math.sin(.5**1.1*math.pi*.65)),-.1225))
        for segment,a,b,parent in [('Mid',.30,.65,side),('Tip',.65,1,side+'Mid')]:
            bone=rig.data.edit_bones.get(side+segment) or rig.data.edit_bones.new(side+segment)
            bone.head=origin.lerp(end,a);bone.tail=origin.lerp(end,b);bone.parent=rig.data.edit_bones[parent];bone.use_connect=False;bone.align_roll(Vector((0,0,1)))
    bpy.ops.object.mode_set(mode='OBJECT');counts={}
    for obj in lods:
        for name in EXTRA:
            if obj.vertex_groups.get(name) is None:obj.vertex_groups.new(name=name)
        counts[obj.name]={}
        for label in ['L','R']:
            side='pectoral'+label;ids,radial=fin_vertices(obj,side);root_count=0
            for index in ids:
                vertex=obj.data.vertices[index]
                if any(obj.vertex_groups[g.group].name not in {side,side+'Mid',side+'Tip'} and g.weight>1e-8 for g in vertex.groups):raise RuntimeError('Do not discard another attachment')
                t=radial[index]
                if t<=.25:weights={side:1};root_count+=1
                elif t<.65:
                    u=(t-.25)/.40;weights={side:1-u,side+'Mid':u}
                else:
                    u=(t-.65)/.35;weights={side+'Mid':1-u,side+'Tip':u}
                for group_index in [g.group for g in vertex.groups]:obj.vertex_groups[group_index].remove([index])
                for name,weight in weights.items():
                    if weight>1e-8:obj.vertex_groups[name].add([index],weight,'REPLACE')
            counts[obj.name][side]=dict(vertices=len(ids),rootQuarterUnchanged=root_count)
    rig['pectoralFlexRecipe']=RECIPE
    return dict(recipe=RECIPE,motionPeakRadians={name:values[0] for name,values in MOTIONS.items()},membraneRefinement=surfaces,additionalBones=4,totalBones=len(rig.data.bones),rootFractionUnchanged=.25,maximumWeightsPerFinVertex=2,components=counts,scope='Authored flexible paired-fin animation; only coplanar paired-fin membrane refinement; original vertices, maps, body, rays and original bones/curves retained, not measured Caranx biomechanics or final art acceptance')

def rotation(semantic,segment,u):
    amplitude,cycles=MOTIONS[semantic];omega=math.tau*cycles;delay=.55 if segment=='Mid' else 1.1;scale=1 if segment=='Mid' else .75
    return amplitude*scale*math.sin(omega*u-delay),amplitude*scale*omega*math.cos(omega*u-delay)

def write_fin_motions(rig):
    present={b.name for b in rig.data.bones}&EXTRA
    if not present:return
    if present!=EXTRA:raise RuntimeError('Expected all four paired-fin controls')
    for semantic in MOTIONS:
        action=bpy.data.actions.get(semantic)
        if action is None:raise RuntimeError('Seven existing actions are required')
        rig.animation_data.action=action;bag=action.layers[0].strips[0].channelbag(rig.animation_data.action_slot)
        for curve in list(bag.fcurves):
            if any(curve.data_path.startswith('pose.bones["'+name+'"]') for name in EXTRA):bag.fcurves.remove(curve)
        for frame in range(1,62,3):
            u=(frame-1)/60
            for name in sorted(EXTRA):
                angle,_=rotation(semantic,'Mid' if name.endswith('Mid') else 'Tip',u)
                bone=rig.pose.bones[name];bone.rotation_mode='XYZ';bone.rotation_euler=(angle,0,0);bone.keyframe_insert(data_path='rotation_euler',frame=frame)
        for curve in bag.fcurves:
            name=curve.data_path.split('"')[1]
            if name not in EXTRA:continue
            for key in curve.keyframe_points:
                _,velocity=rotation(semantic,'Mid' if name.endswith('Mid') else 'Tip',(key.co.x-1)/60);velocity=velocity/60 if curve.array_index==0 else 0
                key.interpolation='BEZIER';key.handle_left_type=key.handle_right_type='FREE';key.handle_left=(key.co.x-1,key.co.y-velocity);key.handle_right=(key.co.x+1,key.co.y+velocity)
            curve.update()
    rig.animation_data.action=bpy.data.actions['idle'];bpy.context.scene.frame_set(1)

def update_existing():
    source=ROOT/'art/source/bluefin/Bluefin.blend';bpy.ops.wm.open_mainfile(filepath=str(source));bpy.context.preferences.filepaths.save_version=0
    rig=bpy.data.objects['BluefinRig'];lods=[bpy.data.objects['Bluefin_LOD'+str(i)] for i in range(3)]
    info=configure_fin_rig(rig,lods);write_fin_motions(rig)
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
    for obj in lods:obj.hide_set(False);obj.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=str(ROOT/'game/Assets/OceanAngler/Art/Bluefin/Bluefin.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_triangles=True,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='RELATIVE')
    for i,obj in enumerate(lods):obj.hide_set(i!=0);obj.hide_render=i!=0
    bpy.ops.wm.save_as_mainfile(filepath=str(source));path=ROOT/'art/source/bluefin/build-report.json';report=json.loads(path.read_text());report['bones']=[b.name for b in rig.data.bones];report['pectoralFlex']=info;report['lods']={obj.name:dict(triangles=len(obj.data.loop_triangles),vertices=len(obj.data.vertices),uvLayers=len(obj.data.uv_layers)) for obj in lods};path.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(info))

if __name__=='__main__':update_existing()
