"""Original bluefin trevally candidate. Run with Blender --background --python.

All geometry, UVs, maps and keyframes are authored here; no downloaded media.
Scientific references and review limitations are recorded beside the .blend.
"""
import bpy, math, json, sys, hashlib
from pathlib import Path
import numpy as np
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'art/source/bluefin';GAME=ROOT/'game/Assets/OceanAngler/Art/Bluefin'
PREVIEW=ROOT/'.local/art/bluefin';OUT.mkdir(parents=True,exist_ok=True);GAME.mkdir(parents=True,exist_ok=True);PREVIEW.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for block in list(bpy.data.materials):bpy.data.materials.remove(block)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.render.fps=30
bpy.context.preferences.filepaths.save_version=0
scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True
scene.render.resolution_x=1440;scene.render.resolution_y=960;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
scene.world.color=(.18,.18,.18)
parts=[]

def image(name,data,noncolor=False):
    if data.ndim==2:data=np.repeat(data[:,:,None],3,axis=2)
    rgba=np.ones((*data.shape[:2],4),dtype=np.float32);rgba[:,:,:data.shape[2]]=np.clip(data,0,1)
    im=bpy.data.images.new(name,width=data.shape[1],height=data.shape[0],alpha=data.shape[2]==4)
    if noncolor:im.colorspace_settings.name='Non-Color'
    im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(GAME/(name+'.png'));im.file_format='PNG';im.save()
    return im

def make_maps():
    h,w=1024,2048;y,x=np.mgrid[0:h,0:w];u=x/(w-1);v=y/(h-1);z=np.cos(v*math.tau)
    upper=np.clip((z+.1)/1.1,0,1);upper=upper*upper*(3-2*upper)
    silver=np.array([.69,.78,.77]);brass=np.array([.24,.34,.28]);color=silver[None,None,:]*(1-upper[:,:,None])+brass[None,None,:]*upper[:,:,None]
    band=np.exp(-((z-.22)/.19)**2);color+=band[:,:,None]*np.array([-.025,.045,.085])
    rng=np.random.default_rng(6931);noise=rng.normal(0,.008,(h,w));color+=noise[:,:,None]
    # Fine overlapping scale relief; head and caudal peduncle are deliberately smoother.
    sy=v*80;sx=u*140+(np.floor(sy)%2)*.5;dx=(sx%1-.5)*2;dy=sy%1
    arc=np.exp(-((dy-(.28+.35*dx*dx))/.045)**2)
    mask=np.clip((u-.15)*8,0,1)*np.clip((.98-u)*12,0,1)
    relief=arc*mask*.003+noise*.012;color-=arc[:,:,None]*mask[:,:,None]*.025
    # Irregular blue-black spots on dorsal/flank regions; never a repeated tile.
    for _ in range(410):
        cx=rng.uniform(.22,.92)*w;cy=rng.uniform(0,h)
        if math.cos(cy/(h-1)*math.tau)<-.12:continue
        r=rng.uniform(1.4,4.1);x0=max(0,int(cx-r*2));x1=min(w,int(cx+r*2)+1);y0=max(0,int(cy-r*2));y1=min(h,int(cy+r*2)+1)
        yy,xx=np.mgrid[y0:y1,x0:x1];dot=np.exp(-((xx-cx)**2+(yy-cy)**2)/(r*r))*.8
        color[y0:y1,x0:x1]=color[y0:y1,x0:x1]*(1-dot[:,:,None])+np.array([.035,.105,.14])*dot[:,:,None]
    gy,gx=np.gradient(relief);normal=np.stack((-gx*95,-gy*95,np.ones_like(gx)),axis=2);normal/=np.linalg.norm(normal,axis=2)[:,:,None]
    base=image('Bluefin_BaseColor',color);normal=image('Bluefin_Normal',normal*.5+.5,True)
    roughness=np.clip(.42+upper*.08+arc*.025+noise,.35,.60)
    rough=image('Bluefin_Roughness',roughness,True)
    image('Bluefin_MetallicSmoothness',np.dstack((np.zeros((h,w,3)),1-roughness)),True)
    # Fin maps follow root-to-edge UV: ribs are subtle, membranes remain opaque in game.
    fy,fx=np.mgrid[0:512,0:512];fu=fx/511;fv=fy/511
    rays=(.5+.5*np.cos(fu*math.tau*23))**9
    fc=np.zeros((512,512,3));fc[:]=[.035,.19,.34];fc+=fv[:,:,None]*np.array([.015,.18,.29]);fc+=rays[:,:,None]*np.array([.015,.055,.08])
    fin=image('Bluefin_Fin',fc)
    return base,normal,rough,fin

base,normal,rough,finmap=make_maps()
def material(name,color,roughness=.3,texture=None):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=roughness
    p.inputs['Coat Weight'].default_value=.25;p.inputs['Coat Roughness'].default_value=.18
    if texture:
        t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=texture;m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color'])
    return m
skin=material('Bluefin_Skin',(.5,.65,.65),texture=base)
p=skin.node_tree.nodes.get('Principled BSDF');p.inputs['Coat Weight'].default_value=.08;p.inputs['Coat Roughness'].default_value=.32
t=skin.node_tree.nodes.new('ShaderNodeTexImage');t.image=rough;skin.node_tree.links.new(t.outputs['Color'],p.inputs['Roughness'])
t=skin.node_tree.nodes.new('ShaderNodeTexImage');t.image=normal;n=skin.node_tree.nodes.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=.35;skin.node_tree.links.new(t.outputs['Color'],n.inputs['Color']);skin.node_tree.links.new(n.outputs['Normal'],p.inputs['Normal'])
finmat=material('Bluefin_Fins',(.04,.3,.58),.50,finmap)
gill=material('Bluefin_Gill',(.10,.18,.17),.58);gill.node_tree.nodes.get('Principled BSDF').inputs['Coat Weight'].default_value=0
iris=material('Bluefin_Iris',(.35,.43,.27),.24);dark=material('Bluefin_Dark',(.009,.023,.029),.23)

# X longitudinal, Y lateral, Z vertical. Profile is purpose-shaped, not an ellipsoid.
profile=[(-1.04,.027,-.018,.028),(-.97,.085,-.10,.055),(-.84,.22,-.21,.095),(-.67,.34,-.28,.135),(-.43,.405,-.32,.172),(-.16,.39,-.315,.178),(.12,.315,-.27,.154),(.38,.225,-.19,.115),(.63,.13,-.105,.070),(.84,.055,-.045,.030),(.96,.040,-.035,.021)]
def section(x):
    for i in range(len(profile)-1):
        if x<=profile[i+1][0]:
            a,b=profile[i:i+2];t=max(0,(x-a[0])/(b[0]-a[0]));span=b[0]-a[0];prev=profile[max(0,i-1)];nxt=profile[min(len(profile)-1,i+2)]
            h00=2*t**3-3*t*t+1;h10=t**3-2*t*t+t;h01=-2*t**3+3*t*t;h11=t**3-t*t
            return tuple(h00*a[j]+h10*span*(b[j]-prev[j])/(b[0]-prev[0])+h01*b[j]+h11*span*(nxt[j]-a[j])/(nxt[0]-a[0]) for j in (1,2,3))
    return profile[-1][1:]
def surface(x,angle,offset=0):
    top,bottom,width=section(x);co=math.cos(angle);si=math.sin(angle)
    cover=.0035*math.sin((x+.79)/.29*math.pi)**2*si*si if -.79<x<-.50 else 0
    return (x,si*(width+offset+cover),co*(top if co>=0 else -bottom)+(offset+cover)*co)
def skin_uv(p):
    top,bottom,width=section(p[0]);z=p[2]/max(.01,top if p[2]>=0 else -bottom);y=p[1]/max(.01,width)
    return (max(0,min(1,(p[0]+1.04)/2)),(math.atan2(y,z)%math.tau)/math.tau)
def weights_for_x(x):
    centers=[-.7,-.25,.15,.49,.78,1.0];names=['head','spine1','spine2','spine3','peduncle','tail']
    if x<=centers[0]:return [(names[0],1)]
    for i in range(5):
        if x<=centers[i+1]:
            t=(x-centers[i])/(centers[i+1]-centers[i]);return [(names[i],1-t),(names[i+1],t)]
    return [('tail',1)]
def mesh(name,verts,faces,mat,uv=None,bone=None):
    data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update();obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);data.materials.append(mat)
    layer=data.uv_layers.new(name='UVMap')
    for face in data.polygons:
        face.use_smooth=True
        for li in face.loop_indices:
            vi=data.loops[li].vertex_index;layer.data[li].uv=uv[vi] if uv else skin_uv(verts[vi])
    for i,p in enumerate(verts):
        for key,value in ([(bone,1)] if bone else weights_for_x(p[0])):
            group=obj.vertex_groups.get(key) or obj.vertex_groups.new(name=key);group.add([i],value,'REPLACE')
    parts.append(obj);return obj

verts=[];uv=[];faces=[];rings=100;sides=48
def gape_z(x):
    line=-.006-.055*max(0,min(1,(x+1.04)/.18));t=max(0,min(1,(x+.95)/.12));t=t*t*(3-2*t)
    return line*(1-t)+section(x)[1]*t
for i in range(rings+1):
    u=i/rings;x=-1.04+2*u
    for j in range(sides+1):
        p=surface(x,j/sides*math.tau)
        if x<-.83:p=(p[0],p[1],max(p[2],gape_z(x)))
        verts.append(p);uv.append((u,j/sides))
for i in range(rings):
    for j in range(sides):
        a=i*(sides+1)+j;b=a+sides+1;faces.append((a,b,b+1,a+1))
faces.append(tuple(range(sides-1,-1,-1)));faces.append(tuple(rings*(sides+1)+j for j in range(sides)))
body=mesh('BluefinBody',verts,faces,skin,uv)
body.data.materials.append(dark)
for face in body.data.polygons:
    points=[body.data.vertices[i].co for i in face.vertices]
    if all(p.x<-.83 and p.z<=gape_z(p.x)+.0001 for p in points):face.material_index=1
# Weld the body's UV seam before adding the mandible, so the mouth seam stays split.
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);bpy.context.view_layer.objects.active=body
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.remove_doubles(threshold=.00001);bpy.ops.object.mode_set(mode='OBJECT');body.select_set(False)
vs=[];fs=[];jaw_rings=12;jaw_sides=24
for i in range(jaw_rings+1):
    x=-1.04+.21*i/jaw_rings;top,bottom,w=section(x);cut=math.acos(max(-1,min(1,gape_z(x)/-bottom)))
    for j in range(jaw_sides+1):vs.append(surface(x,cut+(math.tau-2*cut)*j/jaw_sides))
for i in range(jaw_rings):
    for j in range(jaw_sides):a=i*(jaw_sides+1)+j;fs.append((a,a+jaw_sides+1,a+jaw_sides+2,a+1))
skin_faces=len(fs)
for i in range(jaw_rings):a=i*(jaw_sides+1);b=(i+1)*(jaw_sides+1);fs.append((a,a+jaw_sides,b+jaw_sides,b))
fs.append(tuple(range(jaw_sides,-1,-1)));fs.append(tuple(jaw_rings*(jaw_sides+1)+j for j in range(jaw_sides+1)))
mandible=mesh('LowerJaw',vs,fs,skin,bone='jaw');mandible.data.materials.append(dark)
for face in mandible.data.polygons:
    if face.index>=skin_faces:face.material_index=1

def tube(name,points,radius,mat,bone=None,segments=6):
    vs=[];fs=[]
    for i,pt in enumerate(points):
        p=Vector(pt);direction=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])
        direction.normalize();axis=direction.cross(Vector((0,0,1)))
        if axis.length<.01:axis=direction.cross(Vector((0,1,0)))
        axis.normalize();other=direction.cross(axis).normalized()
        r=radius*(1-.68*i/max(1,len(points)-1))
        for j in range(segments):vs.append(tuple(p+(axis*math.cos(j/segments*math.tau)+other*math.sin(j/segments*math.tau))*r))
    for i in range(len(points)-1):
        for j in range(segments):a=i*segments+j;b=i*segments+(j+1)%segments;fs.append((a,b,b+segments,a+segments))
    fs.append(tuple(range(segments-1,-1,-1)));fs.append(tuple((len(points)-1)*segments+j for j in range(segments)))
    return mesh(name,vs,fs,mat,bone=bone)

def fin(name,roots,tips,bone=None,rays=True):
    vs=[];fs=[];uv=[];steps=6
    for i,(a,b) in enumerate(zip(roots,tips)):
        for j in range(steps+1):
            t=j/steps;p=Vector(a).lerp(Vector(b),t);p.y+=math.sin(t*math.pi)*.004
            vs.append(tuple(p));uv.append((i/(len(roots)-1),t))
    for i in range(len(roots)-1):
        for j in range(steps):a=i*(steps+1)+j;fs.append((a,a+steps+1,a+steps+2,a+1))
    obj=mesh(name,vs,fs,finmat,uv,bone)
    # Thin closed membrane; real geometry prevents single-sided disappearing fins.
    bpy.context.view_layer.objects.active=obj;obj.select_set(True);solid=obj.modifiers.new('MembraneThickness','SOLIDIFY');solid.thickness=.0018;bpy.ops.object.modifier_apply(modifier=solid.name);obj.select_set(False)
    if rays:
        for i,(a,b) in enumerate(zip(roots,tips)):
            mid=Vector(a).lerp(Vector(b),.55);mid.y+=.002
            tube(name+'Ray'+str(i),[a,tuple(mid),b],.0013,finmat,bone,5)

# Eight first-dorsal spines, then a separate long-lobed second dorsal with 23 rays.
roots=[];tips=[]
for i,h in enumerate([.08,.16,.15,.13,.105,.08,.055,.02]):
    x=-.55+i*.065;z=section(x)[0];roots.append((x,0,z));tips.append((x+.025,0,z+h))
fin('FirstDorsal_8Spines',roots,tips)
for name,count,sign,start,end in [('SecondDorsal_23Rays',23,1,-.05,.80),('Anal_19Rays',19,-1,.06,.77)]:
    roots=[];tips=[]
    for i in range(count):
        t=i/(count-1);x=start+(end-start)*t;z=section(x)[0 if sign==1 else 1]
        h=(.34 if sign==1 else .24)*math.exp(-t*7)+.055*math.sin(math.pi*t)+.008
        roots.append((x,0,z));tips.append((x+.14*(1-t),0,z+sign*h))
    fin(name,roots,tips)

roots=[];tips=[]
for i in range(21):
    t=i/20;z=math.cos(t*math.pi)*.47;x=1.13+.35*(abs(z)/.47)**1.3
    roots.append((.93,0,z*.065));tips.append((x,0,z))
fin('Caudal_DeepFork',roots,tips,'tail')
for sign,label in [(1,'L'),(-1,'R')]:
    roots=[];tips=[]
    for i in range(15):
        t=i/14;length=(1-t)**.65
        roots.append((-.48,.16*sign,.035-.14*t));tips.append((-.44+.77*length,sign*(.16+.17*math.sin(length*math.pi*.65)),-.03-.16*length))
    fin('Pectoral_'+label,roots,tips,'pectoral'+label)
    roots=[];tips=[]
    for i in range(7):
        t=i/6;roots.append((-.32+t*.08,.075*sign,-.29));tips.append((-.12+t*.12,sign*(.11+.06*math.sin(t*math.pi)),-.43+.11*t))
    fin('Pelvic_'+label,roots,tips,'pelvic'+label)
    # Opercular volume is integrated into the body. A fine recessed-looking rim
    # follows the same longitudinal weights, avoiding a floating rigid cover.
    seam=[]
    for i in range(23):
        angle=.36+(math.pi-.72)*i/22;angle=angle if sign==1 else -angle
        seam.append(surface(-.55+.055*math.sin(abs(angle)),angle,.0007))
    tube('OpercularSlit_'+label,seam,.0012,gill,None,6)
    # Arched anterior lateral line, straight posterior scutes.
    line=[]
    for i in range(47):
        x=-.44+i/46*1.30;z=.043+.17*math.exp(-((x+.16)/.27)**2);top,bottom,w=section(x);y=w*math.sqrt(max(0,1-(z/top)**2))
        line.append((x,sign*(y+.0018),z))
    tube('LateralLine_'+label,line,.0010,finmat,None,4)
    for i in range(30):
        x=.27+i*.018;z=.043;top,bottom,w=section(x);y=sign*(w*math.sqrt(max(.01,1-(z/top)**2))+.001)
        mesh('Scute',[(x,y,z-.009),(x+.021,y+sign*.002,z),(x,y,z+.009)],[(0,1,2)],skin)
    # Eyes sit in the skull, with a small iris, dark pupil and wet specular surface.
    pos=Vector((-.825,sign*.084,.105))
    for name,offset,scale,mat in [('Eye',0,(.043,.019,.043),iris),('Pupil',.017,(.032,.007,.032),dark)]:
        bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,location=pos+Vector((0,sign*offset,0)))
        obj=bpy.context.object;obj.name=name+'_'+label;obj.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        # Put vertices in common fish coordinates before joining and weighting.
        for v in obj.data.vertices:v.co+=obj.location
        obj.location=(0,0,0);obj.data.materials.append(mat)
        for p in obj.data.polygons:p.use_smooth=True
        g=obj.vertex_groups.new(name='head');g.add(list(range(len(obj.data.vertices))),1,'REPLACE');parts.append(obj)
    nostril=surface(-.94,sign*1.10,.002);tube('Nostril_'+label,[nostril,tuple(Vector(nostril)+Vector((.012,0,-.002)))],.003,dark,'head')
    gape=[]
    for i in range(12):
        t=i/11;x=-1.035+.18*t;z=gape_z(x);top,bottom,w=section(x);y=sign*(w*math.sqrt(max(0,1-(z/-bottom)**2))+.001)
        gape.append((x,y,z))
    tube('JawGape_'+label,gape,.0022,dark,'jaw',6)

# Modest terminal mouth, closed cavity, separately weighted lower lip for bite motion.
mouth=[]
for i in range(33):
    a=i/32*math.tau;mouth.append((-1.044+.012*math.cos(a),math.sin(a)*.031,-.004+math.cos(a)*.012))
tube('MouthRim',mouth,.0033,skin,'head',6)
mesh('MouthCavity',[(-1.044,0,-.004)]+[(-1.045,p[1]*.8,p[2]) for p in mouth[:-1]],[(0,i+1,(i+1)%32+1) for i in range(32)],dark,bone='jaw')

# One shared mesh, a reusable armature, explicit skin weights and lower LODs.
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=body;bpy.ops.object.join();body.name='Bluefin_LOD0'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
# Weld seam duplicates; preserve UV seam loops and material boundaries.
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
rigdata=bpy.data.armatures.new('BluefinSkeleton');rig=bpy.data.objects.new('BluefinRig',rigdata);scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig;bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
spec=[('root',(0,0,0),(0,0,.12),None),('head',(-.74,0,0),(-.25,0,0),'root'),('spine1',(-.25,0,0),(.15,0,0),'head'),('spine2',(.15,0,0),(.49,0,0),'spine1'),('spine3',(.49,0,0),(.78,0,0),'spine2'),('peduncle',(.78,0,0),(.96,0,0),'spine3'),('tail',(.96,0,0),(1.36,0,0),'peduncle'),('jaw',(-.86,0,-.065),(-1.035,0,-.006),'head')]
for s,label in [(1,'L'),(-1,'R')]:
    spec += [('pectoral'+label,(-.48,s*.16,0),(.12,s*.30,-.16),'head'),('pelvic'+label,(-.28,s*.08,-.28),(-.03,s*.15,-.40),'spine1')]
for name,head,tail,parent in spec:
    b=rigdata.edit_bones.new(name);b.head=head;b.tail=tail
    if parent:b.parent=rigdata.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT');rig.show_in_front=True
lods=[body]
for level,ratio in [(1,.4),(2,.15)]:
    obj=body.copy();obj.data=body.data.copy();scene.collection.objects.link(obj);obj.name='Bluefin_LOD'+str(level)
    bpy.context.view_layer.objects.active=obj;mod=obj.modifiers.new('AuthoredReduction','DECIMATE');mod.ratio=ratio;bpy.ops.object.modifier_apply(modifier=mod.name);lods.append(obj)
    for layer in obj.data.uv_layers:
        for loop in layer.data:loop.uv=(max(0,min(1,loop.uv.x)),max(0,min(1,loop.uv.y)))
for obj in lods:
    mod=obj.modifiers.new('SwimDeformation','ARMATURE');mod.object=rig;obj.parent=rig
    obj.hide_render=obj!=body;obj.hide_set(obj!=body)

rig.animation_data_create()
for name,amplitude,cycles in [('idle',.06,1),('swim',.16,2),('turn',.14,1),('burst',.25,3),('bite',.10,1),('hooked',.30,3),('release',.18,2)]:
    action=bpy.data.actions.new(name);rig.animation_data.action=action;action.use_fake_user=True
    for frame in range(1,62,3):
        phase=(frame-1)/60*math.tau*cycles
        for i,bone in enumerate(['head','spine1','spine2','spine3','peduncle','tail']):
            p=rig.pose.bones[bone];p.rotation_mode='XYZ';p.rotation_euler=(0,0,math.sin(phase-i*.65)*amplitude*(i/5)**1.8+(math.sin((frame-1)/60*math.pi)*.12 if name=='turn' and i>0 else 0));p.keyframe_insert(data_path='rotation_euler',frame=frame)
        for s,label in [(1,'L'),(-1,'R')]:
            p=rig.pose.bones['pectoral'+label];p.rotation_mode='XYZ';p.rotation_euler=(math.sin(phase*.5)*.09,0,s*math.sin(phase)*.07);p.keyframe_insert(data_path='rotation_euler',frame=frame)
        p=rig.pose.bones['jaw'];p.rotation_mode='XYZ';p.rotation_euler=(.32*max(0,math.sin(phase)) if name in ('bite','hooked') else .005*math.sin(phase),0,0);p.keyframe_insert(data_path='rotation_euler',frame=frame)
    action['semantic']=name
rig.animation_data.action=bpy.data.actions.get('idle');scene.frame_start=1;scene.frame_end=61;scene.frame_set(1)

# Preserve source before export. FBX includes all LODs, skeleton and seven actions.
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in lods:o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(GAME/'Bluefin.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=.5,path_mode='RELATIVE')
for o in lods:o.hide_set(o!=body)

def area(name,loc,power,color,size,target=(.1,0,0)):
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.color=color;data.shape='DISK';data.size=size
    obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);obj.location=loc;obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler();return obj
area('Softbox',(-1.3,-2.1,3.1),240,(.84,.94,1),3)
area('WarmFill',(-.5,2,1.3),180,(1,.83,.62),2.5)
area('BlueRim',(1.8,.8,1.6),290,(.30,.65,1),2)
floor=material('Studio',(.026,.055,.064),.6)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.79));plane=bpy.context.object;plane.name='PreviewFloor';plane.data.materials.append(floor)
camera=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',camera);scene.collection.objects.link(cam);scene.camera=cam;camera.type='ORTHO';camera.ortho_scale=3.65
def view(loc,target=(.15,0,0)):
    cam.location=loc;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
view((-1.2,-5,1.05));scene.render.filepath='//previews/three-quarter.png'
for im in [base,normal,rough,finmap]:im.filepath=bpy.path.relpath(im.filepath,start=str(OUT))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Bluefin.blend'))
report={'status':'CANDIDATE','species':'Caranx melampygus','source':'tools/art_bluefin.py','blender':bpy.app.version_string,'lods':{},'actions':[a.name for a in bpy.data.actions],'bones':[b.name for b in rig.data.bones],'maps':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in GAME.glob('*.png')},'artAcceptance':'NOT_ACCEPTED','note':'Original authoring candidate; scientific, runtime, deformation, LOD and human visual gates remain separate.'}
for o in lods:
    o.data.calc_loop_triangles();report['lods'][o.name]={'triangles':len(o.data.loop_triangles),'vertices':len(o.data.vertices),'uvLayers':len(o.data.uv_layers)}
(OUT/'build-report.json').write_text(json.dumps(report,indent=2)+'\n')
if '--no-preview' not in sys.argv:
    scene.render.filepath=str(PREVIEW/'three-quarter.png')
    bpy.ops.render.render(write_still=True)
    view((0,-5,.04));scene.render.filepath=str(PREVIEW/'side.png');bpy.ops.render.render(write_still=True)
    view((.2,-.02,5));scene.render.filepath=str(PREVIEW/'top.png');bpy.ops.render.render(write_still=True)
print('BLUEFIN_ART_BUILD_OK '+json.dumps(report['lods']))
