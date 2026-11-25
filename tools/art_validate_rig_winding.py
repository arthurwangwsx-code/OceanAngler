"""检查保存网格中的绕线间隙与实际线圈，不读取作者采样参数。"""
import bpy,math
from mathutils import Vector
from mathutils.bvhtree import BVHTree

def cross(a,b):return a[0]*b[1]-a[1]*b[0]
def radial_triangle_minimum(points):
    a,b,c=[Vector((p.x,p.y)) for p in points]
    area=cross(b-a,c-a);signs=[cross(b-a,-a),cross(c-b,-b),cross(a-c,-c)]
    if abs(area)>1e-16 and (min(signs)>=0 or max(signs)<=0):return 0.
    distances=[]
    for u,v in [(a,b),(b,c),(c,a)]:
        edge=v-u;t=max(0,min(1,-u.dot(edge)/edge.length_squared)) if edge.length_squared>1e-24 else 0
        distances.append((u+edge*t).length)
    return min(distances)

def clip_z(points,z,keep_above):
    output=[]
    for a,b in zip(points,points[1:]+points[:1]):
        ina=a.z>=z if keep_above else a.z<=z;inb=b.z>=z if keep_above else b.z<=z
        if ina:output.append(a)
        if ina!=inb:output.append(a+(b-a)*((z-a.z)/(b.z-a.z)))
    return output

def inspect():
    checks=[];failures=[];samples=[];budgets=[43420,19533,8678];triangles=[]
    def check(ok,text):(checks if ok else failures).append(text)
    axis=bpy.data.objects['SpoolPivot'].matrix_world.inverted()
    stems={'Rod','Housing','Rotor','Spool','Crank','Lure','BailWire','LineRoller'}
    for lod in range(3):
        total=0
        for stem in stems:
            obj=bpy.data.objects[stem+'_LOD'+str(lod)];obj.data.calc_loop_triangles();total+=len(obj.data.loop_triangles)
        triangles.append(total);check(total<=budgets[lod],'保留既有三档面预算 '+str(lod))
        obj=bpy.data.objects['Spool_LOD'+str(lod)];mesh=obj.data;points=[axis@(obj.matrix_world@v.co) for v in mesh.vertices]
        line=[];metal=[];counts={}
        for tri in mesh.loop_triangles:
            material=mesh.materials[mesh.polygons[tri.polygon_index].material_index].name;counts[material]=counts.get(material,0)+1
            if material=='Rig_Line':line.append(tuple(tri.vertices))
            elif material=='Rig_Metal':metal.append(tuple(tri.vertices))
        check(bool(line) and bool(metal),'实际线材与线杯网格存在 '+str(lod))
        if not line or not metal:continue
        ids={i for face in line for i in face};zmin=min(points[i].z for i in ids);zmax=max(points[i].z for i in ids)
        inner=min(radial_triangle_minimum([points[i] for i in face]) for face in line)
        outer=max(math.hypot(points[i].x,points[i].y) for i in ids);barrel=0.
        for face in metal:
            clipped=clip_z(clip_z([points[i] for i in face],zmin,True),zmax,False)
            barrel=max([barrel]+[math.hypot(p.x,p.y) for p in clipped])
        clearance=inner-barrel;check(clearance>.00001,'真实绕线三角面与线杯至少10微米诊断间隙 '+str(lod))
        tree=BVHTree.FromPolygons(points,line,all_triangles=True);ridges=[]
        for meridian in range(8):
            angle=meridian*math.tau/8;direction=Vector((math.cos(angle),math.sin(angle),0));previous=False;count=0
            for step in range(1001):
                z=zmin-.001+(zmax-zmin+.002)*step/1000;hit=tree.ray_cast(Vector((0,0,z)),direction,.05)[0] is not None
                if hit and not previous:count+=1
                previous=hit
            ridges.append(count)
        check(all(25<=n<=26 for n in ridges),'八经线实际可辨25圈及端点 '+str(lod))
        samples.append(dict(lod=lod,materialTriangles=counts,minimumLineRadius=inner,maximumLineRadius=outer,maximumClippedBarrelRadius=barrel,minimumRadialClearance=clearance,meridianRidges=ridges))
    return dict(status='FAIL' if failures else 'ENGINEERING_CHECKS_PASS',scope='actual saved triangle radial envelope and8008BVH rays perLOD; conservative diagnostic clearances, not CAD tolerances or human art acceptance',checks=checks,failures=failures,triangles=triangles,budgets=budgets,samples=samples)

if __name__=='__main__':
    import json
    result=inspect();print(json.dumps(result,ensure_ascii=False,indent=2));raise SystemExit(bool(result['failures']))
