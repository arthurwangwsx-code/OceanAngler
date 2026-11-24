"""Read saved rig topology and real roller/line geometry; no repair or author-data pass."""
import bpy,bmesh,math
from collections import defaultdict
from mathutils import Vector
from mathutils.bvhtree import BVHTree

def solid_stats(obj,material=None):
    mesh=obj.data;bm=bmesh.new();bm.from_mesh(mesh)
    if material:
        remove=[f for f in bm.faces if mesh.materials[f.material_index].name!=material]
        bmesh.ops.delete(bm,geom=remove,context='FACES')
    bm.verts.ensure_lookup_table();bm.faces.ensure_lookup_table();remaining=set(bm.faces);volumes=[]
    while remaining:
        face=remaining.pop();island={face};stack=[face]
        while stack:
            for edge in stack.pop().edges:
                for adjacent in edge.link_faces:
                    if adjacent in remaining:remaining.remove(adjacent);island.add(adjacent);stack.append(adjacent)
        # Recenter each small component: absolute rod coordinates around2m
        # lose precision in float-vector tetrahedra of sub-millimetre fittings.
        vertices={v for f in island for v in f.verts};origin=sum((v.co for v in vertices),Vector())/len(vertices);volume=0.
        for face in island:
            vs=[v.co-origin for v in face.verts]
            for i in range(1,len(vs)-1):volume+=vs[0].dot(vs[i].cross(vs[i+1]))/6
        volumes.append(volume)
    result=dict(faces=len(bm.faces),components=len(volumes),boundaryEdges=sum(e.is_boundary for e in bm.edges),nonManifoldEdges=sum(not e.is_manifold for e in bm.edges if e.link_faces),inconsistentEdges=sum(e.is_manifold and not e.is_contiguous for e in bm.edges),zeroAreaFaces=sum(f.calc_area()<1e-14 for f in bm.faces),nonPositiveIslands=sum(v<=1e-15 for v in volumes),minimumVolume=min(volumes,default=0))
    bm.free();return result

def inspect():
    checks=[];failures=[];parts={};roller_samples=[]
    def check(ok,text):(checks if ok else failures).append(text)
    stems=['Rod','Housing','Rotor','Spool','Crank','Lure','BailWire','LineRoller'];triangles={str(i):0 for i in range(3)}
    for stem in stems:
        for lod in range(3):
            name=stem+'_LOD'+str(lod);obj=bpy.data.objects.get(name);check(obj is not None,'实际部件 '+name)
            if obj is None:continue
            obj.data.calc_loop_triangles();triangles[str(lod)]+=len(obj.data.loop_triangles);stats=solid_stats(obj);parts[name]=stats
            check(stats['boundaryEdges']==stats['nonManifoldEdges']==stats['inconsistentEdges']==stats['zeroAreaFaces']==stats['nonPositiveIslands']==0,'保存实体封闭朝外且无退化面 '+name)
    check(triangles['0']<=43420,'保留43420面LOD0候选上限')
    line={}
    for lod in range(3):
        obj=bpy.data.objects.get('Spool_LOD'+str(lod))
        if obj:
            s=solid_stats(obj,'Rig_Line');line[str(lod)]=s
            check(s['components']==1 and s['faces']>100,'三LOD各自一条真实连续绕线 '+str(lod))
    required=['LineRollerPivot','LineRollerAxis','RollerSurface','SpoolLineRadius','SpoolLipCenter','SpoolLipSurface']
    for name in required:check(bpy.data.objects.get(name) is not None,'实际机构标记 '+name)
    center=bpy.data.objects.get('LineRollerPivot')
    if center:
        origin=center.matrix_world.translation
        for lod in range(3):
            obj=bpy.data.objects.get('LineRoller_LOD'+str(lod))
            if obj is None:continue
            tree=BVHTree.FromPolygons([obj.matrix_world@v.co for v in obj.data.vertices],[tuple(p.vertices) for p in obj.data.polygons])
            radii=defaultdict(list)
            for axial in [-.0035,0,.0035]:
                for step in range(8):
                    angle=step*math.tau/8;direction=Vector((0,math.cos(angle),math.sin(angle)));p=origin+Vector((axial,0,0))
                    inside,_,_,_=tree.ray_cast(p,direction,.015);outside=None
                    if inside is not None:outside,_,_,_=tree.ray_cast(inside+direction*.000002,direction,.015)
                    a=(inside-p).length if inside is not None else None;b=(outside-p).length if outside is not None else None
                    roller_samples.append(dict(lod=lod,axial=axial,angle=step,bore=a,outer=b));check(a is not None and b is not None and .001<a<.0025 and .0035<b<.0075,'实际内孔与外壁 '+str((lod,axial,step)))
                    if b is not None:radii[axial].append(b)
            check(all(len(radii[x])==8 for x in [-.0035,0,.0035]) and min(radii[-.0035]+radii[.0035])>max(radii[0])*1.25,'真实凹槽低于两侧肩部 '+str(lod))
            check(parts[obj.name]['components']==1,'滚轮实体保持单一有孔连通体 '+str(lod))
    return dict(status='FAIL' if failures else 'ENGINEERING_CHECKS_PASS',scope='saved mesh topology, actual line connectivity and BVH roller bore/groove; not CAD, art or device qualification',checks=checks,failures=failures,triangles=triangles,parts=parts,line=line,roller=roller_samples)

if __name__=='__main__':
    import json
    result=inspect();print(json.dumps(result,ensure_ascii=False,indent=2));raise SystemExit(bool(result['failures']))
