"""Original Atlantic halibut anatomical candidate, NOT a finished art approval.
Blender coordinates: X head(-) to tail(+), Y body width, Z eyed side up.
No third-party media: geometry, maps, rig and motion are authored here.
"""
import bpy,bmesh,math,json,sys,hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art/source/halibut';GAME=ROOT/'game/Assets/OceanAngler/Art/Halibut'
for p in (OUT,GAME):p.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.render.fps=30
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=1440;scene.render.resolution_y=960;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.world.color=(.18,.18,.18)
# Hand-designed species-specific cross sections. Front ring is the open mouth.
PROFILE=[(-1.045,.048,.021),(-.99,.09,.028),(-.88,.175,.044),(-.73,.285,.060),(-.52,.405,.073),(-.23,.475,.077),(.04,.467,.073),(.29,.387,.061),(.52,.280,.047),(.72,.170,.034),(.88,.083,.025),(1.0,.058,.021)]
parts=[];anatomy={};maps={}
def section(x):return tuple(float(np.interp(x,[p[0] for p in PROFILE],[p[i] for p in PROFILE])) for i in (1,2))
def surface(x,a,offset=0):
    w,h=section(x);return (x,(w+offset)*math.sin(a),(h+offset)*math.cos(a))
def image(name,values,noncolor=False):
    if values.ndim==2:values=np.repeat(values[:,:,None],3,axis=2)
    rgba=np.ones((*values.shape[:2],4),dtype=np.float32);rgba[:,:,:values.shape[2]]=np.clip(values,0,1)
    im=bpy.data.images.new('Halibut_'+name,width=rgba.shape[1],height=rgba.shape[0],alpha=values.shape[2]==4)
    im.colorspace_settings.name='Non-Color' if noncolor else 'sRGB';im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(GAME/(im.name+'.png'));im.file_format='PNG';im.save();maps[name]=im;return im
def material(name,color,roughness=.5,base=None,normal=None,packed=None):
    mat=bpy.data.materials.new('Halibut_'+name);mat.diffuse_color=(*color,1);mat.use_nodes=True;nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Roughness'].default_value=roughness
    for key,im in [('Base Color',base),('Normal',normal)]:
        if im is None:continue
        tex=nodes.new('ShaderNodeTexImage');tex.image=im
        if key=='Normal':n=nodes.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=.35;links.new(tex.outputs['Color'],n.inputs['Color']);links.new(n.outputs['Normal'],bs.inputs['Normal'])
        else:links.new(tex.outputs['Color'],bs.inputs[key])
    if packed:
        tex=nodes.new('ShaderNodeTexImage');tex.image=packed;inv=nodes.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1;links.new(tex.outputs['Alpha'],inv.inputs[1]);links.new(inv.outputs[0],bs.inputs['Roughness'])
    return mat
# Periodic V noise avoids a cylinder seam; texture is original analytic paint.
h=w=1024;y,x=np.mgrid[0:h,0:w];u=x/(w-1);v=y/(h-1);phi=v*math.tau
upper=np.clip((np.cos(phi)+.13)/.36,0,1);upper=upper*upper*(3-2*upper)
mottle=(np.sin(u*45+2*np.cos(phi*9))+np.cos(u*93+3*np.sin(phi*13))+.45*np.sin(u*177+np.cos(phi*29)))/2.45
fine=np.sin(u*1207+np.sin(phi*78))*np.cos(phi*171+u*43)
base=np.array([.68,.66,.60])[None,None,:]*(1-upper[:,:,None])+(np.array([.205,.205,.137])[None,None,:]+mottle[:,:,None]*np.array([.045,.042,.028]))*upper[:,:,None]
# Small, sparse pale flecks; not a grid of repeated spots.
flecks=np.clip((np.sin(u*297+np.sin(phi*17))*np.sin(phi*141+u*73)-.75)*4,0,1)
base+=upper[:,:,None]*flecks[:,:,None]*.035+fine[:,:,None]*.002
# Curved lateral line arches around the pectoral region on the eyed side.
line_v=.30+.16*np.exp(-((u-.29)/.14)**2);line=np.exp(-((np.sin(phi)-line_v)/.018)**2)*upper*np.clip((u-.17)*15,0,1)
base-=line[:,:,None]*np.array([.025,.023,.016])
rows=v*80;cols=u*150+(np.floor(rows)%2)*.5
relief=np.exp(-(((rows%1)-(.25+.35*(cols%1-.5)**2))/.065)**2)*.00015
relief*=np.clip((u-.18)*12,0,1);gy,gx=np.gradient(relief);normal=np.stack((-gx*65,-gy*65,np.ones_like(gx)),axis=2);normal/=np.linalg.norm(normal,axis=2)[:,:,None]
rough=np.clip(.55+.06*upper+.015*mottle,.48,.66)
image('BaseColor',base);image('Normal',normal*.5+.5,True);image('Roughness',rough,True);image('MetallicSmoothness',np.dstack((np.zeros((h,w,3)),1-rough)),True)
fy,fx=np.mgrid[0:512,0:512];fu=fx/511;fv=fy/511;ray=(.5+.5*np.cos(fu*math.tau*88))**12
# Top/bottom fin colors use opposite halves of this one atlas.
fc=np.where((fv>.5)[:,:,None],np.array([.58,.56,.49]),np.array([.185,.192,.125]));fc+=ray[:,:,None]*.024
image('Fin',fc);fn=np.stack((np.sin(fu*math.tau*88)*.07,np.zeros_like(fu),np.ones_like(fu)),axis=2);fn/=np.linalg.norm(fn,axis=2)[:,:,None];image('Fin_Normal',fn*.5+.5,True)
image('Fin_MetallicSmoothness',np.dstack((np.zeros((512,512,3)),.44+ray*.05)),True)
ey,ex=np.mgrid[0:256,0:256];dx=(ex-127.5)/127.5;dy=(ey-127.5)/127.5;r=np.sqrt(dx*dx+dy*dy);theta=np.arctan2(dy,dx)
iris=np.zeros((256,256,3));iris[:]=[.22,.21,.12];iris+=(np.cos(theta*57+r*21)*.018)[:,:,None];iris*=np.clip((1-r)*7,0,1)[:,:,None];image('Iris',iris)
skin=material('Skin',(.3,.3,.2),base=maps['BaseColor'],normal=maps['Normal'],packed=maps['MetallicSmoothness']);fin=material('Fins',(.25,.25,.16),base=maps['Fin'],normal=maps['Fin_Normal'],packed=maps['Fin_MetallicSmoothness']);eye=material('Iris',(.3,.3,.2),.3,base=maps['Iris']);dark=material('Dark',(.018,.015,.011),.37);tooth=material('Teeth',(.61,.59,.51),.52)
BONE_X=[(-.55,'head'),(-.05,'spine1'),(.32,'spine2'),(.61,'spine3'),(.9,'peduncle'),(1.25,'tail')]
def weights(x):
    if x<=BONE_X[0][0]:return [('head',1)]
    if x>=BONE_X[-1][0]:return [('tail',1)]
    for (a,an),(b,bn) in zip(BONE_X,BONE_X[1:]):
        if a<=x<=b:t=(x-a)/(b-a);return [(an,1-t),(bn,t)]
def jaw_weights(p):
    x,y,z=p;t=np.clip((-.79-x)/.17,0,1)*np.clip((y+.005)/.018,0,1);return [('head',1-float(t)),('jaw',float(t))]
def mesh(name,verts,faces,mat,uv=None,bone=None,jaw=False):
    data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update();obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);data.materials.append(mat);layer=data.uv_layers.new()
    for face in data.polygons:
        face.use_smooth=True
        for li in face.loop_indices:
            vi=data.loops[li].vertex_index;p=verts[vi];layer.data[li].uv=uv[vi] if uv else ((p[0]+1.05)/2.5,.25)
    for i,p in enumerate(verts):
        for key,value in jaw_weights(p) if jaw else [(bone,1)] if bone else weights(p[0]):
            if value>0:(obj.vertex_groups.get(key) or obj.vertex_groups.new(name=key)).add([i],value,'REPLACE')
    bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free();parts.append(obj);return obj
# Body and real mouth lumen, with a throat cap instead of a painted hole.
verts=[];faces=[];uv=[];rings=110;sides=56
for i in range(rings+1):
    x=-1.045+i/rings*2.045
    for j in range(sides+1):verts.append(surface(x,j/sides*math.tau));uv.append((i/rings,j/sides))
for i in range(rings):
    for j in range(sides):a=i*(sides+1)+j;faces.append((a,a+sides+1,a+sides+2,a+1))
faces.append(tuple(rings*(sides+1)+j for j in range(sides)))
body=mesh('Body',verts,faces,skin,uv,jaw=True)
verts=[];faces=[]
for k,(x,w,h) in enumerate([(-1.045,.0475,.0205),(-.975,.042,.018),(-.855,.018,.011)]):
    for j in range(sides):verts.append((x,w*math.sin(j/sides*math.tau),h*math.cos(j/sides*math.tau)))
for i in range(2):
    for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;faces.append((a,b,b+sides,a+sides))
faces.append(tuple(2*sides+j for j in range(sides-1,-1,-1)))
oral=mesh('OralLumen',verts,faces,dark,jaw=True)
# The lumen's visible normals must face inside the opening, not outside.
bm=bmesh.new();bm.from_mesh(oral.data);bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bm.to_mesh(oral.data);bm.free()
anatomy['mouth']={'opening':[-1.045,.048,.021],'throatX':-.855,'jawHinge':[-.81,.065,0]}
def fin_shell(name,roots,tips,bone=None,steps=5):
    # Two faces, a tapered edge and closed rim; no paper-thin triangular fan.
    n=len(roots);verts=[];uv=[];faces=[]
    for side in (1,-1):
        for i,(root,tip) in enumerate(zip(roots,tips)):
            for j in range(steps+1):
                t=j/steps;p=Vector(root).lerp(Vector(tip),t);p.z+=side*(.0012+.0035*(1-t)**1.4);verts.append(tuple(p));uv.append((i/(n-1),(.03+.43*t) if side==1 else (.54+.43*t)))
    stride=steps+1;count=n*stride
    for i in range(n-1):
        for j in range(steps):a=i*stride+j;faces.extend([(a,a+stride,a+stride+1,a+1),(a+count,a+1+count,a+stride+1+count,a+stride+count)])
    border=list(range(stride))+[i*stride+steps for i in range(1,n)]+list(range((n-1)*stride+steps-1,(n-1)*stride-1,-1))+[i*stride for i in range(n-2,0,-1)]
    for a,b in zip(border,border[1:]+border[:1]):faces.append((a,b,b+count,a+count))
    return mesh(name,verts,faces,fin,uv,bone)
for sign,label,start in [(-1,'Dorsal',-.96),(1,'Anal',-.68)]:
    roots=[];tips=[]
    for i in range(65):
        t=i/64;x=start+( .89-start)*t;w,h=section(x);span=.009+.105*math.sin(math.pi*t)**.72
        roots.append((x,sign*w*.98,0));tips.append((x+.016*math.sin(math.pi*t),sign*(w+span),.002*math.sin(t*math.tau*5)))
    fin_shell(label,roots,tips)
    anatomy[label]={'rays':65,'fromX':start,'toX':.89}
# Concave caudal: outer lobes project farther back than the center edge.
roots=[];tips=[]
for i in range(41):
    t=-1+2*i/40;roots.append((.96,.058*t,0));tips.append((1.245+.18*abs(t)**1.7,.28*t,0))
fin_shell('Caudal',roots,tips,'tail',steps=7);anatomy['caudal']={'centerX':1.245,'outerX':1.425,'halfSpan':.28}
# Eyed-side pectoral near gill cover, smaller blind-side pectoral below.
for sign,label in [(1,'PectoralUp'),(-1,'PectoralBlind')]:
    roots=[];tips=[]
    for i in range(17):
        t=i/16;roots.append((-.58+t*.08,.02+t*.075,sign*.07));tips.append((-.49+.32*math.sin(math.pi*t*.82),.035+.21*t,sign*(.071+.012*math.sin(math.pi*t))))
    fin_shell(label,roots,tips,'head',steps=4)
# Eyes sit into the eyed-side hull; iris dome and pupil are actual mesh.
for index,(cx,cy) in enumerate([(-.787,-.064),(-.691,-.185)]):
    w,h=section(cx);cz=h*math.sqrt(max(0,1-(cy/w)**2))
    anatomy['eye'+str(index)]={'center':[cx,cy,cz+.009],'side':'eyed-positive-Z'}
    # Low embedded skin socket, original radial iris UV, pupil dome.
    for role,radius,height,mat in [('Socket',.037,.016,skin),('Iris',.027,.011,eye),('Pupil',.013,.013,dark)]:
        verts=[];uv=[];faces=[];nr=7;ns=32;basez=cz+(.003 if role=='Socket' else .013)
        for a in range(nr+1):
            r=radius*a/nr
            for j in range(ns):ang=j/ns*math.tau;verts.append((cx+r*math.cos(ang),cy+r*math.sin(ang),basez+height*math.sqrt(max(0,1-(r/radius)**2))));uv.append((.5+.5*r/radius*math.cos(ang),.5+.5*r/radius*math.sin(ang)) if role=='Iris' else ((cx+1.045)/2.045,math.acos(min(1,cz/h))/math.tau))
        for a in range(nr):
            for j in range(ns):b=a*ns+j;c=a*ns+(j+1)%ns;faces.append((b,b+ns,c+ns,c))
        obj=mesh('Eye'+str(index)+role,verts,faces,mat,uv,'head')
# Teeth are restrained close-view details, entirely inside the mouth.
for i in range(12):
    a=math.tau*i/12;root=Vector((-1.026,.043*math.sin(a),.018*math.cos(a)));tip=root+Vector((.01,-.006*math.sin(a),-.006*math.cos(a)));axis=(tip-root).normalized();a1=axis.cross(Vector((0,0,1))).normalized();a2=axis.cross(a1)
    vv=[tuple(root+.0018*(math.cos(j*math.tau/5)*a1+math.sin(j*math.tau/5)*a2)) for j in range(5)]+[tuple(tip)];ff=[tuple(range(4,-1,-1))]+[(j,(j+1)%5,5) for j in range(5)];mesh('Tooth'+str(i),vv,ff,tooth,jaw=True)
# Join authored components; UVs and semantic weight groups survive each LOD.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:obj.select_set(True)
bpy.context.view_layer.objects.active=body;bpy.ops.object.join();body.name='Halibut_LOD0'
# Merge only coincident seam/eye pole vertices, preserving UV per loop.
bm=bmesh.new();bm.from_mesh(body.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001);bm.to_mesh(body.data);bm.free();body.data.update()
rigdata=bpy.data.armatures.new('HalibutSkeleton');rig=bpy.data.objects.new('HalibutRig',rigdata);scene.collection.objects.link(rig)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
spec=[('root',(0,0,0),(0,0,.12),None),('head',(-.75,0,0),(-.25,0,0),'root'),('spine1',(-.25,0,0),(.15,0,0),'head'),('spine2',(.15,0,0),(.49,0,0),'spine1'),('spine3',(.49,0,0),(.78,0,0),'spine2'),('peduncle',(.78,0,0),(.96,0,0),'spine3'),('tail',(.96,0,0),(1.425,0,0),'peduncle'),('jaw',(-.81,.065,0),(-1.04,.065,0),'head')]
for name,head,tail,parent in spec:
    bone=rigdata.edit_bones.new(name);bone.head=head;bone.tail=tail
    if name!='root':bone.align_roll(Vector((0,0,1)))
    if parent:bone.parent=rigdata.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT');rig.show_in_front=True
lods=[body];uv_reduction=[];body.data.calc_loop_triangles();base_count=len(body.data.loop_triangles)
for level,ratio in [(1,.4),(2,.15)]:
    obj=body.copy();obj.data=body.data.copy();scene.collection.objects.link(obj);obj.name='Halibut_LOD'+str(level);bpy.context.view_layer.objects.active=obj;mod=obj.modifiers.new('DistanceReduction','DECIMATE');mod.ratio=ratio;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
    # Blender collapse can extrapolate seam UVs beyond their original atlas.
    # Distance assets use the same bounded atlas; retain the measured excursion.
    values=[float(c) for layer in obj.data.uv_layers for loop in layer.data for c in loop.uv]
    uv_reduction.append({'lod':level,'beforeMin':min(values),'beforeMax':max(values),'clampedCoordinates':sum(c<0 or c>1 for c in values)})
    for layer in obj.data.uv_layers:
        for loop in layer.data:loop.uv=(max(0,min(1,loop.uv.x)),max(0,min(1,loop.uv.y)))
    lods.append(obj)
for obj in lods:
    obj.parent=rig;mod=obj.modifiers.new('SwimSkeleton','ARMATURE');mod.object=rig
# Flatfish body undulation is vertical; bone-local X bends in source Z.
SEMANTICS=[('idle',.022,1),('swim',.075,2),('turn',.09,1),('burst',.14,3),('bite',.055,1),('hooked',.17,3),('release',.09,2)]
rig.animation_data_create()
def motion(name,amplitude,cycles,u):
    phase=math.tau*cycles*u;result={}
    for i,bone in enumerate(['head','spine1','spine2','spine3','peduncle','tail']):
        wave=math.sin(phase-i*.7)*amplitude*(i/5)**1.6
        steer=.11*math.sin(math.pi*u)**2 if name=='turn' else 0
        result[bone]=(wave,0,steer*i/5)
    result['jaw']=(0,0,-.40*max(0,math.sin(phase))**2 if name in ('bite','hooked') else -.003*(1+math.sin(phase)))
    return result
for name,amp,cycles in SEMANTICS:
    action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
    for frame in range(1,62,2):
        u=(frame-1)/60
        for bone,rotation in motion(name,amp,cycles,u).items():pose=rig.pose.bones[bone];pose.rotation_mode='XYZ';pose.rotation_euler=rotation;pose.keyframe_insert(data_path='rotation_euler',frame=frame)
    bag=action.layers[0].strips[0].channelbag(rig.animation_data.action_slot)
    for curve in bag.fcurves:
        bone=curve.data_path.split('"')[1];axis=curve.array_index
        for key in curve.keyframe_points:
            u=(key.co.x-1)/60;eps=.00001;deriv=(motion(name,amp,cycles,u+eps)[bone][axis]-motion(name,amp,cycles,u-eps)[bone][axis])/(2*eps*60);key.interpolation='BEZIER';key.handle_left_type='FREE';key.handle_right_type='FREE';key.handle_left=(key.co.x-1,key.co.y-deriv);key.handle_right=(key.co.x+1,key.co.y+deriv)
        curve.update()
    action['semantic']=name
rig.animation_data.action=bpy.data.actions['idle'];scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for obj in lods:obj.hide_set(False);obj.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(GAME/'Halibut.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=.2,path_mode='RELATIVE')
for obj in lods:obj.hide_set(obj!=body);obj.hide_render=obj!=body
# Saved source studio, including a true underside reference view in review tool.
for name,loc,power,size in [('Key',(-2,-3,5),420,4),('Fill',(1,3,2),160,3),('BlindFill',(0,0,-3),110,4)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);obj.location=loc;obj.rotation_euler=(Vector((0,0,0))-obj.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('ReviewCamera');camera=bpy.data.objects.new('ReviewCamera',data);scene.collection.objects.link(camera);scene.camera=camera;data.type='ORTHO';data.ortho_scale=3.2;camera.location=(-1,-3,4);camera.rotation_euler=(Vector((.13,0,0))-camera.location).to_track_quat('-Z','Y').to_euler()
for im in maps.values():im.filepath=bpy.path.relpath(im.filepath,start=str(OUT))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Halibut.blend'))
report={'status':'ANATOMICAL_CANDIDATE_NOT_ART_ACCEPTED','species':'Hippoglossus hippoglossus','coordinateConvention':'X longitudinal, Y body width, +Z eyed side up; blind side faces seabed','lods':{},'distanceUVReduction':uv_reduction,'bones':[b.name for b in rig.data.bones],'motions':[n for n,_,_ in SEMANTICS],'anatomy':anatomy,'maps':{},'license':'MIT; all geometry, UV, maps and keyframes original; reference photos excluded'}
for obj in lods:obj.data.calc_loop_triangles();report['lods'][obj.name]={'vertices':len(obj.data.vertices),'triangles':len(obj.data.loop_triangles)}
for name,im in maps.items():p=GAME/(im.name+'.png');report['maps'][name]={'path':str(p.relative_to(ROOT)),'width':im.size[0],'height':im.size[1],'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
(OUT/'build-report.json').write_text(json.dumps(report,indent=2,ensure_ascii=False)+'\n')
print(json.dumps({'triangles':report['lods'],'status':report['status']}))
