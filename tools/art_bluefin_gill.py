"""Integrate opercular rims into saved skin triangles; protect the original surface."""
import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
RECIPE='triangle-integrated-opercular-rim-v4'
def cross(a,b):return a.x*b.y-a.y*b.x
def clip(poly,a,b):
    result=[];edge=b-a
    for current,next in zip(poly,poly[1:]+poly[:1]):
        u=cross(edge,current[0]-a);v=cross(edge,next[0]-a);inside=u>=-1e-10
        if inside:result.append(current)
        if inside!=(v>=-1e-10):
            t=u/(u-v);result.append((current[0].lerp(next[0],t),current[1].lerp(next[1],t)))
    return result
def replace_rims(lods):
    result={}
    def area(poly):return abs(sum(cross(p[0],q[0]) for p,q in zip(poly,poly[1:]+poly[:1])))/2
    def overlaps(poly,boundary):
        return not (max(p[0].x for p in poly)<min(p.x for p in boundary) or min(p[0].x for p in poly)>max(p.x for p in boundary) or max(p[0].y for p in poly)<min(p.y for p in boundary) or min(p[0].y for p in poly)>max(p.y for p in boundary))
    for level,obj in enumerate(lods):
        data=obj.data;data.calc_loop_triangles();gill=list(data.materials).index(bpy.data.materials['Bluefin_Gill']);old_gill=[p for p in data.polygons if p.material_index==gill];gill_ids={i for p in old_gill for i in p.vertices}
        extents={side:(max(data.vertices[i].co.z for i in gill_ids if data.vertices[i].co.y*side>0),min(data.vertices[i].co.z for i in gill_ids if data.vertices[i].co.y*side>0)) for side in [-1,1]}
        count=[32,16,6][level];half_width=[.0009,.001,.0012][level];boundaries={}
        for side in [-1,1]:
            top,bottom=(v/math.cos(.36) for v in extents[side]);points=[]
            for i in range(count+1):
                theta=.36+(math.pi-.72)*i/count;points.append(Vector((-.55+.055*math.sin(theta),math.cos(theta)*(top if math.cos(theta)>=0 else -bottom))))
            left=[];right=[]
            for i,p in enumerate(points):
                d=(points[min(count,i+1)]-points[max(0,i-1)]).normalized();n=Vector((-d.y,d.x));width=half_width*(.3+.7*math.sin(math.pi*i/count)**.35);left.append(p+n*width);right.append(p-n*width)
            boundaries[side]=[]
            for i in range(count):
                q=[left[i],right[i],right[i+1],left[i+1]]
                if sum(cross(p,r) for p,r in zip(q,q[1:]+q[:1]))<0:q.reverse()
                boundaries[side].append(q)
        triangles={};affected=set()
        for tri in data.loop_triangles:
            face=data.polygons[tri.polygon_index]
            if data.materials[face.material_index].name!='Bluefin_Skin':continue
            pts=[data.vertices[i].co.copy() for i in tri.vertices]
            bounds=[data.vertices[i].co.x for i in face.vertices]
            if max(bounds)<-.56 or min(bounds)>-.48:continue
            side=1 if sum(p.y for p in pts)>0 else -1;projected=[Vector((p.x,p.z)) for p in pts];poly=[(p,Vector((int(i==0),int(i==1),int(i==2)))) for i,p in enumerate(projected)]
            if area(poly)<1e-12:continue
            pieces=[poly];rim=[]
            for boundary in boundaries[side]:
                remaining=[]
                for piece in pieces:
                    if not overlaps(piece,boundary):remaining.append(piece);continue
                    inside=piece;outside=[]
                    for a,b in zip(boundary,boundary[1:]+boundary[:1]):
                        discard=clip(inside,b,a)
                        if len(discard)>=3 and area(discard)>1e-12:outside.append(discard)
                        inside=clip(inside,a,b)
                        if len(inside)<3 or area(inside)<1e-12:inside=[];break
                    remaining.extend(outside)
                    if inside:rim.append(inside)
                pieces=remaining
            weights=[dict((g.group,g.weight) for g in data.vertices[i].groups) for i in tri.vertices];uv=[[layer.data[i].uv.copy() for i in tri.loops] for layer in data.uv_layers]
            triangles.setdefault(face.index,[]).append((pts,weights,uv,face.material_index,[(piece,False) for piece in pieces]+[(piece,True) for piece in rim]))
            if rim:affected.add(face.index)
        # Include every original triangle of a touched polygon. The broad
        # triangle bounds above already include its other half at this rim.
        if not affected:raise RuntimeError('No real body faces intersect the rim')
        for face_id in affected:
            expected=sum(t.polygon_index==face_id for t in data.loop_triangles)
            if len(triangles.get(face_id,[]))!=expected:raise RuntimeError('Incomplete original skin polygon')
        bm=bmesh.new();bm.from_mesh(data);bm.faces.ensure_lookup_table();bm.verts.ensure_lookup_table();deform=bm.verts.layers.deform.verify();original_weights={v:dict(v[deform]) for v in bm.verts};uv_layers=[bm.loops.layers.uv[layer.name] for layer in data.uv_layers]
        skin_vertices={v for f in bm.faces if data.materials[f.material_index].name=='Bluefin_Skin' for v in f.verts};lookup={tuple(round(c,7) for c in v.co):v for v in skin_vertices};patch=set();removed=[bm.faces[i] for i in affected]
        for face in removed:patch.update(face.verts);bm.faces.remove(face)
        old_vertices={v for f in bm.faces if f.material_index==gill for v in f.verts};bmesh.ops.delete(bm,geom=list(old_vertices),context='VERTS')
        for face_id in affected:
            for pts,weights,uv,skin,fragments in triangles[face_id]:
                for poly,is_rim in fragments:
                    vertices=[];coordinates=[]
                    for _,bary in poly:
                        position=sum((pts[i]*bary[i] for i in range(3)),Vector());key=tuple(round(c,7) for c in position);vertex=lookup.get(key)
                        if vertex is None:
                            vertex=bm.verts.new(position);lookup[key]=vertex
                            for group in set().union(*weights):vertex[deform][group]=sum(weights[i].get(group,0)*bary[i] for i in range(3))
                        if vertices and vertex==vertices[-1]:continue
                        vertices.append(vertex);coordinates.append(bary);patch.add(vertex)
                    if len(vertices)>1 and vertices[0]==vertices[-1]:vertices.pop();coordinates.pop()
                    if len(set(vertices))<3:continue
                    face=bm.faces.new(vertices);face.material_index=gill if is_rim else skin;face.smooth=True
                    for loop,bary in zip(face.loops,coordinates):
                        for layer,values in zip(uv_layers,uv):loop[layer].uv=sum((values[i]*bary[i] for i in range(3)),Vector((0,0)))
        # Stitch every collinear intersection into both adjacent faces. A
        # geometric T-junction would crack during linear-blend skinning.
        stitched=0
        for edge in list(bm.edges):
            if not edge.is_valid or not edge.link_faces or not any(v in patch for v in edge.verts):continue
            start,end=edge.verts;delta=end.co-start.co;squared=delta.length_squared
            if squared<1e-14:continue
            candidates=[]
            for vertex in patch:
                if vertex in (start,end) or not vertex.is_valid:continue
                t=(vertex.co-start.co).dot(delta)/squared
                if 1e-6<t<1-1e-6 and (vertex.co-start.co-delta*t).length<1e-7:candidates.append((t,vertex))
            current=start
            for _,target in sorted(candidates,key=lambda v:v[0]):
                if target==current:continue
                segment=next((e for e in current.link_edges if e.is_valid and end in e.verts),None)
                if segment is None:break
                t=(target.co-current.co).length/(end.co-current.co).length
                _,created=bmesh.utils.edge_split(segment,current,t);bmesh.ops.weld_verts(bm,targetmap={created:target});current=target;stitched+=1
        patch=[v for v in patch if v.is_valid]
        bmesh.ops.remove_doubles(bm,verts=patch,dist=5e-7)
        bmesh.ops.dissolve_degenerate(bm,dist=1e-7,edges=[e for e in bm.edges if e.link_faces and any(v in patch for v in e.verts)])
        # Edge stitching can blend custom deform data during welding. Restore
        # every surviving original vertex's actual weights exactly.
        for vertex,old in original_weights.items():
            if not vertex.is_valid:continue
            for key in list(vertex[deform].keys()):del vertex[deform][key]
            for key,value in old.items():vertex[deform][key]=value
        bmesh.ops.delete(bm,geom=[e for e in bm.edges if not e.link_faces],context='EDGES');bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS');bm.to_mesh(data);bm.free();data.update();data.calc_loop_triangles()
        if len(data.loop_triangles)>25000:raise RuntimeError('Original near budget exceeded')
        result[obj.name]=dict(vertices=len(data.vertices),triangles=len(data.loop_triangles),lift=0,halfWidth=half_width,segments=count,subdividedOriginalSkinFaces=len(affected),stitchedJunctions=stitched)
    bpy.context.scene['bluefinGillRecipe']=RECIPE
    return result
def update():
    source=ROOT/'art/source/bluefin/Bluefin.blend';bpy.ops.wm.open_mainfile(filepath=str(source));scene=bpy.context.scene
    if scene.get('bluefinGillRecipe')==RECIPE:print('BLUEFIN_GILL_ALREADY_PRESENT');return
    bpy.context.preferences.filepaths.save_version=0;lods=[bpy.data.objects['Bluefin_LOD'+str(i)] for i in range(3)];result=replace_rims(lods);rig=bpy.data.objects['BluefinRig']
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
    for obj in lods:obj.hide_set(False);obj.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=str(ROOT/'game/Assets/OceanAngler/Art/Bluefin/Bluefin.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_triangles=True,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=.5,path_mode='RELATIVE')
    for obj in lods:obj.hide_set(obj.name!='Bluefin_LOD0')
    bpy.ops.wm.save_as_mainfile(filepath=str(source));path=ROOT/'art/source/bluefin/build-report.json';report=json.loads(path.read_text());report['gillRecipe']=RECIPE;report['gillUpdate']=result
    for name,data in result.items():report['lods'][name].update(vertices=data['vertices'],triangles=data['triangles'])
    path.write_text(json.dumps(report,indent=2)+'\n');print('BLUEFIN_GILL_SAVED '+json.dumps(result))
if __name__=='__main__':update()
