using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace OceanAngler
{
    public sealed partial class RuntimeSmoke
    {
        // Three separately launched phases prove startup selection, rather than
        // replacing the controller's loaded data with a synthetic fixture.
        public IEnumerator RunDeveloperReview(string outputFolder,string phase,bool captureFrames,bool quitProcess=false)
        {
            folder=Path.GetFullPath(outputFolder);Directory.CreateDirectory(folder);
            if(File.Exists(Path.Combine(folder,"receipt.json")))throw new InvalidOperationException("Use a new review folder");
            game=GetComponent<GameController>();began=Time.realtimeSinceStartup;finished=false;journeyMode=gearReviewMode=true;journeyQuit=quitProcess;
            receipt=new Receipt{captureEnabled=captureFrames,scope=(captureFrames?"native Simulator":"headless PlayMode")+" developer mode real UI, isolated saves and explicit write-failure injection; not OS touch or physical-device acceptance"};
            var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(DeveloperReview(phase));
            while(stack.Count>0&&!finished)
            {
                object next=null;bool moved=false;Exception failure=null;
                try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){receipt.failures.Add("developer-exception:"+failure.GetType().Name+":"+failure.Message);break;}
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(next is IEnumerator nested)stack.Push(nested);else yield return next;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
            if(!finished)FinishJourney();
        }
        IEnumerator DeveloperEntry()
        {
            yield return JourneyClick("设置",true);yield return null;yield return null;
            yield return PageTap("家长区与隐私","家长验证");
            string question=FindObjectsByType<Text>(FindObjectsSortMode.None).Single(t=>t.text.StartsWith("9 + ")).text;
            yield return PageTap((9+int.Parse(question.Split(' ')[2])).ToString(),"家长区");
            yield return PageFrame("developer-parent-"+(game.DeveloperMode?"test":"normal"));
            yield return PageTap(game.DeveloperMode?"开发者模式 · 已开启":"开发者模式","开发者模式");
            yield return PageFrame("developer-settings-"+(game.DeveloperMode?"test":"normal"));
        }
        void DeveloperNormalUnchanged(string root)
        {
            foreach(string suffix in new[]{"",".bak"})JourneyRequire(File.ReadAllBytes(Path.Combine(root,"ocean-progress.json")+suffix).SequenceEqual(File.ReadAllBytes(Path.Combine(root,"developer-normal-before"+suffix+".bin"))),"developer-normal-bytes-unchanged"+suffix);
        }
        IEnumerator DeveloperReview(string phase)
        {
            var args=LaunchArguments.Get();int index=Array.IndexOf(args,"--ocean-save-root");JourneyRequire(index>=0&&index+1<args.Length,"developer-explicit-isolated-save-root");
            string root=Path.GetFullPath(args[index+1]),normal=Path.Combine(root,"ocean-progress.json"),test=Path.Combine(root,"ocean-developer-progress.json");
            yield return null;yield return null;var ui=GetComponent<GameUI>();
            if(phase=="enable")
            {
                JourneyRequire(!game.DeveloperMode&&!game.Progress.RequiresRecovery&&game.Progress.Data.totalPoints==0&&!File.Exists(normal)&&!File.Exists(test),"developer-fresh-normal-zero-points");
                // Actual settings commits establish primary/backup baselines.
                game.Configure(s=>s.leftHanded=!s.leftHanded);game.Configure(s=>s.leftHanded=!s.leftHanded);
                foreach(string suffix in new[]{"",".bak"})File.Copy(normal+suffix,Path.Combine(root,"developer-normal-before"+suffix+".bin"));
                yield return DeveloperEntry();yield return PageTap("开启开发者模式","切换模式确认");yield return PageFrame("developer-enable-confirmation");
                yield return PageTap("取消，保持当前模式","开发者模式");JourneyRequire(!game.DeveloperMode&&!File.Exists(test),"developer-cancel-no-mode-or-file-change");
                yield return PageTap("开启开发者模式","切换模式确认");yield return PageTap("确认开启开发者模式","准备");yield return PageFrame("developer-unlocked-preparation");
                JourneyRequire(game.DeveloperMode&&game.Progress.DeveloperSelected&&game.Progress.Data.totalPoints==0&&game.Progress.Data.catches.Count==0,"developer-enabled-without-invented-progress");
                yield return PageTap("上一级","首页");yield return PageTap("海域与装备","准备");
                foreach(var habitat in game.Content.habitats){yield return PageTap(habitat.name,"准备");JourneyRequire(game.Progress.Data.habitatId==habitat.id,"developer-zero-point-habitat-"+habitat.id);yield return PageFrame("developer-sea-"+habitat.id);}
                yield return PageTap("更换钓具","钓具");
                string[] categories={"rod","reel","line","lure"},labels={"鱼竿","卷线器","鱼线","拟饵"};
                for(int c=0;c<4;c++)
                {
                    yield return PageTap(labels[c],"钓具");
                    foreach(var gear in game.Content.gear.Where(g=>g.category==categories[c]))
                    {
                        var button=FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.isActiveAndEnabled&&b.GetComponentInChildren<Text>()?.text.StartsWith(gear.name+"  ")==true);
                        JourneyRequire(button.interactable,"developer-zero-point-gear-"+gear.id);yield return JourneyClick(button.GetComponentInChildren<Text>().text,true);yield return null;yield return null;
                        JourneyRequire(new[]{game.Progress.Data.rodId,game.Progress.Data.reelId,game.Progress.Data.lineId,game.Progress.Data.lureId}.Contains(gear.id),"developer-equipped-"+gear.id);receipt.equippedGear.Add(gear.id);
                    }
                    yield return PageFrame("developer-gear-"+categories[c]);
                }
                yield return JourneyClick("图鉴",true);yield return null;yield return null;
                foreach(var fish in game.Content.fish)
                {
                    yield return PageTap("未发现 · "+fish.name,"鱼类详情");
                    var visual=GetComponent<OceanWorld>().TargetFish;
                    JourneyRequire(visual!=null&&visual.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&r.gameObject.activeInHierarchy),"developer-full-journal-model-"+fish.id);
                    bool colored=false;var block=new MaterialPropertyBlock();
                    foreach(var renderer in visual.GetComponentsInChildren<Renderer>())for(int m=0;m<renderer.sharedMaterials.Length;m++)
                    {
                        renderer.GetPropertyBlock(block,m);Color color=block.HasColor("_BaseColor")?block.GetColor("_BaseColor"):renderer.sharedMaterials[m].color;
                        if(Mathf.Max(color.r,color.g,color.b)>.1f)colored=true;
                    }
                    JourneyRequire(colored,"developer-no-silhouette-"+fish.id);
                    yield return PageFrame("developer-model-"+fish.id,true);yield return PageTap("上一级","图鉴");
                }
                yield return PageTap("保护观察  0 / 3","保护观察");
                foreach(var species in game.Content.observations)
                {
                    yield return PageTap("观察"+species.name,"观察生物");
                    if(!GetComponent<OceanWorld>().Observations.Playing)yield return JourneyClick("安静观察",true);
                    float deadline=Time.realtimeSinceStartup+15;while(!GetComponent<OceanWorld>().Observations.CanRecord&&Time.realtimeSinceStartup<deadline)yield return null;
                    JourneyRequire(GetComponent<OceanWorld>().Observations.CanRecord,"developer-actual-observation-"+species.id);yield return PageFrame("developer-observation-"+species.id,true);
                    yield return JourneyClick("记入观察图鉴",true);yield return null;yield return null;yield return PageTap("上一级","保护观察");
                }
                JourneyRequire(game.Progress.Data.totalPoints==0&&game.Progress.Data.catches.Count==0&&game.Progress.Data.observations.Count==3&&receipt.equippedGear.Count==20,"developer-all-content-at-zero-points");
            }
            else if(phase=="disable")
            {
                JourneyRequire(game.DeveloperMode&&game.Progress.DeveloperSelected&&game.Progress.Data.totalPoints==0&&game.Progress.Data.observations.Count==3,"developer-startup-restores-selected-test-profile");
                yield return PageFrame("developer-restarted-home");
                yield return JourneyClick("海域与装备",true);yield return null;yield return null;yield return JourneyClick("开始钓鱼",true);yield return null;yield return null;
                yield return DeveloperCatch();
                var cast=Find("按住蓄力");Pointer(cast,true);yield return new WaitForSecondsRealtime(.2f);Pointer(cast,false);float deadline=Time.realtimeSinceStartup+20;
                while(game.Session.Phase!=FishingPhase.Bite&&Time.realtimeSinceStartup<deadline)yield return null;
                JourneyRequire(game.Session.Phase==FishingPhase.Bite,"developer-unrecorded-second-bite");yield return null;yield return null;yield return JourneyClick("提竿",true);yield return null;yield return null;
                JourneyRequire(game.Session.Phase==FishingPhase.Fighting,"developer-unrecorded-second-fight");Pointer(Find("按住收线"),true);
                JourneyRequire(!game.SwitchDeveloperMode(false)&&game.DeveloperMode&&game.Reeling,"developer-live-switch-rejected-with-fish-intact");
                yield return DeveloperEntry();yield return PageTap("关闭开发者模式","切换模式确认");yield return PageFrame("developer-disable-confirmation");
                JourneyRequire(game.Session.Phase==FishingPhase.Fighting&&!game.Reeling,"developer-settings-clears-hold-keeps-paused-fish");
                yield return PageTap("取消，保持当前模式","开发者模式");JourneyRequire(game.DeveloperMode,"developer-cancel-off-keeps-test-profile");
                yield return PageTap("关闭开发者模式","切换模式确认");
                byte[] prior=File.ReadAllBytes(test);Directory.CreateDirectory(test+".tmp");
                try
                {
                    yield return JourneyClick("确认关闭开发者模式",true);yield return null;yield return null;
                    JourneyRequire(game.DeveloperMode&&ui.PageName=="切换模式确认"&&prior.SequenceEqual(File.ReadAllBytes(test)),"developer-failed-switch-keeps-current-mode-and-file");yield return PageFrame("developer-write-failure");
                    var feedback=FindObjectsByType<Text>(FindObjectsSortMode.None).Single(t=>t.isActiveAndEnabled&&t.name=="DeveloperModeFeedback");
                    JourneyRequire(feedback.text==game.Notice&&!string.IsNullOrEmpty(feedback.text),"developer-failure-feedback-reports-the-current-error");Canvas.ForceUpdateCanvases();
                    var rect=feedback.rectTransform.rect;var scroll=feedback.GetComponentInParent<ScrollRect>();
                    foreach(var local in new[]{new Vector3(rect.xMin,rect.yMax,0),new Vector3(rect.xMax,rect.yMax-feedback.preferredHeight,0)})
                    {
                        var point=RectTransformUtility.WorldToScreenPoint(null,feedback.rectTransform.TransformPoint(local));
                        JourneyRequire(DisplayMetrics.SafeArea.Contains(point)&&(scroll==null||RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport,point)),"developer-failure-text-visible-without-scrolling");
                    }
                }
                finally{Directory.Delete(test+".tmp");}
                yield return PageTap("确认关闭开发者模式","准备");yield return PageFrame("developer-normal-restored");
                JourneyRequire(!game.DeveloperMode&&!game.Reeling&&game.Session.Phase==FishingPhase.Ready&&game.Progress.Data.totalPoints==0&&game.Progress.Data.catches.Count==0&&game.Progress.Data.observations.Count==0,"developer-normal-progress-restored");
                JourneyRequire(game.Content.habitats.Any(h=>!game.Progress.IsUnlocked(h.unlockPoints))&&game.Content.gear.Any(g=>!game.Progress.IsUnlocked(g.unlockPoints)),"developer-normal-point-gates-restored");
                yield return PageTap("上一级","首页");yield return PageTap("海域与装备","准备");
            }
            else if(phase=="normal-restart")
            {
                JourneyRequire(!game.DeveloperMode&&game.Progress.Data.totalPoints==0&&game.Progress.Data.catches.Count==0,"developer-startup-keeps-explicit-normal-selection");yield return PageFrame("developer-normal-restarted");
                yield return DeveloperEntry();yield return PageTap("开启开发者模式","切换模式确认");yield return PageTap("确认开启开发者模式","准备");
                JourneyRequire(game.DeveloperMode&&game.Progress.Data.catches.Count==1&&game.Progress.Data.totalPoints>0&&game.Progress.Data.observations.Count==3,"developer-test-history-retained-on-reenable");
                yield return PageFrame("developer-test-history-retained");
                yield return DeveloperEntry();yield return PageTap("关闭开发者模式","切换模式确认");yield return PageTap("确认关闭开发者模式","准备");
            }
            else throw new ArgumentException("Unknown developer review phase");
            DeveloperNormalUnchanged(root);receipt.pixelsPerPoint=DisplayMetrics.PixelsPerPoint;receipt.safeWidthPoints=DisplayMetrics.SafeArea.width/DisplayMetrics.PixelsPerPoint;receipt.safeHeightPoints=DisplayMetrics.SafeArea.height/DisplayMetrics.PixelsPerPoint;
        }
        IEnumerator DeveloperCatch()
        {
            JourneyRequire(!game.Session.Paused&&GetComponent<GameUI>().Playing,"developer-playing-has-no-mode-overlay");CheckImmersiveHud();yield return JourneyFrame("developer-playing");
            var cast=Find("按住蓄力");Pointer(cast,true);yield return new WaitForSecondsRealtime(.35f);Pointer(cast,false);float deadline=Time.realtimeSinceStartup+20;
            while(game.Session.Phase!=FishingPhase.Bite&&Time.realtimeSinceStartup<deadline)yield return null;
            JourneyRequire(game.Session.Phase==FishingPhase.Bite,"developer-real-bite");yield return null;yield return null;yield return JourneyClick("提竿",true);yield return null;yield return null;
            JourneyRequire(game.Session.Phase==FishingPhase.Fighting,"developer-real-strike");var reel=Find("按住收线");bool held=false;deadline=Time.realtimeSinceStartup+75;
            while(game.Session.Phase==FishingPhase.Fighting&&Time.realtimeSinceStartup<deadline){bool desired=game.Session.Tension<.65f;if(desired!=held){held=desired;Pointer(reel,held);}yield return null;}
            if(held)Pointer(reel,false);yield return null;yield return null;JourneyRequire(game.Session.Phase==FishingPhase.Landed,"developer-real-catch");yield return JourneyFrame("developer-catch");
            yield return JourneyClick("记录并放流",true);yield return null;yield return null;JourneyRequire(game.Progress.Data.catches.Count==1&&game.Progress.Data.totalPoints>0,"developer-real-catch-in-test-ledger");
        }
    }
}
