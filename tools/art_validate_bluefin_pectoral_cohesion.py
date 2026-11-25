"""Measure opaque ray centres against the actual deformed membrane surface."""
import argparse,bpy,json,sys
from pathlib import Path
from statistics import median
from mathutils import Vector
from mathutils.bvhtree import BVHTree

p=argparse.ArgumentParser();p.add_argument('--reference',required=True);p.add_argument('--output',required=True);a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);original=json.loads(Path(a.reference).read_text());rig=bpy.data.objects['BluefinRig'];lods=[bpy.data.objects['Bluefin_LOD'+str(i)] for i in range(3)];checks=[];failures=[];results={}
def check(value,label):(checks if value else failures).append(label)
def surface(mesh,ids):
    mesh.calc_loop_triangles();triangles=[tuple(t.vertices) for t in mesh.loop_triangles if mesh.materials[mesh.polygons[t.polygon_index].material_index].name=='Bluefin_Fins' and set(t.vertices)<=ids]
    if not triangles:raise RuntimeError('Actual membrane triangles required')
    return BVHTree.FromPolygons([v.co for v in mesh.vertices],triangles,all_triangles=True)
def rings(mesh,ids):
    adjacency={}
    for f in mesh.polygons:
        if mesh.materials[f.material_index].name!='Bluefin_Rays' or not set(f.vertices)<=ids:continue
        for i in f.vertices:adjacency.setdefault(i,set()).update(f.vertices)
    radial={i:[] for i in adjacency}
    for f in mesh.polygons:
        if mesh.materials[f.material_index].name!='Bluefin_Rays':continue
        for li in f.loop_indices:
            i=mesh.loops[li].vertex_index
            if i in radial:radial[i].append(mesh.uv_layers.active.data[li].uv.y)
    result=[]
    while adjacency:
        todo=[next(iter(adjacency))];component=set()
        while todo:
            index=todo.pop()
            if index in component:continue
            component.add(index);todo.extend(adjacency.pop(index,set())-component)
        groups={}
        for i in component:groups.setdefault(round(median(radial[i]),5),[]).append(i)
        ordered=sorted(groups.items())
        result.extend(dict(radial=t,indices=indices) for t,indices in ordered)
        for (ta,ia),(tb,ib) in zip(ordered,ordered[1:]):
            for u in (.25,.5,.75):result.append(dict(radial=ta+(tb-ta)*u,indices=ia+ib,coefficients=[(1-u)/len(ia)]*len(ia)+[u/len(ib)]*len(ib)))
    return result
prepared={}
for obj in lods:
    prepared[obj.name]={};obj.hide_set(False)
    for side,info in original['meshes'][obj.name]['fin'].items():
        original_ids=set(info['indices']);ids={v.index for v in obj.data.vertices if any(obj.vertex_groups[g.group].name in {side,side+'Mid',side+'Tip'} and g.weight>0 for g in v.groups)};tree=surface(obj.data,ids);groups=rings(obj.data,original_ids)
        for g in groups:
            center=sum((obj.data.vertices[i].co*w for i,w in zip(g['indices'],g.get('coefficients',[1/len(g['indices'])]*len(g['indices'])))),Vector());g['restDistance']=tree.find_nearest(center)[3]
        prepared[obj.name][side]=dict(ids=ids,rings=groups)
graph=bpy.context.evaluated_depsgraph_get()
for semantic in ['idle','swim','turn','burst','bite','hooked','release']:
    rig.animation_data.action=bpy.data.actions[semantic];values={o.name:{side:dict(maximumDistance=0,maximumIncrease=0,ringSamples=0) for side in prepared[o.name]} for o in lods}
    for frame in [1,8,16,23,31,38,46,53]:
        bpy.context.scene.frame_set(frame);graph.update()
        for obj in lods:
            evaluated=obj.evaluated_get(graph);mesh=evaluated.to_mesh()
            try:
                for side,info in prepared[obj.name].items():
                    tree=surface(mesh,info['ids']);result=values[obj.name][side]
                    for ring in info['rings']:
                        center=sum((mesh.vertices[i].co*w for i,w in zip(ring['indices'],ring.get('coefficients',[1/len(ring['indices'])]*len(ring['indices'])))),Vector());distance=tree.find_nearest(center)[3];result['maximumDistance']=max(result['maximumDistance'],distance);result['maximumIncrease']=max(result['maximumIncrease'],distance-ring['restDistance']);result['ringSamples']+=1
            finally:evaluated.to_mesh_clear()
    for obj,sides in values.items():
        for side,result in sides.items():check(result['ringSamples']>0 and result['maximumIncrease']<.0004,semantic+' '+obj+' '+side+' added ray/membrane separation below 0.4 source millimetres')
    results[semantic]=values
result=dict(status='PASS' if not failures else 'FAIL',checks=checks,failures=failures,motions=results,scope='Source deformed opaque-ray ring centres and quarter/half/three-quarter segment samples versus membrane BVH, subtract original rest separation; no raster, Player, scientific or final art acceptance')
out=Path(a.output);out.parent.mkdir(parents=True,exist_ok=True)
if out.exists():raise RuntimeError('Retain earlier evidence')
out.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(dict(status=result['status'],failures=failures,maximumIncrease=max(r['maximumIncrease'] for m in results.values() for d in m.values() for r in d.values()))))
if failures:raise RuntimeError('Ray/membrane cohesion failed')
