"""从保存的源文件生成全竿、卷线器、导环与拟饵检查图，不重建源资产。"""
import bpy,argparse,sys,math
from pathlib import Path
from mathutils import Vector
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
 for i in range(36):
  t=i/36*math.tau;view('turn-'+str(i).zfill(3),(math.sin(t)*.65,-.145-math.cos(t)*.65,.48),(0,-.145,.43),.32)
else:
 views=[('whole',(-2,-4,1.6),(0,0,1.16),3.8),('reel',(-.45,-.75,.72),(0,-.12,.42),.50),('reel-back',(.45,.55,.55),(0,-.12,.42),.50),('guides',(-.20,-.45,1.38),(0,-.025,1.05),.56),('lure',(-.18,-.45,2.25),(0,-.13,2.12),.17),('roller',(.18,-.36,.60),(.043,-.145,.565),.075),('spool',(-.10,-.40,.68),(0,-.145,.528),.14),('seat',(-.18,-.34,.44),(0,-.01,.40),.28)]
 if a.motion=='bend':
  rig=bpy.data.objects['RodRig']
  for i in range(10):rig.pose.bones['RodBone'+str(i)].rotation_mode='XYZ';rig.pose.bones['RodBone'+str(i)].rotation_euler.x=.05*(i/9)**1.3
  bpy.context.view_layer.update();views=views[:1]
  update_line()
 for args in views:view(*args)
print('SPINNING_RIG_REVIEW_OK')
