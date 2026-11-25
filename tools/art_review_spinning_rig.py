"""从保存的源文件生成全竿、卷线器、导环与拟饵检查图，不重建源资产。"""
import bpy,argparse,sys,math,json
from pathlib import Path
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
p=argparse.ArgumentParser();p.add_argument('--output',required=True);p.add_argument('--motion',default='idle');p.add_argument('--frame',default=1,type=int);p.add_argument('--turntable',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);out=Path(a.output);scene=bpy.context.scene
if a.motion=='winding':
 angle=math.radians(max(0,a.frame-1)*10);bpy.data.objects['CrankPivot'].rotation_euler.x=-angle;bpy.data.objects['RotorPivot'].rotation_euler.z=angle*5.2;bpy.data.objects['LineRollerPivot'].rotation_euler.x=angle*5.2*.03185/.0045;bpy.data.objects['SpoolPivot'].location.z+=math.sin(angle*.75)*.006
elif a.motion=='bail-open':bpy.data.objects['BailPivot'].rotation_euler.x=math.radians(82)
def update_line():
 bpy.context.view_layer.update();position=lambda name:bpy.data.objects[name].matrix_world.translation
 curve=bpy.data.objects['StudioLine'].data;center=position('LineRollerPivot');axis=(position('LineRollerAxis')-center).normalized();guide=position('Guide0');offset=guide-center;offset-=axis*offset.dot(axis);d=offset.length;along=offset/d;r=(position('RollerSurface')-center).length+curve.bevel_depth
 contact=center+(along*(r/d)+axis.cross(along)*math.sqrt(1-(r/d)**2))*r
 if a.motion=='bail-open':
  center=position('SpoolLipCenter');axis=(position('RotorAxis')-position('RotorPivot')).normalized();outward=guide-center;outward-=axis*outward.dot(axis);contact=center+outward.normalized()*((position('SpoolLipSurface')-center).length+curve.bevel_depth)
 coordinates=[contact]+[position('Guide'+str(i)) for i in range(9)]+[position('LureNose')]
 for point,co in zip(curve.splines[0].points,coordinates):point.co=(*co,1)
update_line()
def view(name,position,target,scale):
 scene.camera.data.ortho_scale=scale;scene.camera.location=position;scene.camera.rotation_euler=(Vector(target)-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
if a.turntable:
 scene.render.resolution_x=960;scene.render.resolution_y=640;scene.cycles.samples=8
 report=dict(scope='saved original source whole-rig and reel orbits; not native Player or physical-device acceptance',motion=a.motion,poseFrame=a.frame,width=960,height=640,cyclesSamples=8,orbits=[])
 for kind,stems,distance in [('whole-turn',{'Rod','Housing','Rotor','Spool','Crank','Lure','BailWire','LineRoller'},4),('turn',{'Housing','Rotor','Spool','Crank','BailWire','LineRoller'},.65)]:
  objects=[o for o in bpy.data.objects if o.name.endswith('_LOD0') and o.name[:-5] in stems]
  if len(objects)!=len(stems):raise RuntimeError('Missing orbit assemblies')
  points=[o.matrix_world@Vector(corner) for o in objects for corner in o.bound_box]
  low=Vector(tuple(min(p[k] for p in points) for k in range(3)));high=Vector(tuple(max(p[k] for p in points) for k in range(3)));target=(low+high)*.5
  initial=max((high.z-low.z)*1.5,high.x-low.x,high.y-low.y);required=0
  def pose(i,scale):
   t=i/36*math.tau;scene.camera.data.ortho_scale=scale;scene.camera.location=target+Vector((math.sin(t)*distance,-math.cos(t)*distance,0));scene.camera.rotation_euler=(target-scene.camera.location).to_track_quat('-Z','Y').to_euler();bpy.context.view_layer.update()
  for i in range(36):
   pose(i,initial)
   for point in points:
    p=world_to_camera_view(scene,scene.camera,point)
    if p.z<=scene.camera.data.clip_start:raise RuntimeError('Orbit bound behind near plane')
    required=max(required,abs(p.x-.5)*2,abs(p.y-.5)*2)
  scale=initial*required/.88;frames=[]
  for i in range(36):
   pose(i,scale);projected=[world_to_camera_view(scene,scene.camera,p) for p in points]
   if any(not(.04<=p.x<=.96 and .04<=p.y<=.96) for p in projected):raise RuntimeError('Orbit clips a required assembly')
   name=kind+'-'+str(i).zfill(3);scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
   frames.append(dict(name=name,angleDegrees=i*10,minX=min(p.x for p in projected),maxX=max(p.x for p in projected),minY=min(p.y for p in projected),maxY=max(p.y for p in projected)))
  report['orbits'].append(dict(name=kind,assemblies=sorted(stems),center=list(target),fixedOrthoScale=scale,frames=frames))
 (out/'turntable.json').write_text(json.dumps(report,indent=2)+'\n')
else:
 views=[('whole',(-2,-4,1.6),(0,0,1.16),3.8),('reel',(-.45,-.75,.72),(0,-.12,.42),.50),('reel-back',(.45,.55,.55),(0,-.12,.42),.50),('guides',(-.20,-.45,1.38),(0,-.025,1.05),.56),('lure',(-.18,-.45,2.25),(0,-.13,2.12),.17),('roller',(.18,-.36,.60),(.043,-.145,.565),.075),('spool',(-.10,-.40,.68),(0,-.145,.528),.14),('seat',(-.18,-.34,.44),(0,-.01,.40),.28)]
 if a.motion=='bend':
  rig=bpy.data.objects['RodRig']
  for i in range(10):rig.pose.bones['RodBone'+str(i)].rotation_mode='XYZ';rig.pose.bones['RodBone'+str(i)].rotation_euler.x=.05*(i/9)**1.3
  bpy.context.view_layer.update();views=views[:1]
  update_line()
 for args in views:view(*args)
print('SPINNING_RIG_REVIEW_OK')
