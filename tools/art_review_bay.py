"""从实际保存的源文件渲染水上、水下、礁石和码头检查图。"""
import bpy,argparse,sys,math
from pathlib import Path
from mathutils import Vector
p=argparse.ArgumentParser();p.add_argument('--output',required=True);p.add_argument('--motion',default='idle');p.add_argument('--frame',default=1,type=int);p.add_argument('--turntable',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);out=Path(a.output);scene=bpy.context.scene;scene.camera.data.clip_end=30000
views=[('underwater',(0,-6.5,1.25),(0,9,-.15),26),('surface',(1,-7,6.1),(-3,5,4.6),27),('reef',(3,4,1.1),(5,11,-.1),38),('pier',(-.3,1,6.7),(-4,4,4.5),34)]
if a.motion=='shadow':
 scene.world.node_tree.nodes['Background'].inputs[1].default_value=.18;bpy.data.lights['BaySun'].energy=.7
 views=views[:1]
for name,position,target,lens in views:
 scene.camera.location=position;scene.camera.rotation_euler=(Vector(target)-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.camera.data.lens=lens;scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
print('CORAL_BAY_REVIEW_OK')
