#!/usr/bin/env python3
"""Serial authoring, immutable logs, and strict Blender Python exit handling."""
import argparse,json,os,shutil,subprocess,time,sys
from pathlib import Path
from storage_gate import storage_command
ROOT=Path(__file__).resolve().parents[1]
def main():
    p=argparse.ArgumentParser();p.add_argument('action',choices=['build','validate','review']);p.add_argument('--asset',choices=['halibut','bluefin','spinning-rig','coral-bay','nearshore-channel','continental-slope'],default='bluefin');p.add_argument('--label',required=True);p.add_argument('--no-preview',action='store_true');p.add_argument('--maps-only',action='store_true');p.add_argument('--motion',default='idle');p.add_argument('--frame',type=int,default=1);p.add_argument('--turntable',action='store_true');p.add_argument('--eyes-only',action='store_true');p.add_argument('--mouth-only',action='store_true')
    p.add_argument('--motions-only',action='store_true')
    p.add_argument('--fin-motion-only',action='store_true')
    p.add_argument('--head-surface-only',action='store_true')
    p.add_argument('--opercular-only',action='store_true')
    p.add_argument('--pectoral-flex-only',action='store_true')
    # Parse the extra opt-in before validating mutually exclusive update modes.
    a=p.parse_args()
    if a.pectoral_flex_only and (a.action!='build' or a.asset!='bluefin' or any((a.maps_only,a.motions_only,a.fin_motion_only,a.head_surface_only,a.opercular_only))):raise RuntimeError('Paired-fin flex requires only the existing bluefin source')
    if a.opercular_only and (a.action!='build' or a.asset!='bluefin' or any((a.maps_only,a.motions_only,a.fin_motion_only,a.head_surface_only))):raise RuntimeError('Opercular update requires only the existing bluefin source')
    if (a.maps_only or a.motions_only) and (a.action!='build' or a.asset!='bluefin' or a.maps_only and a.motions_only):raise RuntimeError('Use one update mode for an existing bluefin build')
    if a.fin_motion_only and (a.action!='build' or a.asset!='halibut' or a.maps_only or a.motions_only):raise RuntimeError('Fin motion update requires the existing halibut source')
    if a.head_surface_only and (a.action!='build' or a.asset!='bluefin' or a.maps_only or a.motions_only or a.fin_motion_only):raise RuntimeError('Head surface update requires the existing bluefin source')
    if a.eyes_only and (a.action!='review' or a.asset!='bluefin' or a.turntable):raise RuntimeError('eyes-only requires bluefin static review')
    if a.mouth_only and (a.action!='review' or a.asset!='bluefin' or a.turntable or a.eyes_only):raise RuntimeError('mouth-only requires a separate bluefin static review')
    if not a.label.replace('-','').isalnum():raise RuntimeError('Use a simple unique evidence label')
    folder=ROOT/'.local/art-runs'/a.label
    if folder.exists():raise RuntimeError('Evidence run already exists')
    folder.mkdir(parents=True)
    blender=Path(os.environ.get('BLENDER','/Applications/Blender.app/Contents/MacOS/Blender'))
    args=[str(blender),'--background','--factory-startup','--threads','8','--python-exit-code','1']
    name,stem={'halibut':('Halibut','halibut'),'bluefin':('Bluefin','bluefin'),'spinning-rig':('SpinningRig','spinning_rig'),'coral-bay':('CoralBay','bay'),'nearshore-channel':('NearshoreChannel','channel'),'continental-slope':('ContinentalSlope','slope')}[a.asset]
    if a.action!='build':args+=[str(ROOT/'art/source'/a.asset/(name+'.blend'))]
    args+=['--python',str(ROOT/'tools'/('art_bluefin_pectoral_motion.py' if a.pectoral_flex_only else 'art_bluefin_gill.py' if a.opercular_only else 'art_bluefin_head_surface.py' if a.head_surface_only else 'art_halibut_fin_motion.py' if a.fin_motion_only else 'art_bluefin_motions.py' if a.motions_only else 'art_'+stem+'.py' if a.action=='build' else 'art_validate_'+stem+'.py' if a.action=='validate' else 'art_review_'+stem+'.py'))]
    if a.motions_only:args+=['--','--update-existing']
    elif a.action=='build' and (a.no_preview or a.maps_only):args+=['--']+(['--no-preview'] if a.no_preview else [])+(['--maps-only'] if a.maps_only else [])
    if a.action=='review':args+=['--','--output',str(folder),'--motion',a.motion,'--frame',str(a.frame)]+(['--turntable'] if a.turntable else [])+(['--eyes-only'] if a.eyes_only else [])+(['--mouth-only'] if a.mouth_only else [])
    lock=ROOT/'.local/unity.lock';owns_lock=False
    try:
        if a.action=='build':fd=os.open(lock,os.O_CREAT|os.O_EXCL|os.O_WRONLY);os.close(fd);owns_lock=True
        with (folder/'process.log').open('w') as output:
            if a.head_surface_only:
                map_result=subprocess.run([sys.executable,str(ROOT/'tools/art_bluefin_eye_map.py'),'--update-existing'],cwd=ROOT,stdout=output,stderr=subprocess.STDOUT)
                if map_result.returncode:raise RuntimeError('Eye map update failed; inspect retained evidence')
            r=subprocess.run(storage_command(args,ROOT,14),cwd=ROOT,stdout=output,stderr=subprocess.STDOUT)
        valid=r.returncode==0
        if a.action=='validate':
            receipt=ROOT/'.local/art'/a.asset/'validation.json'
            if receipt.exists():
                shutil.copyfile(receipt,folder/'validation.json');valid=valid and json.loads(receipt.read_text())['status']=='ENGINEERING_CHECKS_PASS'
            else:valid=False
        if a.action=='build':
            receipt=ROOT/'art/source'/a.asset/'build-report.json'
            valid=valid and receipt.is_file() and (ROOT/'game/Assets/OceanAngler/Art'/name/(name+'.fbx')).is_file()
            if receipt.exists():shutil.copyfile(receipt,folder/'build-report.json')
        result=dict(action=a.action,status='PASS' if valid else 'FAIL',exitCode=r.returncode,label=a.label)
        (folder/'receipt.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
        return 0 if valid else 1
    finally:
        if owns_lock:lock.unlink()
if __name__=='__main__':raise SystemExit(main())
