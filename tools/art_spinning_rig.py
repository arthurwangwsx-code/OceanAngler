"""原创纺车轮钓组候选：外壳、导环、绑线、线杯、转子、摇柄和无倒刺拟饵。
Blender几何与程序贴图；不包含第三方模型、商标或照片。
"""
import bpy,bmesh,math,json,hashlib,sys
from pathlib import Path
from mathutils import Vector,Matrix
import numpy as np
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art/source/spinning-rig';GAME=ROOT/'game/Assets/OceanAngler/Art/SpinningRig';PREVIEW=ROOT/'.local/art/spinning-rig'
for p in [OUT,GAME,PREVIEW]:p.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.context.preferences.filepaths.save_version=0;scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1440;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX';scene.world.color=(.17,.17,.17)
groups={k:[] for k in ['Rod','Housing','Rotor','Spool','Crank','Lure','LineRoller']}
def empty(name,pos=(0,0,0),parent=None):
 o=bpy.data.objects.new(name,None);scene.collection.objects.link(o);o.location=pos
 if parent:o.parent=parent
 return o
source=empty('SpinningRigSource')
def image(name,data,noncolor=False):
 if data.ndim==2:data=np.repeat(data[:,:,None],3,axis=2)
 rgba=np.ones((*data.shape[:2],4),np.float32);rgba[:,:,:3]=np.clip(data,0,1)
 im=bpy.data.images.new(name,width=data.shape[1],height=data.shape[0],alpha=False)
 if noncolor:im.colorspace_settings.name='Non-Color'
 im.pixels.foreach_set(rgba.ravel());im.file_format='PNG';im.filepath_raw=str(GAME/(name+'.png'));im.save();return im
rng=np.random.default_rng(771);y,x=np.mgrid[0:512,0:512];noise=rng.normal(0,.013,(512,512));weave=((x//12+y//12)%2)*.025
carbon=image('Rig_Carbon',np.stack([.025+weave+noise,.045+weave+noise,.052+weave+noise],axis=2))
cork=np.zeros((512,512,3));cork[:]=[.48,.32,.16];cork+=rng.normal(0,.035,(512,512,1))
for _ in range(900):
 cx,cy=rng.integers(0,512,2);r=rng.integers(1,4);x0=max(0,cx-r*3);x1=min(512,cx+r*3);y0=max(0,cy-r*2);y1=min(512,cy+r*2);yy,xx=np.mgrid[y0:y1,x0:x1];dot=np.exp(-((xx-cx)/(r*1.5))**2-((yy-cy)/r)**2)*rng.uniform(.25,.70);cork[y0:y1,x0:x1]*=1-dot[:,:,None]
corkmap=image('Rig_Cork',cork)
def relief(name,h,strength):
 gy,gx=np.gradient(h);n=np.stack((-gx*strength,-gy*strength,np.ones_like(gx)),axis=2);n/=np.linalg.norm(n,axis=2)[:,:,None];return image(name,n*.5+.5,True)
carbonnormal=relief('Rig_Carbon_Normal',weave+noise,4);corknormal=relief('Rig_Cork_Normal',cork[:,:,0],3)
def material(name,color,metallic=0,rough=.35,texture=None,normal=None):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metallic;p.inputs['Roughness'].default_value=rough
 if texture:
  t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=texture;m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color'])
 if normal:
  t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=normal;n=m.node_tree.nodes.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=.5;m.node_tree.links.new(t.outputs['Color'],n.inputs['Color']);m.node_tree.links.new(n.outputs['Normal'],p.inputs['Normal'])
 return m
mats={'Carbon':material('Rig_Carbon',(.03,.05,.06),.15,.3,carbon,carbonnormal),'Cork':material('Rig_Cork',(.48,.32,.16),0,.65,corkmap,corknormal),'Metal':material('Rig_Metal',(.56,.64,.67),.82,.36),'Black':material('Rig_Black',(.025,.033,.037),.15,.42),'Accent':material('Rig_Accent',(.13,.42,.41),.65,.33),'Line':material('Rig_Line',(.59,.68,.43),0,.55),'Lure':material('Rig_Lure',(.23,.63,.65),.25,.23)}
def finish_normals(data,soft_edges=False):
 bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
 for edge in bm.edges:
  if edge.is_manifold:edge.smooth=soft_edges or edge.calc_face_angle()<math.radians(55)
 bm.to_mesh(data);bm.free();data.update()
def rod_weights(z):
 if z<.58:return [('Butt',1)]
 f=max(0,min(9,(z-.58)/1.77*10));i=min(8,int(f));t=f-i;return [('RodBone'+str(i),1-t),('RodBone'+str(i+1),t)]
def mesh(name,vs,fs,mat,group,uv=None,bone=None):
 data=bpy.data.meshes.new(name);data.from_pydata(vs,[],fs);data.update();obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);data.materials.append(mats[mat]);layer=data.uv_layers.new(name='UVMap')
 for p in data.polygons:
  p.use_smooth=True
  for li in p.loop_indices:
   i=data.loops[li].vertex_index;layer.data[li].uv=uv[i] if uv else ((vs[i][0]*8)%1,(vs[i][2]*5)%1)
 if group=='Rod':
  for i,p in enumerate(vs):
   for b,w in ([(bone,1)] if bone else rod_weights(p[2])):
    vg=obj.vertex_groups.get(b) or obj.vertex_groups.new(name=b);vg.add([i],w,'REPLACE')
 finish_normals(data);groups[group].append(obj);return obj
def lathe(name,profile,center,mat,group,bone=None,sides=32):
 vs=[];uv=[];fs=[];lo=profile[0][0];hi=profile[-1][0]
 for z,r in profile:
  for j in range(sides):
   a=j/sides*math.tau;vs.append((center[0]+r*math.cos(a),center[1]+r*math.sin(a),z));uv.append((j/sides,(z-lo)/max(.001,hi-lo)))
 for i in range(len(profile)-1):
  for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;fs.append((a,b,b+sides,a+sides))
 fs.append(tuple(range(sides-1,-1,-1)));fs.append(tuple((len(profile)-1)*sides+j for j in range(sides)))
 obj=mesh(name,vs,fs,mat,group,uv,bone)
 for p in obj.data.polygons:
  if abs(p.normal.z)>.9999:p.use_smooth=False
  if p.index<(len(profile)-1)*sides and p.index%sides==sides-1:
   for li in p.loop_indices:
    if obj.data.loops[li].vertex_index%sides==0:obj.data.uv_layers.active.data[li].uv.x=1
 return obj
def tube(name,points,radius,mat,group,bone=None,sides=6,closed=False):
 vs=[];fs=[];uv=[]
 for i,pt in enumerate(points):
  p=Vector(pt);d=Vector(points[(i+1)%len(points) if closed else min(len(points)-1,i+1)])-Vector(points[(i-1)%len(points) if closed else max(0,i-1)]);d.normalize();a=d.cross(Vector((0,0,1)))
  if a.length<.01:a=d.cross(Vector((0,1,0)))
  a.normalize();b=d.cross(a).normalized()
  for j in range(sides):vs.append(tuple(p+(a*math.cos(j/sides*math.tau)+b*math.sin(j/sides*math.tau))*radius));uv.append((j/sides,i/(len(points) if closed else max(1,len(points)-1))))
 for i in range(len(points) if closed else len(points)-1):
  for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;nextrow=(i+1)%len(points)*sides;fs.append((a,b,nextrow+(j+1)%sides,nextrow+j))
 if not closed:fs+=[tuple(range(sides-1,-1,-1)),tuple((len(points)-1)*sides+j for j in range(sides))]
 obj=mesh(name,vs,fs,mat,group,uv,bone)
 if not closed:
  for p in list(obj.data.polygons)[-2:]:p.use_smooth=False
 for p in list(obj.data.polygons)[:(len(points) if closed else len(points)-1)*sides]:
  for li in p.loop_indices:
   index=obj.data.loops[li].vertex_index;entry=obj.data.uv_layers.active.data[li]
   if p.index%sides==sides-1 and index%sides==0:entry.uv.x=1
   if closed and p.index//sides==len(points)-1 and index<sides:entry.uv.y=1
 obj['SmoothTubeSurface']=True;finish_normals(obj.data,True)
 return obj
def ring(name,c,r,t,mat,group,bone=None,n=32):
 return tube(name,[(c[0]+r*math.cos(i/n*math.tau),c[1]+r*math.sin(i/n*math.tau),c[2]) for i in range(n)],t,mat,group,bone,closed=True)
def box(name,center,scale,mat,group,bevel=.003):
 bpy.ops.mesh.primitive_cube_add(size=1,location=center);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 mod=o.modifiers.new('EdgeMachining','BEVEL');mod.width=bevel;mod.segments=3;bpy.ops.object.modifier_apply(modifier=mod.name)
 for v in o.data.vertices:v.co+=o.location
 o.location=(0,0,0);o.data.materials.append(mats[mat]);groups[group].append(o)
 # Projection UVs are sufficient for untextured machined metal parts.
 if not o.data.uv_layers:o.data.uv_layers.new(name='UVMap')
 for p in o.data.polygons:p.use_smooth=max(abs(v) for v in p.normal)<.9999
 finish_normals(o.data)
 return o
def ellipsoid(name,center,scale,mat,group):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,location=center);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 for v in o.data.vertices:v.co+=o.location
 o.location=(0,0,0);o.data.materials.append(mats[mat]);groups[group].append(o)
 for p in o.data.polygons:p.use_smooth=True
 return o

roller_center=(.043,-.145,.565)
roller_profile=[(-.0045,.004),(-.0042,.006),(-.0035,.0062),(-.0027,.0058),(-.0015,.0047),(-.001,.0045),(.001,.0045),(.0015,.0047),(.0027,.0058),(.0035,.0062),(.0042,.006),(.0045,.004),(.0045,.0019),(-.0045,.0019)]
def axle_shell(name,profile,center,group,sides=32,indicator=False):
 vs=[];fs=[];uv=[]
 for i,(axial,radius) in enumerate(profile):
  for j in range(sides):
   a=j/sides*math.tau;vs.append((center[0]+axial,center[1]+radius*math.cos(a),center[2]+radius*math.sin(a)));uv.append((j/sides,i/len(profile)))
 for i in range(len(profile)):
  for j in range(sides):fs.append((i*sides+j,i*sides+(j+1)%sides,((i+1)%len(profile))*sides+(j+1)%sides,((i+1)%len(profile))*sides+j))
 obj=mesh(name,vs,fs,'Metal',group,uv)
 if indicator:obj.data.materials.append(mats['Accent'])
 for p in obj.data.polygons:
  if abs(p.normal.x)>.9999:p.use_smooth=False
  if indicator and p.index//sides==11 and p.index%sides<max(1,sides//12):p.material_index=1
 return obj

# Actual grip, seat, collar threads and tapered blank; measurements in metres.
lathe('ContinuousGripCore',[(.016,.0105),(.58,.0095),(.605,.0082)],(0,0),'Carbon','Rod','Butt')
lathe('ButtCap',[(0,.014),(.003,.018),(.017,.018),(.023,.016)],(0,0),'Black','Rod','Butt')
lathe('RearCork',[(.023,.016),(.035,.018),(.16,.017),(.18,.015)],(0,0),'Cork','Rod','Butt')
lathe('GripTrim',[(.18,.015),(.185,.016),(.193,.016),(.198,.013)],(0,0),'Accent','Rod','Butt')
lathe('RearGrip',[(.205,.013),(.214,.017),(.265,.017),(.28,.014)],(0,0),'Cork','Rod','Butt')
lathe('ReelSeat',[(.28,.014),(.30,.015),(.48,.015),(.50,.013)],(0,0),'Carbon','Rod','Butt')
for z in [.30,.46]:lathe('SeatCollar',[(z,.015),(z+.003,.018),(z+.018,.018),(z+.021,.015)],(0,0),'Metal','Rod','Butt')
for i in range(15):ring('SeatThread',(0,0,.472+i*.0017),.0145,.00065,'Black','Rod','Butt',24)
lathe('ForeCork',[(.51,.012),(.52,.016),(.57,.015),(.59,.010)],(0,0),'Cork','Rod','Butt')
profile=[(.58+i/38*1.77,.0085*(1-i/38)**1.1+.0012) for i in range(39)]
lathe('CarbonBlank',profile,(0,0),'Carbon','Rod',sides=16)

guide_specs=[(.72,.053,.028),(1.03,.038,.022),(1.31,.027,.016),(1.55,.020,.011),(1.76,.015,.0085),(1.94,.012,.0065),(2.10,.009,.005),(2.23,.007,.004),(2.35,.005,.003)]
guide_bones=[]
for i,(z,offset,r) in enumerate(guide_specs):
 bone='RodBone'+str(min(9,max(0,round((z-.58)/1.77*10))));guide_bones.append(bone)
 frame=ring('GuideFrame'+str(i),(0,-offset,z),r+.0013,.0011 if i<4 else .0007,'Metal','Rod',bone)
 vg=frame.vertex_groups.new(name='__GuideRing'+str(i));vg.add(list(range(len(frame.data.vertices))),1,'REPLACE')
 ring('CeramicLiner'+str(i),(0,-offset,z),r,.0009 if i<4 else .0006,'Black','Rod',bone)
 radius=.0085*max(0,1-(z-.58)/1.77)**1.1+.0012
 for s in [-1,1]:tube('GuideFoot'+str(i),[(0,-radius,z+s*.019),(s*r*.5,-offset+r*.7,z)],.0012 if i<4 else .0007,'Metal','Rod',bone)
 for side in [-1,1]:
  points=[]
  for j in range(55):
   t=j/54;a=t*math.tau*6;points.append((math.cos(a)*(radius+.0005),math.sin(a)*(radius+.0005),z+side*.023+(t-.5)*.012))
  tube('GuideBinding'+str(i),points,.0003,'Accent','Rod',bone,4)

# Original spinning reel casing and stem. No branded silhouette or markings.
box('MountFoot',(0,-.020,.395),(.023,.014,.135),'Metal','Housing')
tube('Stem',[(0,-.026,.41),(0,-.075,.405),(0,-.118,.365)],.011,'Metal','Housing',sides=12)
ellipsoid('Gearcase',(0,-.139,.38),(.041,.032,.071),'Carbon','Housing')
box('SidePlate',(-.034,-.137,.38),(.009,.050,.089),'Metal','Housing',.004)
box('RearProtection',(0,-.138,.315),(.044,.040,.022),'Black','Housing',.006)
for z in [.345,.407]:
 for side in [-1,1]:ellipsoid('CaseFastener',(side*.040,-.142,z),(.003,.003,.003),'Black','Housing')
pinion_profile=[(.417,.018),(.469,.018),(.471,.004),(.522,.004)]
lathe('Pinion',pinion_profile,(0,-.145),'Metal','Housing',sides=12)
for i in range(4):box('CaseVent'+str(i),(.004,-.169,.35+i*.013),(.027,.0015,.003),'Black','Housing',.0005)

rotor=empty('RotorPivot',(0,-.145,.47),source);spool=empty('SpoolPivot',(0,-.145,.52),source);crank=empty('CrankPivot',(-.045,-.142,.38),source)
bail=empty('BailPivot',(0,0,.081),rotor);bail_parts=[]
lathe('RotorCup',[(.448,.020),(.456,.036),(.475,.038),(.480,.034)],(0,-.145),'Carbon','Rotor')
for s in [-1,1]:
 tube('RotorArm',[(s*.031,-.145,.469),(s*.041,-.145,.533),(s*.042,-.145,.551)],.004,'Metal','Rotor',sides=10)
 ellipsoid('BailHinge',(s*.042,-.145,.551),(.007,.006,.0045) if s>0 else (.006,.008,.008),'Black','Rotor')
wire=[(.042*math.cos(t),-.145+.025*math.sin(t),.551+.040*math.sin(t)) for t in np.linspace(0,math.pi,41)]
bail_mesh=tube('BailWire',wire,.0016,'Metal','Rotor',sides=8);groups['Rotor'].remove(bail_mesh);bail_parts.append(bail_mesh)
bpy.context.view_layer.update();roller=empty('LineRollerPivot',roller_center,bail);roller.matrix_world=Matrix.Translation(roller_center)
axle_shell('GroovedLineRoller',roller_profile,roller_center,'LineRoller',32,True)
pickup=tube('PickupFork',[(.042,-.145,.551),(.052,-.145,.565)],.003,'Metal','Rotor',sides=10);groups['Rotor'].remove(pickup);bail_parts.append(pickup)
pickup=tube('RollerSpindle',[(.035,-.145,.565),(.054,-.145,.565)],.0015,'Metal','Rotor',sides=12);groups['Rotor'].remove(pickup);bail_parts.append(pickup)
for side in [-1,1]:
 x=.043+side*.006
 pickup=axle_shell('RollerBearingCheek',[(-.0005,.0068),(.0005,.0068),(.0005,.0016),(-.0005,.0016)],(x,-.145,.565),'Rotor',24);groups['Rotor'].remove(pickup);bail_parts.append(pickup)
spool_profile=[(.497,.020),(.501,.031),(.538,.031),(.543,.034),(.548,.035),(.551,.033)]
lathe('SpoolCore',spool_profile,(0,-.145),'Metal','Spool',sides=48)
def wound_line(name,steps_per_turn,sides,radius):
 points=[(radius*math.cos(a),-.145+radius*math.sin(a),.504+a/math.tau*.00125) for a in np.linspace(0,math.tau*25,25*steps_per_turn+1)]
 return tube(name,points,.00055,'Line','Spool',sides=sides)
wound_line('ContinuousWoundLine',24,6,.03185)
lathe('DragKnob',[(.549,.023),(.555,.024),(.560,.021),(.563,.019)],(0,-.145),'Black','Spool',sides=32)
vs=[];fs=[]
for z,outer in [(.552,True),(.558,True),(.558,False),(.552,False)]:
 for i in range(72):
  a=i/72*math.tau;r=.023+.0015*(.5+.5*math.cos(a*18)) if outer else .0215;vs.append((r*math.cos(a),-.145+r*math.sin(a),z))
for ring_index in range(4):
 for i in range(72):fs.append((ring_index*72+i,ring_index*72+(i+1)%72,(ring_index+1)%4*72+(i+1)%72,(ring_index+1)%4*72+i))
mesh('DragKnurl',vs,fs,'Accent','Spool')
tube('CrankShaft',[(-.045,-.142,.38),(-.074,-.142,.38)],.006,'Metal','Crank',sides=12)
tube('CrankArm',[(-.074,-.142,.38),(-.085,-.167,.405),(-.094,-.182,.43)],.004,'Metal','Crank',sides=10)
ellipsoid('RubberHandle',(-.105,-.182,.43),(.017,.011,.014),'Black','Crank')
ellipsoid('HandleCap',(-.122,-.182,.43),(.0025,.007,.008),'Accent','Crank')

# A small solid minnow, lip, split rings and two single barbless hooks.
lure=empty('LurePivot',(0,-.13,2.12),source)
ellipsoid('MinnowBody',(0,-.13,2.12),(.008,.010,.043),'Lure','Lure')
ellipsoid('MinnowBelly',(0,-.136,2.12),(.006,.005,.038),'Metal','Lure')
for s in [-1,1]:ellipsoid('MinnowEye',(s*.0075,-.131,2.145),(.0018,.0028,.0028),'Black','Lure')
box('DivingLip',(0,-.140,2.155),(.013,.014,.0025),'Lure','Lure',.001)
for z in [2.090,2.126]:
 ring('SplitRing',(0,-.141,z),.0035,.0006,'Metal','Lure',n=20)
 points=[]
 for t in np.linspace(0,math.pi*1.45,22):points.append((math.sin(t)*.0055,-.15-math.cos(t)*.0055,z))
 tube('BarblessHook',points,.00065,'Metal','Lure',sides=5)

# Join each functional assembly; retain movable pivots and three real mesh LODs.
def join_group(objects,name,pivot):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
 o.parent=pivot;o.matrix_parent_inverse=pivot.matrix_world.inverted()
 return o
def solid_volume(data):
 bm=bmesh.new();bm.from_mesh(data);bm.verts.ensure_lookup_table();origin=bm.verts[0].co.copy() if bm.verts else Vector()
 for v in bm.verts:v.co-=origin
 valid=all(e.is_manifold and e.is_contiguous for e in bm.edges) and all(f.calc_area()>1e-14 for f in bm.faces)
 volume=bm.calc_volume(signed=True);genus=(2-(len(bm.verts)-len(bm.edges)+len(bm.faces)))//2;bm.free();return valid,volume,genus
reduction_guards=[]
def reduce_piece(original,ratio):
 original.data.calc_loop_triangles();count=len(original.data.loop_triangles);_,volume,genus=solid_volume(original.data);minimum=24 if genus>0 else 8
 trial=min(1,max(ratio,(minimum+.01)/count));attempts=0
 while True:
  obj=original.copy();obj.data=original.data.copy();scene.collection.objects.link(obj)
  if trial<.9999:
   bpy.context.view_layer.objects.active=obj;m=obj.modifiers.new('SolidDistanceReduction','DECIMATE');m.ratio=trial;bpy.ops.object.modifier_apply(modifier=m.name);finish_normals(obj.data,bool(original.get('SmoothTubeSurface',False)))
  valid,result,_=solid_volume(obj.data)
  if valid and result>max(1e-15,volume*.1):break
  bpy.data.objects.remove(obj,do_unlink=True);attempts+=1
  if trial>=1:raise RuntimeError('Source fitting is not a positive solid: '+original.name)
  trial=min(1,trial*1.5)
 if attempts:reduction_guards.append(dict(part=original.name,requested=ratio,retained=trial,retries=attempts))
 return obj
all_lods=[];lod_report={};assembled={}
assemblies=[('Rod',groups['Rod'],source),('Housing',groups['Housing'],source),('Rotor',groups['Rotor'],rotor),('Spool',groups['Spool'],spool),('Crank',groups['Crank'],crank),('Lure',groups['Lure'],lure),('BailWire',bail_parts,bail),('LineRoller',groups['LineRoller'],roller)]
for stem,source_objects,pivot in assemblies:
 objects=list(source_objects)
 pieces=[objects]
 for level,ratio in [(1,.43),(2,.17)]:
  if stem=='LineRoller':current=[axle_shell('GroovedRollerDistance'+str(level),roller_profile,roller_center,'LineRoller',20 if level==1 else 12,True)]
  elif stem=='Housing':current=[lathe('PinionDistance'+str(level),pinion_profile,(0,-.145),'Metal','Housing',sides=8 if level==1 else 6) if o.name=='Pinion' else reduce_piece(o,ratio) for o in objects]
  elif stem=='Spool':
   current=[]
   for original in objects:
    if original.name=='ContinuousWoundLine':
     # Uniform angular samples keep all25 turns. The coarse path receives a
     # small outward offset so its chords cannot sink into the preserved core.
     current.append(wound_line('ContinuousWoundLineDistance'+str(level),20 if level==1 else 13,3,.03185 if level==1 else .0324))
    elif original.name=='SpoolCore':current.append(lathe('SpoolCoreDistance'+str(level),spool_profile,(0,-.145),'Metal','Spool',sides=24 if level==1 else 12))
    else:current.append(reduce_piece(original,ratio))
  else:current=[reduce_piece(o,ratio) for o in objects]
  pieces.append(current)
 for level,current in enumerate(pieces):
  obj=join_group(current,stem+'_LOD'+str(level),pivot);assembled[stem,level]=obj
  for layer in obj.data.uv_layers:
   for loop in layer.data:loop.uv=(max(0,min(1,loop.uv.x)),max(0,min(1,loop.uv.y)))
  obj.hide_render=level>0;obj.hide_set(level>0);obj.data.calc_loop_triangles();lod_report[obj.name]=len(obj.data.loop_triangles);all_lods.append(obj)
rod=assembled['Rod',0]

arm=bpy.data.armatures.new('RodSkeleton');rig=bpy.data.objects.new('RodRig',arm);scene.collection.objects.link(rig);rig.parent=source;bpy.context.view_layer.objects.active=rig;bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
b=arm.edit_bones.new('Butt');b.head=(0,0,0);b.tail=(0,0,.58)
for i in range(10):
 b=arm.edit_bones.new('RodBone'+str(i));b.head=(0,0,.58+1.77*i/10);b.tail=(0,0,.58+1.77*(i+1)/10);b.parent=arm.edit_bones['Butt' if i==0 else 'RodBone'+str(i-1)];b.use_connect=True
bpy.ops.object.mode_set(mode='OBJECT')
bpy.context.view_layer.update()
for o in all_lods:
 if o.name.startswith('Rod_'):
  m=o.modifiers.new('RodBend','ARMATURE');m.object=rig;o.parent=rig
for i,(z,offset,r) in enumerate(guide_specs):
 marker=empty('Guide'+str(i),(0,-offset,z));marker.parent=rig;marker.parent_type='BONE';marker.parent_bone=guide_bones[i];marker.matrix_world=Matrix.Translation((0,-offset,z))
direction=Vector((0,guide_specs[0][1]*-1-roller_center[1],guide_specs[0][0]-roller_center[2]));distance=direction.length;direction.normalize();r=.0045+.00035
normal=direction*(r/distance)+Vector((1,0,0)).cross(direction)*math.sqrt(1-(r/distance)**2);line_exit=Vector(roller_center)+normal*r
for name,pos,parent in [('ButtAnchor',(0,0,0),source),('TipAnchor',(0,0,2.35),source),('FrontAnchor',(0,-.1,.5),source),('RotorAxis',(0,-.145,.52),rotor),('CrankAxis',(-.065,-.142,.38),crank),('BailAxis',(.02,-.145,.551),bail),('LineExit',line_exit,bail),('LineRollerAxis',(.053,-.145,.565),roller),('RollerSurface',(.043,-.1495,.565),roller),('SpoolLineRadius',(.03185,-.145,.52),spool),('SpoolLipCenter',(0,-.145,.548),spool),('SpoolLipSurface',(.035,-.145,.548),spool),('LureNose',(0,-.13,2.162),lure)]:
 o=empty(name);o.parent=parent;o.matrix_world=Matrix.Translation(pos)
curve=bpy.data.curves.new('StudioLine','CURVE');curve.dimensions='3D';curve.bevel_depth=.00035;curve.bevel_resolution=2;curve.use_fill_caps=True;spline=curve.splines.new('POLY');points=[line_exit]+[(0,-s[1],s[0]) for s in guide_specs]+[(0,-.13,2.162)];spline.points.add(len(points)-1)
for p,co in zip(spline.points,points):p.co=(*co,1)
studio=bpy.data.objects.new('StudioLine',curve);scene.collection.objects.link(studio);studio.parent=source;curve.materials.append(mats['Line'])

bpy.ops.object.select_all(action='DESELECT')
for o in list(source.children_recursive)+[source]:
 if o.name!='StudioLine':o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=source
bpy.ops.export_scene.fbx(filepath=str(GAME/'SpinningRig.fbx'),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='RELATIVE')
for o in all_lods:o.hide_set(not o.name.endswith('LOD0'))

def area(name,loc,power,color,size):
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.color=color;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1.1))-o.location).to_track_quat('-Z','Y').to_euler()
area('Key',(-1.2,-2,2),150,(.9,.96,1),2);area('Fill',(1,1,1.6),140,(1,.85,.66),2);area('Rim',(0,1,2.8),180,(.4,.7,1),1.5)
floor=material('ReviewFloor',(.025,.045,.052),0,.65);bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.03));bpy.context.object.data.materials.append(floor)
d=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',d);scene.collection.objects.link(cam);scene.camera=cam;d.type='ORTHO';d.ortho_scale=3.8;cam.location=(-2,-4,1.6);cam.rotation_euler=(Vector((0,0,1.16))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath='//previews/whole-rig.png'
for im in [carbon,corkmap,carbonnormal,corknormal]:im.filepath=bpy.path.relpath(im.filepath,start=str(OUT))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SpinningRig.blend'))
report=dict(status='CANDIDATE',units='metres',length=2.35,lods=lod_report,bones=[b.name for b in arm.bones],guides=[dict(name='Guide'+str(i),bone=guide_bones[i],position=[0,-s[1],s[0]],radius=s[2]) for i,s in enumerate(guide_specs)],pivots=['RotorPivot','SpoolPivot','CrankPivot','BailPivot','LurePivot','LineRollerPivot'],detail=dict(components=8,lod0Budget=43420,winding='one visible continuous25-turn layer, not dynamic line volume',roller='hollow grooved shell, independent pivot and bearing spindle',optics='stock original materials; human/device acceptance open',solidReductionGuards=reduction_guards),artAcceptance='NOT_ACCEPTED',source='tools/art_spinning_rig.py')
(OUT/'build-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('SPINNING_RIG_BUILD_OK '+json.dumps({str(i):sum(n for k,n in lod_report.items() if k.endswith('LOD'+str(i))) for i in range(3)}))
