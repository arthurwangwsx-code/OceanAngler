"""Independent serialized pre-change mesh comparison for membrane refinement."""
import bpy,json,numpy as np
from mathutils import Vector,geometry
from mathutils.bvhtree import BVHTree

def capture(objects):
    result={}
    for obj in objects:
        mesh=obj.data;mesh.calc_loop_triangles()
        fin={v.index for v in mesh.vertices if any(obj.vertex_groups[g.group].name in {'pectoralL','pectoralR'} and g.weight>0 for g in v.groups)}
        selected={f.index for f in mesh.polygons if mesh.materials[f.material_index].name=='Bluefin_Fins' and set(f.vertices)<=fin}
        def record(face):return dict(indices=list(face.vertices),material=face.material_index,smooth=face.use_smooth,uv=[[list(layer.data[l].uv) for layer in mesh.uv_layers] for l in face.loop_indices])
        result[obj.name]=dict(vertices=[list(v.co) for v in mesh.vertices],materials=[m.name for m in mesh.materials],transform=[list(row) for row in obj.matrix_local],protected=[record(f) for f in mesh.polygons if f.index not in selected],membrane=[dict(indices=list(t.vertices),uv=[[list(layer.data[l].uv) for layer in mesh.uv_layers] for l in t.loops],area=t.area) for t in mesh.loop_triangles if t.polygon_index in selected],finIndices=sorted(fin))
    return result

def closest(point,a,b,c):
    # Double-precision closest-point regions avoid float32 BVH barycentrics on
    # the thin membrane's nearly collinear shell/cap triangles.
    ab=b-a;ac=c-a;ap=point-a;d1=ab@ap;d2=ac@ap
    if d1<=0 and d2<=0:return a,np.array([1.,0.,0.])
    bp=point-b;d3=ab@bp;d4=ac@bp
    if d3>=0 and d4<=d3:return b,np.array([0.,1.,0.])
    vc=d1*d4-d3*d2
    if vc<=0 and d1>=0 and d3<=0:
        v=d1/(d1-d3);return a+v*ab,np.array([1-v,v,0.])
    cp=point-c;d5=ab@cp;d6=ac@cp
    if d6>=0 and d5<=d6:return c,np.array([0.,0.,1.])
    vb=d5*d2-d1*d6
    if vb<=0 and d2>=0 and d6<=0:
        w=d2/(d2-d6);return a+w*ac,np.array([1-w,0.,w])
    va=d3*d6-d5*d4
    if va<=0 and d4-d3>=0 and d5-d6>=0:
        w=(d4-d3)/((d4-d3)+(d5-d6));return b+w*(c-b),np.array([0.,1-w,w])
    denominator=va+vb+vc
    if abs(denominator)<1e-30:raise RuntimeError('Degenerate source triangle needs explicit review')
    v=vb/denominator;w=vc/denominator;return a+ab*v+ac*w,np.array([1-v-w,v,w])

def verify_surface(objects,reference,check):
    original=json.loads(reference.read_text());results={}
    for obj in objects:
        old=original['meshes'][obj.name];new=obj.data;new.calc_loop_triangles();fin=set(old['finIndices']);count=len(old['vertices'])
        def selected(face):return new.materials[face.material_index].name=='Bluefin_Fins' and all(i in fin or i>=count for i in face.vertices)
        check(len(new.vertices)>=count and all(v==list(new.vertices[i].co) for i,v in enumerate(old['vertices'])),obj.name+' every original rest vertex remains exactly at its original index')
        check([list(row) for row in obj.matrix_local]==old['transform'] and old['materials']==[m.name for m in new.materials],obj.name+' object transform and material slot identity')
        protected=[dict(indices=list(f.vertices),material=f.material_index,smooth=f.use_smooth,uv=[[list(layer.data[l].uv) for layer in new.uv_layers] for l in f.loop_indices]) for f in new.polygons if not selected(f)]
        check(protected==old['protected'],obj.name+' every nonpectoral-membrane polygon, UV, material and smooth flag exactly retained, including all opaque rays')
        original_triangles=old['membrane'];current=[t for t in new.loop_triangles if selected(new.polygons[t.polygon_index])]
        tree=BVHTree.FromPolygons(old['vertices'],[t['indices'] for t in original_triangles],all_triangles=True)
        distance=uv_error=0;points=np.asarray(old['vertices'],dtype=np.float64);vertex_candidates={}
        for triangle in current:
            for vi,li in zip(triangle.vertices,triangle.loops):
                if vi not in vertex_candidates:
                    point=np.asarray(new.vertices[vi].co,dtype=np.float64);nearest=tree.find_nearest(Vector(point));indices={item[2] for item in tree.find_nearest_range(Vector(point),.0001)}
                    if nearest[2] is not None:indices.add(nearest[2])
                    candidates=[]
                    for j in indices:
                        t=original_triangles[j];hit,bary=closest(point,*(points[i] for i in t['indices']));candidates.append((float(np.linalg.norm(hit-point)),j,bary))
                    if not candidates:raise RuntimeError('Actual original membrane surface is required')
                    vertex_candidates[vi]=candidates;distance=max(distance,min(d for d,_,_ in candidates))
                errors=[]
                for d,j,bary in vertex_candidates[vi]:
                    if d>=3e-7:continue
                    expected=np.tensordot(bary,np.asarray(original_triangles[j]['uv'],dtype=np.float64),axes=(0,0))
                    errors.append(max(abs(new.uv_layers[k].data[li].uv[axis]-expected[k,axis]) for k in range(len(new.uv_layers)) for axis in (0,1)))
                uv_error=max(uv_error,min(errors,default=1))
        old_area=sum(t['area'] for t in original_triangles);new_area=sum(t.area for t in current)
        check(bool(original_triangles) and bool(current) and distance<3e-7,obj.name+' refined membrane lies on original triangle surface within 0.3 micrometres')
        check(uv_error<3e-6,obj.name+' membrane per-corner UV interpolates original surface')
        check(abs(new_area-old_area)<2e-6,obj.name+' membrane rest surface area retained')
        used={i for f in new.polygons for i in f.vertices};check(all(i in used for i in range(count,len(new.vertices))),obj.name+' all added vertices belong to actual membrane faces')
        results[obj.name]=dict(originalVertices=count,newVertices=len(new.vertices)-count,newTriangles=len(new.loop_triangles),protectedPolygons=len(old['protected']),maximumSurfaceDistance=distance,maximumUvError=uv_error,oldMembraneArea=old_area,newMembraneArea=new_area)
    return results
