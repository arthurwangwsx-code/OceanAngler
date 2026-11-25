"""Repair the saved mouth roof without regenerating other reviewed surfaces."""
import bpy,bmesh,hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
RECIPE='separate-internal-mouth-roof-v1'
def face_records(obj):
    mesh=obj.data;layers=mesh.uv_layers
    records=[]
    for face in mesh.polygons:
        loops=[]
        for li in face.loop_indices:
            vertex=mesh.vertices[mesh.loops[li].vertex_index]
            loops.append((tuple(vertex.co),tuple(tuple(layer.data[li].uv) for layer in layers),tuple(sorted((obj.vertex_groups[g.group].name,g.weight) for g in vertex.groups))))
        # Vertex indices may change, but geometry, winding, UVs and skin weights
        # on every retained face must remain exactly the same.
        first=min(range(len(loops)),key=lambda i:loops[i]);loops=loops[first:]+loops[:first]
        records.append((mesh.materials[face.material_index].name,face.use_smooth,tuple(loops)))
    return records
def digest(records):
    return hashlib.sha256(json.dumps(sorted(records),separators=(',',':')).encode()).hexdigest()
def refine_mouth(obj):
    before=face_records(obj);mesh=obj.data;bm=bmesh.new();bm.from_mesh(mesh);removed=[]
    try:
        for face in list(bm.faces):
            if mesh.materials[face.material_index].name!='Bluefin_Dark':continue
            points=[v.co for v in face.verts]
            # Only the hidden posterior jaw roof: the true mouth lining,
            # anterior lip, eyes, cheek skin and their coordinates stay intact.
            if min(p.x for p in points)>=-.8602 and max(p.x for p in points)<-.75 and max(abs(p.y) for p in points)>.005 and max(p.z for p in points)<0:
                removed.append(face);bm.faces.remove(face)
        # Detach the remaining roof's material boundary so its internal normals
        # cannot average into the cheek. Split preserves coordinates and all
        # loop/deformation data; it does not make a geometric gap.
        edges=[]
        for edge in bm.edges:
            materials={mesh.materials[f.material_index].name for f in edge.link_faces}
            center=(edge.verts[0].co+edge.verts[1].co)*.5
            if {'Bluefin_Dark','Bluefin_Skin'}<=materials and -.95<center.x<-.75 and center.z<0:
                edges.append(edge)
        if not removed or not edges:raise RuntimeError('Expected existing internal roof; do not apply blindly to another source')
        bmesh.ops.split_edges(bm,edges=edges)
        bm.normal_update();bm.to_mesh(mesh);mesh.update();mesh.calc_loop_triangles()
    finally:bm.free()
    after=face_records(obj)
    from collections import Counter
    old,new=Counter(before),Counter(after);added=list((new-old).elements());deleted=list((old-new).elements())
    if added or len(deleted)!=len(removed) or any(r[0]!='Bluefin_Dark' for r in deleted):raise RuntimeError('Unexpected surface/UV/weight change; preserve evidence')
    if not all(min(p[0][0] for p in r[2])>=-.8602 for r in deleted):raise RuntimeError('Unexpected deletion outside posterior roof')
    return dict(removedHiddenRoofFaces=len(deleted),splitBoundaryEdges=len(edges),retainedFaces=len(after),retainedFaceDigest=digest(after),allRetainedFaceGeometryWindingUvWeightsExact=True,triangles=len(mesh.loop_triangles),vertices=len(mesh.vertices))
def update_existing():
    source=ROOT/'art/source/bluefin/Bluefin.blend';bpy.ops.wm.open_mainfile(filepath=str(source));bpy.context.preferences.filepaths.save_version=0
    if bpy.context.scene.get('bluefinMouthSurfaceRecipe')==RECIPE:raise RuntimeError('Recipe already present; no repeat export')
    rig=bpy.data.objects['BluefinRig'];lods=[bpy.data.objects['Bluefin_LOD'+str(i)] for i in range(3)]
    info={obj.name:refine_mouth(obj) for obj in lods}
    bpy.context.scene['bluefinMouthSurfaceRecipe']=RECIPE
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
    for obj in lods:obj.hide_set(False);obj.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=str(ROOT/'game/Assets/OceanAngler/Art/Bluefin/Bluefin.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_triangles=True,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='RELATIVE')
    for i,obj in enumerate(lods):obj.hide_set(i!=0);obj.hide_render=i!=0
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    path=ROOT/'art/source/bluefin/build-report.json';report=json.loads(path.read_text());report['mouthSurface']={'recipe':RECIPE,'lods':info,'scope':'Hidden jaw roof and normal separation only; not complete mouth anatomy/art acceptance'}
    for obj in lods:report['lods'][obj.name].update(triangles=len(obj.data.loop_triangles),vertices=len(obj.data.vertices))
    path.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(info))
if __name__=='__main__':update_existing()
