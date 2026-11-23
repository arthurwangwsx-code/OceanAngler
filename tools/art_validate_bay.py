"""独立读取保存的海湾源文件，检查实际几何、LOD、比例和贴图依赖。"""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
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
for name,co in [('OriginAnchor',(0,0,0)),('NorthAnchor',(0,1,0)),('UpAnchor',(0,0,1))]:
 check((bpy.data.objects[name].matrix_world.translation-Vector(co)).length<.0001,'坐标标尺 '+name)
terrain=bpy.data.objects['Terrain_LOD0'];heights=[(terrain.matrix_world@v.co).z for v in terrain.data.vertices]
check(max(heights)-min(heights)>6,'海床到岸边存在真实高差')
water=bpy.data.objects['Water_LOD0'];check(all(abs((water.matrix_world@v.co).z-4.5)<.001 for v in water.data.vertices),'水面标高4.5米')
check(bpy.data.objects['UnderwaterCamera'].location.z<4.5<bpy.data.objects['SurfaceCamera'].location.z,'水上水下镜头分层')
camera=bpy.context.scene.camera
for name in ['StudioWaterVolume','StudioInfiniteWater']:
 obj=bpy.data.objects[name];extent=max((obj.matrix_world@Vector(c)-camera.matrix_world.translation).length for c in obj.bound_box)
 check(camera.data.clip_end>extent,'摄影远裁剪覆盖实际水体 '+name)
check(sum(v for k,v in triangles.items() if k.endswith('LOD0'))<500000,'首景候选LOD0总面数预算')
for image in bpy.data.images:
 if image.source=='FILE':check(Path(bpy.path.abspath(image.filepath)).is_file(),'原始贴图可解析 '+image.name)
report=dict(scope='saved-coral-bay-engineering',status='FAIL' if failures else 'ENGINEERING_CHECKS_PASS',checks=checks,failures=failures,triangles=triangles,visualAcceptance='NOT_ACCEPTED',scientificReview='PENDING',deviceAcceptance='NOT_RUN')
out=ROOT/'.local/art/coral-bay/validation.json';out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(report,ensure_ascii=False))
if failures:raise RuntimeError('Coral Bay source checks failed')
