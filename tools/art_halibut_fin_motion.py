"""Add editable median-fin waves to the existing original halibut source.

Coordinates, topology, UVs, maps, original bones and original motion curves stay
intact. This is an animation candidate, not scientific or final art approval.
Posterior skin weights are deliberately repaired to follow the original spine;
the anterior jaw weighting remains intact.
"""
import bpy, json, math, sys, hashlib
from pathlib import Path
from statistics import median
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'art/source/halibut'
GAME=ROOT/'game/Assets/OceanAngler/Art/Halibut'
BODY=[(-.55,'head'),(-.05,'spine1'),(.32,'spine2'),(.61,'spine3'),(.9,'peduncle'),(1.25,'tail')]
PROFILE=[(-1.045,.048),(-.99,.09),(-.88,.175),(-.73,.285),(-.52,.405),(-.23,.475),(.04,.467),(.29,.387),(.52,.280),(.72,.170),(.88,.083),(1,.058)]
SIDES=[('dorsal',-1,-.96),('anal',1,-.68)]
STATIONS=17
MOTIONS={'idle':(.045,1),'swim':(.22,2),'turn':(.26,1),'burst':(.32,3),'bite':(.13,1),'hooked':(.28,3),'release':(.24,2)}

def width(x):
    for (a,wa),(b,wb) in zip(PROFILE,PROFILE[1:]):
        if a<=x<=b:return wa+(wb-wa)*(x-a)/(b-a)
    return PROFILE[0 if x<PROFILE[0][0] else -1][1]

def long_fins(obj):
    m=obj.data;vertices={v for p in m.polygons if m.materials[p.material_index].name=='Halibut_Fins' for v in p.vertices}
    adjacency={v:set() for v in vertices}
    for p in m.polygons:
        if m.materials[p.material_index].name!='Halibut_Fins':continue
        for a in p.vertices:adjacency[a].update(set(p.vertices)-{a})
    result={}
    while vertices:
        seed=vertices.pop();group={seed};pending=[seed]
        while pending:
            for v in adjacency[pending.pop()]&vertices:vertices.remove(v);group.add(v);pending.append(v)
        points=[m.vertices[i].co for i in group]
        if max(p.x for p in points)-min(p.x for p in points)>1.3:
            name='dorsal' if sum(p.y for p in points)<0 else 'anal'
            if name in result:raise RuntimeError('Ambiguous median fin component')
            result[name]=group
    if set(result)!={'dorsal','anal'}:raise RuntimeError('Both closed long fin components are required')
    return result

def apply_fin_motion(rig,lods):
    existing={b.name for b in rig.data.bones if b.name.startswith(('dorsal_','anal_'))}
    expected={side+'_'+str(i).zfill(2) for side,_,_ in SIDES for i in range(STATIONS)}
    if existing and existing!=expected:raise RuntimeError('Unexpected existing fin rig; preserve it for review')
    bpy.ops.object.select_all(action='DESELECT');rig.hide_set(False);rig.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='EDIT')
    for side,sign,start in SIDES:
        for i in range(STATIONS):
            x=start+(.89-start)*i/(STATIONS-1);name=side+'_'+str(i).zfill(2)
            bone=rig.data.edit_bones.get(name) or rig.data.edit_bones.new(name)
            bone.head=(x,sign*width(x)*.98,0);bone.tail=bone.head+Vector((0,sign*.07,0));bone.align_roll(Vector((0,0,1)))
            bone.parent=rig.data.edit_bones[min(BODY,key=lambda item:abs(item[0]-x))[1]]
    bpy.ops.object.mode_set(mode='OBJECT')
    counts={}
    for obj in lods:
        # The old jaw helper assigned the entire skin to head/jaw, leaving the
        # posterior hull rigid while its fins moved. Repair only posterior
        # Skin vertices; never reweight mouth, eyes or another fin component.
        posterior={i for p in obj.data.polygons if obj.data.materials[p.material_index].name=='Halibut_Skin' for i in p.vertices if obj.data.vertices[i].co.x>BODY[0][0]}
        for vi in posterior:
            x=obj.data.vertices[vi].co.x
            a,an=BODY[0];b,bn=BODY[1]
            for (a,an),(b,bn) in zip(BODY,BODY[1:]):
                if a<=x<=b:break
            t=max(0,min(1,(x-a)/(b-a)))
            for group_index in [g.group for g in obj.data.vertices[vi].groups]:obj.vertex_groups[group_index].remove([vi])
            for name,w in [(an,1-t),(bn,t)]:
                if w>1e-8:(obj.vertex_groups.get(name) or obj.vertex_groups.new(name=name)).add([vi],w,'REPLACE')
        fins=long_fins(obj);mesh=obj.data;uvs={v:[] for group in fins.values() for v in group}
        for p in mesh.polygons:
            if mesh.materials[p.material_index].name!='Halibut_Fins':continue
            for li in p.loop_indices:
                vi=mesh.loops[li].vertex_index
                if vi in uvs:
                    v=mesh.uv_layers.active.data[li].uv.y;uvs[vi].append((v-(.54 if v>.5 else .03))/.43)
        for name in expected:
            if obj.vertex_groups.get(name) is None:obj.vertex_groups.new(name=name)
        body_names={b.name for b in rig.data.bones}-expected
        reduced=0
        for side,_,start in SIDES:
            for vi in fins[side]:
                vertex=mesh.vertices[vi];base={obj.vertex_groups[g.group].name:g.weight for g in vertex.groups if obj.vertex_groups[g.group].name in body_names and g.weight>0}
                total=sum(base.values())
                radial=max(0,min(1,median(uvs[vi])));t=max(0,min(1,(radial-.25)/.75));influence=.98*t*t*(3-2*t)
                if influence==0:continue
                if total<1e-8:raise RuntimeError('Missing original fin attachment')
                if len(base)>2:base=dict(sorted(base.items(),key=lambda item:item[1],reverse=True)[:2]);total=sum(base.values());reduced+=1
                base={n:w/total for n,w in base.items()}
                # Keeping 2% at the tip preserves the original attachment weights
                # for repeatable edits, with at most four active weights total.
                position=max(0,min(STATIONS-1,(vertex.co.x-start)/(.89-start)*(STATIONS-1)));lower=min(STATIONS-2,int(position));fraction=position-lower
                assigned={n:w*(1-influence) for n,w in base.items()}
                for i,w in [(lower,1-fraction),(lower+1,fraction)]:
                    if influence*w>1e-8:assigned[side+'_'+str(i).zfill(2)]=influence*w
                # RNA elements refer to a mutable array. Freeze integer indices
                # before removal, otherwise compaction can leave stale weights.
                for group_index in [g.group for g in vertex.groups]:obj.vertex_groups[group_index].remove([vi])
                for name,w in assigned.items():
                    if w>1e-8:obj.vertex_groups[name].add([vi],w,'REPLACE')
        counts[obj.name]=dict(vertices={side:len(ids) for side,ids in fins.items()},posteriorSkinVerticesReboundToSpine=len(posterior),outerVerticesReducedToTwoBodyInfluences=reduced)
    for semantic,(amplitude,cycles) in MOTIONS.items():
        action=bpy.data.actions.get(semantic)
        if action is None:raise RuntimeError('Missing existing semantic '+semantic)
        rig.animation_data.action=action;bag=action.layers[0].strips[0].channelbag(rig.animation_data.action_slot)
        for curve in list(bag.fcurves):
            if any(curve.data_path.startswith('pose.bones["'+name+'"]') for name in expected):bag.fcurves.remove(curve)
        for frame in range(1,62,2):
            u=(frame-1)/60
            for side,_,_ in SIDES:
                for i in range(STATIONS):
                    s=i/(STATIONS-1);envelope=math.sin(math.pi*s)**.6;angle=amplitude*envelope*math.sin(math.tau*(cycles*u-1.1*s))
                    bone=rig.pose.bones[side+'_'+str(i).zfill(2)];bone.rotation_mode='XYZ';bone.rotation_euler=(angle,0,0);bone.keyframe_insert(data_path='rotation_euler',frame=frame)
        for curve in bag.fcurves:
            bone=curve.data_path.split('"')[1]
            if bone not in expected:continue
            i=int(bone.rsplit('_',1)[1]);s=i/(STATIONS-1);envelope=math.sin(math.pi*s)**.6
            for key in curve.keyframe_points:
                u=(key.co.x-1)/60;derivative=amplitude*envelope*math.cos(math.tau*(cycles*u-1.1*s))*math.tau*cycles/60 if curve.array_index==0 else 0
                key.interpolation='BEZIER';key.handle_left_type=key.handle_right_type='FREE';key.handle_left=(key.co.x-1,key.co.y-derivative);key.handle_right=(key.co.x+1,key.co.y+derivative)
            curve.update()
    rig.animation_data.action=bpy.data.actions['idle'];bpy.context.scene.frame_set(1)
    return dict(version=1,stationsPerSide=STATIONS,additionalBones=len(expected),totalBones=len(rig.data.bones),spatialCycles=1.1,rootUnchangedThroughRadialFraction=.25,maxFinInfluence=.98,maxWeights=4,components=counts,anglesAndCycles={k:list(v) for k,v in MOTIONS.items()},scope='Authored median-fin animation candidate, not measured adult halibut biomechanics or final art acceptance')

def update_existing():
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE/'Halibut.blend'));bpy.context.preferences.filepaths.save_version=0
    rig=bpy.data.objects['HalibutRig'];lods=[bpy.data.objects['Halibut_LOD'+str(i)] for i in range(3)]
    info=apply_fin_motion(rig,lods)
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
    for obj in lods:obj.hide_set(False);obj.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=str(GAME/'Halibut.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=.2,path_mode='RELATIVE')
    for i,obj in enumerate(lods):obj.hide_set(i!=0);obj.hide_render=i!=0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Halibut.blend'))
    report=json.loads((SOURCE/'build-report.json').read_text());report['bones']=[b.name for b in rig.data.bones];report['finMotion']=info
    for obj in lods:
        obj.data.calc_loop_triangles()
        if len(obj.data.loop_triangles)!=report['lods'][obj.name]['triangles']:raise RuntimeError('Existing geometry budget changed')
    (SOURCE/'build-report.json').write_text(json.dumps(report,indent=2,ensure_ascii=False)+'\n');print(json.dumps(info))

if __name__=='__main__':update_existing()
