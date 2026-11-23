"""从保存的源文件生成全竿、卷线器、导环与拟饵检查图，不重建源资产。"""
import bpy,argparse,sys,math
from pathlib import Path
from mathutils import Vector
p=argparse.ArgumentParser();p.add_argument('--output',required=True);p.add_argument('--motion',default='idle');p.add_argument('--frame',default=1,type=int);p.add_argument('--turntable',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);out=Path(a.output);scene=bpy.context.scene
def view(name,position,target,scale):
 scene.camera.data.ortho_scale=scale;scene.camera.location=position;scene.camera.rotation_euler=(Vector(target)-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
if a.turntable:
 scene.render.resolution_x=960;scene.render.resolution_y=640;scene.cycles.samples=8
 for i in range(36):
  t=i/36*math.tau;view('turn-'+str(i).zfill(3),(math.sin(t)*.65,-.145-math.cos(t)*.65,.48),(0,-.145,.43),.32)
else:
 views=[('whole',(-2,-4,1.6),(0,0,1.16),3.8),('reel',(-.45,-.75,.72),(0,-.12,.42),.50),('reel-back',(.45,.55,.55),(0,-.12,.42),.50),('guides',(-.20,-.45,1.38),(0,-.025,1.05),.56),('lure',(-.18,-.45,2.25),(0,-.13,2.12),.17)]
 if a.motion=='bend':
  rig=bpy.data.objects['RodRig']
  for i in range(10):rig.pose.bones['RodBone'+str(i)].rotation_mode='XYZ';rig.pose.bones['RodBone'+str(i)].rotation_euler.x=.05*(i/9)**1.3
  bpy.context.view_layer.update();views=views[:1]
  line=bpy.data.objects['StudioLine'];coords=[bpy.data.objects['LineExit'].matrix_world.translation]+[bpy.data.objects['Guide'+str(i)].matrix_world.translation for i in range(9)]+[bpy.data.objects['LureNose'].matrix_world.translation]
  for p,co in zip(line.data.splines[0].points,coords):p.co=(*co,1)
 for args in views:view(*args)
print('SPINNING_RIG_REVIEW_OK')
