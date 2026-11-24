"""Own nearshore channel candidate; authored meshes/PBR plus our existing dinghy."""
import bpy,bmesh,math,json,random,hashlib
from pathlib import Path
from mathutils import Vector
import numpy as np
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art/source/nearshore-channel';GAME=ROOT/'game/Assets/OceanAngler/Art/NearshoreChannel';BAY=ROOT/'art/source/coral-bay';BAY_GAME=ROOT/'game/Assets/OceanAngler/Art/CoralBay'
for p in [OUT,GAME]:p.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.render.resolution_x=1440;scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
source=bpy.data.objects.new('NearshoreChannelSource',None);scene.collection.objects.link(source);groups={};mats={};orientation_fixes={};rand=random.Random(5481);rng=np.random.default_rng(5481)
def image(name,data,linear=False):
 if data.ndim==2:data=np.repeat(data[:,:,None],3,axis=2)
 rgba=np.ones((*data.shape[:2],4),np.float32);rgba[:,:,:3]=np.clip(data[:,:,:3],0,1)
 if data.shape[2]==4:rgba[:,:,3]=data[:,:,3]
 im=bpy.data.images.new(name,width=data.shape[1],height=data.shape[0],alpha=data.shape[2]==4);im.colorspace_settings.name='Non-Color' if linear else 'sRGB';im.pixels.foreach_set(rgba.ravel());im.file_format='PNG';im.filepath_raw=str(GAME/(name+'.png'));im.save();return im

def material(name,color,rough=.8,base=None,normal=None,roughmap=None):
 m=bpy.data.materials.new('Channel_'+name);m.use_nodes=True;m.diffuse_color=(*color,1);p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough
 for im,input_name in [(base,'Base Color'),(roughmap,'Roughness')]:
  if im:
   node=m.node_tree.nodes.new('ShaderNodeTexImage');node.image=im;m.node_tree.links.new(node.outputs['Color'],p.inputs[input_name])
 if normal:
  node=m.node_tree.nodes.new('ShaderNodeTexImage');node.image=normal;n=m.node_tree.nodes.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=.35;m.node_tree.links.new(node.outputs['Color'],n.inputs['Color']);m.node_tree.links.new(n.outputs['Normal'],p.inputs['Normal'])
 mats[name]=m;return m

y,x=np.mgrid[0:512,0:512]/512
def field(cells):
 grid=rng.random((cells,cells));u=x*cells;v=y*cells;ix=np.floor(u).astype(int);iy=np.floor(v).astype(int);a=u-ix;b=v-iy;a=a*a*(3-2*a);b=b*b*(3-2*b)
 return (grid[iy%cells,ix%cells]*(1-a)+grid[iy%cells,(ix+1)%cells]*a)*(1-b)+(grid[(iy+1)%cells,ix%cells]*(1-a)+grid[(iy+1)%cells,(ix+1)%cells]*a)*b
for name,color,rough in [('Sediment',(.49,.47,.38),.94),('Stone',(.39,.42,.40),.87),('Kelp',(.16,.28,.12),.74),('CoastalSoil',(.24,.28,.15),.93)]:
 if name=='Sediment':h=np.sin((y*14+np.sin(x*19)*.2)*math.tau)*.009;variation=np.sin(x*19)*np.cos(y*23)*.014+rng.normal(0,.005,x.shape)
 elif name=='Stone':h=(field(4)-.5)*.16+(field(13)-.5)*.07+(field(47)-.5)*.025;variation=h*.35+(field(7)-.5)*.025+rng.normal(0,.005,x.shape)
 elif name=='CoastalSoil':h=(field(11)-.5)*.035+(field(51)-.5)*.012;variation=(field(5)-.5)*.07+h*.25+rng.normal(0,.004,x.shape)
 else:h=np.sin(x*math.tau*14)*.018+np.cos(y*math.tau*4)*.008;variation=h+rng.normal(0,.003,x.shape)
 base=image('Channel_'+name,np.stack([c+variation for c in color],axis=2));gy,gx=np.gradient(h);n=np.stack((-gx*12,-gy*12,np.ones_like(h)),axis=2);n/=np.linalg.norm(n,axis=2)[:,:,None]
 normal=image('Channel_'+name+'_Normal',n*.5+.5,True);r=image('Channel_'+name+'_Roughness',np.clip(rough+h*.15,0,1),True);packed=np.zeros((*x.shape,4));packed[:,:,3]=1-np.clip(rough+h*.15,0,1);image('Channel_'+name+'_MetallicSmoothness',packed,True);material(name,(1,1,1),rough,base,normal,r)
material('Buoy',(.62,.22,.12),.5);material('Mooring',(.38,.34,.25),.9)

def mesh(name,vertices,faces,mat,group,uv=None):
 data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);data.update();o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.parent=source;data.materials.append(mats[mat]);groups.setdefault(group,[]).append(o)
 layer=data.uv_layers.new(name='UVMap')
 for face in data.polygons:
  face.use_smooth=True
  for li in face.loop_indices:
   v=data.vertices[data.loops[li].vertex_index].co;layer.data[li].uv=uv[data.loops[li].vertex_index] if uv else (v.x*.35,v.y*.35)
 bm=bmesh.new();bm.from_mesh(data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
 if all(e.is_manifold for e in bm.edges):
  analysis=bm.copy();bmesh.ops.triangulate(analysis,faces=list(analysis.faces));points=np.asarray([[tuple(v.co) for v in f.verts] for f in analysis.faces],dtype=np.float64);points-=points[0,0].copy();volume=float(np.einsum('ij,ij->i',points[:,0],np.cross(points[:,1],points[:,2])).sum()/6);analysis.free()
  if volume<0:bmesh.ops.reverse_faces(bm,faces=list(bm.faces));orientation_fixes[group]=orientation_fixes.get(group,0)+1
 bm.to_mesh(data);bm.free();data.update();return o

def floor(a,b):return -6.2-.017*(b+5)+.28*math.sin(a*.18)*math.cos(b*.17)+.1*math.sin(a*.8+b*.45)
xs=[-180,-100,-60,-36]+list(range(-26,27,2))+[36,60,100,180];ys=[-100,-60,-32]+list(range(-18,51,2))+[70,100,160,260]
vs=[(a,b,floor(a,b)) for b in ys for a in xs];faces=[]
for j in range(len(ys)-1):
 for i in range(len(xs)-1):q=j*len(xs)+i;faces.append((q,q+1,q+len(xs)+1,q+len(xs)))
boundary=list(range(len(xs)))+[j*len(xs)+len(xs)-1 for j in range(1,len(ys))]+[(len(ys)-1)*len(xs)+i for i in range(len(xs)-2,-1,-1)]+[j*len(xs) for j in range(len(ys)-2,0,-1)]
bottom=[]
for v in boundary:bottom.append(len(vs));vs.append((vs[v][0],vs[v][1],-30))
for k in range(len(boundary)):j=(k+1)%len(boundary);faces.append((boundary[k],bottom[k],bottom[j],boundary[j]))
faces.append(tuple(reversed(bottom)));mesh('SedimentShelf',vs,faces,'Sediment','Seabed')

def rock(name,a,b,z,width,depth,height,seed,group):
 vs=[];uv=[];faces=[];segments=36;rings=20
 for j in range(rings+1):
  theta=j/rings*math.pi
  for k in range(segments+1):
   phi=k/segments*math.tau;erosion=1+.1*math.sin(phi*3+seed)*math.sin(theta*4)+.045*math.cos(phi*9-theta*5+seed);radius=math.sin(theta)*erosion
   vs.append((a+math.cos(phi)*radius*width,b+math.sin(phi)*radius*depth,z+math.cos(theta)*height+math.sin(phi*4+theta*8+seed)*height*.035*math.sin(theta)));uv.append((k/segments*max(width,depth)*.7,j/rings*height*.7))
 for j in range(rings):
  for k in range(segments):q=j*(segments+1)+k;faces.append((q,q+segments+1,q+segments+2,q+1))
 return mesh(name,vs,faces,'Stone',group,uv)
island_centres=[(-28,82,15,22,17.5),(32,110,18,25,20),(-65,180,25,31,23)]
for seed,(a,b,width,depth,peak) in enumerate(island_centres):
 vertices=[];faces=[];uv=[];segments=64
 terraces=[(-24,1.23),(-13,1.18),(-4,1.06),(2,.99),(4.2,.94),(6.5,.86),(9,.78),(12,.65),(14.5,.43),(16,.16),(17.5,.025)]
 for j,(height,scale) in enumerate(terraces):
  for k in range(segments):
   phi=k/segments*math.tau;coast=1+.13*math.sin(phi*3+seed)+.07*math.cos(phi*5-seed)+.025*math.sin(phi*13+j*.7)
   z=height if j<3 else height/17.5*peak+(1 if j<9 else .3 if j==9 else .05)*(math.sin(phi*3+seed)*.8+math.sin(phi*7-j*.9)*.3)
   vertices.append((a+math.cos(phi)*width*scale*coast,b+math.sin(phi)*depth*scale*coast,z));uv.append((k/segments*width*.6,j*.65))
 for j in range(len(terraces)-1):
  for k in range(segments):q=j*segments+k;n=j*segments+(k+1)%segments;faces.append((q,n,n+segments,q+segments))
 faces.extend([tuple(reversed(range(segments))),tuple((len(terraces)-1)*segments+k for k in range(segments))]);obj=mesh('GroundedRockIsland'+str(seed),vertices,faces,'Stone','Islands',uv);obj.data.materials.append(mats['CoastalSoil'])
 for face in obj.data.polygons:
  if sum(obj.data.vertices[v].co.z for v in face.vertices)/len(face.vertices)>peak*.65:face.material_index=1

rocks=[]
for i in range(14):
 a=rand.choice([-1,1])*rand.uniform(7,18);b=rand.uniform(13,42);s=rand.uniform(.9,2.4);z=floor(a,b)+s*.2;rock('SubmergedOutcrop'+str(i),a,b,z,s,s*.75,s*.7,i+11,'Outcrops');rocks.append((a,b,z+s*.7))

def tube(name,points,widths,thickness,mat,group,sides=8):
 vs=[];uv=[];faces=[]
 for i,point in enumerate(points):
  p=Vector(point);direction=(Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])).normalized();u=direction.cross(Vector((0,0,1)))
  if u.length<.01:u=direction.cross(Vector((0,1,0)))
  u.normalize();v=direction.cross(u).normalized()
  for k in range(sides):t=k/sides*math.tau;vs.append(tuple(p+u*math.cos(t)*widths[i]+v*math.sin(t)*thickness));uv.append((k/sides,i/(len(points)-1)))
 for i in range(len(points)-1):
  for k in range(sides):q=i*sides+k;n=i*sides+(k+1)%sides;faces.append((q,n,n+sides,q+sides))
 faces.extend([tuple(reversed(range(sides))),tuple((len(points)-1)*sides+k for k in range(sides))]);return mesh(name,vs,faces,mat,group,uv)
for i in range(30):
 a,b,z=rocks[i%len(rocks)];a+=rand.uniform(-.7,.7);b+=rand.uniform(-.7,.7)
 for branch in range(3):
  h=rand.uniform(1.3,2.6);angle=rand.random()*math.tau;points=[(a+math.sin(j/8*1.7+angle)*j/8*.4,b+math.cos(angle)*j/8*.4,z+j/8*h) for j in range(9)];widths=[.012+.075*math.sin(math.pi*j/8)**.7 for j in range(9)]
  # Resample the same closed elliptical section rather than collapsing its
  # millimetre-thick volume with a generic surface decimator.
  for level,stride,sides in [(0,1,8),(1,2,6),(2,4,4)]:tube('KelpFrond',points[::stride],widths[::stride],.003,'Kelp','KelpL'+str(level),sides)

# Reuse our own authored dinghy. Remove its old pier tether, then re-moor it.
with bpy.data.libraries.load(str(BAY/'CoralBay.blend'),link=False) as (available,loaded):loaded.objects=['Boat_LOD0']
boat=loaded.objects[0];scene.collection.objects.link(boat);boat.parent=source;boat.hide_render=False;boat.hide_set(False);boat.name='ChannelDinghy'
bm=bmesh.new();bm.from_mesh(boat.data);rope=[i for i,m in enumerate(boat.data.materials) if m.name=='Bay_Rope'];bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.material_index in rope],context='FACES');bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS');bm.to_mesh(boat.data);bm.free()
old=json.loads((BAY/'boat-profile.json').read_text());origin=[-5,6,4.5];shift=Vector(origin)-Vector(old['origin'])
for v in boat.data.vertices:v.co+=shift
groups['Boat']=[boat];old['origin']=origin;(OUT/'boat-profile.json').write_text(json.dumps(old,indent=2)+'\n')
for im in bpy.data.images:
 if im.source=='FILE' and im.name.startswith('Bay_'):im.filepath=str(BAY_GAME/Path(im.filepath).name)
rock('MooringBuoy',-7,6.7,4.55,.23,.23,.35,2,'Mooring');buoy=groups['Mooring'][0];buoy.data.materials.clear();buoy.data.materials.append(mats['Buoy'])
tube('NewBowMooring',[(-7,6.7,4.8),(-6.2,7.2,4.72),(-5,7.75,4.96)],[.018]*3,.018,'Mooring','Mooring')
water=material('Water',(.86,.96,.99),.1);bs=water.node_tree.nodes['Principled BSDF'];bs.inputs['Transmission Weight'].default_value=.96;bs.inputs['IOR'].default_value=1.333
noise=water.node_tree.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=3.5;geometry=water.node_tree.nodes.new('ShaderNodeNewGeometry');water.node_tree.links.new(geometry.outputs['Position'],noise.inputs['Vector']);bump=water.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.2;bump.inputs['Distance'].default_value=.08;water.node_tree.links.new(noise.outputs['Fac'],bump.inputs['Height']);water.node_tree.links.new(bump.outputs['Normal'],bs.inputs['Normal'])
lightpath=water.node_tree.nodes.new('ShaderNodeLightPath');transparent=water.node_tree.nodes.new('ShaderNodeBsdfTransparent');mix=water.node_tree.nodes.new('ShaderNodeMixShader');water.node_tree.links.new(lightpath.outputs['Is Shadow Ray'],mix.inputs[0]);water.node_tree.links.new(bs.outputs[0],mix.inputs[1]);water.node_tree.links.new(transparent.outputs[0],mix.inputs[2]);water.node_tree.links.new(mix.outputs[0],water.node_tree.nodes['Material Output'].inputs['Surface'])
vs=[];faces=[];size=16
for j in range(size+1):
 for i in range(size+1):vs.append((-200+i*25,-200+j*25,4.5))
for j in range(size):
 for i in range(size):q=j*(size+1)+i;faces.append((q,q+1,q+size+2,q+size+1))
mesh('Water',vs,faces,'Water','Water')
report={};lods=[]
for group,objects in groups.items():
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();base=bpy.context.object;kelp=group.startswith('KelpL');base.name='Kelp_LOD'+group[-1] if kelp else group+'_LOD0';levels=[base]
 if group!='Water' and not kelp:
  for level,ratio in [(1,.5),(2,.2)]:
   obj=base.copy();obj.data=base.data.copy();scene.collection.objects.link(obj);obj.name=group+'_LOD'+str(level);bpy.context.view_layer.objects.active=obj;m=obj.modifiers.new('DistanceReduction','DECIMATE');m.ratio=ratio;bpy.ops.object.modifier_apply(modifier=m.name);levels.append(obj)
 for level,obj in enumerate(levels):obj.hide_render=not obj.name.endswith('LOD0');obj.data.calc_loop_triangles();report[obj.name]=len(obj.data.loop_triangles);lods.append(obj)
markers={'OriginAnchor':(0,0,0),'NorthAnchor':(0,1,0),'UpAnchor':(0,0,1),'UnderwaterCamera':(0,-6.5,1.25),'UnderwaterTarget':(0,6,0),'SurfaceCamera':(0,-9,6.4),'SurfaceTarget':(-1,18,4.6),'ReefCamera':(2,3,-.2),'ReefTarget':(8,17,-4),'BoatCamera':(-.7,2,6.7),'BoatTarget':(-5,6,4.5)}
for name,co in markers.items():o=bpy.data.objects.new(name,None);scene.collection.objects.link(o);o.parent=source;o.location=co
bpy.ops.object.select_all(action='DESELECT')
for obj in [source,*source.children_recursive]:obj.select_set(True)
bpy.context.view_layer.objects.active=source;bpy.ops.export_scene.fbx(filepath=str(GAME/'NearshoreChannel.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',path_mode='RELATIVE',bake_anim=False)
for o in lods:o.hide_set(not o.name.endswith('LOD0'))
world=bpy.data.worlds.new('ChannelDaylight');scene.world=world;world.use_nodes=True;sky=world.node_tree.nodes.new('ShaderNodeTexSky');sky.sky_type='MULTIPLE_SCATTERING';sky.sun_elevation=math.radians(38);sky.sun_rotation=math.radians(135);world.node_tree.links.new(sky.outputs[0],world.node_tree.nodes['Background'].inputs[0]);world.node_tree.nodes['Background'].inputs[1].default_value=.18
sun=bpy.data.lights.new('ChannelSun','SUN');sun.energy=1.8;sun.angle=.04;o=bpy.data.objects.new('ChannelSun',sun);scene.collection.objects.link(o);o.rotation_euler=(math.radians(30),math.radians(-20),math.radians(-35))
volume=bpy.data.materials.new('ChannelStudioVolume');volume.use_nodes=True;volume.node_tree.nodes.clear();out=volume.node_tree.nodes.new('ShaderNodeOutputMaterial');v=volume.node_tree.nodes.new('ShaderNodeVolumePrincipled');v.inputs['Color'].default_value=(.14,.40,.58,1);v.inputs['Density'].default_value=.022;v.inputs['Anisotropy'].default_value=.4;volume.node_tree.links.new(v.outputs['Volume'],out.inputs['Volume'])
bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,-10.25));o=bpy.context.object;o.name='StudioWaterVolume';o.scale=(20000,20000,29.49);o.data.materials.append(volume);bpy.data.objects['Water_LOD0'].hide_render=True
bpy.ops.mesh.primitive_plane_add(size=20000,location=(0,0,4.5));o=bpy.context.object;o.name='StudioInfiniteWater';o.data.materials.append(water)
fill=bpy.data.lights.new('ChannelStudioFill','AREA');fill.energy=600;fill.shape='DISK';fill.size=15;fill.color=(.4,.67,.87);o=bpy.data.objects.new('ChannelStudioFill',fill);scene.collection.objects.link(o);o.location=(0,4,3.7);o.rotation_euler=(Vector((0,14,-5))-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',d);scene.collection.objects.link(cam);scene.camera=cam;cam.location=markers['SurfaceCamera'];cam.rotation_euler=(Vector(markers['SurfaceTarget'])-cam.location).to_track_quat('-Z','Y').to_euler();d.lens=26;d.clip_end=30000
for im in bpy.data.images:
 if im.source=='FILE':im.filepath=bpy.path.relpath(im.filepath,start=str(OUT))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'NearshoreChannel.blend'))
result=dict(status='CANDIDATE',units='metres',waterLevel=4.5,lods=report,solidCounts={'Seabed':1,'Islands':3,'Outcrops':14,'Kelp':90},source='tools/art_channel.py',islandCentres=island_centres,orientationCorrections=orientation_fixes,reuse=dict(boatSource='art/source/coral-bay/CoralBay.blend',sha256=hashlib.sha256((BAY/'CoralBay.blend').read_bytes()).hexdigest(),change='Removed former pier tether and translated authored dinghy; own new buoy/mooring'),markers=markers,artAcceptance='NOT_ACCEPTED',deviceAcceptance='NOT_RUN')
(OUT/'build-report.json').write_text(json.dumps(result,indent=2)+'\n');print('CHANNEL_BUILD_OK '+json.dumps(result))
