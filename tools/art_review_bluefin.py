"""Render the saved asset; do not regenerate or alter the canonical source."""
import argparse,bpy,sys,math
from pathlib import Path
from mathutils import Vector
p=argparse.ArgumentParser();p.add_argument('--output',required=True);p.add_argument('--motion',default='idle');p.add_argument('--frame',type=int,default=1);p.add_argument('--turntable',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:])
folder=Path(a.output);scene=bpy.context.scene;rig=bpy.data.objects['BluefinRig'];rig.animation_data.action=bpy.data.actions[a.motion];scene.frame_set(a.frame)
def view(name,position):
    scene.camera.location=position;scene.camera.rotation_euler=(Vector((.15,0,0))-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(folder/(name+'.png'));bpy.ops.render.render(write_still=True)
if a.turntable:
    scene.render.resolution_x=960;scene.render.resolution_y=640;scene.cycles.samples=8
    for i in range(36):
        theta=i/36*math.tau;scene.frame_set(1+i%30)
        view('turn-'+str(i).zfill(3),(.15+math.sin(theta)*5,-math.cos(theta)*5,.8))
else:
    views=[('three-quarter',(-1.2,-5,1.05)),('side',(0,-5,.04)),('top',(.2,-.02,5)),('front',(-5,-.04,.1))]
    for name,position in (views if a.motion=='idle' else views[:1]):view(name,position)
print('BLUEFIN_REVIEW_RENDER_OK')
