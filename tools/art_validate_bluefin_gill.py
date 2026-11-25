"""Inspect saved source; first-hit coverage uses actual faces, not authoring paths."""
import argparse,bpy,json,hashlib,sys,math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
p=argparse.ArgumentParser();p.add_argument('--baseline');p.add_argument('--baseline-output');p.add_argument('--output',required=True);p.add_argument('--require-continuous',action='store_true');p.add_argument('--rest-only',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:])
sys.path.insert(0,str(Path(__file__).resolve().parent))
from bluefin_rig_contract import known_rig
checks=[];failures=[]
def check(ok,text):(checks if ok else failures).append(text)
def digest(v):return hashlib.sha256(json.dumps(v,sort_keys=True,separators=(',',':')).encode()).hexdigest()
def records(obj):
    data=obj.data;result=[]
    for face in data.polygons:
        material=data.materials[face.material_index].name
        if material=='Bluefin_Gill':continue
        if material=='Bluefin_Skin' and max(mesh_v.co.x for mesh_v in (data.vertices[i] for i in face.vertices))>=-.75 and min(data.vertices[i].co.x for i in face.vertices)<=-.32:continue
        corners=[]
        for index in face.loop_indices:
            v=data.vertices[data.loops[index].vertex_index]
            corners.append((tuple(v.co),sorted((obj.vertex_groups[g.group].name,g.weight) for g in v.groups),[tuple(layer.data[index].uv) for layer in data.uv_layers]))
        variants=[corners[i:]+corners[:i] for i in range(len(corners))]
        result.append((material,min(json.dumps(v,separators=(',',':')) for v in variants),face.use_smooth))
    return sorted(result)
def actions():
    result={}
    for action in bpy.data.actions:
        result[action.name]=sorted((curve.data_path,curve.array_index,[(tuple(k.co),tuple(k.handle_left),tuple(k.handle_right),k.interpolation,k.handle_left_type,k.handle_right_type) for k in curve.keyframe_points]) for layer in action.layers for strip in layer.strips for bag in strip.channelbags for curve in bag.fcurves)
    return result
def surface_snapshot(obj):
    data=obj.data;data.calc_loop_triangles();triangles=[];indices=set()
    for tri in data.loop_triangles:
        if data.materials[data.polygons[tri.polygon_index].material_index].name!='Bluefin_Skin':continue
        indices.update(tri.vertices);triangles.append(dict(indices=list(tri.vertices),uv=[[list(layer.data[i].uv) for i in tri.loops] for layer in data.uv_layers]))
    return dict(vertices={str(i):dict(co=list(data.vertices[i].co),weights=sorted((obj.vertex_groups[g.group].name,g.weight) for g in data.vertices[i].groups)) for i in indices},triangles=triangles)
def verify_surface(obj,original):
    verts={int(i):v for i,v in original['vertices'].items()};points=[Vector(v['co']) for v in verts.values()];indices=list(verts);index_map={v:i for i,v in enumerate(indices)};triangles=original['triangles'];tree=BVHTree.FromPolygons(points,[[index_map[i] for i in t['indices']] for t in triangles],all_triangles=True)
    old_uv={};old_weights={}
    for v in verts.values():old_weights.setdefault(tuple(v['co']),[]).append(v['weights'])
    for tri in triangles:
        for i,index in enumerate(tri['indices']):old_uv.setdefault(tuple(verts[index]['co']),[]).append([layer[i] for layer in tri['uv']])
    data=obj.data;data.calc_loop_triangles();surface=[t for t in data.loop_triangles if data.materials[data.polygons[t.polygon_index].material_index].name in ['Bluefin_Skin','Bluefin_Gill']];surface_vertices={i for t in surface for i in t.vertices};max_distance=max_weight=max_uv=0;new=0;expected={}
    for i in surface_vertices:
        v=data.vertices[i];co=tuple(v.co);weights=sorted((obj.vertex_groups[g.group].name,g.weight) for g in v.groups)
        if co in old_weights:
            if not any(weights==[tuple(g) for g in option] for option in old_weights[co]):max_weight=1
            continue
        new+=1;hit,_,index,distance=tree.find_nearest(v.co);max_distance=max(max_distance,distance);tri=triangles[index];q=[Vector(verts[i]['co']) for i in tri['indices']];u=q[1]-q[0];w=q[2]-q[0];d=hit-q[0];den=u.dot(u)*w.dot(w)-u.dot(w)**2
        b1=(d.dot(u)*w.dot(w)-d.dot(w)*u.dot(w))/den;b2=(d.dot(w)*u.dot(u)-d.dot(u)*u.dot(w))/den;bary=[1-b1-b2,b1,b2];source_weights=[dict(verts[j]['weights']) for j in tri['indices']];actual=dict(weights)
        for name in set(actual).union(*(set(s) for s in source_weights)):max_weight=max(max_weight,abs(actual.get(name,0)-sum(source_weights[k].get(name,0)*bary[k] for k in range(3))))
        expected[i]=[[sum(layer[k][axis]*bary[k] for k in range(3)) for axis in range(2)] for layer in tri['uv']]
    for tri in surface:
        for loop in tri.loops:
            index=data.loops[loop].vertex_index;co=tuple(data.vertices[index].co);actual=[list(layer.data[loop].uv) for layer in data.uv_layers];options=old_uv.get(co) or [expected[index]]
            max_uv=max(max_uv,min(max(abs(actual[l][axis]-option[l][axis]) for l in range(len(actual)) for axis in range(2)) for option in options))
    old_area=sum((Vector(verts[t['indices'][1]]['co'])-Vector(verts[t['indices'][0]]['co'])).cross(Vector(verts[t['indices'][2]]['co'])-Vector(verts[t['indices'][0]]['co'])).length/2 for t in triangles);new_area=sum(t.area for t in surface)
    check(max_distance<.0000003,obj.name+' new vertices lie on original body triangles');check(max_weight<.00003,obj.name+' original/interpolated body weights preserved');check(max_uv<.000003,obj.name+' original/interpolated body UV preserved');check(abs(new_area-old_area)<.00001,obj.name+' original body surface area preserved')
    return dict(newSurfaceVertices=new,maximumDistance=max_distance,maximumWeightError=max_weight,maximumUvError=max_uv,oldArea=old_area,newArea=new_area)
lods=[bpy.data.objects['Bluefin_LOD'+str(i)] for i in range(3)];rig=bpy.data.objects['BluefinRig'];root=Path(__file__).resolve().parents[1]
snapshot=dict(meshes={o.name:dict(protectedFaces=digest(records(o)),protectedFaceCount=len(records(o)),materials=[m.name for m in o.data.materials]) for o in lods},rest=digest([(bone.name,bone.parent.name if bone.parent else None,[tuple(v) for v in bone.matrix_local]) for bone in rig.data.bones]),actions=digest(actions()),maps={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (root/'game/Assets/OceanAngler/Art/Bluefin').glob('*.png')})
snapshot['version']=2;snapshot['bodySurface']={obj.name:surface_snapshot(obj) for obj in lods}
if a.baseline_output:
    path=Path(a.baseline_output)
    if path.exists():raise RuntimeError('Original baseline must not be overwritten')
    path.write_text(json.dumps(snapshot,indent=2)+'\n')
if a.baseline:
    old=json.loads(Path(a.baseline).read_text())
    check(old.get('version')==2,'Original pre-update source with body interpolation data')
    for key in ['meshes','rest','actions','maps']:check(snapshot[key]==old[key],key+' unchanged outside local skin patch')
    surface_results={obj.name:verify_surface(obj,old['bodySurface'][obj.name]) for obj in lods}
check(known_rig(rig) and len(bpy.data.actions)==7,'known exact source rig version and seven actions')
surface_results=locals().get('surface_results',{});previous=10**9
for obj in lods:
    data=obj.data;data.calc_loop_triangles();count=len(data.loop_triangles);check(count<previous and count<=25000,obj.name+' decreasing original budget');previous=count
    check(all(abs(sum(g.weight for g in v.groups)-1)<.002 for v in data.vertices),obj.name+' normalized skin weights')
    adjacency={}
    for face in data.polygons:
        if data.materials[face.material_index].name=='Bluefin_Gill':
            for index in face.vertices:adjacency.setdefault(index,set()).update(face.vertices)
    islands=[]
    while adjacency:
        todo=[next(iter(adjacency))];found=set()
        while todo:
            index=todo.pop()
            if index in found:continue
            found.add(index);todo.extend(adjacency.pop(index,set())-found)
        islands.append(found)
    check(len(islands)==2,obj.name+' one connected rim on each side')
    if len(islands)==2:check(all(max(data.vertices[i].co.y for i in group)*min(data.vertices[i].co.y for i in group)>0 for group in islands),obj.name+' two sides remain separate')
    edge_faces={}
    for face in data.polygons:
        for edge in face.edge_keys:edge_faces.setdefault(tuple(sorted(edge)),[]).append(data.materials[face.material_index].name)
    boundary=[values for values in edge_faces.values() if values.count('Bluefin_Gill')==1]
    if a.require_continuous:check(len(boundary)>10 and all(sorted(v)==['Bluefin_Gill','Bluefin_Skin'] for v in boundary),obj.name+' rim boundary shares closed skin edges')
samples=[];hidden={obj.name:obj.hide_get() for obj in lods}
for obj in lods:obj.hide_set(False)
graph=bpy.context.evaluated_depsgraph_get();original_action=rig.animation_data.action;original_frame=bpy.context.scene.frame_current
try:
    poses=[('idle',1)] if a.rest_only else [(name,frame) for name in sorted(actions()) for frame in [1,16,31,46,61]]
    for motion,frame in poses:
        rig.animation_data.action=bpy.data.actions[motion];bpy.context.scene.frame_set(frame);graph.update()
        for obj in lods:
            evaluated=obj.evaluated_get(graph);data=evaluated.to_mesh()
            try:
                data.calc_loop_triangles();triangles=[t for t in data.loop_triangles if data.materials[data.polygons[t.polygon_index].material_index].name in ['Bluefin_Skin','Bluefin_Gill']]
                tree=BVHTree.FromPolygons([v.co for v in data.vertices],[tuple(t.vertices) for t in triangles],all_triangles=True)
                for side in [-1,1]:
                    total=visible=0;distances=[]
                    for t in triangles:
                        if data.materials[data.polygons[t.polygon_index].material_index].name!='Bluefin_Gill':continue
                        points=[data.vertices[i].co for i in t.vertices];center=sum(points,Vector())/3;normal=(points[1]-points[0]).cross(points[2]-points[0]).normalized()
                        if side*center.y<=0 or side*normal.y<.25:continue
                        total+=1;hit,_,index,distance=tree.ray_cast(center+Vector((0,side*.1,0)),Vector((0,-side,0)),.2)
                        if index is not None and data.materials[data.polygons[triangles[index].polygon_index].material_index].name=='Bluefin_Gill' and (hit-center).length<.00015:visible+=1
                        if hit is not None:distances.append((hit-center).length)
                    fraction=visible/max(1,total);samples.append(dict(lod=obj.name,motion=motion,frame=frame,side=side,tested=total,visible=visible,fraction=fraction,maximumFirstHitDeviation=max(distances,default=1)))
                    if a.require_continuous:check(total>10 and fraction>=.99,obj.name+'/'+motion+'/'+str(frame)+'/'+str(side)+' visible rim coverage')
            finally:evaluated.to_mesh_clear()
finally:
    rig.animation_data.action=original_action;bpy.context.scene.frame_set(original_frame)
    for obj in lods:obj.hide_set(hidden[obj.name])
result=dict(status='FAIL' if failures else 'ENGINEERING_CHECKS_PASS',scope='Saved source identity and triangle-centroid first-hit coverage against body/gill geometry only. Fins excluded from occlusion fixture. Not rendered appearance, scientific/user or native qualification.',checks=checks,failures=failures,samples=samples,surfaceIdentity=surface_results,minimumCoverage=min(s['fraction'] for s in samples),protectedFaces=sum(v['protectedFaceCount'] for v in snapshot['meshes'].values()))
Path(a.output).write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(dict(status=result['status'],checks=len(checks),failures=failures[:8],minimumCoverage=result['minimumCoverage'],samples=len(samples))));raise SystemExit(bool(failures))
