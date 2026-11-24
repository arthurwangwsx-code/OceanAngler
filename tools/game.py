#!/usr/bin/env python3
"""Serialized Unity project entry points; raw logs remain local."""
import argparse, os, subprocess, json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('action',choices=['waterline-review','motion-review','fish-lighting-review','prepare','tests','ios','simulator','pectoral-review','mac','bluefin','spinning-rig','coral-bay','nearshore-channel','continental-slope','shoal','water-review','fish-review','school-review','encounter-review','observation-review','channel-review','slope-review','fin-review','rig-review']);p.add_argument('--build',type=int);p.add_argument('--label',default='a');p.add_argument('--review-target',choices=['mac','ios'],default='mac');p.add_argument('--test-platform',choices=['EditMode','PlayMode'],default='EditMode');p.add_argument('--test-filter');p.add_argument('--motion-reference',type=Path);a=p.parse_args()
editor=Path(os.environ.get('UNITY_EDITOR','/Applications/Unity/Hub/Editor/6000.3.29f1/Unity.app/Contents/MacOS/Unity'))
local=ROOT/'.local';local.mkdir(exist_ok=True)
if not editor.is_file():raise SystemExit('Unity Editor not found')
env=dict(os.environ);log=local/(a.action+'.log')
reviews={'waterline-review':('waterline-runs','OCEAN_WATERLINE_REVIEW_OUTPUT','OceanAngler.Editor.WaterlineRenderReview.Capture'),'motion-review':('motion-runs','OCEAN_MOTION_REVIEW_OUTPUT','OceanAngler.Editor.FishMotionReview.Capture'),'fish-lighting-review':('fish-lighting-runs','OCEAN_FISH_LIGHTING_REVIEW_OUTPUT','OceanAngler.Editor.FishLightingReview.Capture'),'pectoral-review':('pectoral-runs','OCEAN_PECTORAL_REVIEW_OUTPUT','OceanAngler.Editor.PectoralRenderReview.Capture'),'rig-review':('rig-runs','OCEAN_RIG_REVIEW_OUTPUT','OceanAngler.Editor.RigRenderReview.Capture'),'fin-review':('fin-runs','OCEAN_FIN_REVIEW_OUTPUT','OceanAngler.Editor.FinRenderReview.Capture'),'slope-review':('slope-runs','OCEAN_SLOPE_REVIEW_OUTPUT','OceanAngler.Editor.WaterRenderReview.CaptureSlope'),'channel-review':('channel-runs','OCEAN_CHANNEL_REVIEW_OUTPUT','OceanAngler.Editor.WaterRenderReview.CaptureChannel'),'observation-review':('observation-runs','OCEAN_OBSERVATION_REVIEW_OUTPUT','OceanAngler.Editor.ObservationRenderReview.Capture'),'water-review':('water-runs','OCEAN_WATER_REVIEW_OUTPUT','OceanAngler.Editor.WaterRenderReview.Capture'),'fish-review':('fish-runs','OCEAN_FISH_REVIEW_OUTPUT','OceanAngler.Editor.FishRenderReview.Capture'),'school-review':('school-runs','OCEAN_SCHOOL_REVIEW_OUTPUT','OceanAngler.Editor.SchoolRenderReview.Capture'),'encounter-review':('encounter-runs','OCEAN_ENCOUNTER_REVIEW_OUTPUT','OceanAngler.Editor.EncounterRenderReview.Capture')}
gpu_review=a.action in reviews
if a.motion_reference is not None and a.action!='motion-review':raise SystemExit('motion reference requires motion-review')
if a.action=='motion-review':
 if a.motion_reference is None or not a.motion_reference.is_file():raise SystemExit('existing own reference FBX required')
 env['OCEAN_MOTION_REFERENCE_FBX']=str(a.motion_reference.resolve())
if gpu_review:
 env['OCEAN_REVIEW_TARGET']='iOS' if a.review_target=='ios' else 'StandaloneOSX'
 if not a.label.isalnum():raise SystemExit('simple review label required')
 folder=local/reviews[a.action][0]/a.label
 if folder.exists():raise SystemExit('Use a fresh review label; evidence is immutable')
 folder.mkdir(parents=True);log=folder/'review.log';env[reviews[a.action][1]]=str(folder)
cmd=[str(editor),'-batchmode']+(['-buildTarget','ios' if a.review_target=='ios' else 'osxuniversal'] if gpu_review else [])+([] if gpu_review else ['-nographics'])+['-projectPath',str(ROOT/'game'),'-logFile',str(log)]
if a.action=='tests':
 cmd+=['-runTests','-testPlatform',a.test_platform,'-testResults',str(local/(a.test_platform.lower()+'.xml'))]
 if a.test_filter:cmd+=['-testFilter',a.test_filter]
 if a.test_platform=='PlayMode':
  if not a.label.isalnum():raise SystemExit('simple test label required')
  save=local/'unity-test-saves'/a.label
  if save.exists():raise SystemExit('Use a fresh PlayMode label; test saves are isolated and retained')
  save.mkdir(parents=True);cmd+=['--ocean-save-root',str(save)]
else:
 method='OceanAngler.Editor.BuildTools.ExportIOSSimulator' if a.action=='simulator' else reviews[a.action][2] if gpu_review else 'OceanAngler.Editor.BaySchoolBuilder.Prepare' if a.action=='shoal' else 'OceanAngler.Editor.FishRenderReview.Capture' if a.action=='fish-review' else 'OceanAngler.Editor.WaterRenderReview.Capture' if a.action=='water-review' else 'OceanAngler.Editor.BayAssetBuilder.Prepare' if a.action=='coral-bay' else 'OceanAngler.Editor.SlopeAssetBuilder.Prepare' if a.action=='continental-slope' else 'OceanAngler.Editor.ChannelAssetBuilder.Prepare' if a.action=='nearshore-channel' else 'OceanAngler.Editor.SpinningRigAssetBuilder.Prepare' if a.action=='spinning-rig' else 'OceanAngler.Editor.BluefinAssetBuilder.Prepare' if a.action=='bluefin' else 'OceanAngler.Editor.BuildTools.'+('Prepare' if a.action=='prepare' else 'ExportMac' if a.action=='mac' else 'ExportIOS')
 cmd+=([] if a.action in ('waterline-review','motion-review','fish-lighting-review','pectoral-review','rig-review','fin-review','fish-review','school-review','encounter-review','observation-review') else ['-quit'])+['-executeMethod',method]
 if a.action=='mac':
  output=ROOT/'build/OceanAngler.app';output.parent.mkdir(exist_ok=True);env['OCEAN_MAC_OUTPUT']=str(output)
 if a.action=='simulator':
  if not a.label.isalnum():raise SystemExit('simple simulator export label required')
  output=ROOT/'build'/('ios-simulator-'+a.label)
  if output.exists():raise SystemExit('simulator export already exists; use a new label')
  output.parent.mkdir(exist_ok=True);env['OCEAN_SIMULATOR_OUTPUT']=str(output);cmd+=['-buildTarget','iOS']
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
graphics_path=ROOT/'game/ProjectSettings/GraphicsSettings.asset'
graphics_before=None
try:
 os.close(fd)
 if a.action=='fish-lighting-review':graphics_before=graphics_path.read_bytes()
 result=subprocess.run(cmd,env=env,cwd=ROOT)
 if gpu_review and result.returncode==0:
  receipt=folder/'receipt.json'
  if not receipt.is_file():raise SystemExit('GPU review receipt missing')
  state=json.loads(receipt.read_text()).get('status')
  if state!='PASS':raise SystemExit('GPU review result: '+str(state))
 print(json.dumps({'action':a.action,'exitCode':result.returncode,'log':str(log)}));raise SystemExit(result.returncode)
finally:
 try:
  if graphics_before is not None:
   import hashlib
   graphics_after=graphics_path.read_bytes()
   (folder/'graphics-settings-during.asset').write_bytes(graphics_after)
   permitted=graphics_before.replace(b'm_LightsUseColorTemperature: 0',b'm_LightsUseColorTemperature: 1')
   if graphics_after not in (graphics_before,permitted):raise RuntimeError('Unexpected GraphicsSettings mutation; preserved for inspection instead of overwriting')
   graphics_path.write_bytes(graphics_before)
   (folder/'graphics-settings-restoration.json').write_text(json.dumps(dict(status='PASS',scope='Restore only known Editor reflection-review color-temperature initialization; preserve all caller settings',beforeSha256=hashlib.sha256(graphics_before).hexdigest(),restoredSha256=hashlib.sha256(graphics_path.read_bytes()).hexdigest()),indent=2)+'\n')
 finally:lock.unlink()
