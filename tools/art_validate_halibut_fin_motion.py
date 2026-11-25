"""Independent saved-mesh identity and body-held fin-motion ablation checks.

Reference capture does not certify motion. Validation samples evaluated vertices,
then zeros only median-fin pose rotations while retaining the same body pose.
"""
import argparse,bpy,cmath,hashlib,json,math,statistics,sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

p=argparse.ArgumentParser();p.add_argument('--record-reference',type=Path);p.add_argument('--reference',type=Path);p.add_argument('--output',type=Path,required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);assert bool(a.record_reference)!=bool(a.reference);a.output.mkdir(parents=True,exist_ok=False)
rig=bpy.data.objects['HalibutRig'];lods=[bpy.data.objects['Halibut_LOD'+str(i)] for i in range(3)];checks=[];failures=[]
def check(ok,name):(checks if ok else failures).append(name)
def digest(value):return hashlib.sha256(json.dumps(value,separators=(',',':'),sort_keys=True).encode()).hexdigest()
def is_fin(name):return name.startswith(('dorsal_','anal_'))
def posterior_skin(obj):
    m=obj.data
    return {i for poly in m.polygons if m.materials[poly.material_index].name=='Halibut_Skin' for i in poly.vertices if m.vertices[i].co.x>-.55}
def regions(obj):
    mesh=obj.data;adj={i:set() for poly in mesh.polygons if mesh.materials[poly.material_index].name=='Halibut_Fins' for i in poly.vertices}
    for poly in mesh.polygons:
        if mesh.materials[poly.material_index].name!='Halibut_Fins':continue
        for i in poly.vertices:adj[i]|=set(poly.vertices)-{i}
    todo=set(adj);result={}
    while todo:
        start=todo.pop();group={start};stack=[start]
        while stack:
            neighbors=adj[stack.pop()]&todo;todo-=neighbors;group|=neighbors;stack.extend(neighbors)
        points=[mesh.vertices[i].co for i in group]
        if max(v.x for v in points)-min(v.x for v in points)>1.3:result['dorsal' if statistics.mean(v.y for v in points)<0 else 'anal']=sorted(group)
    return result
def radial_values(obj,ids):
    m=obj.data;uvs={i:[] for i in ids}
    for poly in m.polygons:
        if m.materials[poly.material_index].name!='Halibut_Fins':continue
        for li in poly.loop_indices:
            vi=m.loops[li].vertex_index
            if vi in uvs:
                v=m.uv_layers.active.data[li].uv.y;uvs[vi].append((v-(.54 if v>.5 else .03))/.43)
    return {i:statistics.median(values) for i,values in uvs.items()}
def snapshot():
    r={'meshes':{},'bones':{},'curves':{}}
    for obj in lods:
        m=obj.data;fins=regions(obj);ids=set(sum(fins.values(),[]));m.calc_loop_triangles()
        geometry=dict(vertices=[list(v.co) for v in m.vertices],polygons=[(list(poly.vertices),poly.material_index) for poly in m.polygons],uvs=[[list(loop.uv) for loop in uv.data] for uv in m.uv_layers],materials=[mat.name for mat in m.materials])
        nonfin=[(v.index,sorted((obj.vertex_groups[g.group].name,g.weight) for g in v.groups)) for v in m.vertices if v.index not in ids]
        posterior=posterior_skin(obj);protected=[item for item in nonfin if item[0] not in posterior]
        radial=radial_values(obj,ids);roots=[(v.index,sorted((obj.vertex_groups[g.group].name,g.weight) for g in v.groups)) for v in m.vertices if v.index in ids and radial[v.index]<=.21]
        r['meshes'][obj.name]=dict(geometry=digest(geometry),nonFinWeights=digest(nonfin),protectedWeights=digest(protected),finRootWeights=digest(roots),vertices=len(m.vertices),triangles=len(m.loop_triangles),regions={k:len(v) for k,v in fins.items()})
    for bone in rig.data.bones:
        if not is_fin(bone.name):r['bones'][bone.name]=dict(parent=bone.parent.name if bone.parent else None,matrix=[list(row) for row in bone.matrix_local])
    for action in bpy.data.actions:
        if action.name not in {'idle','swim','turn','burst','bite','hooked','release'}:continue
        rig.animation_data.action=action;bag=action.layers[0].strips[0].channelbag(rig.animation_data.action_slot)
        data=[]
        for curve in bag.fcurves:
            name=curve.data_path.split('"')[1]
            if is_fin(name):continue
            data.append((curve.data_path,curve.array_index,[(list(k.co),list(k.handle_left),list(k.handle_right),k.interpolation,k.handle_left_type,k.handle_right_type) for k in curve.keyframe_points]))
        r['curves'][action.name]=digest(sorted(data))
    return r

current=snapshot()
if a.record_reference:
    assert not a.record_reference.exists();a.record_reference.write_text(json.dumps(current,indent=2)+'\n');r=dict(status='REFERENCE_CAPTURED_NOT_MOTION_ACCEPTANCE',referenceSha256=hashlib.sha256(a.record_reference.read_bytes()).hexdigest(),snapshot=current);(a.output/'validation.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps({'status':r['status']}));raise SystemExit(0)
reference=json.loads(a.reference.read_text());r=dict(status='FAIL',scope='Saved-source identity and actual evaluated median-fin ablation, not science, native rendering or final art acceptance',checks=checks,failures=failures,poses=[],phaseLags=[],referenceSha256=hashlib.sha256(a.reference.read_bytes()).hexdigest())
for name,old in reference['meshes'].items():check({k:v for k,v in current['meshes'][name].items() if k!='nonFinWeights'}=={k:v for k,v in old.items() if k!='nonFinWeights'},name+' exact geometry/UV/material/front anatomy/other fin/root weights and triangle identity')
check(current['bones']==reference['bones'],'All original rest bones and parents unchanged');check(current['curves']==reference['curves'],'All original seven body/jaw action curves unchanged')
fin_bones=[bone for bone in rig.pose.bones if is_fin(bone.name)];check(len(fin_bones)==34 and len(rig.data.bones)==42,'34 independent median-fin bones within 42-bone candidate budget')
for obj in lods:
    check(all(1<=len(v.groups)<=4 and abs(sum(g.weight for g in v.groups)-1)<.00001 for v in obj.data.vertices),obj.name+' normalized one-to-four skin influences')
if fin_bones:
    dep=bpy.context.evaluated_depsgraph_get()
    def vertices(obj):
        dep.update();evaluated=obj.evaluated_get(dep);m=evaluated.to_mesh()
        try:return [v.co.copy() for v in m.vertices]
        finally:evaluated.to_mesh_clear()
    def inside(tree,point):
        direction=Vector((0,1,0));count=0;point=point.copy()
        for _ in range(8):
            hit,_,_,_=tree.ray_cast(point,direction,10)
            if hit is None:break
            count+=1;point=hit+direction*.00001
        return count%2==1
    for obj in lods:
        obj.hide_set(False);fins=regions(obj);all_fin=set(sum(fins.values(),[]));nonfin=set(range(len(obj.data.vertices)))-all_fin;radial=radial_values(obj,all_fin)
        roots={i for i in all_fin if radial[i]<=.21};tips={i for i in all_fin if radial[i]>=.8};series={}
        skin_faces=[list(poly.vertices) for poly in obj.data.polygons if obj.data.materials[poly.material_index].name=='Halibut_Skin'];posterior=posterior_skin(obj)
        root_xy={}
        for i in roots:
            if abs(radial[i])<.001:
                v=obj.data.vertices[i].co;root_xy.setdefault((round(v.x,6),round(v.y,6)),[]).append(i)
        root_pairs=[group for group in root_xy.values() if len(group)==2]
        if obj.name.endswith('0'):check(len(root_pairs)==130,'130 upper/lower median-root attachment centers identified')
        thin_pairs=[]
        if obj.name.endswith('0'):
            xy={}
            for i in all_fin:
                v=obj.data.vertices[i].co;xy.setdefault((round(v.x,6),round(v.y,6)),[]).append(i)
            thin_pairs=[pair for pair in xy.values() if len(pair)==2]
            check(len(thin_pairs)>700,'Actual upper/lower closed fin-shell pairs identified')
        for semantic in ['idle','swim','turn','burst','bite','hooked','release']:
            rig.animation_data.action=bpy.data.actions[semantic];frames=range(1,62,2) if semantic=='swim' else [1,16,31,46,61];peaks=[];root_peak=body_peak=0;thin_min=1;orientation_min=1;outside_max=0;posterior_movement=0;start=None
            for frame in frames:
                bpy.context.scene.frame_set(frame);with_fin=vertices(obj)
                if start is None:start=with_fin
                posterior_movement=max(posterior_movement,max((with_fin[i]-start[i]).length for i in posterior))
                # LOD0 roots are explicit matched shell pairs. Test their real
                # placement against the moving hull, not only control ablation.
                if obj.name.endswith('0'):
                    tree=BVHTree.FromPolygons(with_fin,skin_faces,all_triangles=False)
                    outside_max=max(outside_max,sum(not inside(tree,(with_fin[i]+with_fin[j])/2) for i,j in root_pairs))
                rotations={bone.name:bone.rotation_euler.copy() for bone in fin_bones}
                for bone in fin_bones:bone.rotation_euler=(0,0,0)
                held=vertices(obj)
                for bone in fin_bones:bone.rotation_euler=rotations[bone.name]
                root_peak=max(root_peak,max((with_fin[i]-held[i]).length for i in roots));body_peak=max(body_peak,max((with_fin[i]-held[i]).length for i in nonfin));peak=max((with_fin[i]-held[i]).length for i in tips);peaks.append(peak)
                check(all(math.isfinite(c) for v in with_fin for c in v),obj.name+' '+semantic+' '+str(frame)+' finite geometry')
                for i,j in thin_pairs:
                    before=held[j]-held[i];after=with_fin[j]-with_fin[i]
                    if before.length>1e-6:thin_min=min(thin_min,after.length/before.length);orientation_min=min(orientation_min,before.normalized().dot(after.normalized()))
                if semantic=='swim' and frame<61:
                    for side,ids in fins.items():
                        x0=min(obj.data.vertices[i].co.x for i in ids);span=max(obj.data.vertices[i].co.x for i in ids)-x0
                        for label,location in [('front',.3),('rear',.6)]:
                            roi=[i for i in ids if radial[i]>=.8 and abs((obj.data.vertices[i].co.x-x0)/span-location)<.055]
                            if not roi:raise RuntimeError('Missing actual tip sample')
                            series.setdefault((side,label),[]).append(statistics.mean(with_fin[i].z-held[i].z for i in roi))
            check(root_peak<.00001,obj.name+' '+semantic+' body-held root attachment fixed');check(body_peak<.000001,obj.name+' '+semantic+' independent fin controls leave body/eyes/mouth/other fins fixed');check(max(peaks)>.0015,obj.name+' '+semantic+' real outer-fin movement exceeds 1.5mm')
            check(thin_min>.8 and orientation_min>.8,obj.name+' '+semantic+' sampled thin-shell separation and side orientation retained')
            if obj.name.endswith('0'):check(outside_max==0,semantic+' actual median-root centers stay inside the moving skin hull')
            if semantic in {'swim','burst','hooked'}:check(posterior_movement>.005,obj.name+' '+semantic+' actual posterior hull follows body animation')
            r['poses'].append(dict(lod=obj.name,action=semantic,samples=len(peaks),maxTipDisplacement=max(peaks),rootDisplacement=root_peak,nonFinDisplacement=body_peak,thinSeparationRatio=thin_min,thinOrientationDot=orientation_min,maxRootCentersOutsideHull=outside_max if obj.name.endswith('0') else None,posteriorHullMovement=posterior_movement))
        for side in fins:
            coeff=[]
            for location in ['front','rear']:
                samples=series[(side,location)];coeff.append(sum(value*cmath.exp(-1j*math.tau*2*i/len(samples)) for i,value in enumerate(samples)))
            lag=cmath.phase(coeff[1]/coeff[0]);check(-2.8<lag<-.65,obj.name+' '+side+' observed head-to-tail phase lag, not rigid synchronous flap');r['phaseLags'].append(dict(lod=obj.name,side=side,radians=lag))
    for action in bpy.data.actions:
        if action.name not in reference['curves']:continue
        rig.animation_data.action=action;bag=action.layers[0].strips[0].channelbag(rig.animation_data.action_slot)
        for curve in bag.fcurves:
            if not is_fin(curve.data_path.split('"')[1]):continue
            keys=curve.keyframe_points;check(abs(curve.evaluate(1)-curve.evaluate(61))<.0001 and abs(keys[0].handle_right.y-keys[0].co.y-keys[-1].handle_right.y+keys[-1].co.y)<.0001,action.name+' '+curve.data_path+' periodic value/velocity')
r['status']='FAIL' if failures else 'ENGINEERING_CHECKS_PASS';(a.output/'validation.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps({'status':r['status'],'checks':len(checks),'failures':failures}))
if failures:raise RuntimeError('Median-fin engineering checks failed; retain this result')
