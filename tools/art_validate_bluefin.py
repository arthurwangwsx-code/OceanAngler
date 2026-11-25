"""Read the saved Blender asset and inspect real weights, UVs and motion.
Run: Blender --background art/source/bluefin/Bluefin.blend --python tools/art_validate_bluefin.py
"""
import bpy,json,math
from pathlib import Path
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1];failures=[];checks=[]
def check(condition,text):
    (checks if condition else failures).append(text)
rig=bpy.data.objects.get('BluefinRig');check(rig is not None and rig.type=='ARMATURE','可编辑骨骼存在')
report={'scope':'saved-blend-engineering','visualAcceptance':'NOT_ACCEPTED','lods':{},'motion':{},'failures':failures,'checks':checks}
lods=[bpy.data.objects.get('Bluefin_LOD'+str(i)) for i in range(3)]
check(all(o is not None for o in lods),'三个实际LOD存在')
if rig and all(lods):
    previous=10**9
    for i,obj in enumerate(lods):
        data=obj.data;data.calc_loop_triangles();count=len(data.loop_triangles);bad_weights=0
        for v in data.vertices:
            total=sum(g.weight for g in v.groups)
            if not math.isfinite(total) or abs(total-1)>.002:bad_weights+=1
        invalid_uv=sum(not all(math.isfinite(x) and -.0001<=x<=1.0001 for x in loop.uv) for layer in data.uv_layers for loop in layer.data)
        check(count<previous,'LOD'+str(i)+'面数递减');previous=count
        check(bad_weights==0,'LOD'+str(i)+'全部顶点权重归一且非空')
        check(invalid_uv==0,'LOD'+str(i)+' UV有限且位于贴图范围')
        check(all(math.isfinite(n) for v in data.vertices for n in v.co),'LOD'+str(i)+'无非法顶点')
        check(any(m.type=='ARMATURE' and m.object==rig for m in obj.modifiers),'LOD'+str(i)+'使用共同骨骼')
        report['lods'][obj.name]={'triangles':count,'invalidWeightVertices':bad_weights,'invalidUVLoops':invalid_uv}
    check(8000<=len(lods[0].data.loop_triangles)<=25000,'LOD0在候选面数预算内')
    check(set(b.name for b in rig.data.bones)>={'jaw','tail','pectoralL','pectoralR','pelvicL','pelvicR'},'颌/尾/成对鳍均有独立骨骼')
    # Inspect actual front-lip vertices. A moving jaw bone alone never proves
    # the visible lower lip follows it (the previous rigid rim did not).
    data=lods[0].data;skin_vertices={v for p in data.polygons if data.materials[p.material_index].name=='Bluefin_Skin' for v in p.vertices}
    lower_lip=[v.index for v in data.vertices if v.index in skin_vertices and v.co.x<-1.043 and v.co.z<-.0075]
    check(len(lower_lip)>=4,'可独立检查的前下唇几何存在')
    jaw_index=lods[0].vertex_groups['jaw'].index
    lip_weights=[sum(g.weight for g in data.vertices[i].groups if g.group==jaw_index) for i in lower_lip]
    check(bool(lip_weights) and sum(lip_weights)/len(lip_weights)>.9,'前下唇实际绑定下颌而非头骨')
    corners=[v.index for v in data.vertices if v.index in skin_vertices and -.874<v.co.x<-.866 and abs(v.co.y)>.025 and -.12<v.co.z<-.02]
    corner_weights=[sum(g.weight for g in data.vertices[i].groups if g.group==jaw_index) for i in corners]
    check(len(corners)>=4 and max(corner_weights,default=1)<.025,'实际嘴角皮肤保持头骨锚定而非随下颌漂移')
    oral={v for p in data.polygons if data.materials[p.material_index].name=='Bluefin_Oral' for v in p.vertices}
    oral_depth=max((data.vertices[i].co.x for i in oral),default=0)-min((data.vertices[i].co.x for i in oral),default=0)
    check(oral_depth>.12,'口腔是有纵深的内壁而非单片盖板')
    back=max((data.vertices[i].co.x for i in oral),default=0)
    throat=[p for p in data.polygons if data.materials[p.material_index].name=='Bluefin_Oral' and all(abs(data.vertices[i].co.x-back)<.0001 for i in p.vertices)]
    throat_normal=sum(p.normal.x*p.area for p in throat)/max(.000001,sum(p.area for p in throat))
    check(bool(throat) and throat_normal<-.9,'口腔喉壁朝向口内而非背向观察者')
    strips=0
    for face in data.polygons:
        if data.materials[face.material_index].name!='Bluefin_Fins' or len(face.vertices)!=4:continue
        uv=[data.uv_layers.active.data[i].uv for i in face.loop_indices]
        # Each strip occupies one ray interval inside its padded atlas half.
        if abs(max(p.x for p in uv)-min(p.x for p in uv)-.492)<.001 and max(p.y for p in uv)-min(p.y for p in uv)>.1:strips+=1
    check(strips>=600,'鳍膜条段使用单个鳍条间隔UV')
    report['mouth']={'lowerLipVertices':len(lower_lip),'meanJawWeight':sum(lip_weights)/max(1,len(lip_weights)),'cornerVertices':len(corners),'maximumCornerJawWeight':max(corner_weights,default=1),'liningDepth':oral_depth,'throatNormalX':throat_normal};report['finUvStripFaces']=strips
    skin_edge_faces={tuple(sorted(edge.vertices)):0 for edge in data.edges}
    for face in data.polygons:
        if data.materials[face.material_index].name=='Bluefin_Skin':
            for key in face.edge_keys:skin_edge_faces[tuple(sorted(key))]+=1
    posterior_edges=[edge for edge in data.edges if skin_edge_faces[tuple(sorted(edge.vertices))]>0 and all(-.855<data.vertices[i].co.x<-.765 and data.vertices[i].co.z<-.05 and abs(data.vertices[i].co.y)>.005 for i in edge.vertices)]
    open_posterior=sum(skin_edge_faces[tuple(sorted(edge.vertices))]==1 for edge in posterior_edges)
    check(bool(posterior_edges) and open_posterior==0,'嘴角后方颌部皮肤拓扑闭合而非重叠分离边')
    report['mouth'].update(posteriorSkinEdges=len(posterior_edges),openPosteriorSkinEdges=open_posterior)
    atlas=bpy.data.images.get('Bluefin_Fin');check(atlas is not None and tuple(atlas.size)==(1024,512),'鳍色分区共用一张1024x512纹理')
    palette_counts={'blue':0,'pale':0};anatomy_counts={'firstDorsal':0,'paired':0,'secondDorsal':0,'caudal':0}
    for face in data.polygons:
        if data.materials[face.material_index].name not in ('Bluefin_Fins','Bluefin_Rays'):continue
        values=[data.uv_layers.active.data[i].uv.x for i in face.loop_indices];blue=max(values)<.5;pale=min(values)>.5
        check(blue or pale,'鳍面UV不横跨色区边界：'+str(face.index)) if not (blue or pale) else None
        if blue:palette_counts['blue']+=1
        if pale:palette_counts['pale']+=1
        center=face.center;expected=None;region=None
        if -.56<center.x<-.06 and center.z>.412:region='firstDorsal';expected=pale
        elif abs(center.y)>.18:region='paired';expected=pale
        elif 0<center.x<.3 and center.z>.25:region='secondDorsal';expected=blue
        elif center.x>1.03:region='caudal';expected=blue
        if region:
            anatomy_counts[region]+=1
            if not expected:check(False,'实际几何鳍色区错误：'+region+' face='+str(face.index))
    check(all(n>0 for n in palette_counts.values()),'实际鳍面使用蓝色与灰铜色两个区域')
    check(all(n>0 for n in anatomy_counts.values()),'第一背鳍/成对鳍/第二背鳍/尾鳍均有独立几何颜色核验')
    check(len(data.materials)==7,'七个源材质槽分离实体鳍条与透明鳍膜')
    report['finPalette']={'faces':palette_counts,'anatomySamples':anatomy_counts,'materialSlots':len(data.materials)}
    eye_material=data.materials.get('Bluefin_Iris')
    eye_maps=[node.image for node in eye_material.node_tree.nodes if node.type=='TEX_IMAGE' and node.image] if eye_material else []
    check(len(eye_maps)==1 and tuple(eye_maps[0].size)==(256,256),'虹膜使用实际256x256原创贴图')
    check(bool(eye_maps) and eye_maps[0].colorspace_settings.name=='sRGB','虹膜色图使用sRGB')
    eye_sides={}
    for sign,label in [(-1,'negativeY'),(1,'positiveY')]:
        faces=[face for face in data.polygons if data.materials[face.material_index].name=='Bluefin_Iris' and sign*face.center.y>0]
        uv=[data.uv_layers.active.data[i].uv for face in faces for i in face.loop_indices]
        span=[max(p[j] for p in uv)-min(p[j] for p in uv) for j in (0,1)] if uv else [0,0]
        vertices={i for face in faces for i in face.vertices};head=lods[0].vertex_groups['head'].index
        attached=all(sum(g.weight for g in data.vertices[i].groups if g.group==head)>.99 for i in vertices)
        check(bool(faces) and min(span)>.95 and attached,'独立眼球UV覆盖虹膜且绑定头骨：'+label)
        eye_sides[label]={'faces':len(faces),'uvSpan':span,'headAttached':attached}
    report['iris']={'maps':[image.name for image in eye_maps],'sides':eye_sides}
    top_spans=[]
    for face in data.polygons:
        if len(face.vertices)==4 and data.materials[face.material_index].name=='Bluefin_Skin' and all(data.vertices[i].co.z>.15 for i in face.vertices):
            values=[data.uv_layers.active.data[i].uv.y for i in face.loop_indices];top_spans.append(max(values)-min(values))
    check(bool(top_spans) and max(top_spans)<.1,'背部四边面不跨越整幅纹理的UV接缝')
    report['maximumBackUvSpan']=max(top_spans,default=0)
    for image in bpy.data.images:
        if image.source=='FILE':check(Path(bpy.path.abspath(image.filepath)).is_file(),'贴图文件存在：'+image.name)
    for name in ['idle','swim','turn','burst','bite','hooked','release']:
        action=bpy.data.actions.get(name);check(action is not None,'语义动作存在：'+name)
        if action is None:continue
        rig.animation_data.action=action;poses=[];jaw=[];gill_clearance=[];lip_positions=[];corner_positions=[]
        for frame in [1,16,31,46,61]:
            bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
            poses.append(tuple(rig.pose.bones['tail'].rotation_euler));jaw.append(tuple(rig.pose.bones['jaw'].rotation_euler))
            evaluated=lods[0].evaluated_get(bpy.context.evaluated_depsgraph_get());deformed=evaluated.to_mesh()
            try:
                vertices=[v.co.copy() for v in deformed.vertices]
                if lower_lip:lip_positions.append(sum((vertices[i] for i in lower_lip),vertices[lower_lip[0]]*0)/len(lower_lip))
                corner_positions.append([vertices[i] for i in corners])
                skin=[tuple(p.vertices) for p in deformed.polygons if deformed.materials[p.material_index].name=='Bluefin_Skin']
                gills={v for p in deformed.polygons if deformed.materials[p.material_index].name=='Bluefin_Gill' for v in p.vertices}
                tree=BVHTree.FromPolygons(vertices,skin)
                gill_clearance.append(max((tree.find_nearest(vertices[v])[3] for v in gills),default=float('inf')))
                if name=='bite' and frame==16:
                    from mathutils import Vector
                    walls=[tuple(p.vertices) for p in deformed.polygons if deformed.materials[p.material_index].name=='Bluefin_Oral']
                    cavity=BVHTree.FromPolygons(vertices,walls);hit,normal,_,distance=cavity.ray_cast(Vector((-.98,0,-.045)),Vector((0,1,0)),.2)
                    check(hit is not None and normal.y<-.5,'张口时侧向射线命中朝内的口腔壁')
                    report['mouth']['farWallRayDistance']=distance;report['mouth']['farWallNormalY']=None if normal is None else normal.y
            finally:evaluated.to_mesh_clear()
        spread=max(sum((a-b)**2 for a,b in zip(p,poses[0]))**.5 for p in poses)
        jaw_spread=max(sum((a-b)**2 for a,b in zip(p,jaw[0]))**.5 for p in jaw)
        check(spread>.001,'语义动作具有真实尾摆：'+name)
        if name=='bite':
            check(jaw_spread>.1,'咬钩动作有实际下颌运动')
            displacement=max(((p-lip_positions[0]).length for p in lip_positions),default=0)
            check(displacement>.015,'咬钩实际变形下唇随下颌张开');report['mouth']['biteLipDisplacement']=displacement
            down=min((p.z-lip_positions[0].z for p in lip_positions),default=0);side=max((abs(p.y-lip_positions[0].y) for p in lip_positions),default=0)
            check(down<-.015,'咬钩下唇向下张开而非转入头部');check(side<.003,'咬钩下唇无明显横向扭转')
            report['mouth'].update(biteLipVerticalDisplacement=down,biteLipSidewaysDrift=side)
        check(all(math.isfinite(d) and d<.006 for d in gill_clearance),'变形后鳃缘紧贴鱼体：'+name)
        corner_drift=max(((p-corner_positions[0][i]).length for pose in corner_positions for i,p in enumerate(pose)),default=float('inf'))
        check(corner_drift<.001,'动作中实际嘴角皮肤无明显漂移：'+name)
        report['motion'][name]={'tailRotationRange':spread,'jawRotationRange':jaw_spread,'maximumGillDistanceToBody':max(gill_clearance),'maximumMouthCornerDrift':corner_drift}
mouth_path=ROOT/'tools/art_validate_mouth_surface.py';scope={'__file__':str(mouth_path),'__name__':'mouth_surface_validation'};exec(compile(mouth_path.read_text(),str(mouth_path),'exec'),scope);report['mouthTopology']=scope['inspect']();check(report['mouthTopology']['status']=='PASS','内部口腔盖面不共享外部面颊平滑拓扑')
tissue_path=ROOT/'tools/art_validate_fin_tissue.py';scope={'__file__':str(tissue_path),'__name__':'fin_tissue_validation'};exec(compile(tissue_path.read_text(),str(tissue_path),'exec'),scope);report['finTissue']=scope['inspect']();checks.extend(report['finTissue']['checks']);failures.extend(report['finTissue']['failures'])
report['status']='FAIL' if failures else 'ENGINEERING_CHECKS_PASS'
path=ROOT/'.local/art/bluefin/validation.json';path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(report,ensure_ascii=False,indent=2))
if failures:raise RuntimeError('Saved asset checks failed; see private validation report')
