"""Independently inspect saved slope meshes and dependencies; never repair."""
import bpy,bmesh,json,math
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1];manifest=json.loads((ROOT/'art/source/continental-slope/build-report.json').read_text());checks=[];failures=[];triangles={};solids={};profiles=[];weights=[]
def check(ok,label):(checks if ok else failures).append(label)
for name in manifest['lods']:
 obj=bpy.data.objects.get(name);check(obj is not None,'saved mesh '+name)
 if obj is None:continue
 obj.data.calc_loop_triangles();triangles[name]=len(obj.data.loop_triangles);check(triangles[name]>0 and len(obj.data.uv_layers)>0,'nonempty mesh and UV '+name);check(all(math.isfinite(c) for v in obj.data.vertices for c in v.co),'finite coordinates '+name);check(all(math.isfinite(c) for uv in obj.data.uv_layers for item in uv.data for c in item.uv),'finite UV '+name)
for group in ['Terrain','Scree','ROV','Boat','Survey']:
 check(triangles.get(group+'_LOD0',0)>triangles.get(group+'_LOD1',0)>triangles.get(group+'_LOD2',0)>0,'strict LOD reduction '+group)
for group in ['Terrain','Scree','ROV','Survey']:
 for level in range(3):
  name=group+'_LOD'+str(level);obj=bpy.data.objects[name];bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7);bmesh.ops.triangulate(bm,faces=list(bm.faces));remaining=set(bm.faces);volumes=[]
  while remaining:
   seed=remaining.pop();stack=[seed];component=[seed]
   while stack:
    for edge in stack.pop().edges:
     for neighbor in edge.link_faces:
      if neighbor in remaining:remaining.remove(neighbor);stack.append(neighbor);component.append(neighbor)
   points=np.asarray([[tuple(v.co) for v in f.verts] for f in component],dtype=np.float64);points-=points[0,0].copy();volumes.append(float(np.einsum('ij,ij->i',points[:,0],np.cross(points[:,1],points[:,2])).sum()/6))
  bad=sum(not e.is_manifold for e in bm.edges);samples=[[list(v.co) for v in e.verts] for e in bm.edges if not e.is_manifold][:8];solids[name]=dict(components=len(volumes),nonManifoldEdges=bad,edgeSamples=samples,minSignedVolume=min(volumes,default=0),inward=sum(v<=1e-9 for v in volumes));check(bad==0,'closed manifold '+name);check(all(v>1e-9 for v in volumes),'outward positive solids '+name)
  expected=manifest['solidCounts'].get(group)
  if expected is not None:check(len(volumes)==expected,'all authored components retained '+name)
  bm.free()
sea=manifest['seaLevel'];check(sea==200,'declared deep water column');water=bpy.data.objects['Water_LOD0'];check(all(abs(v.co.z-sea)<1e-4 for v in water.data.vertices),'saved water at declared level')
for name,point in [('OriginAnchor',(0,0,0)),('NorthAnchor',(0,1,0)),('UpAnchor',(0,0,1))]:check((bpy.data.objects[name].matrix_world.translation-Vector(point)).length<1e-5,'metre coordinate anchor '+name)
check(bpy.data.objects['UnderwaterCamera'].location.z<sea<bpy.data.objects['SurfaceCamera'].location.z,'separate deep and surface cameras');check(not any(o.name.lower().startswith('kelp') for o in bpy.data.objects),'no reused shallow kelp')
for lod in range(3):
 obj=bpy.data.objects['Terrain_LOD'+str(lod)];tree=BVHTree.FromObject(obj,bpy.context.evaluated_depsgraph_get());inverse=obj.matrix_world.inverted()
 def bed(x,y):
  a=inverse@Vector((x,y,80));b=inverse@Vector((x,y,-100));ray=b-a;hit,_,_,_=tree.ray_cast(a,ray.normalized(),ray.length);return None if hit is None else float((obj.matrix_world@hit).z)
 for x,y in [(0,-3),(0,10),(0,30),(-5,10),(5,10),(-5,26),(5,26)]:
  h=bed(x,y);profiles.append(dict(lod=lod,kind='fishing-corridor',point=[x,y],height=h));check(h is not None and -20<h<-4.5,'actual saved open corridor '+str((lod,x,y)))
 rov=bpy.data.objects['ROV_LOD'+str(lod)];bottom=min((rov.matrix_world@v.co).z for v in rov.data.vertices)
 for dx,dy in [(0,0),(-1.3,-.65),(1.3,-.65),(-1.3,.65),(1.3,.65)]:
  x,y=manifest['rovGround'][0]+dx,manifest['rovGround'][1]+dy;h=bed(x,y);gap=None if h is None else bottom-h;profiles.append(dict(lod=lod,kind='rov-hover-clearance',point=[x,y],height=h,minimumVehicleHeight=bottom,gap=gap));check(gap is not None and .05<gap<.9,'survey hover over actual bench '+str((lod,dx,dy)))
 layer=obj.data.color_attributes.get('TerrainBlend');check(layer is not None and layer.domain=='POINT','vertex sediment mask '+str(lod))
 if layer is not None:
  values=[float(v.color[0]) for v in layer.data];stats=dict(lod=lod,count=len(values),minimum=min(values),maximum=max(values),intermediate=sum(.01<v<.99 for v in values));weights.append(stats);check(len(values)==len(obj.data.vertices) and all(math.isfinite(v) and 0<=v<=1 for v in values),'finite per-vertex weights '+str(lod));check(stats['minimum']<.01 and stats['maximum']>.5 and stats['intermediate']>100,'continuous rock/sediment blend '+str(lod))
# Inspect the saved view ray against every terrain LOD; a valid camera marker
# alone cannot prove that the survey vehicle is visible.
for lod in range(3):
 obj=bpy.data.objects['Terrain_LOD'+str(lod)];tree=BVHTree.FromObject(obj,bpy.context.evaluated_depsgraph_get());inverse=obj.matrix_world.inverted();start=inverse@bpy.data.objects['ReefCamera'].matrix_world.translation;end=inverse@bpy.data.objects['ReefTarget'].matrix_world.translation;ray=end-start;hit,_,_,distance=tree.ray_cast(start,ray.normalized(),ray.length);check(hit is None,'actual terrain does not occlude ROV inspection view '+str(lod))
for im in bpy.data.images:
 if im.source=='FILE':check(Path(bpy.path.abspath(im.filepath)).is_file(),'image dependency '+im.name)
check(sum(v for k,v in triangles.items() if k.endswith('_LOD0'))<100000,'environment LOD0 budget below100k')
profile=json.loads((ROOT/'art/source/continental-slope/boat-profile.json').read_text());check(profile['origin']==[-5,8,200] and len(profile['stations'])==7,'saved hull profile at survey sea level')
report=dict(status='FAIL' if failures else 'ENGINEERING_CHECKS_PASS',scope='saved source topology, source dependencies, real mesh ray profiles; no rendered/child/device acceptance',checks=checks,failures=failures,triangles=triangles,solids=solids,profiles=profiles,weights=weights,artAcceptance='NOT_ACCEPTED',deviceAcceptance='NOT_RUN');out=ROOT/'.local/art/continental-slope/validation.json';out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(dict(status=report['status'],failures=failures,solids=solids)))
if failures:raise RuntimeError('Saved slope validation failed')
