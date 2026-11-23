#!/usr/bin/env python3
"""Original app icon, drawn from project-owned geometric artwork."""
from PIL import Image,ImageDraw
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[1]
image=Image.new('RGB',(1024,1024),(6,35,52));d=ImageDraw.Draw(image)
for r in range(470,0,-1):
 t=r/470;d.ellipse((512-r,512-r,512+r,512+r),fill=(int(10+8*(1-t)),int(76+42*(1-t)),int(93+32*(1-t))))
d.arc((155,100,865,810),205,340,fill=(80,204,201),width=20)
d.polygon([(686,493),(838,359),(808,515),(838,666),(686,554)],fill=(37,165,191))
d.ellipse((205,371,737,648),fill=(218,230,210))
d.ellipse((225,377,716,570),fill=(118,193,197))
d.polygon([(416,399),(541,259),(590,405)],fill=(46,153,180))
d.polygon([(437,619),(557,713),(583,613)],fill=(46,153,180))
d.ellipse((281,448,328,495),fill=(7,37,52));d.ellipse((288,452,301,465),fill=(240,250,238))
d.arc((242,447,472,627),-70,70,fill=(47,120,135),width=9)
d.line([(748,177),(748,296)],fill=(251,195,80),width=13);d.arc((690,254,805,369),0,180,fill=(251,195,80),width=13);d.polygon([(692,303),(713,328),(689,337)],fill=(251,195,80))
p=ROOT/'game/Assets/OceanAngler/Art/AppIcon.png';p.parent.mkdir(parents=True,exist_ok=True);image.save(p)
print(json.dumps(dict(path=str(p.relative_to(ROOT)),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),author='OceanAngler contributors',license='MIT')))
