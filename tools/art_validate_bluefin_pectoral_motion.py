"""Inspect saved paired-fin deformation in the moving root frame, not bone names alone."""
import argparse,bpy,hashlib,json,math,sys
from pathlib import Path
from statistics import median
from mathutils import Vector

p=argparse.ArgumentParser();reference=p.add_mutually_exclusive_group(required=True);reference.add_argument('--record-reference');reference.add_argument('--reference');p.add_argument('--surface-reference');p.add_argument('--output',required=True);p.add_argument('--repeat-check',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:])
if a.surface_reference and not a.reference:p.error('--surface-reference requires the independently preserved original rig/action reference')
root=Path(__file__).resolve().parents[1];rig=bpy.data.objects['BluefinRig'];lods=[bpy.data.objects['Bluefin_LOD'+str(i)] for i in range(3)]
extra={side+segment for side in ['pectoralL','pectoralR'] for segment in ['Mid','Tip']};semantics=['idle','swim','turn','burst','bite','hooked','release'];checks=[];failures=[]
def check(value,label):(checks if value else failures).append(label)
def digest(value):return hashlib.sha256(json.dumps(value,sort_keys=True,separators=(',',':')).encode()).hexdigest()
def curves():
    return {action.name:sorted((curve.data_path,curve.array_index,[(list(k.co),list(k.handle_left),list(k.handle_right),k.interpolation,k.handle_left_type,k.handle_right_type) for k in curve.keyframe_points]) for layer in action.layers for strip in layer.strips for bag in strip.channelbags for curve in bag.fcurves if not any('"'+name+'"' in curve.data_path for name in extra)) for action in bpy.data.actions}
def weights(obj,vertex):return sorted((obj.vertex_groups[g.group].name,g.weight) for g in vertex.groups if g.weight>1e-8)
def radial_values(obj,indices):
    values={i:[] for i in indices}
    for face in obj.data.polygons:
        for index in face.loop_indices:
            v=obj.data.loops[index].vertex_index
            if v in values:values[v].append(obj.data.uv_layers.active.data[index].uv.y)
    return {str(i):median(v) for i,v in values.items()}
def snapshot():
    result=dict(version=1,meshes={},bones={b.name:dict(parent=b.parent.name if b.parent else None,matrix=[list(row) for row in b.matrix_local]) for b in rig.data.bones if b.name not in extra},actions=curves(),maps={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (root/'game/Assets/OceanAngler/Art/Bluefin').glob('*.png')})
    for obj in lods:
        m=obj.data;m.calc_loop_triangles();fin={}
        for side in ['pectoralL','pectoralR']:
            ids={v.index for v in m.vertices if any(obj.vertex_groups[g.group].name in {side,side+'Mid',side+'Tip'} and g.weight>1e-8 for g in v.groups)}
            fin[side]=dict(indices=sorted(ids),radial=radial_values(obj,ids))
        result['meshes'][obj.name]=dict(geometry=digest(dict(vertices=[list(v.co) for v in m.vertices],faces=[(list(f.vertices),m.materials[f.material_index].name,f.use_smooth) for f in m.polygons],uv=[[list(v.uv) for v in layer.data] for layer in m.uv_layers],transform=[list(v) for v in obj.matrix_local])),triangles=len(m.loop_triangles),fin=fin,weights={str(v.index):weights(obj,v) for v in m.vertices})
    # Compare the persisted JSON schema, not Python tuple-vs-list containers.
    return json.loads(json.dumps(result))
current=snapshot()
if a.record_reference:
    path=Path(a.record_reference)
    if path.exists():raise RuntimeError('Preserve the original reference')
    path.write_text(json.dumps(current,indent=2)+'\n')
if a.reference:
    original=json.loads(Path(a.reference).read_text());check(original['version']==1,'original reference version')
    check(current['bones']==original['bones'],'all twelve original bone rest transforms and parents unchanged')
    check(current['actions']==original['actions'],'every original action curve/key/handle unchanged')
    check(current['maps']==original['maps'],'all nine exported maps byte-identical')
else:original=current
expected_base={'root','head','spine1','spine2','spine3','peduncle','tail','jaw','pectoralL','pectoralR','pelvicL','pelvicR'}
check({b.name for b in rig.data.bones}==expected_base|extra,'exact original twelve plus four flexible controls')
check(set(current['actions'])==set(semantics),'exact seven existing semantics')
for obj in lods:
    old=original['meshes'][obj.name];now=current['meshes'][obj.name];check(bool(a.surface_reference) or now['geometry']==old['geometry'],obj.name+' rest geometry identity or separately verified membrane-only refinement')
    check(old['triangles']<=now['triangles']<=25000,obj.name+' original 25000 triangle budget')
    fin_ids=set().union(*(set(v['indices']) for v in old['fin'].values()))
    check(all(now['weights'][i]==v for i,v in old['weights'].items() if int(i) not in fin_ids),obj.name+' every nonpectoral weight unchanged')
    for side,info in old['fin'].items():
        check(bool(info['indices']) and set(info['indices'])<=set(now['fin'][side]['indices']),obj.name+' '+side+' original membrane/ray vertices retained')
        roots=[str(i) for i in info['indices'] if info['radial'][str(i)]<=.250001]
        check(bool(roots) and all(now['weights'][i]==old['weights'][i] for i in roots),obj.name+' '+side+' root quarter weights unchanged')
        check(all(0<len(now['weights'][str(i)])<=4 and abs(sum(w for _,w in now['weights'][str(i)])-1)<2e-6 for i in info['indices']),obj.name+' '+side+' finite normalized max-four weights')
for action in bpy.data.actions:
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for curve in bag.fcurves:
                    if not any('"'+name+'"' in curve.data_path for name in extra):continue
                    keys=curve.keyframe_points;first=keys[0];last=keys[-1]
                    check(abs(first.co.y-last.co.y)<1e-6,action.name+' flexible curve values periodic '+curve.data_path+'/'+str(curve.array_index))
                    before=(first.co.y-first.handle_left.y)/(first.co.x-first.handle_left.x);after=(last.handle_right.y-last.co.y)/(last.handle_right.x-last.co.x)
                    check(abs(before-after)<1e-6,action.name+' flexible curve boundary derivatives match '+curve.data_path+'/'+str(curve.array_index))
motions={};prior_action=rig.animation_data.action;prior_frame=bpy.context.scene.frame_current;hidden={obj.name:obj.hide_get() for obj in lods}
for obj in lods:obj.hide_set(False)
graph=bpy.context.evaluated_depsgraph_get()
try:
    for semantic in semantics:
        rig.animation_data.action=bpy.data.actions[semantic];results={obj.name:{s:dict(maximumOuterNonRigidDisplacement=0,maximumRootError=0,outerSampleCount=0) for s in ['pectoralL','pectoralR']} for obj in lods}
        for frame in [1,8,16,23,31,38,46,53]:
            bpy.context.scene.frame_set(frame);graph.update()
            for obj in lods:
                evaluated=obj.evaluated_get(graph);mesh=evaluated.to_mesh();to_rig=rig.matrix_world.inverted()@obj.matrix_world
                try:
                    for side,info in original['meshes'][obj.name]['fin'].items():
                        inverse=rig.pose.bones[side].matrix.inverted();rest_inverse=rig.data.bones[side].matrix_local.inverted();result=results[obj.name][side]
                        for vi in info['indices']:
                            delta=(inverse@to_rig@mesh.vertices[vi].co-rest_inverse@to_rig@obj.data.vertices[vi].co).length
                            t=info['radial'][str(vi)]
                            if t<=.250001:result['maximumRootError']=max(result['maximumRootError'],delta)
                            if t>=.85:result['maximumOuterNonRigidDisplacement']=max(result['maximumOuterNonRigidDisplacement'],delta);result['outerSampleCount']+=1
                finally:evaluated.to_mesh_clear()
        for obj,values in results.items():
            for side,result in values.items():
                check(result['maximumRootError']<2e-6,semantic+' '+obj+' '+side+' moving root remains attached')
                check(result['outerSampleCount']>0 and result['maximumOuterNonRigidDisplacement']>.002,semantic+' '+obj+' '+side+' actual distal surface bends beyond rigid root rotation')
        motions[semantic]=results
    if a.repeat_check:
        sys.path.insert(0,str(root/'tools'));from art_bluefin_pectoral_motion import configure_fin_rig,write_fin_motions
        before=snapshot();configure_fin_rig(rig,lods);write_fin_motions(rig);after=snapshot();maximum=0
        for name,m in before['meshes'].items():
            for index,value in m['weights'].items():
                av=dict(after['meshes'][name]['weights'][index]);bv=dict(value)
                maximum=max(maximum,max((abs(av.get(k,0)-bv.get(k,0)) for k in av.keys()|bv.keys()),default=0))
        check(maximum<1e-7,'repeat authoring does not accumulate weight drift');check(before['actions']==after['actions'],'repeat authoring keeps original curves');check(all(before['meshes'][k]['geometry']==after['meshes'][k]['geometry'] for k in before['meshes']),'repeat authoring keeps original geometry')
        repeat=dict(maximumWeightDifference=maximum,savedToSource=False)
finally:
    rig.animation_data.action=prior_action;bpy.context.scene.frame_set(prior_frame)
    for obj in lods:obj.hide_set(hidden[obj.name])
if a.surface_reference:
    from art_validate_bluefin_pectoral_surface import verify_surface
    surface_identity=verify_surface(lods,Path(a.surface_reference),check)
result=dict(status='PASS' if not failures else 'FAIL',checks=checks,failures=failures,motions=motions,bones=len(rig.data.bones),maps=current['maps'],scope='Saved Blender engineering identity and actual deformed mesh in moving root frame; not measured fish biomechanics, Unity/Player or final art acceptance')
if a.repeat_check:result['repeat']=repeat
if a.surface_reference:result['surfaceIdentity']=surface_identity
path=Path(a.output);path.parent.mkdir(parents=True,exist_ok=True)
if path.exists():raise RuntimeError('Retain prior evidence instead of overwriting')
path.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(dict(status=result['status'],checks=len(checks),failures=failures,bones=result['bones'])),flush=True)
if failures:raise RuntimeError('Paired-fin source qualification failed')
