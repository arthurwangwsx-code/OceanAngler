"""Independent saved-mesh/texture inspection for the first hero fin candidate."""
import bpy,json,math
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parents[1]
def inspect():
    checks=[];failures=[];counts={};profiles=[]
    def check(ok,label):(checks if ok else failures).append(label)
    report=json.loads((ROOT/'art/source/bluefin/build-report.json').read_text())
    samples=report.get('finTissueSamples',[])
    check(len(samples)==24 and len({s['fin'] for s in samples})==8,'八片鳍各三处独立壁厚取样入口')
    membrane=bpy.data.materials.get('Bluefin_Fins');rays=bpy.data.materials.get('Bluefin_Rays')
    check(membrane is not None and rays is not None,'鳍膜与实体鳍条材质分离')
    if membrane and rays:
        alpha=membrane.node_tree.nodes.get('Principled BSDF').inputs['Alpha'];solid=rays.node_tree.nodes.get('Principled BSDF').inputs['Alpha']
        check(alpha.is_linked,'膜的实际透明度来自纹理而非固定不透明常量')
        check(not solid.is_linked and solid.default_value==1,'实体鳍条仍完全不透明')
    path=ROOT/'game/Assets/OceanAngler/Art/Bluefin/Bluefin_Fin.png'
    check(path.read_bytes()[25]==6,'实际鳍膜PNG保留RGBA而非RGB')
    image=bpy.data.images.get('Bluefin_Fin');alpha_stats={}
    if image:
        pixels=np.asarray(image.pixels[:],dtype=np.float32).reshape(image.size[1],image.size[0],4);a=pixels[:,:,3]
        alpha_stats=dict(minimum=float(a.min()),maximum=float(a.max()),translucentFraction=float(np.mean(a<.85)),denseFraction=float(np.mean(a>.9)))
        check(np.isfinite(a).all() and .4<float(a.min())<.8 and .9<float(a.max())<=1,'膜色素透明度有限且保留浓淡')
        check(alpha_stats['translucentFraction']>.15 and alpha_stats['denseFraction']>.02,'膜中真实透光区域与较密实根部均存在')
    for name in ['Bluefin_Fin_Normal','Bluefin_Fin_Roughness','Bluefin_Fin_MetallicSmoothness']:
        im=bpy.data.images.get(name);check(im is not None and tuple(im.size)==(1024,512),'实际PBR尺寸 '+name)
        if im:check(im.colorspace_settings.name=='Non-Color','技术图线性色彩空间 '+name)
    for lod in range(3):
        obj=bpy.data.objects.get('Bluefin_LOD'+str(lod))
        if not obj:check(False,'实际LOD存在 '+str(lod));continue
        membrane_faces=[tuple(f.vertices) for f in obj.data.polygons if obj.data.materials[f.material_index].name=='Bluefin_Fins']
        ray_faces=[f for f in obj.data.polygons if obj.data.materials[f.material_index].name=='Bluefin_Rays']
        counts[str(lod)]=dict(membraneFaces=len(membrane_faces),opaqueRayFaces=len(ray_faces))
        check(bool(membrane_faces) and bool(ray_faces),'三LOD均保留透明膜与实体鳍条 '+str(lod))
        if not membrane_faces or not samples:continue
        tree=BVHTree.FromPolygons([v.co for v in obj.data.vertices],membrane_faces)
        for sample in samples:
            n=Vector(sample['normal']).normalized();p=Vector(sample['point']);hit,_,_,_=tree.ray_cast(p-n*.01,n,.02);thickness=None
            if hit is not None:
                far,_,_,_=tree.ray_cast(hit+n*.000002,n,.02)
                if far is not None:thickness=(far-hit).length
            profiles.append(dict(lod=lod,fin=sample['fin'],t=sample['t'],thickness=thickness))
            check(thickness is not None and .00015<thickness<.0025,'实际双壁厚度 '+str((lod,sample['fin'],sample['t'])))
        for fin in {s['fin'] for s in samples}:
            values=sorted((s['t'],s['thickness']) for s in profiles if s['lod']==lod and s['fin']==fin)
            check(all(v is not None for _,v in values) and values[0][1]>values[-1][1]*1.2,'保存网格根部至末端渐薄 '+str((lod,fin)))
    return dict(status='FAIL' if failures else 'ENGINEERING_CHECKS_PASS',scope='saved source mesh wall rays, material roles and real texture data; not art or device acceptance',checks=checks,failures=failures,alpha=alpha_stats,faces=counts,wallProfiles=profiles)

if __name__=='__main__':
    result=inspect();print(json.dumps(result));raise SystemExit(bool(result['failures']))
