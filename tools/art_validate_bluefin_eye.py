"""Independent saved geometry/UV/weight/motion inspection, including actual eye rays."""
import argparse,bpy,json,hashlib,sys,math
from pathlib import Path
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
p=argparse.ArgumentParser();p.add_argument('--baseline');p.add_argument('--baseline-output');p.add_argument('--output',required=True);a=p.parse_args(sys.argv[sys.argv.index('--')+1:])
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(Path(__file__).resolve().parent))
from bluefin_rig_contract import known_rig
checks=[];failures=[]
def check(ok,label):(checks if ok else failures).append(label)
def digest(value):return hashlib.sha256(json.dumps(value,sort_keys=True,separators=(',',':')).encode()).hexdigest()
def islands(mesh,material):
    adjacent={}
    for face in mesh.polygons:
        if mesh.materials[face.material_index].name!=material:continue
        for v in face.vertices:adjacent.setdefault(v,set()).update(face.vertices)
    result=[]
    while adjacent:
        todo=[next(iter(adjacent))];group=set()
        while todo:
            v=todo.pop()
            if v in group:continue
            group.add(v);todo.extend(adjacent.pop(v,set())-group)
        result.append(group)
    return result
def pupil_vertices(mesh):
    found=[]
    for group in islands(mesh,'Bluefin_Dark'):
        xyz=np.mean([mesh.vertices[v].co[:] for v in group],axis=0)
        if -.90<xyz[0]<-.76 and .04<xyz[2]<.17:found.append(group)
    return found
def protected_faces(obj):
    mesh=obj.data;removed=set().union(*pupil_vertices(mesh));result=[]
    for face in mesh.polygons:
        material=mesh.materials[face.material_index].name
        if material=='Bluefin_Iris' or any(v in removed for v in face.vertices):continue
        records=[]
        for loop in face.loop_indices:
            v=mesh.vertices[mesh.loops[loop].vertex_index]
            records.append((tuple(v.co),tuple(sorted((obj.vertex_groups[g.group].name,g.weight) for g in v.groups)),tuple(tuple(layer.data[loop].uv) for layer in mesh.uv_layers)))
        # Vertex reindexing after removing a disconnected object is irrelevant;
        # preserve polygon winding, coordinates, UVs and weights exactly.
        result.append((material,min(tuple(records[i:]+records[:i]) for i in range(len(records))),face.use_smooth))
    return sorted(result)
def motion():
    result={}
    for action in bpy.data.actions:
        curves=[]
        for layer in action.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for curve in bag.fcurves:curves.append((curve.data_path,curve.array_index,[(tuple(k.co),tuple(k.handle_left),tuple(k.handle_right),k.interpolation,k.handle_left_type,k.handle_right_type) for k in curve.keyframe_points]))
        result[action.name]=sorted(curves)
    return result
def iris_projection(obj,frames):
    mesh=obj.data;uvs={}
    for face in mesh.polygons:
        if mesh.materials[face.material_index].name!='Bluefin_Iris':continue
        for loop in face.loop_indices:uvs.setdefault(mesh.loops[loop].vertex_index,set()).add(tuple(tuple(layer.data[loop].uv) for layer in mesh.uv_layers))
    result={}
    for i,coordinates in uvs.items():
        v=mesh.vertices[i];sign='1' if v.co.y>0 else '-1';frame=frames[sign];center=Vector(frame['center']);axis=Vector(frame['axis']);delta=v.co-center;depth=delta.dot(axis);radial=delta-axis*depth
        key=digest((sign,depth>=-.0000001,sorted((obj.vertex_groups[g.group].name,g.weight) for g in v.groups),sorted(coordinates)))
        result.setdefault(key,[]).append(tuple(radial))
    return {key:sorted(values) for key,values in result.items()}
lods=[bpy.data.objects['Bluefin_LOD'+str(i)] for i in range(3)];rig=bpy.data.objects['BluefinRig']
snapshot=dict(version=2,meshes={obj.name:dict(protectedFaces=digest(protected_faces(obj)),protectedFaceCount=len(protected_faces(obj)),materials=[m.name for m in obj.data.materials]) for obj in lods},rest=digest([(b.name,b.parent.name if b.parent else None,[tuple(row) for row in b.matrix_local],tuple(b.head_local),tuple(b.tail_local)) for b in rig.data.bones]),motion=digest(motion()),frames={})
for group in islands(lods[0].data,'Bluefin_Iris'):
    points=np.array([lods[0].data.vertices[v].co[:] for v in group]);center=points.mean(axis=0);_,vectors=np.linalg.eigh((points-center).T@(points-center));axis=vectors[:,0];sign=1 if center[1]>0 else -1
    if axis[1]*sign<0:axis=-axis
    tangent=np.cross(axis,[0,0,1]);tangent/=np.linalg.norm(tangent);other=np.cross(axis,tangent)
    snapshot['frames'][str(sign)]=dict(center=center.tolist(),axis=axis.tolist(),tangent=tangent.tolist(),other=other.tolist())
baseline=json.loads(Path(a.baseline).read_text()) if a.baseline else snapshot
snapshot['irisProjection']={obj.name:iris_projection(obj,baseline['frames']) for obj in lods}
if a.baseline_output:
    path=Path(a.baseline_output)
    if path.exists():raise RuntimeError('Baseline already exists')
    path.write_text(json.dumps(snapshot,indent=2)+'\n')
if a.baseline:
    check(baseline.get('version')==2,'使用包含真实眼表投影的原源快照')
    check(snapshot['meshes']==baseline['meshes'],'全部非眼表面/UV/权重/材质槽逐记录保持')
    check(snapshot['rest']==baseline['rest'],'12骨静态骨架保持')
    check(snapshot['motion']==baseline['motion'],'七动作的所有曲线/关键帧/切线保持')
    for obj in lods:
        old=baseline.get('irisProjection',{}).get(obj.name,{});new=snapshot['irisProjection'][obj.name];same=old.keys()==new.keys();maximum=0
        if same:
            for key,values in old.items():
                if len(values)!=len(new[key]):same=False;continue
                maximum=max(maximum,max((Vector(p)-Vector(q)).length for p,q in zip(values,new[key])))
        check(same and maximum<.000001,obj.name+'眼表实际径向轮廓/UV/头骨权重保持')
check(known_rig(rig) and len(bpy.data.actions)==7,'已知精确骨架版本与七动作保持')
iris=bpy.data.images['Bluefin_Iris'];pixels=np.asarray(iris.pixels[:]).reshape(iris.size[1],iris.size[0],4);check(tuple(iris.size)==(256,256),'实际256虹膜贴图')
check(iris.colorspace_settings.name=='sRGB','虹膜色图sRGB')
check(float(pixels[128,128,:3].max())<.12,'瞳孔颜料在连续眼表色图内')
result_lods={};ray_results=[];last=10**9
for obj in lods:
    mesh=obj.data;mesh.calc_loop_triangles();count=len(mesh.loop_triangles)
    check(count<last and count<=25000,obj.name+'递减且不超25000预算');last=count
    check(not pupil_vertices(mesh),obj.name+'没有独立凸起瞳孔片')
    check(len(islands(mesh,'Bluefin_Iris'))==2,obj.name+'双眼各为连续眼表')
    triangles=list(mesh.loop_triangles);tree=BVHTree.FromPolygons([v.co for v in mesh.vertices],[t.vertices for t in triangles],all_triangles=True)
    for sign,frame in baseline['frames'].items():
        center=Vector(frame['center']);axis=Vector(frame['axis']);tx=Vector(frame['tangent']);tz=Vector(frame['other']);wrong=0;depths=[]
        for radius in [0,.016,.028]:
            for angle in range(0,360,30):
                theta=math.radians(angle);point=center+(tx*math.cos(theta)+tz*math.sin(theta))*radius
                hit,normal,index,distance=tree.ray_cast(point+axis*.08,-axis,.16)
                visible=index is not None and mesh.materials[triangles[index].material_index].name=='Bluefin_Iris'
                if not visible:wrong+=1
                if radius==0 and hit is not None:depths.append((hit-center).dot(axis))
        check(wrong==0,obj.name+'眼表可见瞳孔区域无鱼颊或旧凸片遮挡 '+sign)
        check(bool(depths) and .012<min(depths)<.026,obj.name+'连续眼表突出有限 '+sign)
        ray_results.append(dict(lod=obj.name,side=sign,rays=36,wrongFirstSurface=wrong,centerDepth=min(depths) if depths else None))
    result_lods[obj.name]=dict(triangles=count,vertices=len(mesh.vertices),protectedFaces=snapshot['meshes'][obj.name]['protectedFaceCount'])
result=dict(status='FAIL' if failures else 'ENGINEERING_CHECKS_PASS',scope='Saved actual source geometry/UV/weights/rest/actions and finite eye first-hit rays; not render/art/science/native/physical acceptance',checks=checks,failures=failures,lods=result_lods,eyeRays=ray_results)
Path(a.output).write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(dict(status=result['status'],checks=len(checks),failures=failures,lods=result_lods),ensure_ascii=False));raise SystemExit(bool(failures))
