#!/usr/bin/env python3
"""Serialized Unity project entry points; raw logs remain local."""
import argparse, os, subprocess, json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('action',choices=['prepare','tests','ios','mac','bluefin','spinning-rig','coral-bay','shoal','water-review','fish-review','school-review','encounter-review']);p.add_argument('--build',type=int);p.add_argument('--label',default='a');p.add_argument('--test-platform',choices=['EditMode','PlayMode'],default='EditMode');a=p.parse_args()
editor=Path(os.environ.get('UNITY_EDITOR','/Applications/Unity/Hub/Editor/6000.3.29f1/Unity.app/Contents/MacOS/Unity'))
local=ROOT/'.local';local.mkdir(exist_ok=True)
if not editor.is_file():raise SystemExit('Unity Editor not found')
env=dict(os.environ);log=local/(a.action+'.log')
reviews={'water-review':('water-runs','OCEAN_WATER_REVIEW_OUTPUT','OceanAngler.Editor.WaterRenderReview.Capture'),'fish-review':('fish-runs','OCEAN_FISH_REVIEW_OUTPUT','OceanAngler.Editor.FishRenderReview.Capture'),'school-review':('school-runs','OCEAN_SCHOOL_REVIEW_OUTPUT','OceanAngler.Editor.SchoolRenderReview.Capture'),'encounter-review':('encounter-runs','OCEAN_ENCOUNTER_REVIEW_OUTPUT','OceanAngler.Editor.EncounterRenderReview.Capture')}
gpu_review=a.action in reviews
if gpu_review:
 if not a.label.isalnum():raise SystemExit('simple review label required')
 folder=local/reviews[a.action][0]/a.label
 if folder.exists():raise SystemExit('Use a fresh review label; evidence is immutable')
 folder.mkdir(parents=True);log=folder/'review.log';env[reviews[a.action][1]]=str(folder)
cmd=[str(editor),'-batchmode']+([] if gpu_review else ['-nographics'])+['-projectPath',str(ROOT/'game'),'-logFile',str(log)]
if a.action=='tests':
 cmd+=['-runTests','-testPlatform',a.test_platform,'-testResults',str(local/(a.test_platform.lower()+'.xml'))]
 if a.test_platform=='PlayMode':
  if not a.label.isalnum():raise SystemExit('simple test label required')
  save=local/'unity-test-saves'/a.label
  if save.exists():raise SystemExit('Use a fresh PlayMode label; test saves are isolated and retained')
  save.mkdir(parents=True);cmd+=['--ocean-save-root',str(save)]
else:
 method=reviews[a.action][2] if gpu_review else 'OceanAngler.Editor.BaySchoolBuilder.Prepare' if a.action=='shoal' else 'OceanAngler.Editor.FishRenderReview.Capture' if a.action=='fish-review' else 'OceanAngler.Editor.WaterRenderReview.Capture' if a.action=='water-review' else 'OceanAngler.Editor.BayAssetBuilder.Prepare' if a.action=='coral-bay' else 'OceanAngler.Editor.SpinningRigAssetBuilder.Prepare' if a.action=='spinning-rig' else 'OceanAngler.Editor.BluefinAssetBuilder.Prepare' if a.action=='bluefin' else 'OceanAngler.Editor.BuildTools.'+('Prepare' if a.action=='prepare' else 'ExportMac' if a.action=='mac' else 'ExportIOS')
 cmd+=([] if a.action in ('fish-review','school-review','encounter-review') else ['-quit'])+['-executeMethod',method]
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
 os.close(fd);result=subprocess.run(cmd,env=env,cwd=ROOT)
 if gpu_review and result.returncode==0:
  receipt=folder/'receipt.json'
  if not receipt.is_file() or json.loads(receipt.read_text()).get('status')!='PASS':raise SystemExit('GPU review receipt missing or failed')
 print(json.dumps({'action':a.action,'exitCode':result.returncode,'log':str(log)}));raise SystemExit(result.returncode)
finally:lock.unlink()
