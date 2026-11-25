"""Inspect actual saved meshes, anatomy and sampled skeletal deformation."""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1];checks=[];failures=[]
def check(ok,label):(checks if ok else failures).append(label)
rig=bpy.data.objects.get('HalibutRig');check(rig is not None,'Editable skeleton exists');report={'scope':'saved-Blender engineering only; not science or visual acceptance','status':'FAIL','checks':checks,'failures':failures,'lods':{},'sampledPoses':[]}
if rig:
    for i in range(3):
        obj=bpy.data.objects.get('Halibut_LOD'+str(i));check(obj is not None,'LOD'+str(i)+' exists')
        if obj is None:continue
        data=obj.data;data.calc_loop_triangles();count=len(data.loop_triangles);report['lods'][obj.name]=count
        check(8000<=count<=25000 if i==0 else count<report['lods']['Halibut_LOD'+str(i-1)],'LOD'+str(i)+' triangle budget')
        check(all(abs(sum(g.weight for g in v.groups)-1)<.002 for v in data.vertices),'LOD'+str(i)+' all normalized nonempty skin weights')
        check(all(math.isfinite(n) for v in data.vertices for n in v.co),'LOD'+str(i)+' finite vertices')
        check(all(all(math.isfinite(n) and -.0001<=n<=1.0001 for n in loop.uv) for uv in data.uv_layers for loop in uv.data),'LOD'+str(i)+' finite bounded UVs')
        check(all(p.area>1e-12 for p in data.polygons),'LOD'+str(i)+' nondegenerate polygons')
        check(any(m.type=='ARMATURE' and m.object==rig for m in obj.modifiers),'LOD'+str(i)+' shares actual rig')
        check(obj.hide_render==(i!=0),'LOD'+str(i)+' studio render visibility avoids overlapping distance meshes')
        # Imported eye material defines both actual iris surface islands.
        iris={j for j,m in enumerate(data.materials) if m.name=='Halibut_Iris'};eye_vertices={v for p in data.polygons if p.material_index in iris for v in p.vertices}
        check(len(eye_vertices)>10,'LOD'+str(i)+' retains eye surfaces')
        check(all(data.vertices[v].co.z>.02 for v in eye_vertices),'LOD'+str(i)+' both eyes on upper/eyed side')
        check(any(data.vertices[v].co.x<-.75 for v in eye_vertices) and any(data.vertices[v].co.x>-.72 for v in eye_vertices),'LOD'+str(i)+' retains two distinct eye locations')
        if i==0:
            skin={j for j,m in enumerate(data.materials) if m.name=='Halibut_Skin'};hull=[data.vertices[v].co for p in data.polygons if p.material_index in skin for v in p.vertices];width=max(v.y for v in hull)-min(v.y for v in hull);thickness=max(v.z for v in hull)-min(v.z for v in hull)
            check(width>.85 and thickness/width<.22,'flattened horizontal body rather than round fish');report['bodyWidth']=width;report['bodyThickness']=thickness
            tail=[v.co for v in data.vertices if v.co.x>1.18];center=max(v.x for v in tail if abs(v.y)<.016);outer=max(v.x for v in tail if abs(v.y)>.25);check(outer-center>.13,'actual concave tail edge');report['tailConcavity']=outer-center
    names={'idle','swim','turn','burst','bite','hooked','release'};check(names<=set(a.name for a in bpy.data.actions),'seven real semantic actions')
    obj=bpy.data.objects['Halibut_LOD0'];obj.hide_set(False);dep=bpy.context.evaluated_depsgraph_get();data=obj.data
    gum_slots={i for i,m in enumerate(data.materials) if m.name=='Halibut_Gum'};tooth_slots={i for i,m in enumerate(data.materials) if m.name=='Halibut_Teeth'}
    gum_faces=[list(p.vertices) for p in data.polygons if p.material_index in gum_slots];check(len(gum_faces)>50,'real continuous gum bands exist')
    tooth_vertices={v for p in data.polygons if p.material_index in tooth_slots for v in p.vertices};adj={v:set() for v in tooth_vertices}
    for p in data.polygons:
        if p.material_index not in tooth_slots:continue
        for v in p.vertices:adj[v].update(set(p.vertices)-{v})
    groups=[];unseen=set(tooth_vertices)
    while unseen:
        pending=[unseen.pop()];group=set(pending)
        while pending:
            for v in adj[pending.pop()]&unseen:unseen.remove(v);group.add(v);pending.append(v)
        groups.append(group)
    roots=[]
    for group in groups:
        tip=max(group,key=lambda i:data.vertices[i].co.x);roots.append(sorted(group-{tip}))
    check(len(roots)==12 and all(len(g)==5 for g in roots),'twelve actual tooth bases identified')
    bm=bmesh.new();bm.from_mesh(data);bad_edges=[e for e in bm.edges if max(v.co.x for v in e.verts)<-.86 and len(e.link_faces)!=2];check(not bad_edges,'outer lip, gum and throat have no open mouth-region edges');report['mouthOpenEdges']=len(bad_edges);bm.free()
    report['toothRootDistanceByPose']=[]
    for name in sorted(names):
        action=bpy.data.actions.get(name)
        if action is None:continue
        rig.animation_data.action=action
        for frame in (1,16,31,46,61):
            bpy.context.scene.frame_set(frame);dep.update();evaluated=obj.evaluated_get(dep);mesh=evaluated.to_mesh()
            try:
                coords=[v.co for v in mesh.vertices];check(all(math.isfinite(c) for v in coords for c in v),name+' frame '+str(frame)+' finite deformed geometry');check(max(v.z for v in coords)-min(v.z for v in coords)<.85,name+' frame '+str(frame)+' no exploded pose');report['sampledPoses'].append({'action':name,'frame':frame,'height':max(v.z for v in coords)-min(v.z for v in coords)})
                if gum_faces:
                    tree=BVHTree.FromPolygons(coords,gum_faces,all_triangles=False);distances=[tree.find_nearest(sum((coords[i] for i in group),Vector())/len(group))[3] for group in roots];maximum=max(distances)
                    check(maximum<.001,name+' frame '+str(frame)+' actual tooth roots stay in gum within 1mm');report['toothRootDistanceByPose'].append({'action':name,'frame':frame,'maximum':maximum})
            finally:evaluated.to_mesh_clear()
        bag=action.layers[0].strips[0].channelbag(rig.animation_data.action_slot)
        check(all(abs(fc.evaluate(1)-fc.evaluate(61))<.0001 for fc in bag.fcurves),name+' closed endpoints')
        check(all(abs(fc.keyframe_points[0].handle_right.y-fc.keyframe_points[0].co.y-(fc.keyframe_points[-1].handle_right.y-fc.keyframe_points[-1].co.y))<.0001 for fc in bag.fcurves),name+' periodic endpoint tangents')
    jaw=rig.pose.bones['jaw'];rig.animation_data.action=bpy.data.actions['bite'];bpy.context.scene.frame_set(16);check(abs(jaw.rotation_euler.z)>.35,'bite opens actual jaw');report['biteJawRadians']=float(jaw.rotation_euler.z)
report['status']='FAIL' if failures else 'ENGINEERING_CHECKS_PASS';out=ROOT/'.local/art/halibut';out.mkdir(parents=True,exist_ok=True);(out/'validation.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'status':report['status'],'checks':len(checks),'failures':failures}));
if failures:raise RuntimeError('Halibut engineering checks failed')
