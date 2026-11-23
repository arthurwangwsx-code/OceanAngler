#!/usr/bin/env python3
"""Serialized Unity project entry points; raw logs remain local."""
import argparse, os, subprocess, json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('action',choices=['prepare','tests','ios','mac','bluefin','coral-bay']);p.add_argument('--build',type=int);p.add_argument('--label',default='a');a=p.parse_args()
editor=Path(os.environ.get('UNITY_EDITOR','/Applications/Unity/Hub/Editor/6000.3.29f1/Unity.app/Contents/MacOS/Unity'))
local=ROOT/'.local';local.mkdir(exist_ok=True)
if not editor.is_file():raise SystemExit('Unity Editor not found')
env=dict(os.environ);cmd=[str(editor),'-batchmode','-nographics','-projectPath',str(ROOT/'game'),'-logFile',str(local/(a.action+'.log'))]
if a.action=='tests':cmd+=['-runTests','-testPlatform','EditMode','-testResults',str(local/'editmode.xml')]
else:
 method='OceanAngler.Editor.BayAssetBuilder.Prepare' if a.action=='coral-bay' else 'OceanAngler.Editor.BluefinAssetBuilder.Prepare' if a.action=='bluefin' else 'OceanAngler.Editor.BuildTools.'+('Prepare' if a.action=='prepare' else 'ExportMac' if a.action=='mac' else 'ExportIOS')
 cmd+=['-quit','-executeMethod',method]
 if a.action=='mac':
  output=ROOT/'build/OceanAngler.app';output.parent.mkdir(exist_ok=True);env['OCEAN_MAC_OUTPUT']=str(output)
 if a.action=='ios':
  cmd+=['-buildTarget','iOS']
  if not a.build or a.build<1:raise SystemExit('positive --build required')
  if not a.label.isalnum():raise SystemExit('simple export label required')
  output=ROOT/'build'/('ios-'+str(a.build)+'-'+a.label)
  if output.exists():raise SystemExit('export already exists; use a new build number')
  output.parent.mkdir(exist_ok=True);env.update(OCEAN_BUILD_NUMBER=str(a.build),OCEAN_IOS_OUTPUT=str(output))
lock=local/'unity.lock'
try:fd=os.open(lock,os.O_CREAT|os.O_EXCL|os.O_WRONLY)
except FileExistsError:raise SystemExit('Unity job already active; verify the running process')
try:
 os.close(fd);result=subprocess.run(cmd,env=env,cwd=ROOT);print(json.dumps({'action':a.action,'exitCode':result.returncode,'log':str(local/(a.action+'.log'))}));raise SystemExit(result.returncode)
finally:lock.unlink()
