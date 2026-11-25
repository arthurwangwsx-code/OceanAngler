"""Render saved halibut; source/FBX remain untouched."""
import argparse,bpy,sys,math
from pathlib import Path
from mathutils import Vector
p=argparse.ArgumentParser();p.add_argument('--output',required=True);p.add_argument('--motion',default='idle');p.add_argument('--frame',type=int,default=1);p.add_argument('--turntable',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);folder=Path(a.output);scene=bpy.context.scene;rig=bpy.data.objects['HalibutRig'];rig.animation_data.action=bpy.data.actions[a.motion];scene.frame_set(a.frame)
def view(name,position,target=(.15,0,0),scale=3.05):
    camera=scene.camera;camera.data.ortho_scale=scale;camera.location=position
    if abs(position[2]-target[2])>4 and math.hypot(position[0]-target[0],position[1]-target[1])<.12:camera.rotation_euler=(0 if position[2]>target[2] else math.pi,0,0)
    else:camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(folder/(name+'.png'));bpy.ops.render.render(write_still=True)
if a.turntable:
    scene.render.resolution_x=960;scene.render.resolution_y=640;scene.cycles.samples=8
    for i in range(36):
        theta=i/36*math.tau;scene.frame_set(1+round(i/36*60));view('turn-'+str(i).zfill(3),(.15+math.sin(theta)*4,-math.cos(theta)*4,2.2))
else:
    for name,loc in [('eyed',(.1,0,5)),('blind',(.1,0,-5)),('three-quarter',(-1,-3,4)),('edge',(0,-5,.06)),('front',(-5,0,.12))]:view(name,loc)
    view('eyes',(-.8,-.15,5),(-.74,-.12,.06),.43)
    view('mouth',(-4,-2,1),(-.97,.015,0),.32)
print('HALIBUT_REVIEW_RENDER_OK')
