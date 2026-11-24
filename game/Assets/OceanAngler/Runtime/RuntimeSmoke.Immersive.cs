using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace OceanAngler
{
    public sealed partial class RuntimeSmoke
    {
        [Serializable] sealed class HudSample { public string phase;public bool leftHanded,centralClear;public int buttons;public float occupiedRatio; }
        [Serializable] sealed class MeterLabelSample { public string text;public float preferredWidth,availableWidth,preferredHeight,availableHeight;public bool fits; }
        [Serializable] sealed class WaterlineSample { public string stage,phase;public bool leftHanded,surface;public float seaLevel,cameraY,rigY,lureY,fade,guideError; }
        [Serializable] sealed class RestTimerResult { public string status="RUNNING",scope="real unmodified active timer on a QA player; not child or physical-device qualification";public float observedSeconds;public bool paused,reminderDue,acknowledged,progressUnchanged; }
        [Serializable] sealed class ImmersiveResult { public string status,scope;public string[] failures;public List<HudSample> samples;public List<MeterLabelSample> meterLabels;public List<WaterlineSample> waterline;public int catchesBefore,catchesAfter,pointsBefore,pointsAfter;public bool unfinishedExitPreservedLedger,settingsRestored;public RestTimerResult restTimer; }
        RestTimerResult restTimer;
        readonly List<HudSample> hudSamples=new List<HudSample>();
        readonly List<MeterLabelSample> meterLabels=new List<MeterLabelSample>();
        readonly HashSet<string> seenMeterLabels=new HashSet<string>();
        readonly List<WaterlineSample> waterlineSamples=new List<WaterlineSample>();bool waterlineReview;
        public IEnumerator RunImmersiveReview(string outputFolder,bool captureFrames,bool quitProcess=false,bool checkWaterline=false)
        {
            folder=Path.GetFullPath(outputFolder);Directory.CreateDirectory(folder);if(File.Exists(Path.Combine(folder,"receipt.json")))throw new InvalidOperationException("Use a new review folder");
            game=GetComponent<GameController>();began=Time.realtimeSinceStartup;finished=false;journeyMode=true;gearReviewMode=!LaunchArguments.Get().Contains("--ocean-rest-timer");journeyQuit=quitProcess;completeContent=false;hudSamples.Clear();meterLabels.Clear();seenMeterLabels.Clear();restTimer=null;
            waterlineSamples.Clear();waterlineReview=checkWaterline||LaunchArguments.Get().Contains("--ocean-waterline-review");
            receipt=new Receipt{captureEnabled=captureFrames,scope=captureFrames?"native focused immersive UI EventSystem review; not OS touch, child or full progression":"headless immersive UI regression; no rendered acceptance"};
            var before=game.Progress.Data.Clone();var stack=new Stack<IEnumerator>();stack.Push(ImmersiveReview());
            while(stack.Count>0&&!finished)
            {
                object next=null;bool moved=false;Exception failure=null;
                try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){receipt.failures.Add("immersive-exception:"+failure.GetType().Name+":"+failure.Message);break;}
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
            if(!finished)FinishJourney();
            File.WriteAllText(Path.Combine(folder,"immersive.json"),JsonUtility.ToJson(new ImmersiveResult{status=receipt.status,scope=receipt.scope,failures=receipt.failures.ToArray(),samples=new List<HudSample>(hudSamples),meterLabels=new List<MeterLabelSample>(meterLabels),waterline=new List<WaterlineSample>(waterlineSamples),catchesBefore=before.catches.Count,catchesAfter=game.Progress.Data.catches.Count,pointsBefore=before.totalPoints,pointsAfter=game.Progress.Data.totalPoints,unfinishedExitPreservedLedger=game.Progress.Data.catches.Count==before.catches.Count+1,settingsRestored=JsonUtility.ToJson(before.settings)==JsonUtility.ToJson(game.Progress.Data.settings),restTimer=restTimer},true));
        }
        void CheckWaterline(string stage,bool surface)
        {
            var world=GetComponent<OceanWorld>();var rig=FindFirstObjectByType<AuthoredSpinningRig>();var sample=new WaterlineSample{stage=stage,phase=game.Session.Phase.ToString(),leftHanded=game.Progress.Data.settings.leftHanded,surface=world.FishingSurfaceView,seaLevel=world.ActiveHabitat.SeaLevel,cameraY=Camera.main.transform.position.y,rigY=rig.transform.position.y,lureY=rig.lureNose.position.y,fade=world.CameraFade,guideError=rig.MaximumGuideLineError()};waterlineSamples.Add(sample);
            JourneyRequire(sample.surface==surface&&(surface?sample.cameraY>sample.seaLevel:sample.cameraY<sample.seaLevel)&&sample.rigY>sample.seaLevel&&sample.guideError<.0001f,"waterline-"+stage+"-actual-camera-rig-and-guides");
        }
        IEnumerator WaterlineClick(string label)
        {yield return JourneyClick(label,true);yield return null;yield return null;}
        void CheckLiveMeterLabels()
        {
            Canvas.ForceUpdateCanvases();
            foreach(var text in FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t=>t.isActiveAndEnabled&&t.transform.parent.name=="FightHUD"))
            {
                if(!seenMeterLabels.Add(text.text))continue;
                var rect=text.rectTransform.rect;var sample=new MeterLabelSample{text=text.text,preferredWidth=text.preferredWidth,availableWidth=rect.width,preferredHeight=text.preferredHeight,availableHeight=rect.height};
                sample.fits=sample.preferredWidth<=sample.availableWidth+.1f&&sample.preferredHeight<=sample.availableHeight+.1f;meterLabels.Add(sample);
                JourneyRequire(sample.fits,"immersive-live-meter-label-fits-"+text.text);
            }
        }
        void CheckImmersiveHud()
        {
            Canvas.ForceUpdateCanvases();var ui=GetComponent<GameUI>();JourneyRequire(ui.Playing,"immersive-active-play");
            var buttons=FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.isActiveAndEnabled).ToArray();
            JourneyRequire(buttons.Length==2&&buttons.Any(b=>b.GetComponentInChildren<Text>()?.text=="暂停"),"immersive-only-pause-and-current-action");
            JourneyRequire(!FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>new[]{"Header","Navigation","Footer","Info","Controls","Preparation"}.Contains(t.name)),"immersive-no-global-chrome-or-panels");
            var regions=new List<Rect>();float p=DisplayMetrics.PixelsPerPoint;var safe=DisplayMetrics.SafeArea;
            // Include visible panel backgrounds as well as declared HUD regions.
            // An unmarked result card must not escape the obstruction budget.
            var footprints=FindObjectsByType<HudFootprint>(FindObjectsSortMode.None).Select(m=>m.GetComponent<RectTransform>())
                .Concat(FindObjectsByType<Image>(FindObjectsSortMode.None).Where(i=>i.isActiveAndEnabled&&i.color.a>.01f).Select(i=>i.rectTransform)).Distinct();
            foreach(var footprint in footprints)
            {
                var corners=new Vector3[4];footprint.GetWorldCorners(corners);regions.Add(Rect.MinMaxRect((corners[0].x-safe.xMin)/p,(corners[0].y-safe.yMin)/p,(corners[2].x-safe.xMin)/p,(corners[2].y-safe.yMin)/p));
            }
            float w=safe.width/p,h=safe.height/p;var xs=regions.SelectMany(r=>new[]{r.xMin,r.xMax}).Distinct().OrderBy(v=>v).ToArray();float area=0;
            for(int i=1;i<xs.Length;i++)
            {
                var ys=regions.Where(r=>r.xMin<xs[i]&&r.xMax>xs[i-1]).OrderBy(r=>r.yMin).ToArray();float length=0,end=float.NegativeInfinity;
                foreach(var r in ys){length+=Mathf.Max(0,r.yMax-Mathf.Max(end,r.yMin));end=Mathf.Max(end,r.yMax);}area+=(xs[i]-xs[i-1])*length;
            }
            var center=new Rect(w*.25f,h*.45f,w*.5f,h*.4f);bool clear=regions.All(r=>!r.Overlaps(center));float ratio=area/(w*h);
            hudSamples.Add(new HudSample{phase=game.Session.Phase.ToString(),leftHanded=game.Progress.Data.settings.leftHanded,buttons=buttons.Length,occupiedRatio=ratio,centralClear=clear});
            JourneyRequire(ratio<=.15f&&clear,"immersive-hud-under-15-percent-and-central-view-clear-"+game.Session.Phase);
            JourneyRequire(FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>t.name=="FightHUD")== (game.Session.Phase==FishingPhase.Fighting),"immersive-meters-only-while-fighting");
        }
        IEnumerator ImmersiveReview()
        {
            var args=LaunchArguments.Get();int index=Array.IndexOf(args,"--ocean-save-root");JourneyRequire(index>=0&&index+1<args.Length,"immersive-isolated-save-root");string save=Path.Combine(args[index+1],"ocean-progress.json");
            JourneyRequire(!game.Progress.RequiresRecovery&&game.Session.Phase==FishingPhase.Ready,"immersive-ready-valid-save");var original=game.Progress.Data.Clone();
            yield return null;yield return null;
            bool verifyRest=LaunchArguments.Get().Contains("--ocean-rest-timer");float restStart=Time.realtimeSinceStartup;
            if(verifyRest)JourneyRequire(game.Session.Paused&&!game.RestReminder,"rest-clock-starts-from-paused-new-process");
            if(game.Session.Paused){yield return JourneyClick("继续探索",true);yield return null;yield return null;}
            if(verifyRest)yield return VerifyRealRestTimer(restStart);
            foreach(bool left in new[]{false,true})
            {
                yield return JourneyClick("设置",true);yield return null;yield return null;
                if(game.Progress.Data.settings.leftHanded!=left){yield return JourneyClick("左手操作");yield return null;yield return null;}
                yield return JourneyClick("出海",true);yield return null;yield return null;
                yield return JourneyClick("返回首页",true);yield return null;yield return null;yield return JourneyFrame(left?"left-home":"home");
                yield return JourneyClick("海域与装备",true);yield return null;yield return null;JourneyRequire(game.Session.Paused&&GetComponent<GameUI>().PageName=="准备","immersive-separate-preparation");yield return JourneyFrame(left?"left-preparation":"preparation");
                var departure=FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.isActiveAndEnabled&&b.GetComponentInChildren<Text>()?.text=="开始钓鱼");
                JourneyRequire(departure.GetComponentInParent<ScrollRect>()==null,"immersive-departure-visible-without-scrolling");
                var corners=new Vector3[4];departure.GetComponent<RectTransform>().GetWorldCorners(corners);var safe=DisplayMetrics.SafeArea;
                JourneyRequire(corners[0].x>=safe.xMin&&corners[0].y>=safe.yMin&&corners[2].x<=safe.xMax&&corners[2].y<=safe.yMax,"immersive-departure-inside-safe-area");
                Pointer(departure,true);Pointer(departure,false);
                yield return JourneyClick("开始钓鱼",true);yield return null;yield return null;
                bool wantsSurface=!game.Progress.Data.settings.underwaterOnly&&!game.Progress.Data.settings.reducedMotion;float cameraDeadline=Time.realtimeSinceStartup+4;
                while(wantsSurface&&(!GetComponent<OceanWorld>().FishingSurfaceView||GetComponent<OceanWorld>().CameraFade>0)&&Time.realtimeSinceStartup<cameraDeadline)yield return null;
                CheckImmersiveHud();yield return JourneyFrame(left?"left-ready":"ready");CheckWaterline("ready",wantsSurface);
                if(waterlineReview&&!left)
                {
                    foreach(string setting in new[]{"固定水下镜头","减弱动态效果"})
                    {
                        yield return WaterlineClick("暂停");yield return WaterlineClick("设置");yield return JourneyClick(setting);yield return null;yield return null;yield return WaterlineClick("出海");yield return WaterlineClick("继续探索");
                        CheckWaterline(setting,false);JourneyRequire(GetComponent<OceanWorld>().CameraFade==0,"waterline-fixed-camera-no-fade-"+setting);yield return JourneyFrame(setting=="固定水下镜头"?"fixed-underwater":"reduced-underwater");
                        yield return WaterlineClick("暂停");yield return WaterlineClick("设置");yield return JourneyClick(setting);yield return null;yield return null;yield return WaterlineClick("出海");yield return WaterlineClick("继续探索");CheckWaterline("restored-auto",true);
                    }
                }
                var cast=Find("按住蓄力");Pointer(cast,true);yield return new WaitForSecondsRealtime(.25f);
                yield return JourneyClick("暂停",true);yield return null;yield return null;
                JourneyRequire(game.Session.Phase==FishingPhase.Ready&&game.Session.Paused&&!game.Reeling,"immersive-menu-cancels-cast-charge");
                yield return JourneyClick("继续探索",true);yield return null;yield return null;
                cast=Find("按住蓄力");Pointer(cast,true);yield return new WaitForSecondsRealtime(.3f);Pointer(cast,false);
                yield return null;yield return null;
                if(waterlineReview&&!left){CheckWaterline("casting",true);yield return JourneyFrame("surface-casting");}
                float deadline=Time.realtimeSinceStartup+20;while(game.Session.Phase!=FishingPhase.Waiting&&Time.realtimeSinceStartup<deadline)yield return null;
                if(waterlineReview&&!left){yield return null;CheckWaterline("impact",true);yield return JourneyFrame("surface-impact");}
                while((GetComponent<OceanWorld>().FishingSurfaceView||GetComponent<OceanWorld>().CameraFade>0)&&Time.realtimeSinceStartup<deadline)yield return null;
                yield return null;yield return null;CheckImmersiveHud();if(!left)yield return JourneyFrame("waiting");
                CheckWaterline("underwater-waiting",false);JourneyRequire(GetComponent<OceanWorld>().CameraFade==0,"waterline-transition-completes");
                while(game.Session.Phase!=FishingPhase.Bite&&Time.realtimeSinceStartup<deadline)yield return null;
                JourneyRequire(game.Session.Phase==FishingPhase.Bite,"immersive-normal-bite");yield return null;yield return null;CheckImmersiveHud();if(!left)yield return JourneyFrame("bite");
                yield return JourneyClick("提竿",true);yield return null;yield return null;CheckImmersiveHud();var caught=game.Session.Catch;
                var reel=Find("按住收线");Pointer(reel,true);yield return new WaitForSecondsRealtime(.2f);yield return JourneyClick("暂停",true);yield return null;yield return null;
                JourneyRequire(game.Session.Paused&&!game.Reeling,"immersive-pause-releases-held-reel");float t=game.Session.Tension,progress=game.Session.Progress;yield return new WaitForSecondsRealtime(.2f);JourneyRequire(t==game.Session.Tension&&progress==game.Session.Progress,"immersive-pause-freezes-current-fish");
                yield return JourneyFrame(left?"left-pause":"pause");
                yield return JourneyClick("设置",true);yield return null;yield return null;JourneyRequire(game.Session.Catch==caught&&game.Session.Paused,"immersive-settings-retain-catch");if(!left)yield return JourneyFrame("settings");
                yield return JourneyClick("上一级",true);yield return null;yield return null;JourneyRequire(game.Session.Paused,"immersive-back-does-not-resume");
                yield return JourneyClick("结束本次 · 返回准备",true);yield return null;yield return null;yield return JourneyFrame(left?"left-exit":"exit");
                yield return JourneyClick("取消，继续留在这里",true);yield return null;yield return null;JourneyRequire(game.Session.Catch==caught&&game.Session.Paused&&game.Session.Phase==FishingPhase.Fighting,"immersive-exit-cancel-keeps-current-fish");
                if(left)
                {
                    int points=game.Progress.Data.totalPoints,count=game.Progress.Data.catches.Count;string disk=File.ReadAllText(save);
                    yield return JourneyClick("结束本次 · 返回准备",true);yield return null;yield return null;yield return JourneyClick("确认结束并返回准备",true);yield return null;yield return null;
                    JourneyRequire(game.Session.Catch==null&&game.Session.Phase==FishingPhase.Ready&&game.Session.Paused&&GetComponent<GameUI>().PageName=="准备","immersive-confirm-exit-to-preparation");
                    JourneyRequire(points==game.Progress.Data.totalPoints&&count==game.Progress.Data.catches.Count&&disk==File.ReadAllText(save),"immersive-unrecorded-exit-preserves-existing-disk-ledger");
                    continue;
                }
                yield return JourneyClick("继续探索",true);yield return null;yield return null;CheckImmersiveHud();yield return JourneyFrame("fighting");
                reel=Find("按住收线");Pointer(reel,true);game.gameObject.SendMessage("OnApplicationFocus",false);yield return null;yield return null;
                JourneyRequire(game.Session.Paused&&!game.Reeling,"immersive-focus-loss-cancels-input");game.gameObject.SendMessage("OnApplicationFocus",true);yield return null;yield return null;JourneyRequire(game.Session.Paused,"immersive-focus-return-awaits-continue");
                yield return JourneyClick("继续探索",true);yield return null;yield return null;reel=Find("按住收线");bool held=false;deadline=Time.realtimeSinceStartup+90;
                while(game.Session.Phase==FishingPhase.Fighting&&Time.realtimeSinceStartup<deadline){CheckLiveMeterLabels();bool desired=game.Session.Tension<.65f;if(desired!=held){held=desired;Pointer(reel,held);}yield return null;}
                JourneyRequire(meterLabels.Any(s=>s.text.StartsWith("收鱼")&&System.Text.RegularExpressions.Regex.IsMatch(s.text,@"\b[1-9][0-9]%")),"immersive-observed-real-two-digit-progress");
                if(held)Pointer(reel,false);yield return null;yield return null;JourneyRequire(game.Session.Phase==FishingPhase.Landed,"immersive-real-fish-landed");CheckImmersiveHud();yield return JourneyFrame("landed");
                yield return JourneyClick("记录并放流",true);yield return null;yield return null;JourneyRequire(game.Progress.Data.catches.Count==original.catches.Count+1,"immersive-one-real-record");
            }
            // Restore only the setting toggled by the UI review, never records.
            yield return JourneyClick("设置",true);yield return null;yield return null;
            if(game.Progress.Data.settings.leftHanded!=original.settings.leftHanded){yield return JourneyClick("左手操作");yield return null;yield return null;}
            var loaded=new SaveStore(save,game.Content).Load();JourneyRequire(!loaded.requiresRecovery&&loaded.data.catches.Count==original.catches.Count+1&&loaded.data.totalPoints==loaded.data.catches.Sum(c=>c.awardedPoints)&&JsonUtility.ToJson(loaded.data.settings)==JsonUtility.ToJson(original.settings),"immersive-restored-settings-real-disk-ledger");
            receipt.pixelsPerPoint=DisplayMetrics.PixelsPerPoint;receipt.safeWidthPoints=DisplayMetrics.SafeArea.width/DisplayMetrics.PixelsPerPoint;receipt.safeHeightPoints=DisplayMetrics.SafeArea.height/DisplayMetrics.PixelsPerPoint;
        }
        IEnumerator VerifyRealRestTimer(float started)
        {
            string before=JsonUtility.ToJson(game.Progress.Data);restTimer=new RestTimerResult();float next=0;
            CheckImmersiveHud();yield return JourneyFrame("rest-before");
            while(!game.RestReminder&&Time.realtimeSinceStartup-started<930)
            {
                restTimer.observedSeconds=Time.realtimeSinceStartup-started;restTimer.paused=game.Session.Paused;
                JourneyRequire(!restTimer.paused&&game.Session.Phase==FishingPhase.Ready,"rest-clock-uninterrupted-ready-no-auto-pause");
                if(restTimer.observedSeconds>=next){File.WriteAllText(Path.Combine(folder,"rest-timer.json"),JsonUtility.ToJson(restTimer,true));next+=30;}
                yield return null;
            }
            restTimer.observedSeconds=Time.realtimeSinceStartup-started;restTimer.reminderDue=game.RestReminder;
            JourneyRequire(restTimer.reminderDue&&restTimer.observedSeconds>=899.5f&&restTimer.observedSeconds<930,"rest-real-quarter-hour-trigger-without-clock-injection");
            yield return null;CheckImmersiveHud();yield return JourneyFrame("rest-ready");
            yield return JourneyClick("暂停",true);yield return null;yield return null;CheckLayout("rest-pause");
            JourneyRequire(game.Session.Paused&&game.RestReminder&&FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="和家人休息一下，准备好再继续。"),"rest-visible-paused-context");yield return JourneyFrame("rest-pause");
            yield return JourneyClick("休息好了，继续",true);yield return null;yield return null;
            restTimer.acknowledged=!game.RestReminder&&!game.Session.Paused&&!game.Reeling;restTimer.progressUnchanged=before==JsonUtility.ToJson(game.Progress.Data);
            JourneyRequire(restTimer.acknowledged&&restTimer.progressUnchanged,"rest-explicit-continue-clears-without-ledger-change");CheckImmersiveHud();yield return JourneyFrame("rest-resumed");
            restTimer.status="PASS";File.WriteAllText(Path.Combine(folder,"rest-timer.json"),JsonUtility.ToJson(restTimer,true));
        }
    }
}
