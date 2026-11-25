"""Update only the saved original eye surfaces/materials; keep other authored content."""
import bpy,bmesh,json,hashlib,sys
from pathlib import Path
import numpy as np
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools'))
from bluefin_eye_recipe import CORNEAL_RADIUS,IRIS_ROUGHNESS,GILL_COLOR,RECIPE,surface_height
def components(mesh,material):
    graph={}
    for face in mesh.polygons:
        if mesh.materials[face.material_index].name!=material:continue
        for index in face.vertices:graph.setdefault(index,set()).update(face.vertices)
    result=[]
    while graph:
        pending=[next(iter(graph))];found=set()
        while pending:
            index=pending.pop()
            if index in found:continue
            found.add(index);pending.extend(graph.pop(index,set())-found)
        result.append(found)
    return result
def update():
    source=ROOT/'art/source/bluefin/Bluefin.blend';bpy.ops.wm.open_mainfile(filepath=str(source));scene=bpy.context.scene
    if scene.get('bluefinHeadSurfaceRecipe')==RECIPE:print('BLUEFIN_CONTINUOUS_EYE_ALREADY_PRESENT');return
    bpy.context.preferences.filepaths.save_version=0
    rig=bpy.data.objects['BluefinRig'];lods=[bpy.data.objects['Bluefin_LOD'+str(i)] for i in range(3)];frames={}
    for ids in components(lods[0].data,'Bluefin_Iris'):
        points=np.array([lods[0].data.vertices[i].co[:] for i in ids]);center=points.mean(axis=0);values,vectors=np.linalg.eigh((points-center).T@(points-center));axis=vectors[:,0];sign=1 if center[1]>0 else -1
        if axis[1]*sign<0:axis=-axis
        if abs(center[0]+.825)>.001 or abs(center[2]-.105)>.001 or values[0]>=values[1]*.5:raise RuntimeError('Unexpected existing iris geometry')
        frames[sign]=(center,axis)
    if set(frames)!={-1,1}:raise RuntimeError('Two complete original eyes required')
    changed={}
    for obj in lods:
        mesh=obj.data;pupils=[];old_count=len(mesh.vertices)
        for ids in components(mesh,'Bluefin_Dark'):
            center=np.mean([mesh.vertices[i].co[:] for i in ids],axis=0)
            if -.90<center[0]<-.76 and .04<center[2]<.17:pupils.append(ids)
        if len(pupils)!=2:raise RuntimeError('Exactly two separate old pupil meshes required in '+obj.name)
        moved=0
        for ids in components(mesh,'Bluefin_Iris'):
            sign=1 if np.mean([mesh.vertices[i].co.y for i in ids])>0 else -1;center,axis=frames[sign]
            for i in ids:
                point=np.array(mesh.vertices[i].co[:]);delta=point-center;depth=float(delta@axis);radial=delta-depth*axis;radius=float(np.linalg.norm(radial))
                # Existing decimated eyes have slightly different footprints.
                # Keep those radial coordinates, rather than clamping them to
                # the ideal LOD0 radius and silently changing their silhouette.
                if radius>=CORNEAL_RADIUS:raise RuntimeError('Iris exceeds spherical surface domain')
                if depth>=-.0000001:mesh.vertices[i].co=Vector(center+radial+axis*surface_height(radius));moved+=1
        bm=bmesh.new();bm.from_mesh(mesh);bm.verts.ensure_lookup_table();remove=set.union(*pupils);bmesh.ops.delete(bm,geom=[bm.verts[i] for i in remove],context='VERTS');bm.to_mesh(mesh);bm.free();mesh.update();mesh.calc_loop_triangles()
        changed[obj.name]=dict(oldVertices=old_count,removedPupilVertices=len(remove),curvedIrisVertices=moved,vertices=len(mesh.vertices),triangles=len(mesh.loop_triangles))
    iris=bpy.data.materials['Bluefin_Iris'];iris.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=IRIS_ROUGHNESS
    gill=bpy.data.materials['Bluefin_Gill'];gill.diffuse_color=(*GILL_COLOR,1);gill.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*GILL_COLOR,1)
    image=bpy.data.images['Bluefin_Iris'];image.source='FILE';image.reload();image.colorspace_settings.name='sRGB'
    scene['bluefinHeadSurfaceRecipe']=RECIPE
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
    for obj in lods:obj.hide_set(False);obj.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=str(ROOT/'game/Assets/OceanAngler/Art/Bluefin/Bluefin.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=.5,path_mode='RELATIVE')
    for obj in lods:obj.hide_set(obj.name!='Bluefin_LOD0')
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    report=ROOT/'art/source/bluefin/build-report.json';data=json.loads(report.read_text());data['headSurfaceRecipe']=RECIPE;data['headSurfaceUpdate']=changed;data['headSurfaceSource']='tools/art_bluefin_head_surface.py'
    for name,value in changed.items():data['lods'][name].update(triangles=value['triangles'],vertices=value['vertices'])
    path=ROOT/'game/Assets/OceanAngler/Art/Bluefin/Bluefin_Iris.png';data['maps'][path.name]=hashlib.sha256(path.read_bytes()).hexdigest();report.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');print('BLUEFIN_CONTINUOUS_EYE_SAVED '+json.dumps(changed))
if __name__=='__main__':update()
