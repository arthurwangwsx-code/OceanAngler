"""Split only paired-fin membrane triangles at deformation/ray sampling rows.

New vertices interpolate an existing triangle, retaining the original rest surface.
No body, eye, mouth, gill, opaque ray, texture or original vertex is moved.
"""
import bpy
from mathutils import Vector

ROWS=(.25,.5,.65,.75)

def refine_membranes(obj):
    mesh=obj.data;mesh.calc_loop_triangles()
    sides={v.index for v in mesh.vertices if any(obj.vertex_groups[g.group].name.startswith('pectoral') and g.weight>0 for g in v.groups)}
    selected={f.index for f in mesh.polygons if mesh.materials[f.material_index].name=='Bluefin_Fins' and set(f.vertices)<=sides}
    original_vertices=[tuple(v.co) for v in mesh.vertices];vertices=list(original_vertices)
    weights=[[(g.group,g.weight) for g in v.groups] for v in mesh.vertices]
    group_names=[g.name for g in obj.vertex_groups]
    materials=list(mesh.materials);layers=[layer.name for layer in mesh.uv_layers]
    polygons=[];attributes=[];cache={};new_weights={};split=0
    def original(index,loop):return dict(index=index,co=mesh.vertices[index].co.copy(),uv=[layer.data[loop].uv.copy() for layer in mesh.uv_layers])
    def intersect(a,b,t):
        u=(t-a['uv'][0].y)/(b['uv'][0].y-a['uv'][0].y)
        if u<1e-7:return a
        if u>1-1e-7:return b
        point=dict(co=a['co'].lerp(b['co'],u),uv=[x.lerp(y,u) for x,y in zip(a['uv'],b['uv'])])
        key=tuple(round(x,9) for x in point['co'])
        if key not in cache:
            cache[key]=len(vertices);vertices.append(tuple(point['co']))
            av=dict(weights[a['index']] if a['index']<len(weights) else new_weights[a['index']]);bv=dict(weights[b['index']] if b['index']<len(weights) else new_weights[b['index']])
            new_weights[cache[key]]=[(g,av.get(g,0)*(1-u)+bv.get(g,0)*u) for g in av.keys()|bv.keys()]
        point['index']=cache[key];return point
    def clip(points,t,lower):
        result=[]
        for a,b in zip(points,points[1:]+points[:1]):
            ain=a['uv'][0].y>=t-1e-7 if lower else a['uv'][0].y<=t+1e-7
            bin=b['uv'][0].y>=t-1e-7 if lower else b['uv'][0].y<=t+1e-7
            if ain:result.append(a)
            if ain!=bin:result.append(intersect(a,b,t))
        # Endpoints already on the cut must not produce duplicate corners.
        clean=[]
        for q in result:
            if not clean or (clean[-1]['co']-q['co']).length>1e-9:clean.append(q)
        if len(clean)>1 and (clean[0]['co']-clean[-1]['co']).length<1e-9:clean.pop()
        return clean
    triangles={}
    for t in mesh.loop_triangles:triangles.setdefault(t.polygon_index,[]).append(t)
    for face in mesh.polygons:
        points=[original(mesh.loops[l].vertex_index,l) for l in face.loop_indices]
        lo=min(q['uv'][0].y for q in points);hi=max(q['uv'][0].y for q in points)
        cuts=[t for t in ROWS if lo+1e-6<t<hi-1e-6] if face.index in selected else []
        if not cuts:
            polygons.append(tuple(face.vertices));attributes.append((face.material_index,face.use_smooth,[q['uv'] for q in points]));continue
        split+=1
        for triangle in triangles[face.index]:
            corners=[original(v,l) for v,l in zip(triangle.vertices,triangle.loops)]
            for low,high in zip([lo]+cuts,cuts+[hi]):
                band=clip(clip(corners,low,True),high,False)
                if len(band)<3:continue
                polygons.append(tuple(q['index'] for q in band));attributes.append((face.material_index,face.use_smooth,[q['uv'] for q in band]))
    if not split:return dict(splitFaces=0,newVertices=0)
    if mesh.has_custom_normals:raise RuntimeError('Review custom normal preservation before changing this source')
    result=bpy.data.meshes.new(mesh.name+'PectoralRows');result.from_pydata(vertices,[],polygons)
    for material in materials:result.materials.append(material)
    for name in layers:result.uv_layers.new(name=name)
    for face,(material,smooth,corners) in zip(result.polygons,attributes):
        face.material_index=material;face.use_smooth=smooth
        for l,values in zip(face.loop_indices,corners):
            for layer,uv in zip(result.uv_layers,values):layer.data[l].uv=uv
    obj.data=result
    for name in group_names:
        if obj.vertex_groups.get(name) is None:obj.vertex_groups.new(name=name)
    if [g.name for g in obj.vertex_groups]!=group_names:raise RuntimeError('Original vertex group indices must be preserved')
    for index,groups in enumerate(weights):
        for group,weight in groups:obj.vertex_groups[group].add([index],weight,'REPLACE')
    for index,groups in new_weights.items():
        for group,weight in groups:
            if weight>1e-8:obj.vertex_groups[group].add([index],weight,'REPLACE')
    result.update();result.calc_loop_triangles()
    if len(result.loop_triangles)>25000:raise RuntimeError('Paired-fin refinement exceeds the unchanged 25000 triangle budget')
    return dict(splitFaces=split,newVertices=len(vertices)-len(original_vertices),triangles=len(result.loop_triangles),rows=list(ROWS),restSurface='Original membrane triangle interpolation; original vertices and all non-membrane polygons retained')
