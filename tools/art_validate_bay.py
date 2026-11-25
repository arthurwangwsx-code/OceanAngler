"""独立读取保存的海湾源文件，检查实际几何、LOD、比例和贴图依赖。"""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1];failures=[];checks=[];triangles={}
def check(ok,message):(checks if ok else failures).append(message)
manifest=json.loads((ROOT/'art/source/coral-bay/build-report.json').read_text())
for name in manifest['lods']:
 obj=bpy.data.objects.get(name);check(obj is not None,'存在网格 '+name)
 if obj is None:continue
 obj.data.calc_loop_triangles();triangles[name]=len(obj.data.loop_triangles)
 check(len(obj.data.polygons)>0 and len(obj.data.uv_layers)>0,'非空几何与UV '+name)
 check(all(math.isfinite(c) for v in obj.data.vertices for c in v.co),'有限顶点 '+name)
 check(all(math.isfinite(c) for layer in obj.data.uv_layers for loop in layer.data for c in loop.uv),'有限平铺UV '+name)
for group in ['Terrain','Reef','BranchingCoral','PlateCoral','DomeCoral','Seagrass','Pier','Boat']:
 check(all(triangles.get(group+'_LOD'+str(i),0)>triangles.get(group+'_LOD'+str(i+1),0)>0 for i in range(2)),'三档几何递减 '+group)
# Independent geometry check: weld a private analysis copy, then inspect actual
# connected solids and oriented volume. Never repair the candidate while validating.
solid_report={}
for group,expected in [('Reef',24),('DomeCoral',8)]:
 for level in range(3):
  name=group+'_LOD'+str(level);bm=bmesh.new();bm.from_mesh(bpy.data.objects[name].data)
  bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001)
  bmesh.ops.triangulate(bm,faces=list(bm.faces));remaining=set(bm.faces);volumes=[]
  while remaining:
   seed=remaining.pop();pending=[seed];island=[seed]
   while pending:
    face=pending.pop()
    for edge in face.edges:
     for neighbor in edge.link_faces:
      if neighbor in remaining:remaining.remove(neighbor);pending.append(neighbor);island.append(neighbor)
   volumes.append(sum(f.verts[0].co.dot(f.verts[1].co.cross(f.verts[2].co)) for f in island)/6)
  open_edges=sum(not edge.is_manifold for edge in bm.edges)
  solid_report[name]=dict(components=len(volumes),nonManifoldEdges=open_edges,minSignedVolume=min(volumes,default=0),inwardComponents=sum(v<=.000001 for v in volumes))
  check(len(volumes)==expected,'独立实体数量 '+name)
  check(open_edges==0,'实体无开放或非流形边 '+name)
  check(len(volumes)>0 and all(v>.000001 for v in volumes),'逐实体朝外正体积 '+name)
  bm.free()
for name,co in [('OriginAnchor',(0,0,0)),('NorthAnchor',(0,1,0)),('UpAnchor',(0,0,1))]:
 check((bpy.data.objects[name].matrix_world.translation-Vector(co)).length<.0001,'坐标标尺 '+name)
terrain=bpy.data.objects['Terrain_LOD0'];heights=[(terrain.matrix_world@v.co).z for v in terrain.data.vertices]
check(max(heights)-min(heights)>6,'海床到岸边存在真实高差')
# Probe the saved mesh, not the generator's height formula. The western shore
# must lead onto land rather than form a thin ridge with sea on both sides.
shore_profiles=[]
for level in range(3):
 terrain_lod=bpy.data.objects['Terrain_LOD'+str(level)];bvh=BVHTree.FromObject(terrain_lod,bpy.context.evaluated_depsgraph_get());inverse=terrain_lod.matrix_world.inverted()
 for along in [-5,8,20,35]:
  samples=[]
  for across in range(-65,-7):
   start=inverse@Vector((across,along,30));end=inverse@Vector((across,along,-50));ray=end-start
   hit,_,_,_=bvh.ray_cast(start,ray.normalized(),ray.length)
   samples.append(None if hit is None else float((terrain_lod.matrix_world@hit).z))
  crossings=sum((a>=4.5)!=(b>=4.5) for a,b in zip(samples,samples[1:]) if a is not None and b is not None)
  shore_profiles.append(dict(lod=level,along=along,westHeight=samples[0],eastHeight=samples[-1],waterlineCrossings=crossings))
  label='LOD'+str(level)+' / '+str(along)
  check(all(v is not None for v in samples),'岸线剖面连续覆盖 '+label)
  check(samples[0] is not None and samples[0]>4.7 and samples[-1] is not None and samples[-1]<4.0,'西侧陆地连接水下浅滩 '+label)
  check(crossings==1,'岸线剖面仅穿越一次水位 '+label)
water=bpy.data.objects['Water_LOD0'];check(all(abs((water.matrix_world@v.co).z-4.5)<.001 for v in water.data.vertices),'水面标高4.5米')
# Read actual saved leaf roots and the triangulated terrain, not the generator's
# analytic height function. Keep the center clear at leaf tips as well as roots.
grass=bpy.data.objects['Seagrass_LOD0'];layer=grass.data.uv_layers.active.data
roots={loop.vertex_index:grass.matrix_world@grass.data.vertices[loop.vertex_index].co for loop in grass.data.loops if abs(layer[loop.index].uv.y)<.000001}
terrain=bpy.data.objects['Terrain_LOD0'];bvh=BVHTree.FromObject(terrain,bpy.context.evaluated_depsgraph_get());inverse=terrain.matrix_world.inverted();offsets=[]
for root in roots.values():
 start=inverse@Vector((root.x,root.y,20));end=inverse@Vector((root.x,root.y,-40));ray=end-start;hit,_,_,_=bvh.ray_cast(start,ray.normalized(),ray.length)
 if hit is not None:offsets.append(root.z-(terrain.matrix_world@hit).z)
check(len(roots)>0 and len(offsets)==len(roots),'全部草根覆盖实际海床')
check(len(roots)==manifest.get('seagrassBlades',-1)*2,'保存的独立叶根与构建数量一致')
check(bool(offsets) and min(offsets)>-.25 and max(offsets)<.015,'草根埋入海床且无明显悬空')
check(all(abs((grass.matrix_world@v.co).x)>=2 for v in grass.data.vertices),'整片海草叶保留中央四米通道')
check(triangles['Seagrass_LOD0']<=18390,'海草未增加既有近景面数预算')
seagrass_report=dict(rootEdgeVertices=len(roots),bedOffsets=[min(offsets),max(offsets)] if offsets else [],minimumCenterClearance=min(abs((grass.matrix_world@v.co).x) for v in grass.data.vertices))
check(bpy.data.objects['UnderwaterCamera'].location.z<4.5<bpy.data.objects['SurfaceCamera'].location.z,'水上水下镜头分层')
camera=bpy.context.scene.camera
for name in ['StudioWaterVolume','StudioInfiniteWater']:
 obj=bpy.data.objects[name];extent=max((obj.matrix_world@Vector(c)-camera.matrix_world.translation).length for c in obj.bound_box)
 check(camera.data.clip_end>extent,'摄影远裁剪覆盖实际水体 '+name)
check(sum(v for k,v in triangles.items() if k.endswith('LOD0'))<500000,'首景候选LOD0总面数预算')
for image in bpy.data.images:
 if image.source=='FILE':check(Path(bpy.path.abspath(image.filepath)).is_file(),'原始贴图可解析 '+image.name)
report=dict(scope='saved-coral-bay-engineering',status='FAIL' if failures else 'ENGINEERING_CHECKS_PASS',checks=checks,failures=failures,triangles=triangles,solids=solid_report,shoreProfiles=shore_profiles,seagrass=seagrass_report,visualAcceptance='NOT_ACCEPTED',scientificReview='PENDING',deviceAcceptance='NOT_RUN')
out=ROOT/'.local/art/coral-bay/validation.json';out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(report,ensure_ascii=False))
if failures:raise RuntimeError('Coral Bay source checks failed')
