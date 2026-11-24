"""Read the saved candidate; never repair its geometry while validating."""
import bpy,bmesh,json,math
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1];manifest=json.loads((ROOT/'art/source/nearshore-channel/build-report.json').read_text());checks=[];failures=[];triangles={};solids={}
def check(ok,label):(checks if ok else failures).append(label)
for name in manifest['lods']:
 obj=bpy.data.objects.get(name);check(obj is not None,'mesh '+name)
 if obj is None:continue
 obj.data.calc_loop_triangles();triangles[name]=len(obj.data.loop_triangles);check(triangles[name]>0 and len(obj.data.uv_layers)>0,'nonempty mesh/UV '+name);check(all(math.isfinite(c) for v in obj.data.vertices for c in v.co),'finite vertices '+name);check(all(math.isfinite(c) for layer in obj.data.uv_layers for item in layer.data for c in item.uv),'finite UV '+name)
for group in ['Seabed','Islands','Outcrops','Kelp','Boat','Mooring']:check(triangles[group+'_LOD0']>triangles[group+'_LOD1']>triangles[group+'_LOD2']>0,'LOD reduction '+group)
for group,expected in manifest['solidCounts'].items():
 for level in range(3):
  name=group+'_LOD'+str(level);bm=bmesh.new();bm.from_mesh(bpy.data.objects[name].data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7);bmesh.ops.triangulate(bm,faces=list(bm.faces));remaining=set(bm.faces);volumes=[];world_frame_volumes=[]
  while remaining:
   seed=remaining.pop();pending=[seed];island=[seed]
   while pending:
    f=pending.pop()
    for edge in f.edges:
     for neighbor in edge.link_faces:
      if neighbor in remaining:remaining.remove(neighbor);pending.append(neighbor);island.append(neighbor)
   world_frame_volumes.append(sum(f.verts[0].co.dot(f.verts[1].co.cross(f.verts[2].co)) for f in island)/6)
   # Thin leaves far from the origin lose their small signed volume when
   # float32 world-coordinate scalar triple products cancel. Rebase each
   # closed connected component and compute the same invariant in float64.
   points=np.asarray([[tuple(v.co) for v in f.verts] for f in island],dtype=np.float64);points-=points[0,0].copy()
   volumes.append(float(np.einsum('ij,ij->i',points[:,0],np.cross(points[:,1],points[:,2])).sum()/6))
  bad=sum(not e.is_manifold for e in bm.edges);solids[name]=dict(components=len(volumes),nonManifoldEdges=bad,minSignedVolume=min(volumes,default=0),inwardComponents=sum(v<=1e-8 for v in volumes),worldFrameFloat32Minimum=min(world_frame_volumes,default=0));check(len(volumes)==expected,'component count '+name);check(bad==0,'closed manifold '+name);check(all(v>1e-8 for v in volumes),'outward positive solids '+name);bm.free()
for name,point in [('OriginAnchor',(0,0,0)),('NorthAnchor',(0,1,0)),('UpAnchor',(0,0,1))]:check((bpy.data.objects[name].matrix_world.translation-Vector(point)).length<1e-5,'coordinate marker '+name)
water=bpy.data.objects['Water_LOD0'];check(all(abs((water.matrix_world@v.co).z-4.5)<1e-4 for v in water.data.vertices),'water elevation');check(bpy.data.objects['UnderwaterCamera'].location.z<4.5<bpy.data.objects['SurfaceCamera'].location.z,'surface/underwater cameras')
# Read mesh ray hits, not the generator floor() function.
profiles=[]
for lod in range(3):
 obj=bpy.data.objects['Seabed_LOD'+str(lod)];tree=BVHTree.FromObject(obj,bpy.context.evaluated_depsgraph_get());inverse=obj.matrix_world.inverted();hits=[]
 for x,y in [(0,0),(0,10),(0,30),(-5,10),(5,10)]:
  a=inverse@Vector((x,y,20));b=inverse@Vector((x,y,-40));ray=b-a;hit,_,_,_=tree.ray_cast(a,ray.normalized(),ray.length);height=None if hit is None else float((obj.matrix_world@hit).z);hits.append(height);check(height is not None and -12<height<-4,'open deep corridor LOD'+str(lod)+' '+str((x,y)))
 profiles.append(dict(lod=lod,heights=hits))
# The islands must extend through the actual saved seabed. Sample both the
# centre and broad shoulders of every island at every LOD; do not infer a
# foundation merely from its name or generator parameters.
island_profiles=[]
for lod in range(3):
 bed=bpy.data.objects['Seabed_LOD'+str(lod)];islands=bpy.data.objects['Islands_LOD'+str(lod)];deps=bpy.context.evaluated_depsgraph_get();bed_tree=BVHTree.FromObject(bed,deps);island_tree=BVHTree.FromObject(islands,deps)
 def height_at(obj,tree,x,y,start,end):
  inverse=obj.matrix_world.inverted();a=inverse@Vector((x,y,start));b=inverse@Vector((x,y,end));ray=b-a;hit,_,_,_=tree.ray_cast(a,ray.normalized(),ray.length)
  return None if hit is None else float((obj.matrix_world@hit).z)
 centres=manifest.get('islandCentres',[]);check(len(centres)==3,'three grounded island profiles LOD'+str(lod))
 for index,(x,y,width,depth,_) in enumerate(centres):
  for dx,dy in [(0,0),(-.4*width,0),(.4*width,0),(0,-.4*depth),(0,.4*depth)]:
   px,py=x+dx,y+dy;floor_height=height_at(bed,bed_tree,px,py,40,-40);top=height_at(islands,island_tree,px,py,80,-40);bottom=height_at(islands,island_tree,px,py,-40,80)
   island_profiles.append(dict(lod=lod,island=index,offset=[dx,dy],seabed=floor_height,top=top,bottom=bottom));check(all(v is not None for v in [floor_height,top,bottom]) and bottom<-18 and bottom<floor_height<top and top>5.5,'island intersects seabed LOD'+str(lod)+' island'+str(index)+' '+str((dx,dy)))
for im in bpy.data.images:
 if im.source=='FILE':check(Path(bpy.path.abspath(im.filepath)).is_file(),'image dependency '+im.name)
check(sum(n for k,n in triangles.items() if k.endswith('_LOD0'))<180000,'candidate geometry budget')
report=dict(status='FAIL' if failures else 'ENGINEERING_CHECKS_PASS',scope='saved nearshore-channel source geometry and dependencies',checks=checks,failures=failures,triangles=triangles,solids=solids,corridorProfiles=profiles,islandProfiles=island_profiles,artAcceptance='NOT_ACCEPTED',deviceAcceptance='NOT_RUN')
p=ROOT/'.local/art/nearshore-channel/validation.json';p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(dict(status=report['status'],failures=failures,solids=solids)))
if failures:raise RuntimeError('Saved channel validation failed')
