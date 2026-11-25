"""Original continental-slope sample, survey ROV and owned dinghy conversion."""
import bpy,bmesh,math,json,random,hashlib
from pathlib import Path
from mathutils import Vector
import numpy as np
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art/source/continental-slope';GAME=ROOT/'game/Assets/OceanAngler/Art/ContinentalSlope';BAY=ROOT/'art/source/coral-bay';BAY_GAME=ROOT/'game/Assets/OceanAngler/Art/CoralBay'
for p in [OUT,GAME]:p.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.render.resolution_x=1440;scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast';scene.view_settings.exposure=-.6
source=bpy.data.objects.new('ContinentalSlopeSource',None);scene.collection.objects.link(source);groups={};mats={};orientation_fixes={};rand=random.Random(21947);rng=np.random.default_rng(21947);SEA=200.0
def smooth(x):x=max(0,min(1,x));return x*x*(3-2*x)
def image(name,data,linear=False):
 if data.ndim==2:data=np.repeat(data[:,:,None],3,axis=2)
 rgba=np.ones((*data.shape[:2],4),np.float32);rgba[:,:,:3]=np.clip(data[:,:,:3],0,1)
 if data.shape[2]==4:rgba[:,:,3]=data[:,:,3]
 im=bpy.data.images.new(name,width=data.shape[1],height=data.shape[0],alpha=data.shape[2]==4);im.colorspace_settings.name='Non-Color' if linear else 'sRGB';im.pixels.foreach_set(rgba.ravel());im.file_format='PNG';im.filepath_raw=str(GAME/(name+'.png'));im.save();return im
def material(name,color,rough=.7,metal=0):
 m=bpy.data.materials.new('Slope_'+name);m.use_nodes=True;m.diffuse_color=(*color,1);p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal;mats[name]=m;return m
y,x=np.mgrid[0:1024,0:1024]/1024
def field(cells):
 grid=rng.random((cells,cells));u=x*cells;v=y*cells;ix=np.floor(u).astype(int);iy=np.floor(v).astype(int);a=u-ix;b=v-iy;a=a*a*(3-2*a);b=b*b*(3-2*b)
 return (grid[iy%cells,ix%cells]*(1-a)+grid[iy%cells,(ix+1)%cells]*a)*(1-b)+(grid[(iy+1)%cells,ix%cells]*(1-a)+grid[(iy+1)%cells,(ix+1)%cells]*a)*b
for name,color,rough in [('Strata',(.31,.32,.35),.88),('Silt',(.43,.40,.35),.95)]:
 if name=='Strata':
  broad=(field(4)-.5)*.11+(field(13)-.5)*.055;cracks=np.maximum(0,1-np.abs(field(27)-.5)*48);bedding=np.sin((y*9+np.sin(x*7)*.15)*math.tau)*.007;h=broad+(field(61)-.5)*.018-cracks*.023+bedding;variation=h*.6+(field(7)-.5)*.035
 else:h=(field(8)-.5)*.012+(field(43)-.5)*.009;variation=(field(6)-.5)*.045+rng.normal(0,.003,x.shape)
 base=image('Slope_'+name,np.stack([c+variation for c in color],axis=2));gy,gx=np.gradient(h);n=np.stack((-gx*25,-gy*25,np.ones_like(h)),axis=2);n/=np.linalg.norm(n,axis=2)[:,:,None];normal=image('Slope_'+name+'_Normal',n*.5+.5,True);roughness=np.clip(rough+h*.15,0,1);r=image('Slope_'+name+'_Roughness',roughness,True);packed=np.zeros((*x.shape,4));packed[:,:,3]=1-roughness;image('Slope_'+name+'_MetallicSmoothness',packed,True)
 m=material(name,(1,1,1),rough);nodes=m.node_tree.nodes;p=nodes['Principled BSDF']
 for im,prop in [(base,'Base Color'),(r,'Roughness')]:t=nodes.new('ShaderNodeTexImage');t.image=im;m.node_tree.links.new(t.outputs['Color'],p.inputs[prop])
 t=nodes.new('ShaderNodeTexImage');t.image=normal;n=nodes.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=.4;m.node_tree.links.new(t.outputs['Color'],n.inputs['Color']);m.node_tree.links.new(n.outputs['Normal'],p.inputs['Normal'])
# World-box source preview; Unity uses the existing world-triplanar PBR shader.
blend=bpy.data.materials.new('Slope_Terrain');blend.use_nodes=True;nodes=blend.node_tree.nodes;nodes.clear();output=nodes.new('ShaderNodeOutputMaterial');mix=nodes.new('ShaderNodeMixShader');attr=nodes.new('ShaderNodeAttribute');attr.attribute_name='TerrainBlend';blend.node_tree.links.new(attr.outputs['Fac'],mix.inputs[0]);blend.node_tree.links.new(mix.outputs[0],output.inputs['Surface'])
for index,name in enumerate(['Strata','Silt']):
 p=nodes.new('ShaderNodeBsdfPrincipled');blend.node_tree.links.new(p.outputs[0],mix.inputs[index+1]);position=nodes.new('ShaderNodeNewGeometry');scale=nodes.new('ShaderNodeVectorMath');scale.operation='SCALE';scale.inputs[3].default_value=.32;blend.node_tree.links.new(position.outputs['Position'],scale.inputs[0])
 for suffix,prop in [('', 'Base Color'),('_Roughness','Roughness')]:
  t=nodes.new('ShaderNodeTexImage');t.image=bpy.data.images['Slope_'+name+suffix];t.projection='BOX';t.projection_blend=.3;blend.node_tree.links.new(scale.outputs[0],t.inputs['Vector']);blend.node_tree.links.new(t.outputs['Color'],p.inputs[prop])
 bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.22;bump.inputs['Distance'].default_value=.035;blend.node_tree.links.new(t.outputs['Color'],bump.inputs['Height']);blend.node_tree.links.new(bump.outputs[0],p.inputs['Normal'])
mats['Terrain']=blend
for args in [('SurveyYellow',(.8,.48,.065),.48,.05),('FrameMetal',(.22,.25,.29),.33,.65),('Optics',(.018,.045,.07),.15,.1),('Rope',(.12,.18,.2),.8,0),('InstrumentWhite',(.78,.79,.73),.6,0),('Marker',(.68,.23,.065),.6,0)]:material(*args)
def mesh(name,vertices,faces,mat,group):
 data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);data.update();obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);obj.parent=source;data.materials.append(mats[mat]);groups.setdefault(group,[]).append(obj);uv=data.uv_layers.new(name='UVMap')
 for f in data.polygons:
  f.use_smooth=True;axis=max(range(3),key=lambda k:abs(f.normal[k]))
  for li in f.loop_indices:
   v=data.vertices[data.loops[li].vertex_index].co;uv.data[li].uv=((v.y if axis==0 else v.x)*.32,(v.y if axis==2 else v.z)*.32)
 bm=bmesh.new();bm.from_mesh(data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces))
 if all(e.is_manifold for e in bm.edges):
  points=np.asarray([[tuple(v.co) for v in f.verts] for f in bm.faces],dtype=np.float64);points-=points[0,0].copy();volume=float(np.einsum('ij,ij->i',points[:,0],np.cross(points[:,1],points[:,2])).sum()/6)
  if volume<0:bmesh.ops.reverse_faces(bm,faces=list(bm.faces));orientation_fixes[group]=orientation_fixes.get(group,0)+1
 bm.to_mesh(data);bm.free();data.update();return obj
def terrain_height(a,b):
 center=2.5*math.sin((b+10)*.026)+.9*math.sin(b*.07);d=abs(a-center);base=-9-.065*max(0,b)+.25*math.sin(a*.27)*math.cos(b*.17)
 rise=23*smooth((d-8.5)/12)**.58;erosion=(.7*math.sin(b*.39+a*.11)+.4*math.sin(b*.83-a*.18))*smooth((d-9)/8);gully=1.4*math.exp(-((a+19+2*math.sin(b*.06))/3.4)**2)
 result=base+rise+erosion-gully
 # A broad eroded bench gives the survey ROV room to hover over the floor,
 # without cutting into the shared fishing/observation corridor.
 bench=1-smooth((math.hypot((a+14)*1.1,b-18)-3.0)/2.2)
 return result*(1-bench)+(-5.8+.025*(b-18)+.04*math.sin(a*.4))*bench
xs=[-180,-120,-80,-60]+[i*1.5 for i in range(-32,33)]+[60,80,120,180];ys=[-120,-64,-40]+[i*1.5 for i in range(-16,65)]+[120,180,280]
vertices=[(a,b,terrain_height(a,b)) for b in ys for a in xs];faces=[];nx=len(xs)
for j in range(len(ys)-1):
 for i in range(nx-1):q=j*nx+i;faces.append((q,q+1,q+nx+1,q+nx))
boundary=list(range(nx))+[j*nx+nx-1 for j in range(1,len(ys))]+[(len(ys)-1)*nx+i for i in range(nx-2,-1,-1)]+[j*nx for j in range(len(ys)-2,0,-1)];bottom=[]
for v in boundary:bottom.append(len(vertices));vertices.append((vertices[v][0],vertices[v][1],-80))
for k in range(len(boundary)):n=(k+1)%len(boundary);faces.append((boundary[k],bottom[k],bottom[n],boundary[n]))
faces.append(tuple(reversed(bottom)));terrain=mesh('IncisedSlope',vertices,faces,'Terrain','Terrain')
def mask(obj,force_rock=False):
 layer=obj.data.color_attributes.new(name='TerrainBlend',type='FLOAT_COLOR',domain='POINT')
 for v in obj.data.vertices:
  cover=smooth((v.normal.z-.5)/.4)*(.55+.25*math.sin(v.co.x*.13+v.co.y*.11));value=max(0,min(1,cover)) if not force_rock else .03
  layer.data[v.index].color=(value,value,value,1)
 obj.data.color_attributes.active_color=layer
mask(terrain)
# Angular convex fragments interrupt the broad cliff silhouette, rather than
# recycling smooth spheres. Their bases intersect the actual terrain surface.
rock_positions=[]
for i in range(32):
 a=rand.choice([-1,1])*rand.uniform(9.5,23);b=rand.uniform(11,65);size=rand.uniform(.45,1.7);height=terrain_height(a,b);points=[]
 for z in [-1,1]:
  for y0 in [-1,1]:
   for x0 in [-1,1]:points.append((a+(x0+rand.uniform(-.28,.28))*size,b+(y0+rand.uniform(-.28,.28))*size*.75,height+size*.25+(z+rand.uniform(-.23,.23))*size*.58))
 bm=bmesh.new();verts=[bm.verts.new(p) for p in points];bmesh.ops.convex_hull(bm,input=verts);bm.verts.ensure_lookup_table();bm.faces.ensure_lookup_table();v=[tuple(v.co) for v in bm.verts];indices={v:i for i,v in enumerate(bm.verts)};f=[tuple(indices[v] for v in face.verts) for face in bm.faces];bm.free();obj=mesh('Scree'+str(i),v,f,'Terrain','Scree');mask(obj,True);rock_positions.append([a,b,height,size])
def tube(name,points,radius,mat,group,sides=12):
 verts=[];faces=[]
 for i,point in enumerate(points):
  p=Vector(point);direction=(Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])).normalized();u=direction.cross(Vector((0,0,1)))
  if u.length<.01:u=direction.cross(Vector((0,1,0)))
  u.normalize();v=direction.cross(u).normalized()
  for k in range(sides):t=k/sides*math.tau;verts.append(tuple(p+(u*math.cos(t)+v*math.sin(t))*radius))
 for i in range(len(points)-1):
  for k in range(sides):q=i*sides+k;n=i*sides+(k+1)%sides;faces.append((q,n,n+sides,q+sides))
 faces.extend([tuple(reversed(range(sides))),tuple((len(points)-1)*sides+k for k in range(sides))]);return mesh(name,verts,faces,mat,group)
def box(name,co,dimensions,mat,group,bevel=.04):
 bpy.ops.object.select_all(action='DESELECT');bpy.ops.mesh.primitive_cube_add(size=1,location=co);obj=bpy.context.object;obj.name=name;obj.dimensions=dimensions;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 if bevel:
  m=obj.modifiers.new('MachinedEdges','BEVEL');m.width=bevel;m.segments=2;bpy.ops.object.modifier_apply(modifier=m.name)
 obj.parent=source;obj.data.materials.append(mats[mat]);groups.setdefault(group,[]).append(obj);return obj
def duct(name,a,b,outer,inner):
 verts=[];faces=[];sides=24
 for xx,radius in [(a[0],outer),(b[0],outer),(a[0],inner),(b[0],inner)]:
  for i in range(sides):t=i/sides*math.tau;verts.append((xx,a[1]+math.cos(t)*radius,a[2]+math.sin(t)*radius))
 for i in range(sides):
  j=(i+1)%sides;faces.extend([(i,j,sides+j,sides+i),(2*sides+j,2*sides+i,3*sides+i,3*sides+j),(j,i,2*sides+i,2*sides+j),(sides+i,sides+j,3*sides+j,3*sides+i)])
 return mesh(name,verts,faces,'FrameMetal','ROV')
cx,cy=-14,18;cz=terrain_height(cx,cy)+.22
for yy in [-.65,.65]:
 tube('LandingSkid',[(cx-1.3,cy+yy,cz+.18),(cx+1.25,cy+yy,cz+.18)],.055,'FrameMetal','ROV')
 for xx in [-.95,.95]:tube('SurveyUpright',[(cx+xx,cy+yy,cz+.18),(cx+xx,cy+yy,cz+1.45)],.045,'FrameMetal','ROV')
 for z in [.42,1.35]:tube('SurveyLongRail',[(cx-1.05,cy+yy,cz+z),(cx+1.05,cy+yy,cz+z)],.045,'FrameMetal','ROV')
for xx in [-.95,.95]:
 for z in [.42,1.35]:tube('SurveyCrossRail',[(cx+xx,cy-.66,cz+z),(cx+xx,cy+.66,cz+z)],.045,'FrameMetal','ROV')
box('BuoyancyFoam',(cx-.1,cy,cz+1.45),(1.8,1.25,.28),'SurveyYellow','ROV',.13)
tube('PressureHousing',[(cx-.75,cy,cz+.79),(cx+.8,cy,cz+.79)],.28,'InstrumentWhite','ROV',24)
for xx in [-.76,.81]:tube('EndFlange',[(cx+xx-.025,cy,cz+.79),(cx+xx+.025,cy,cz+.79)],.31,'FrameMetal','ROV',24)
tube('CameraHousing',[(cx+.83,cy,cz+.79),(cx+1.08,cy,cz+.79)],.18,'FrameMetal','ROV',24)
tube('CameraPort',[(cx+1.08,cy,cz+.79),(cx+1.095,cy,cz+.79)],.145,'Optics','ROV',24)
for yy in [-.46,.46]:
 tube('LampHousing',[(cx+.65,cy+yy,cz+1.08),(cx+1.03,cy+yy,cz+1.08)],.11,'SurveyYellow','ROV',16)
 tube('LampPort',[(cx+1.03,cy+yy,cz+1.08),(cx+1.045,cy+yy,cz+1.08)],.087,'InstrumentWhite','ROV',16)
for yy in [-.9,.9]:
 duct('OpenThrusterDuct',(cx-.38,cy+yy,cz+.78),(cx+.15,cy+yy,cz+.78),.17,.145)
 for k in range(2):
  angle=k*math.pi/2;dy,dz=math.cos(angle)*.13,math.sin(angle)*.13;tube('ThrusterGuard',[(cx+.16,cy+yy-dy,cz+.78-dz),(cx+.16,cy+yy+dy,cz+.78+dz)],.016,'InstrumentWhite','ROV',8)
 box('ThrusterBladeA',(cx-.12,cy+yy,cz+.78),(.035,.24,.035),'InstrumentWhite','ROV',.008)
 box('ThrusterBladeB',(cx-.12,cy+yy,cz+.78),(.035,.035,.24),'InstrumentWhite','ROV',.008)
 box('ThrusterHub',(cx+.18,cy+yy,cz+.78),(.05,.08,.08),'FrameMetal','ROV',.012)
box('ElectronicsTray',(cx-.3,cy,cz+.41),(1.05,.72,.16),'Optics','ROV')
box('SampleTray',(cx+.63,cy,cz+.36),(.48,.7,.11),'SurveyYellow','ROV',.02)
# Use our owned hull and its measured water mask, with a new survey mast.
with bpy.data.libraries.load(str(BAY/'CoralBay.blend'),link=False) as (_,loaded):loaded.objects=['Boat_LOD0']
boat=loaded.objects[0];scene.collection.objects.link(boat);boat.parent=source;boat.hide_render=False;boat.hide_set(False);boat.name='SurveyDinghy';bm=bmesh.new();bm.from_mesh(boat.data);rope=[i for i,m in enumerate(boat.data.materials) if m.name=='Bay_Rope'];bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.material_index in rope],context='FACES');bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS');bm.to_mesh(boat.data);bm.free()
profile=json.loads((BAY/'boat-profile.json').read_text());origin=[-5,8,SEA];shift=Vector(origin)-Vector(profile['origin'])
for v in boat.data.vertices:v.co+=shift
groups['Boat']=[boat];profile['origin']=origin;(OUT/'boat-profile.json').write_text(json.dumps(profile,indent=2)+'\n')
for im in bpy.data.images:
 if im.source=='FILE' and im.name.startswith('Bay_'):im.filepath=str(BAY_GAME/Path(im.filepath).name)
for yy in [7.3,8.7]:tube('SurveyMast',[(-5.5,yy,SEA+.32),(-5.5,yy,SEA+1.5)],.035,'FrameMetal','Boat')
tube('SurveyMastCrossbar',[(-5.5,7.3,SEA+1.5),(-5.5,8.7,SEA+1.5)],.035,'FrameMetal','Boat');box('SurveyReceiver',(-5.5,8,SEA+1.63),(.28,.45,.21),'InstrumentWhite','Boat');tube('SurveyAntenna',[(-5.5,8,SEA+1.74),(-5.5,8,SEA+2.15)],.012,'FrameMetal','Boat',8)
for level,steps,sides in [(0,32,10),(1,16,8),(2,8,6)]:
 points=[]
 for i in range(steps+1):t=i/steps;points.append((cx+(cx*-1-7)*t-.8*math.sin(math.pi*t),cy-3*t,cz+1.6+(SEA-cz-1.5)*t))
 tube('SurveyTether',points,.016,'Rope','SurveyL'+str(level),sides)
 box('SurveyFloat',(-7,13,SEA+.05),(.4,.4,.5),'Marker','SurveyL'+str(level),.12 if level==0 else .04)
 tube('FloatFlag',[(-7,13,SEA+.25),(-7,13,SEA+.8)],.012,'FrameMetal','SurveyL'+str(level),8)
water=material('Water',(.86,.96,.99),.1);p=water.node_tree.nodes['Principled BSDF'];p.inputs['Transmission Weight'].default_value=.96;p.inputs['IOR'].default_value=1.333;noise=water.node_tree.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=3.5;geom=water.node_tree.nodes.new('ShaderNodeNewGeometry');water.node_tree.links.new(geom.outputs['Position'],noise.inputs['Vector']);bump=water.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.2;bump.inputs['Distance'].default_value=.08;water.node_tree.links.new(noise.outputs['Fac'],bump.inputs['Height']);water.node_tree.links.new(bump.outputs[0],p.inputs['Normal'])
vs=[(-600+i*75,-600+j*75,SEA) for j in range(17) for i in range(17)];faces=[]
for j in range(16):
 for i in range(16):q=j*17+i;faces.append((q,q+1,q+18,q+17))
mesh('SlopeWater',vs,faces,'Water','Water')
report={};lods=[];components={key:len(value) for key,value in groups.items() if not key.startswith('Survey')}
for group,objects in groups.items():
 survey=group.startswith('SurveyL');pieces=[objects]
 if group!='Water' and not survey:
  # Simplify each solid before joining. A global target collapsed small rocks
  # into closed but zero-volume double faces. Keep a positive solid minimum.
  for level,ratio,minimum in [(1,.5,6),(2,.2,4)]:
   copies=[]
   for original in objects:
    obj=original.copy();obj.data=original.data.copy();scene.collection.objects.link(obj);obj.data.calc_loop_triangles();count=len(obj.data.loop_triangles);bpy.context.view_layer.objects.active=obj;m=obj.modifiers.new('DistanceReduction','DECIMATE');m.ratio=min(1,max(ratio,(minimum+.01)/count));bpy.ops.object.modifier_apply(modifier=m.name);copies.append(obj)
   pieces.append(copies)
 for level,current in enumerate(pieces):
  bpy.ops.object.select_all(action='DESELECT')
  for obj in current:obj.select_set(True)
  bpy.context.view_layer.objects.active=current[0];bpy.ops.object.join();obj=bpy.context.object;obj.name='Survey_LOD'+group[-1] if survey else group+'_LOD'+str(level);obj.hide_render=not obj.name.endswith('LOD0');obj.data.calc_loop_triangles();report[obj.name]=len(obj.data.loop_triangles);lods.append(obj)
markers={'OriginAnchor':(0,0,0),'NorthAnchor':(0,1,0),'UpAnchor':(0,0,1),'UnderwaterCamera':(0,-6.5,1.25),'UnderwaterTarget':(0,6,0),'SurfaceCamera':(8,-7,SEA+4),'SurfaceTarget':(-5,12,SEA),'ReefCamera':(-11.5,15.5,-2),'ReefTarget':(cx,cy,cz+.7),'BoatCamera':(-8,5,SEA+2.6),'BoatTarget':(-5,8,SEA+.4),'LampA':(cx+1.08,cy-.46,cz+1.08),'LampB':(cx+1.08,cy+.46,cz+1.08),'RovCenter':(cx,cy,cz+.7)}
for name,point in markers.items():obj=bpy.data.objects.new(name,None);scene.collection.objects.link(obj);obj.parent=source;obj.location=point
bpy.ops.object.select_all(action='DESELECT')
for obj in [source,*source.children_recursive]:obj.select_set(True)
bpy.context.view_layer.objects.active=source;bpy.ops.export_scene.fbx(filepath=str(GAME/'ContinentalSlope.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',path_mode='RELATIVE',bake_anim=False)
for obj in lods:obj.hide_set(not obj.name.endswith('LOD0'))
world=bpy.data.worlds.new('SlopeDaylight');scene.world=world;world.use_nodes=True;sky=world.node_tree.nodes.new('ShaderNodeTexSky');sky.sky_type='MULTIPLE_SCATTERING';sky.sun_elevation=math.radians(38);sky.sun_rotation=math.radians(135);world.node_tree.links.new(sky.outputs[0],world.node_tree.nodes['Background'].inputs[0]);world.node_tree.nodes['Background'].inputs[1].default_value=.12
sun=bpy.data.lights.new('SlopeSun','SUN');sun.energy=1.8;sun.angle=.04;obj=bpy.data.objects.new('SlopeSun',sun);scene.collection.objects.link(obj);obj.rotation_euler=(math.radians(30),math.radians(-20),math.radians(-35))
volume=bpy.data.materials.new('SlopeStudioVolume');volume.use_nodes=True;volume.node_tree.nodes.clear();output=volume.node_tree.nodes.new('ShaderNodeOutputMaterial');v=volume.node_tree.nodes.new('ShaderNodeVolumePrincipled');v.inputs['Color'].default_value=(.45,.62,.8,1);v.inputs['Density'].default_value=.004;v.inputs['Anisotropy'].default_value=.25;volume.node_tree.links.new(v.outputs['Volume'],output.inputs['Volume']);bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,60));obj=bpy.context.object;obj.name='StudioDeepWater';obj.scale=(2000,2000,280);obj.data.materials.append(volume)
fill=bpy.data.lights.new('StudioResearchFill','AREA');fill.energy=8000;fill.shape='DISK';fill.size=18;fill.color=(.3,.48,.76);obj=bpy.data.objects.new('StudioResearchFill',fill);scene.collection.objects.link(obj);obj.location=(0,-3,5);obj.rotation_euler=(Vector((0,16,-7))-obj.location).to_track_quat('-Z','Y').to_euler()
for name,target in [('LampA',(-1,8,-3)),('LampB',(-6,20,-7))]:
 light=bpy.data.lights.new('Studio'+name,'SPOT');light.energy=800;light.color=(1,.72,.42);light.spot_size=math.radians(85);light.spot_blend=.6;obj=bpy.data.objects.new('Studio'+name,light);scene.collection.objects.link(obj);obj.location=markers[name];obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
camera=bpy.data.cameras.new('ReviewCamera');obj=bpy.data.objects.new('ReviewCamera',camera);scene.collection.objects.link(obj);scene.camera=obj;obj.location=markers['UnderwaterCamera'];obj.rotation_euler=(Vector(markers['UnderwaterTarget'])-obj.location).to_track_quat('-Z','Y').to_euler();camera.lens=28;camera.clip_end=4000
for im in bpy.data.images:
 if im.source=='FILE':im.filepath=bpy.path.relpath(im.filepath,start=str(OUT))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ContinentalSlope.blend'))
result=dict(status='CANDIDATE',units='metres',seaLevel=SEA,lods=report,solidCounts={'Terrain':1,'Scree':32},componentsBeforeJoin=components,screePositions=rock_positions,rovGround=[cx,cy,cz],markers=markers,orientationCorrections=orientation_fixes,source='tools/art_slope.py',terrainProjection='Unity world triplanar; Cycles world-box PBR approximation',reuse=dict(boatSource='art/source/coral-bay/CoralBay.blend',sha256=hashlib.sha256((BAY/'CoralBay.blend').read_bytes()).hexdigest(),modification='Removed former pier tether, moved hull to survey coordinates, added original survey mast/receiver/antenna'),ecology='Art-directed slope sample; no kelp or claim of a shared species community',artAcceptance='NOT_ACCEPTED',deviceAcceptance='NOT_RUN')
(OUT/'build-report.json').write_text(json.dumps(result,indent=2)+'\n');print('SLOPE_BUILD_OK '+json.dumps(dict(status=result['status'],triangles=report,seaLevel=SEA)))
