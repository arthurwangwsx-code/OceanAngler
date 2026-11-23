#!/usr/bin/env python3
"""Run an isolated native UI smoke test and verify its own receipt/screenshots."""
import argparse,json,plistlib,re,struct,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--label',required=True);p.add_argument('--width',type=int,default=1440);p.add_argument('--height',type=int,default=900);p.add_argument('--point-scale',type=float,default=1);p.add_argument('--safe-insets',default='0,0,0,0');a=p.parse_args()
if not a.label.replace('-','').isalnum() or min(a.width,a.height)<600:raise SystemExit('Invalid test label or size')
folder=ROOT/'.local/runs'/a.label
if folder.exists():raise SystemExit('Use a fresh label; evidence is immutable')
folder.mkdir(parents=True)
app=ROOT/'build/OceanAngler.app';info=plistlib.loads((app/'Contents/Info.plist').read_bytes());binary=app/'Contents/MacOS'/info['CFBundleExecutable']
args=[str(binary),'--ocean-smoke',str(folder/'screens'),'--ocean-save-root',str(folder/'save'),'--ocean-point-scale',str(a.point_scale),'--ocean-safe-insets',a.safe_insets,'-screen-width',str(a.width),'-screen-height',str(a.height),'-screen-fullscreen','0','-logFile',str(folder/'player.log')]
try:
 with (folder/'process.log').open('w') as output:result=subprocess.run(args,cwd=ROOT,stdout=output,stderr=subprocess.STDOUT,timeout=140)
except subprocess.TimeoutExpired:raise SystemExit('Native smoke timed out; see private log')
receipt=json.loads((folder/'screens/receipt.json').read_text())
images=list((folder/'screens').glob('*.png'));dimensions={}
for image in images:
 raw=image.read_bytes();assert raw[:8]==b'\x89PNG\r\n\x1a\n';dimensions[image.name]=list(struct.unpack('>II',raw[16:24]))
summary=dict(status=receipt['status'],exitCode=result.returncode,checks=receipt['checks'],failures=receipt['failures'],catches=receipt['catches'],points=receipt['points'],screenshots=dimensions,pixelsPerPoint=receipt.get('pixelsPerPoint'),safeWidthPoints=receipt.get('safeWidthPoints'),safeHeightPoints=receipt.get('safeHeightPoints'))
problems=[line for line in (folder/'player.log').read_text(errors='replace').splitlines() if re.search(r'Exception:|is registered with more than one LODGroup|Shader .*not supported|The referenced script .*missing',line)]
if problems:summary['status']='FAIL';summary['failures']+=['runtime-log:'+line[:180] for line in problems]
(folder/'summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n');print(json.dumps(summary,ensure_ascii=False,indent=2))
raise SystemExit(0 if result.returncode==0 and summary['status']=='PASS' and len(images)==12 else 1)
