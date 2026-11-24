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
        [Serializable] sealed class ImmersiveResult { public string status,scope;public string[] failures;public List<HudSample> samples;public int catchesBefore,catchesAfter,pointsBefore,pointsAfter;public bool unfinishedExitPreservedLedger,settingsRestored; }
        readonly List<HudSample> hudSamples=new List<HudSample>();
        public IEnumerator RunImmersiveReview(string outputFolder,bool captureFrames,bool quitProcess=false)
        {
            folder=Path.GetFullPath(outputFolder);Directory.CreateDirectory(folder);if(File.Exists(Path.Combine(folder,"receipt.json")))throw new InvalidOperationException("Use a new review folder");
            game=GetComponent<GameController>();began=Time.realtimeSinceStartup;finished=false;journeyMode=gearReviewMode=true;journeyQuit=quitProcess;completeContent=false;hudSamples.Clear();
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
            File.WriteAllText(Path.Combine(folder,"immersive.json"),JsonUtility.ToJson(new ImmersiveResult{status=receipt.status,scope=receipt.scope,failures=receipt.failures.ToArray(),samples=new List<HudSample>(hudSamples),catchesBefore=before.catches.Count,catchesAfter=game.Progress.Data.catches.Count,pointsBefore=before.totalPoints,pointsAfter=game.Progress.Data.totalPoints,unfinishedExitPreservedLedger=game.Progress.Data.catches.Count==before.catches.Count+1,settingsRestored=JsonUtility.ToJson(before.settings)==JsonUtility.ToJson(game.Progress.Data.settings)},true));
        }
        void CheckImmersiveHud()
        {
            Canvas.ForceUpdateCanvases();var ui=GetComponent<GameUI>();JourneyRequire(ui.Playing,"immersive-active-play");
            var buttons=FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.isActiveAndEnabled).ToArray();
            JourneyRequire(buttons.Length==2&&buttons.Any(b=>b.GetComponentInChildren<Text>()?.text=="暂停"),"immersive-only-pause-and-current-action");
            JourneyRequire(!FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>new[]{"Header","Navigation","Footer","Info","Controls","Preparation"}.Contains(t.name)),"immersive-no-global-chrome-or-panels");
            var regions=new List<Rect>();float p=DisplayMetrics.PixelsPerPoint;var safe=DisplayMetrics.SafeArea;
            foreach(var mark in FindObjectsByType<HudFootprint>(FindObjectsSortMode.None))
            {
                var corners=new Vector3[4];mark.GetComponent<RectTransform>().GetWorldCorners(corners);regions.Add(Rect.MinMaxRect((corners[0].x-safe.xMin)/p,(corners[0].y-safe.yMin)/p,(corners[2].x-safe.xMin)/p,(corners[2].y-safe.yMin)/p));
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
            if(game.Session.Paused){yield return JourneyClick("继续探索",true);yield return null;yield return null;}
            foreach(bool left in new[]{false,true})
            {
                yield return JourneyClick("设置",true);yield return null;yield return null;
                if(game.Progress.Data.settings.leftHanded!=left){yield return JourneyClick("左手操作");yield return null;yield return null;}
                yield return JourneyClick("出海",true);yield return null;yield return null;
                yield return JourneyClick("返回首页",true);yield return null;yield return null;yield return JourneyFrame(left?"left-home":"home");
                yield return JourneyClick("海域与装备",true);yield return null;yield return null;JourneyRequire(game.Session.Paused&&GetComponent<GameUI>().PageName=="准备","immersive-separate-preparation");yield return JourneyFrame(left?"left-preparation":"preparation");
                yield return JourneyClick("开始钓鱼",true);yield return null;yield return null;CheckImmersiveHud();yield return JourneyFrame(left?"left-ready":"ready");
                var cast=Find("按住蓄力");Pointer(cast,true);yield return new WaitForSecondsRealtime(.25f);
                yield return JourneyClick("暂停",true);yield return null;yield return null;
                JourneyRequire(game.Session.Phase==FishingPhase.Ready&&game.Session.Paused&&!game.Reeling,"immersive-menu-cancels-cast-charge");
                yield return JourneyClick("继续探索",true);yield return null;yield return null;
                cast=Find("按住蓄力");Pointer(cast,true);yield return new WaitForSecondsRealtime(.3f);Pointer(cast,false);
                float deadline=Time.realtimeSinceStartup+20;while(game.Session.Phase!=FishingPhase.Waiting&&Time.realtimeSinceStartup<deadline)yield return null;
                yield return null;yield return null;CheckImmersiveHud();if(!left)yield return JourneyFrame("waiting");
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
                while(game.Session.Phase==FishingPhase.Fighting&&Time.realtimeSinceStartup<deadline){bool desired=game.Session.Tension<.65f;if(desired!=held){held=desired;Pointer(reel,held);}yield return null;}
                if(held)Pointer(reel,false);yield return null;yield return null;JourneyRequire(game.Session.Phase==FishingPhase.Landed,"immersive-real-fish-landed");yield return JourneyFrame("landed");
                yield return JourneyClick("记录并放流",true);yield return null;yield return null;JourneyRequire(game.Progress.Data.catches.Count==original.catches.Count+1,"immersive-one-real-record");
            }
            // Restore only the setting toggled by the UI review, never records.
            yield return JourneyClick("设置",true);yield return null;yield return null;
            if(game.Progress.Data.settings.leftHanded!=original.settings.leftHanded){yield return JourneyClick("左手操作");yield return null;yield return null;}
            var loaded=new SaveStore(save,game.Content).Load();JourneyRequire(!loaded.requiresRecovery&&loaded.data.catches.Count==original.catches.Count+1&&loaded.data.totalPoints==loaded.data.catches.Sum(c=>c.awardedPoints)&&JsonUtility.ToJson(loaded.data.settings)==JsonUtility.ToJson(original.settings),"immersive-restored-settings-real-disk-ledger");
            receipt.pixelsPerPoint=DisplayMetrics.PixelsPerPoint;receipt.safeWidthPoints=DisplayMetrics.SafeArea.width/DisplayMetrics.PixelsPerPoint;receipt.safeHeightPoints=DisplayMetrics.SafeArea.height/DisplayMetrics.PixelsPerPoint;
        }
    }
}
