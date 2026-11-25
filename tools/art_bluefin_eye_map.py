"""Place pupil pigmentation on the existing original iris map, keeping the rim exact."""
import math,sys
from pathlib import Path
from PIL import Image
from bluefin_eye_recipe import PUPIL_COLOR,pupil_cover
def update():
    path=Path(__file__).resolve().parents[1]/'game/Assets/OceanAngler/Art/Bluefin/Bluefin_Iris.png'
    image=Image.open(path);image.load()
    if image.size!=(256,256) or image.mode not in ('RGB','RGBA'):raise RuntimeError('Existing256 RGB/RGBA iris required')
    rgb=tuple(round(c*255) for c in PUPIL_COLOR)
    if image.getpixel((128,128))[:3]==rgb:
        inner=[image.getpixel((x,y))[:3] for y in range(256) for x in range(256) if pupil_cover(math.hypot((x-127.5)/127.5,(y-127.5)/127.5))==1]
        if not inner or any(pixel!=rgb for pixel in inner):raise RuntimeError('Partially updated pupil map; inspect before authoring')
        print('BLUEFIN_PUPIL_MAP_ALREADY_PRESENT');return
    pixels=image.load()
    for y in range(256):
        for x in range(256):
            alpha=pupil_cover(math.hypot((x-127.5)/127.5,(y-127.5)/127.5))
            if alpha==0:continue
            old=pixels[x,y];pixels[x,y]=tuple(round(old[i]*(1-alpha)+rgb[i]*alpha) for i in range(3))+old[3:]
    image.save(path)
if __name__=='__main__':
    if sys.argv[1:]!=['--update-existing']:raise SystemExit('Use tools/art.py build --asset bluefin --head-surface-only')
    update()
