"""Render saved slope source with its stored studio light/volume, not Player UI."""
import argparse,bpy,sys
from pathlib import Path
from mathutils import Vector
p=argparse.ArgumentParser();p.add_argument('--output',required=True);p.add_argument('--motion',default='idle');p.add_argument('--frame',type=int,default=1);p.add_argument('--turntable',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);folder=Path(a.output);scene=bpy.context.scene
for name,camera,target,lens in [('surface','SurfaceCamera','SurfaceTarget',28),('underwater','UnderwaterCamera','UnderwaterTarget',28),('survey-rov','ReefCamera','ReefTarget',35),('survey-boat','BoatCamera','BoatTarget',35)]:
 if a.motion!='idle' and a.motion!=name:continue
 scene.camera.location=bpy.data.objects[camera].location;scene.camera.rotation_euler=(bpy.data.objects[target].location-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.camera.data.lens=lens;scene.render.filepath=str(folder/(name+'.png'));bpy.ops.render.render(write_still=True)
print('SLOPE_STUDIO_REVIEW_OK')
