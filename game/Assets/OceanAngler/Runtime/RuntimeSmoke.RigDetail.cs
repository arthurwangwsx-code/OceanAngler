using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace OceanAngler
{
    public sealed partial class RuntimeSmoke
    {
        [Serializable] sealed class RigSample {public string view,phase;public bool rollerRoute;public int requestedLod;public float bailDegrees,rollerDegrees,guideError,surfaceGap,tangentDot,payoutPlaneError,payoutRadiusError,lineWidth;}
        [Serializable] sealed class RigDetailResult {public string status,scope;public string[] failures;public List<RigSample> samples;public int catchesBefore,catchesAfter,pointsBefore,pointsAfter;public float rollerTravel;}
        readonly List<RigSample> rigSamples=new List<RigSample>();float rigRollerTravel;int rigRequestedLod=-1;
        public IEnumerator RunRigDetailReview(string outputFolder,bool captureFrames,bool quitProcess=false)
        {
            folder=Path.GetFullPath(outputFolder);Directory.CreateDirectory(folder);if(File.Exists(Path.Combine(folder,"receipt.json")))throw new InvalidOperationException("Use a new rig review folder");
            game=GetComponent<GameController>();began=Time.realtimeSinceStartup;finished=false;journeyMode=gearReviewMode=true;journeyQuit=quitProcess;completeContent=false;rigSamples.Clear();rigRollerTravel=0;rigRequestedLod=-1;
            receipt=new Receipt{captureEnabled=captureFrames,scope=captureFrames?"native first-tutorial rig16-frame EventSystem review including forcedLOD1/2 diagnostic; not automatic distance selection, OS touch, full progression, CAD or device qualification":"headless rig review; no rendered acceptance"};
            var before=game.Progress.Data.Clone();var stack=new Stack<IEnumerator>();stack.Push(RigDetailReview());
            while(stack.Count>0&&!finished)
            {
                object next=null;bool moved=false;Exception error=null;try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){error=e;}
                if(error!=null){receipt.failures.Add("rig-detail-exception:"+error.GetType().Name+":"+error.Message);break;}
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();if(!finished)FinishJourney();
            File.WriteAllText(Path.Combine(folder,"rig-detail.json"),JsonUtility.ToJson(new RigDetailResult{status=receipt.status,scope=receipt.scope,failures=receipt.failures.ToArray(),samples=new List<RigSample>(rigSamples),catchesBefore=before.catches.Count,catchesAfter=game.Progress.Data.catches.Count,pointsBefore=before.totalPoints,pointsAfter=game.Progress.Data.totalPoints,rollerTravel=rigRollerTravel},true));
        }
        void CheckRig(string view)
        {
            var rig=FindFirstObjectByType<AuthoredSpinningRig>();JourneyRequire(rig!=null,"rig-detail-present");
            var lods=rig.GetComponent<LODGroup>().GetLODs();JourneyRequire(lods.Length==3&&lods.All(l=>l.renderers.Length==8)&&lods.SelectMany(l=>l.renderers).Distinct().Count()==24,"rig-detail-eight-assemblies-three-lods-single-owner");
            float radius=Vector3.Distance(rig.roller.position,rig.rollerSurface.position);var normal=(rig.lineExit.position-rig.roller.position).normalized;
            var axis=(rig.rotorAxis.position-rig.rotor.position).normalized;var payout=rig.fishingLine.GetPosition(0)-rig.spoolLipCenter.position;
            var sample=new RigSample{view=view,phase=game.Session.Phase.ToString(),rollerRoute=rig.LinePassesRoller,requestedLod=rigRequestedLod,bailDegrees=rig.BailOpenDegrees,rollerDegrees=rig.RollerDegrees,lineWidth=rig.fishingLine.startWidth,guideError=rig.MaximumGuideLineError(),surfaceGap=Vector3.Distance(rig.lineExit.position,rig.roller.position)-radius,tangentDot=Mathf.Abs(Vector3.Dot((rig.guides[0].position-rig.lineExit.position).normalized,normal)),payoutPlaneError=Mathf.Abs(Vector3.Dot(payout,axis)),payoutRadiusError=Mathf.Abs(payout.magnitude-Vector3.Distance(rig.spoolLipSurface.position,rig.spoolLipCenter.position)-rig.fishingLine.startWidth*.5f)};rigSamples.Add(sample);
            JourneyRequire(sample.guideError<.0001f&&sample.tangentDot<.0005f&&Mathf.Abs(sample.surfaceGap-rig.fishingLine.startWidth*.5f)<.0001f,"rig-detail-guides-and-groove-contact-"+view);
            JourneyRequire(rig.LinePassesRoller?Vector3.Distance(rig.fishingLine.GetPosition(0),rig.lineExit.position)<.0001f:sample.payoutPlaneError<.0001f&&sample.payoutRadiusError<.0001f,"rig-detail-active-line-route-"+view);
        }
        IEnumerator RigFrame(string name){CheckRig(name);yield return JourneyFrame(name);}
        IEnumerator RigDetailReview()
        {
            yield return new WaitForSecondsRealtime(1);JourneyRequire(!game.Progress.RequiresRecovery&&game.Progress.Data.catches.Count==0&&game.Progress.Data.totalPoints==0,"rig-detail-new-normal-tutorial-save");
            yield return JourneyClick("钓具",true);yield return null;yield return null;yield return JourneyClick("观察入门钓组",true);yield return null;yield return null;yield return JourneyClick("近看卷线器",true);yield return null;yield return null;
            var rig=FindFirstObjectByType<AuthoredSpinningRig>();JourneyRequire(GetComponent<GameUI>().InspectingGear&&game.Session.Paused,"rig-detail-inspection-pauses-fishing");yield return RigFrame("rig-rest");
            // Development review only: inspect both distance meshes at the
            // existing reel camera. Always restore automatic selection.
            var group=rig.GetComponent<LODGroup>();
            try
            {
                for(int level=1;level<=2;level++){group.ForceLOD(level);rigRequestedLod=level;yield return null;yield return null;yield return RigFrame("rig-forced-lod"+level);}
            }
            finally{group.ForceLOD(-1);rigRequestedLod=-1;}
            yield return null;yield return null;
            yield return JourneyClick("试摇卷线器",true);var rotation=rig.roller.localRotation;float end=Time.realtimeSinceStartup+.25f;
            while(Time.realtimeSinceStartup<end){yield return null;rigRollerTravel+=Quaternion.Angle(rotation,rig.roller.localRotation);rotation=rig.roller.localRotation;}
            JourneyRequire(rigRollerTravel>5,"rig-detail-real-roller-turns");yield return RigFrame("rig-winding-a");yield return new WaitForSecondsRealtime(.17f);yield return RigFrame("rig-winding-b");
            yield return JourneyClick("停止收线演示",true);yield return null;yield return null;rotation=rig.roller.localRotation;yield return new WaitForSecondsRealtime(.15f);JourneyRequire(Quaternion.Angle(rotation,rig.roller.localRotation)<.02f,"rig-detail-stopped-roller-freezes");
            for(int i=0;i<3;i++){yield return JourneyClick("旋转钓组",true);yield return null;yield return null;yield return RigFrame("rig-angle-"+i);}
            yield return JourneyClick("查看全竿",true);yield return null;yield return null;yield return RigFrame("rig-whole");
            yield return JourneyClick("返回钓具",true);yield return null;yield return null;yield return JourneyClick("出海",true);yield return null;yield return null;yield return JourneyClick("继续探索",true);yield return null;yield return null;yield return RigFrame("ready");
            var cast=Find("按住蓄力");Pointer(cast,true);yield return new WaitForSecondsRealtime(.3f);Pointer(cast,false);float deadline=Time.realtimeSinceStartup+5;
            while(game.Session.Phase==FishingPhase.Casting&&rig.BailOpenDegrees<60&&Time.realtimeSinceStartup<deadline)yield return null;
            JourneyRequire(game.Session.Phase==FishingPhase.Casting&&!rig.LinePassesRoller,"rig-detail-natural-cast-bypasses-roller");yield return RigFrame("casting-open-bail");
            deadline=Time.realtimeSinceStartup+20;while((game.Session.Phase==FishingPhase.Casting||rig.BailOpenDegrees>0)&&Time.realtimeSinceStartup<deadline)yield return null;
            JourneyRequire(rig.LinePassesRoller,"rig-detail-waiting-restores-roller");yield return RigFrame("waiting-line-contact");
            while(game.Session.Phase!=FishingPhase.Bite&&Time.realtimeSinceStartup<deadline)yield return null;JourneyRequire(game.Session.Phase==FishingPhase.Bite,"rig-detail-natural-bite");yield return null;yield return null;yield return JourneyClick("提竿",true);yield return null;yield return null;yield return RigFrame("fighting");
            var reel=Find("按住收线");bool held=false;deadline=Time.realtimeSinceStartup+80;
            while(game.Session.Phase==FishingPhase.Fighting&&Time.realtimeSinceStartup<deadline){bool wanted=game.Session.Tension<.65f;if(wanted!=held){held=wanted;Pointer(reel,held);}yield return null;}
            if(held)Pointer(reel,false);yield return null;yield return null;JourneyRequire(game.Session.Phase==FishingPhase.Landed,"rig-detail-normal-landed");yield return RigFrame("landed");
            yield return JourneyClick("记录并放流",true);yield return null;yield return null;yield return new WaitForSecondsRealtime(.65f);yield return RigFrame("release");
            JourneyRequire(game.Progress.Data.catches.Count==1&&game.Progress.Data.catches[0].fishId=="trevally"&&game.Progress.Data.catches[0].released&&game.Progress.Data.totalPoints==game.Progress.Data.catches[0].awardedPoints,"rig-detail-first-hero-recorded-once");
            yield return JourneyClick("暂停",true);yield return null;yield return null;yield return JourneyFrame("recorded-home");
        }
    }
}
