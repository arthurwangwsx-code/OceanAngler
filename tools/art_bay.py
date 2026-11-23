"""原创珊瑚海湾候选：地形、岩礁、三种珊瑚形态、海草、码头及系泊艇。"""
import bpy,bmesh,math,json,random
from pathlib import Path
from mathutils import Vector
import numpy as np

ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art/source/coral-bay';GAME=ROOT/'game/Assets/OceanAngler/Art/CoralBay'
for p in [OUT,GAME]:p.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1440;scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX'
rand=random.Random(9147);rng=np.random.default_rng(9147);groups={};materials={}
source=bpy.data.objects.new('CoralBaySource',None);scene.collection.objects.link(source)

def texture(name,data,noncolor=False):
 if data.ndim==2:data=np.repeat(data[:,:,None],3,axis=2)
 rgba=np.ones((*data.shape[:2],4),np.float32);rgba[:,:,:3]=np.clip(data[:,:,:3],0,1)
 if data.shape[2]==4:rgba[:,:,3]=np.clip(data[:,:,3],0,1)
 im=bpy.data.images.new(name,width=data.shape[1],height=data.shape[0],alpha=data.shape[2]==4)
 if noncolor:im.colorspace_settings.name='Non-Color'
 im.pixels.foreach_set(rgba.ravel());im.file_format='PNG';im.filepath_raw=str(GAME/(name+'.png'));im.save();return im

def surface(name,color,height,rough,normal_scale=7):
 base=texture('Bay_'+name,color);gy,gx=np.gradient(height);n=np.stack((-gx*normal_scale,-gy*normal_scale,np.ones_like(gx)),axis=2);n/=np.linalg.norm(n,axis=2)[:,:,None]
 normal=texture('Bay_'+name+'_Normal',n*.5+.5,True);r=texture('Bay_'+name+'_Roughness',np.clip(rough+height*.15,0,1),True)
 packed=np.zeros((*height.shape,4));packed[:,:,3]=1-np.clip(rough+height*.15,0,1);texture('Bay_'+name+'_MetallicSmoothness',packed,True)
 return material(name,(1,1,1),rough,base=base,normal=normal,roughmap=r)

def material(name,color,rough=.65,metal=0,base=None,normal=None,roughmap=None):
 m=bpy.data.materials.new('Bay_'+name);m.use_nodes=True;m.diffuse_color=(*color,1);p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
 if base:
  t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=base;m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color'])
 if normal:
  t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=normal;n=m.node_tree.nodes.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=.4;m.node_tree.links.new(t.outputs['Color'],n.inputs['Color']);m.node_tree.links.new(n.outputs['Normal'],p.inputs['Normal'])
 if roughmap:
  t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=roughmap;m.node_tree.links.new(t.outputs['Color'],p.inputs['Roughness'])
 materials[name]=m;return m

y,x=np.mgrid[0:512,0:512]/512
noise=rng.normal(0,.035,(512,512))
# Colour carries grains and broad deposits; ripples mostly belong in the normal
# map. A separate RNG preserves every other asset's existing random sequence.
sand_rng=np.random.default_rng(91471)
def sand_noise(cells):
 grid=sand_rng.random((cells,cells));u=x*cells;v=y*cells;ix=np.floor(u).astype(int);iy=np.floor(v).astype(int);fx=u-ix;fy=v-iy;fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy)
 return (grid[iy%cells,ix%cells]*(1-fx)+grid[iy%cells,(ix+1)%cells]*fx)*(1-fy)+(grid[(iy+1)%cells,ix%cells]*(1-fx)+grid[(iy+1)%cells,(ix+1)%cells]*fx)*fy
deposit=sand_noise(4);patch=sand_noise(8);fine=sand_noise(32);grains=sand_rng.normal(0,.006,(512,512))
phase=y*18+np.sin(x*math.tau*3)*.16+(deposit-.5)*1.8+(patch-.5)*.4
ripple=np.sin(phase*math.tau)*.018*(.35+.65*deposit)
h=ripple+(fine-.5)*.004
tone=(deposit-.5)*.035+(patch-.5)*.012+grains+ripple*.08
surface('Sand',np.stack([.66+tone,.59+tone*.92,.45+tone*.78],axis=2),h,.91,normal_scale=24)
h=np.zeros((512,512))
for frequency,amp in [(3,.12),(7,.07),(19,.035),(61,.018)]:h+=amp*np.sin(x*math.tau*frequency+np.sin(y*math.tau*(frequency+2)))*np.cos(y*math.tau*frequency)
h+=noise;surface('Limestone',np.stack([.30+h,.34+h*.8,.29+h*.7],axis=2),h,.84)
grain=np.sin((x*45+np.sin(y*8)*.5)*math.tau)*.025+np.sin(x*math.tau*130+y*8)*.012+noise*.25
for k in range(7):
 cx,cy=rng.random(2);rad=np.sqrt(((x-cx)*2.5)**2+((y-cy)*.6)**2);grain+=np.sin(rad*180)*np.exp(-rad*25)*.05
surface('Wood',np.stack([.30+grain,.22+grain*.9,.14+grain*.7],axis=2),grain,.7)
for name,color,rough in [('BranchCoral',(.57,.32,.22),.78),('PlateCoral',(.43,.46,.25),.8),('DomeCoral',(.40,.31,.22),.86)]:
 h=(np.cos(x*math.tau*28+np.sin(y*math.tau*9)*2)*np.cos(y*math.tau*23))*.045+noise*.25
 rgb=np.stack([c+h*.4 for c in color],axis=2);surface(name,rgb,h,rough)
material('Seagrass',(.12,.27,.12),.68);material('Algae',(.16,.25,.12),.85);material('Rope',(.40,.34,.23),.9)
material('BoatHull',(.67,.73,.70),.38);material('BoatTrim',(.15,.38,.40),.36);material('Metal',(.28,.33,.33),.38,.72)
pattern=np.power(np.clip(1-np.abs(np.sin(x*math.tau*4+np.sin(y*math.tau*3)*.6)+np.cos(y*math.tau*5+np.sin(x*math.tau*2)*.7))*3,0,1),3);texture('Bay_Caustic',pattern,True)
dot=np.ones((512,512,4));dot[:,:,3]=np.clip((.48-np.sqrt((x-.5)**2+(y-.5)**2))/.24,0,1)**2;texture('Bay_Particle',dot)
water=material('Water',(.86,.97,.99),.10);p=water.node_tree.nodes.get('Principled BSDF');p.inputs['Transmission Weight'].default_value=.96;p.inputs['IOR'].default_value=1.333
noise_node=water.node_tree.nodes.new('ShaderNodeTexNoise');noise_node.inputs['Scale'].default_value=4;noise_node.inputs['Detail'].default_value=2
geometry=water.node_tree.nodes.new('ShaderNodeNewGeometry');water.node_tree.links.new(geometry.outputs['Position'],noise_node.inputs['Vector'])
bump=water.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.22;bump.inputs['Distance'].default_value=.08;water.node_tree.links.new(noise_node.outputs['Fac'],bump.inputs['Height']);water.node_tree.links.new(bump.outputs['Normal'],p.inputs['Normal'])
# Camera rays retain refraction. Transparent shadow rays approximate daylight transmission,
# matching the runtime water's non-shadow-casting surface without a costly caustic solver.
lightpath=water.node_tree.nodes.new('ShaderNodeLightPath');transparent=water.node_tree.nodes.new('ShaderNodeBsdfTransparent');mix=water.node_tree.nodes.new('ShaderNodeMixShader')
water.node_tree.links.new(lightpath.outputs['Is Shadow Ray'],mix.inputs[0]);water.node_tree.links.new(p.outputs[0],mix.inputs[1]);water.node_tree.links.new(transparent.outputs[0],mix.inputs[2]);water.node_tree.links.new(mix.outputs[0],water.node_tree.nodes.get('Material Output').inputs['Surface'])

def mesh(name,vs,faces,mat,group,uv=None):
 data=bpy.data.meshes.new(name);data.from_pydata(vs,[],faces);data.update();obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);obj.parent=source;data.materials.append(materials[mat]);groups.setdefault(group,[]).append(obj)
 layer=data.uv_layers.new(name='UVMap')
 for face in data.polygons:
  face.use_smooth=True
  for li in face.loop_indices:
   vi=data.loops[li].vertex_index;co=vs[vi];layer.data[li].uv=uv[vi] if uv else (co[0]*.7,co[2]*.7)
 return obj

def weld_solid(obj):
 # Merge UV-seam/pole vertices before decimation. UVs remain per face corner;
 # no two-sided material or normal repair conceals incorrectly oriented faces.
 bm=bmesh.new();bm.from_mesh(obj.data)
 bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001)
 bm.to_mesh(obj.data);bm.free();obj.data.update()
 return obj

def tube(name,points,radii,mat,group,sides=8):
 vs=[];uv=[];faces=[];distance=0
 for i,co in enumerate(points):
  pt=Vector(co);direction=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)]);direction.normalize();u=direction.cross(Vector((0,0,1)))
  if u.length<.01:u=direction.cross(Vector((0,1,0)))
  u.normalize();v=direction.cross(u).normalized();r=radii[i] if isinstance(radii,list) else radii
  if i:distance+=(pt-Vector(points[i-1])).length
  for j in range(sides+1):
   a=j/sides*math.tau;vs.append(tuple(pt+(u*math.cos(a)+v*math.sin(a))*r));uv.append((j/sides,distance*2))
 for i in range(len(points)-1):
  for j in range(sides):a=i*(sides+1)+j;faces.append((a,a+1,a+sides+2,a+sides+1))
 faces+=[tuple(range(sides,-1,-1)),tuple((len(points)-1)*(sides+1)+j for j in range(sides+1))]
 return mesh(name,vs,faces,mat,group,uv)

def box(name,center,size,mat,group,bevel=.02):
 bpy.ops.mesh.primitive_cube_add(size=1,location=center);o=bpy.context.object;o.name=name;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 m=o.modifiers.new('WornEdges','BEVEL');m.width=bevel;m.segments=2;bpy.ops.object.modifier_apply(modifier=m.name)
 for v in o.data.vertices:v.co+=o.location
 o.location=(0,0,0);o.parent=source;o.data.materials.append(materials[mat]);groups.setdefault(group,[]).append(o)
 layer=o.data.uv_layers.active or o.data.uv_layers.new(name='UVMap')
 for p in o.data.polygons:
  for li in p.loop_indices:
   co=o.data.vertices[o.data.loops[li].vertex_index].co;layer.data[li].uv=(co.y*3+co.z*.15,co.x*.5) if size[0]>size[1] else (co.x*3,co.y*.5+co.z*.15)
 return o

def seabed(x,y):
 # An open sandy corridor merges into one continuous western landmass. The
 # curved cove and broad slope replace the former isolated Gaussian sand ridge.
 floor=-1.25-.018*(y+5)+.16*math.sin(x*.32)*math.cos(y*.24)+.07*math.sin(y*1.7+x*.2)
 shore=-18+2*math.sin(y*.045+.6)+1.4*math.sin(y*.12+1.2)-5*math.exp(-((y-14)/13)**2)
 u=max(0,min(1,(shore-x+10)/22));blend=u*u*(3-2*u)
 land=6.7+.35*math.sin(y*.08)+.2*math.sin(x*.14)*math.cos(y*.08)
 return floor*(1-blend)+land*blend

vs=[];uv=[];faces=[]
xs=[-180,-110,-70,-46]+list(range(-32,33))+[46,70,110,180]
ys=[-90,-56,-32]+list(range(-18,47))+[64,90,140,210,270]
for b in ys:
 for a in xs:vs.append((a,b,seabed(a,b)));uv.append((a/4,b/4))
for j in range(len(ys)-1):
 for i in range(len(xs)-1):a=j*len(xs)+i;faces.append((a,a+1,a+len(xs)+1,a+len(xs)))
mesh('SandShelf',vs,faces,'Sand','Terrain',uv)

rock_positions=[]
for i in range(24):
 a,b,s=[(-3.4,4.5,1.3),(3.5,6,1.4),(5.3,9,1.8),(-5,10,1.3),(7.3,13,2.0),(-7,17,1.5)][i] if i<6 else (rand.choice([-1,1])*rand.uniform(4,18),rand.uniform(7,32),rand.uniform(.7,2.1));z=seabed(a,b);rock_positions.append((a,b,z,s))
 vs=[];faces=[];uv=[];segments=24;rings=12
 for j in range(rings+1):
  t=j/rings*math.pi
  for k in range(segments+1):
   p=k/segments*math.tau;rough=1+.13*math.sin(p*3+i)*math.sin(t*4)+.08*math.cos(p*7-t*5+i);r=math.sin(t)*s*rough
   vs.append((a+r*math.cos(p),b+r*math.sin(p)*.78,z+.15+math.cos(t)*s*.66+math.sin(p*4+t*7)*s*.04*math.sin(t)));uv.append((k/segments*2,j/rings*2))
 for j in range(rings):
  for k in range(segments):q=j*(segments+1)+k;faces.append((q,q+segments+1,q+segments+2,q+1))
 weld_solid(mesh('ErodedLimestone'+str(i),vs,faces,'Limestone','Reef',uv))

def branch(start,direction,length,radius,depth):
 end=Vector(start)+Vector(direction).normalized()*length;mid=Vector(start).lerp(end,.5)+Vector((rand.uniform(-.04,.04),rand.uniform(-.04,.04),0))
 tube('CoralBranch',[start,mid,end],[radius,radius*.9,radius*.72],'BranchCoral','BranchingCoral',7)
 if depth:
  for k in range(3):
   angle=rand.random()*math.tau;d=Vector((math.cos(angle)*.65,math.sin(angle)*.65,rand.uniform(.7,1.1)));branch(end,d,length*.64,radius*.67,depth-1)

for i,(a,b,z,s) in enumerate(rock_positions):
 top=z+.15+s*.66
 if i%3==0:branch(Vector((a,b,top)),Vector((0,0,1)),.55*s,.045*s,3)
 elif i%3==1:
  for level in range(3):
   radius=s*(.37-level*.06);height=top+.07+level*.19
   tube('PlateStem',[(a,b,top-.015),(a,b,height)],.035*s,'PlateCoral','PlateCoral')
   vs=[(a,b,height)];uv=[(.5,.5)];faces=[];segs=64
   for j in range(segs+1):
    t=j/segs*math.tau;r=radius*(1+.05*math.sin(t*7+i));vs.append((a+math.cos(t)*r,b+math.sin(t)*r,height+.035*math.sin(t*5)+r*.08));uv.append((.5+math.cos(t)*.5,.5+math.sin(t)*.5))
   for j in range(segs):faces.append((0,j+1,j+2))
   plate=mesh('CoralPlate',vs,faces,'PlateCoral','PlateCoral',uv);solid=plate.modifiers.new('LivingRim','SOLIDIFY');solid.thickness=.025; bpy.context.view_layer.objects.active=plate;bpy.ops.object.modifier_apply(modifier=solid.name)
 else:
  vs=[];faces=[];uv=[];segments=48;rings=14
  for j in range(rings+1):
   t=j/rings*math.pi*.55
   for k in range(segments+1):
    p=k/segments*math.tau;r=s*.38*math.sin(t)*(1+.035*math.sin(p*12+math.sin(t*8)*2));vs.append((a+r*math.cos(p),b+r*math.sin(p),top-.02+s*.29*math.cos(t)+.012*math.sin(p*18+t*9)*math.sin(t)));uv.append((k/segments,j/rings))
  for j in range(rings):
   for k in range(segments):q=j*(segments+1)+k;faces.append((q,q+segments+1,q+segments+2,q+1))
  # Closed, buried base; the full solid survives every LOD simplification.
  faces.append(tuple(rings*(segments+1)+k for k in range(segments,-1,-1)))
  weld_solid(mesh('MoundedCoral',vs,faces,'DomeCoral','DomeCoral',uv))

# Curved, tapered ribbons rather than cylinders; split clumps leave the play corridor open.
vs=[];faces=[];uv=[]
for clump in range(260):
 a=rand.choice([-1,1])*rand.uniform(2.5,22);b=rand.uniform(-1,35);z=seabed(a,b)
 for blade in range(rand.randint(5,9)):
  angle=rand.random()*math.tau;length=rand.uniform(.22,.65);lean=rand.uniform(.08,.30);width=rand.uniform(.018,.035);base=len(vs)
  for k in range(6):
   t=k/5;center=Vector((a+math.cos(angle)*lean*t*t,b+math.sin(angle)*lean*t*t,z+length*t));side=Vector((-math.sin(angle),math.cos(angle),0))*width*(1-t*.94)
   for sign in [-1,1]:vs.append(tuple(center+side*sign));uv.append(((sign+1)/2,t))
  for k in range(5):q=base+k*2;faces.append((q,q+1,q+3,q+2))
mesh('SeagrassMeadow',vs,faces,'Seagrass','Seagrass',uv)

# Full pier supports, beams, individual planks, posts, rope rails and fasteners.
for a in [-5.8,-4.2]:
 for b in [-3,1,5,8]:
  tube('PierPile',[(a,b,seabed(a,b)-.3),(a,b,5.5)],[.16,.13],'Wood','Pier',12)
  tube('AlgaeTideBand',[(a,b,3.65),(a,b,4.45)],[.163,.155],'Algae','Pier',12)
 tube('RopeRail',[(a,-3,5.4),(a,-1,5.27),(a,1,5.4),(a,3,5.27),(a,5,5.4),(a,6.5,5.27),(a,8,5.4)],.019,'Rope','Pier',6)
for b in [-3,1,5,8]:box('CrossBeam',(-5,b,4.58),(2,.16,.22),'Wood','Pier')
for a in [-5.55,-4.45]:box('LongBeam',(a,2.5,4.61),(.13,11.4,.2),'Wood','Pier')
for i in range(43):
 b=-3+i*.26;box('DeckPlank',(-5,b,4.8),(2.1,.247,.11),'Wood','Pier',.009)
 for a in [-5.65,-4.35]:tube('DeckBolt',[(a,b,4.852),(a,b,4.861)],.018,'Metal','Pier',6)

# Small open research dinghy with an actual shell, gunwale, interior, benches and tether.
boat_profile=json.loads((OUT/'boat-profile.json').read_text());boat=tuple(boat_profile['origin']);rim=boat_profile['rimHeight'];stations=[(s['z'],s['width'],s['keel']) for s in boat_profile['stations']]
vs=[];faces=[];uv=[];cross=10
for j,(b,width,keel) in enumerate(stations):
 for k in range(cross+1):
  t=-math.pi*.5+k/cross*math.pi;x=math.sin(t)*width;z=keel+(rim-keel)*(abs(math.sin(t))**1.5)
  vs.append((boat[0]+x,boat[1]+b,boat[2]+z));uv.append((k/cross,j/(len(stations)-1)))
for j in range(len(stations)-1):
 for k in range(cross):q=j*(cross+1)+k;faces.append((q,q+1,q+cross+2,q+cross+1))
hull=mesh('DinghyHull',vs,faces,'BoatHull','Boat',uv);m=hull.modifiers.new('HullThickness','SOLIDIFY');m.thickness=.035;bpy.context.view_layer.objects.active=hull;bpy.ops.object.modifier_apply(modifier=m.name)
for sign in [-1,1]:
 points=[(boat[0]+sign*w,boat[1]+b,boat[2]+rim) for b,w,k in stations];tube('Gunwale',points,.035,'BoatTrim','Boat',10)
for b in [-.7,.65]:box('DinghyBench',(boat[0],boat[1]+b,boat[2]+.26),(1.08,.28,.06),'Wood','Boat',.015)
box('Floorboard',(boat[0],boat[1],boat[2]-.20),(.55,2.6,.045),'Wood','Boat')
for b in [-.7,.7]:tube('BenchSupport',[(boat[0]-.4,boat[1]+b,boat[2]-.12),(boat[0]-.4,boat[1]+b,boat[2]+.25)],.022,'Metal','Boat')
for xsign in [-1,1]:
 pts=[(boat[0]+xsign*.7,boat[1]-.4,boat[2]+.52),(boat[0]+xsign*.48,boat[1]+1.4,boat[2]+.50)];tube('StowedOar',pts,.022,'Wood','Boat')
 box('OarBlade',(boat[0]+xsign*.47,boat[1]+1.35,boat[2]+.50),(.12,.44,.035),'Wood','Boat',.014)
tube('BowTether',[(-4.2,4.5,5.2),(-3.7,4.9,4.72),(boat[0],boat[1]+1.75,boat[2]+.46)],.018,'Rope','Boat')

# Tessellated water surface is a separate transparent mesh; runtime supplies waves/refraction.
vs=[];uv=[];faces=[];n=128
for j in range(n+1):
 for i in range(n+1):vs.append((-200+i*3.125,-200+j*3.125,4.5));uv.append((i/8,j/8))
for j in range(n):
 for i in range(n):q=j*(n+1)+i;faces.append((q,q+1,q+n+2,q+n+1))
mesh('WaterSurface',vs,faces,'Water','Water',uv)

lod_report={};lods=[]
for group,objects in groups.items():
 bpy.ops.object.select_all(action='DESELECT')
 for obj in objects:obj.select_set(True)
 bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();base=bpy.context.object;base.name=group+'_LOD0'
 levels=[base]
 if group!='Water':
  for level,ratio in [(1,.5),(2,.2)]:
   obj=base.copy();obj.data=base.data.copy();scene.collection.objects.link(obj);obj.name=group+'_LOD'+str(level);bpy.context.view_layer.objects.active=obj;m=obj.modifiers.new('DistanceReduction','DECIMATE');m.ratio=ratio;bpy.ops.object.modifier_apply(modifier=m.name);levels.append(obj)
 for level,obj in enumerate(levels):obj.hide_render=level>0;obj.data.calc_loop_triangles();lod_report[obj.name]=len(obj.data.loop_triangles);lods.append(obj)

for name,co in [('OriginAnchor',(0,0,0)),('NorthAnchor',(0,1,0)),('UpAnchor',(0,0,1)),('UnderwaterCamera',(0,-6.5,1.25)),('UnderwaterTarget',(0,6,0)),('SurfaceCamera',(1,-7,6.1)),('SurfaceTarget',(-3,5,4.6))]:
 obj=bpy.data.objects.new(name,None);scene.collection.objects.link(obj);obj.parent=source;obj.location=co
bpy.ops.object.select_all(action='DESELECT')
for obj in [source,*source.children_recursive]:obj.select_set(True)
bpy.context.view_layer.objects.active=source
bpy.ops.export_scene.fbx(filepath=str(GAME/'CoralBay.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',path_mode='RELATIVE',bake_anim=False)
for obj in lods:obj.hide_set(not obj.name.endswith('LOD0'))

# Studio setup is excluded from the FBX. Scientific acceptance is separate.
world=bpy.data.worlds.new('BayDaylight');scene.world=world;world.use_nodes=True;sky=world.node_tree.nodes.new('ShaderNodeTexSky');sky.sky_type='MULTIPLE_SCATTERING';sky.sun_elevation=math.radians(38);sky.sun_rotation=math.radians(135);world.node_tree.links.new(sky.outputs[0],world.node_tree.nodes['Background'].inputs[0]);world.node_tree.nodes['Background'].inputs[1].default_value=.18
sun=bpy.data.lights.new('BaySun','SUN');sun.energy=1.8;sun.angle=.04;obj=bpy.data.objects.new('BaySun',sun);scene.collection.objects.link(obj);obj.rotation_euler=(math.radians(30),math.radians(-20),math.radians(-35));scene.view_settings.look='AgX - Medium High Contrast';scene.view_settings.exposure=-.3
volume=bpy.data.materials.new('StudioWaterVolume');volume.use_nodes=True;volume.node_tree.nodes.clear();out=volume.node_tree.nodes.new('ShaderNodeOutputMaterial');v=volume.node_tree.nodes.new('ShaderNodeVolumePrincipled');v.inputs['Color'].default_value=(.18,.53,.60,1);v.inputs['Density'].default_value=.035;v.inputs['Anisotropy'].default_value=.45;volume.node_tree.links.new(v.outputs['Volume'],out.inputs['Volume'])
bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,-1.75));obj=bpy.context.object;obj.name='StudioWaterVolume';obj.scale=(20000,20000,12.49);obj.data.materials.append(volume)
# Cycles' total internal reflection needs a practically unbounded studio surface.
# The exported, tessellated runtime patch remains finite and fades into engine fog.
bpy.data.objects['Water_LOD0'].hide_render=True
bpy.ops.mesh.primitive_plane_add(size=20000,location=(0,0,4.5));obj=bpy.context.object;obj.name='StudioInfiniteWater';obj.data.materials.append(water)
fill=bpy.data.lights.new('StudioUnderwaterFill','AREA');fill.energy=550;fill.shape='DISK';fill.size=12;fill.color=(.45,.77,.89);obj=bpy.data.objects.new('StudioUnderwaterFill',fill);scene.collection.objects.link(obj);obj.location=(0,1,3.8);obj.rotation_euler=(Vector((0,9,-1))-obj.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',d);scene.collection.objects.link(cam);scene.camera=cam;cam.location=(0,-6.5,1.25);cam.rotation_euler=(Vector((0,6,0))-cam.location).to_track_quat('-Z','Y').to_euler();d.lens=26;d.clip_end=30000;scene.render.filepath='//previews/underwater.png'
for im in bpy.data.images:
 if im.source=='FILE':im.filepath=bpy.path.relpath(im.filepath,start=str(OUT))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'CoralBay.blend'))
report=dict(status='CANDIDATE',units='metres',waterLevel=4.5,lods=lod_report,rockPatches=len(rock_positions),seagrassClumps=260,coralForms=['branching','plate','mound'],artAcceptance='NOT_ACCEPTED',scientificReview='PENDING',source='tools/art_bay.py')
(OUT/'build-report.json').write_text(json.dumps(report,indent=2)+'\n');print('CORAL_BAY_BUILD_OK '+json.dumps(report))
