#!/usr/bin/env python3
"""Serial authoring, immutable logs, and strict Blender Python exit handling."""
import argparse,json,os,shutil,subprocess,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def main():
    p=argparse.ArgumentParser();p.add_argument('action',choices=['build','validate','review']);p.add_argument('--label',required=True);p.add_argument('--no-preview',action='store_true');p.add_argument('--motion',default='idle');p.add_argument('--frame',type=int,default=1);p.add_argument('--turntable',action='store_true');a=p.parse_args()
    if not a.label.replace('-','').isalnum():raise RuntimeError('Use a simple unique evidence label')
    folder=ROOT/'.local/art-runs'/a.label
    if folder.exists():raise RuntimeError('Evidence run already exists')
    folder.mkdir(parents=True)
    blender=Path(os.environ.get('BLENDER','/Applications/Blender.app/Contents/MacOS/Blender'))
    args=[str(blender),'--background','--threads','8','--python-exit-code','1']
    if a.action!='build':args+=[str(ROOT/'art/source/bluefin/Bluefin.blend')]
    args+=['--python',str(ROOT/'tools'/('art_bluefin.py' if a.action=='build' else 'art_validate_bluefin.py' if a.action=='validate' else 'art_review_bluefin.py'))]
    if a.action=='build' and a.no_preview:args+=['--','--no-preview']
    if a.action=='review':args+=['--','--output',str(folder),'--motion',a.motion,'--frame',str(a.frame)]+(['--turntable'] if a.turntable else [])
    lock=ROOT/'.local/unity.lock';owns_lock=False
    try:
        if a.action=='build':fd=os.open(lock,os.O_CREAT|os.O_EXCL|os.O_WRONLY);os.close(fd);owns_lock=True
        with (folder/'process.log').open('w') as output:r=subprocess.run(args,cwd=ROOT,stdout=output,stderr=subprocess.STDOUT)
        valid=r.returncode==0
        if a.action=='validate':
            receipt=ROOT/'.local/art/bluefin/validation.json'
            if receipt.exists():
                shutil.copyfile(receipt,folder/'validation.json');valid=valid and json.loads(receipt.read_text())['status']=='ENGINEERING_CHECKS_PASS'
            else:valid=False
        if a.action=='build':
            receipt=ROOT/'art/source/bluefin/build-report.json'
            valid=valid and receipt.is_file() and (ROOT/'game/Assets/OceanAngler/Art/Bluefin/Bluefin.fbx').is_file()
            if receipt.exists():shutil.copyfile(receipt,folder/'build-report.json')
        result=dict(action=a.action,status='PASS' if valid else 'FAIL',exitCode=r.returncode,label=a.label)
        (folder/'receipt.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
        return 0 if valid else 1
    finally:
        if owns_lock:lock.unlink()
if __name__=='__main__':raise SystemExit(main())
