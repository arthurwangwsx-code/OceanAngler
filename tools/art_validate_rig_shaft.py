"""通过真实轴线截面检查转子与往复线杯之间的可见主轴。"""
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

def inspect():
    checks=[];failures=[];samples=[]
    def check(ok,text):(checks if ok else failures).append(text)
    center=bpy.data.objects['SpoolPivot'].matrix_world.translation;axis=(bpy.data.objects['RotorAxis'].matrix_world.translation-bpy.data.objects['RotorPivot'].matrix_world.translation).normalized()
    radial=axis.cross(Vector((0,1,0))).normalized();origin=center-axis*.2
    def tree(name,material=None):
        obj=bpy.data.objects[name];mesh=obj.data;mesh.calc_loop_triangles()
        faces=[tuple(t.vertices) for t in mesh.loop_triangles if material is None or mesh.materials[mesh.polygons[t.polygon_index].material_index].name==material]
        return BVHTree.FromPolygons([obj.matrix_world@v.co for v in mesh.vertices],faces,all_triangles=True)
    for lod in range(3):
        rotor=tree('Rotor_LOD'+str(lod));spool=tree('Spool_LOD'+str(lod),'Rig_Metal');housing=tree('Housing_LOD'+str(lod),'Rig_Metal')
        entry=rotor.ray_cast(origin,axis,.3)[0];exit=rotor.ray_cast(entry+axis*.000001,axis,.3)[0] if entry is not None else None;front=spool.ray_cast(origin,axis,.3)[0]
        check(exit is not None and front is not None,'真实转子与线杯轴向界面 '+str(lod))
        if exit is None or front is None:continue
        gaps=[];radii=[];missing=0
        for stroke in [-.006,0,.006]:
            end=front+axis*stroke;gap=(end-exit).dot(axis);gaps.append(gap)
            if gap<=0:missing+=8;continue
            for i in range(8):
                p=exit+(end-exit)*((i+.5)/8);hit=housing.ray_cast(p,radial,.02)[0]
                if hit is None:missing+=1
                else:radii.append((hit-p).length)
        check(missing==0 and len(radii)==24 and all(.002<r<.008 for r in radii),'往复全跨度有真实金属主轴 '+str(lod))
        samples.append(dict(lod=lod,visibleGapsAtStroke=gaps,shaftRadii=radii,missingSamples=missing))
    return dict(status='FAIL' if failures else 'ENGINEERING_CHECKS_PASS',scope='actual source rotor/spool axial faces and72radial metal-shaft samples across authored+/-6mm spool stroke; not internal gears, CAD, manufacturing or physical-device acceptance',checks=checks,failures=failures,samples=samples)

if __name__=='__main__':
    import json
    result=inspect();print(json.dumps(result,ensure_ascii=False,indent=2));raise SystemExit(bool(result['failures']))
