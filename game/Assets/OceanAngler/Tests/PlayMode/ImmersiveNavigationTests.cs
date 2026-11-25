using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace OceanAngler.Tests
{
    public sealed class UiUpdateDispatchProbe : MonoBehaviour
    {
        public int updates;
        void Update(){updates++;}
    }
    public sealed class ImmersiveNavigationTests
    {
        static void UpdateUiOnly(GameUI ui)
        {
            // Component.SendMessage broadcasts to every behaviour on the same
            // object, including a disabled controller. Invoke just this UI.
            typeof(GameUI).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(ui,null);
        }
        [Test] public void UiRefreshAvoidsSendMessageBroadcastToDisabledBehaviours()
        {
            var root=new GameObject("UiDispatchFixture");
            try
            {
                var ui=root.AddComponent<GameUI>();var probe=root.AddComponent<UiUpdateDispatchProbe>();probe.enabled=false;
                ui.SendMessage("Update");Assert.That(probe.updates,Is.EqualTo(1),"A disabled sibling still receives SendMessage");
                UpdateUiOnly(ui);Assert.That(probe.updates,Is.EqualTo(1),"Targeted UI update must not broadcast");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Serializable] sealed class Result { public string status;public string[] failures;public int catchesBefore,catchesAfter;public bool unfinishedExitPreservedLedger,settingsRestored; }
        [Serializable] sealed class MeterResult { public string status,scope;public bool injectedFocusLoss,saveBytesUnchanged;public int hands,valuesPerHand,labelChecks,focusPauses,explicitResumes; }
        Scene scene;GameController game;string root,archive;readonly Dictionary<string,string> originals=new Dictionary<string,string>();
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(game!=null)game.Pause(true);
            if(scene.IsValid()&&scene.isLoaded){var empty=SceneManager.CreateScene("AfterImmersiveNavigation");SceneManager.SetActiveScene(empty);yield return SceneManager.UnloadSceneAsync(scene);}
            if(root!=null)foreach(string suffix in new[]{"",".bak",".tmp"}){string file=Path.Combine(root,"ocean-progress.json")+suffix;if(File.Exists(file))File.Move(file,Path.Combine(archive,"after"+suffix+".json"));}
            foreach(var pair in originals)File.Move(pair.Value,pair.Key);originals.Clear();
        }
        [UnityTest,Timeout(90000)] public IEnumerator EveryMeterValueFitsTheUnchangedHudRegion()
            =>CheckMeterValues(false);
        [UnityTest,Timeout(90000)] public IEnumerator EveryMeterValueFitsAfterFocusLossAndExplicitResume()
            =>CheckMeterValues(true);
        IEnumerator CheckMeterValues(bool injectFocusLoss)
        {
            var args=LaunchArguments.Get();int index=Array.IndexOf(args,"--ocean-save-root");Assert.That(index,Is.GreaterThanOrEqualTo(0));root=Path.GetFullPath(args[index+1]);Assert.That(root,Does.Contain("unity-test-saves"));
            archive=Path.Combine(root,injectFocusLoss?"meter-focus-before-after":"meter-values-before-after");Assert.That(Directory.Exists(archive),Is.False);Directory.CreateDirectory(archive);
            foreach(string suffix in new[]{"",".bak",".tmp"}){string file=Path.Combine(root,"ocean-progress.json")+suffix;if(File.Exists(file)){string saved=Path.Combine(archive,"before"+suffix+".json");File.Move(file,saved);originals.Add(file,saved);}}
            yield return SceneManager.LoadSceneAsync("Bay",LoadSceneMode.Single);scene=SceneManager.GetSceneByName("Bay");yield return null;yield return null;game=UnityEngine.Object.FindFirstObjectByType<GameController>();
            yield return RestTap("海域与装备");yield return RestTap("开始钓鱼");game.Cast(.6f);float deadline=Time.realtimeSinceStartup+20;
            while(game.Session.Phase!=FishingPhase.Bite&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Bite));game.Strike();yield return null;yield return null;
            var ui=game.GetComponent<GameUI>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var progress=typeof(FishingSession).GetField("<Progress>k__BackingField",flags);var tension=typeof(FishingSession).GetField("<Tension>k__BackingField",flags);Assert.That(progress,Is.Not.Null);Assert.That(tension,Is.Not.Null);
            Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Fighting),"The layout fixture must start with a real struck fish");
            float originalProgress=game.Session.Progress,originalTension=game.Session.Tension;bool enabled=game.enabled;
            var result=new MeterResult{scope="Headless real UI meter boundary and optional lifecycle-callback injection; not OS focus, natural fishing, child or device qualification",injectedFocusLoss=injectFocusLoss,valuesPerHand=101};
            // Freeze simulation across both UI rebuilds as well as the value
            // sweep. Keep the real pause/resume UI; never force a missing meter.
            game.enabled=false;
            try
            {
                foreach(bool left in new[]{false,true})
                {
                    game.Configure(s=>s.leftHanded=left);yield return null;yield return null;
                    byte[] saved=File.ReadAllBytes(Path.Combine(root,"ocean-progress.json"));
                    if(injectFocusLoss)
                    {
                        game.HoldReel(true);Assert.That(game.Reeling,Is.True);
                        game.SendMessage("OnApplicationFocus",false);UpdateUiOnly(ui);
                        Assert.That(game.Session.Paused,Is.True);Assert.That(game.Reeling||ui.Playing,Is.False);
                        Assert.That(UnityEngine.Object.FindObjectsByType<HudFootprint>(FindObjectsSortMode.None).Any(m=>m.name=="FightHUD"),Is.False,"Paused menus must not leave a live fighting meter");
                        yield return null;yield return null;
                        Assert.That(game.Session.Progress,Is.EqualTo(originalProgress));Assert.That(game.Session.Tension,Is.EqualTo(originalTension));result.focusPauses++;
                    }
                    if(game.Session.Paused)
                    {
                        UpdateUiOnly(ui);yield return RestTap("继续探索");result.explicitResumes++;
                    }
                    Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Fighting));Assert.That(ui.Playing,Is.True,"Use the real visible continue action before checking live meters");
                    Assert.That(game.Session.Paused||game.Reeling,Is.False,"Continue must require a fresh reel press instead of replaying the old hold");
                    try
                    {
                        // Boundary values exist only in memory. Never tick the
                        // fishing model or save synthetic progress/tension values.
                        for(int value=0;value<=100;value++)
                        {
                            progress.SetValue(game.Session,value/100f);tension.SetValue(game.Session,value/100f);UpdateUiOnly(ui);Canvas.ForceUpdateCanvases();
                            foreach(string field in new[]{"tensionText","progressText"})
                            {
                                var text=(Text)typeof(GameUI).GetField(field,flags).GetValue(ui);string label=field+" value="+value+" left="+left;
                                Assert.That(text,Is.Not.Null,label+" phase="+game.Session.Phase+" paused="+game.Session.Paused+" page="+ui.PageName);
                                Assert.That(text.fontSize,Is.EqualTo(17),label);Assert.That(text.preferredWidth,Is.LessThanOrEqualTo(text.rectTransform.rect.width+.1f),label+" "+text.text);
                                Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+.1f),label);Assert.That(text.rectTransform.parent.GetComponent<RectTransform>().rect.size,Is.EqualTo(new Vector2(228,48)));result.labelChecks++;
                            }
                        }
                    }
                    finally{progress.SetValue(game.Session,originalProgress);tension.SetValue(game.Session,originalTension);UpdateUiOnly(ui);}
                    CollectionAssert.AreEqual(saved,File.ReadAllBytes(Path.Combine(root,"ocean-progress.json")),"Focus/resume and boundary values must not rewrite the save");result.hands++;
                }
            }
            finally{progress.SetValue(game.Session,originalProgress);tension.SetValue(game.Session,originalTension);game.enabled=enabled;UpdateUiOnly(ui);}
            result.status="PASS";result.saveBytesUnchanged=true;File.WriteAllText(Path.Combine(archive,"meter-values.json"),JsonUtility.ToJson(result,true));
        }
        [UnityTest,Timeout(240000)] public IEnumerator MinimalHudAndHierarchicalMenusPreserveFishingAndPersistOnlyRecordedFish()
        {
            var args=LaunchArguments.Get();int index=Array.IndexOf(args,"--ocean-save-root");Assert.That(index,Is.GreaterThanOrEqualTo(0));root=Path.GetFullPath(args[index+1]);Assert.That(root,Does.Contain("unity-test-saves"));
            archive=Path.Combine(root,"immersive-before-after");Assert.That(Directory.Exists(archive),Is.False);Directory.CreateDirectory(archive);
            foreach(string suffix in new[]{"",".bak",".tmp"}){string file=Path.Combine(root,"ocean-progress.json")+suffix;if(File.Exists(file)){string saved=Path.Combine(archive,"before"+suffix+".json");File.Move(file,saved);originals.Add(file,saved);}}
            yield return SceneManager.LoadSceneAsync("Bay",LoadSceneMode.Single);scene=SceneManager.GetSceneByName("Bay");yield return null;yield return null;
            game=UnityEngine.Object.FindFirstObjectByType<GameController>();string output=Path.Combine(root,"immersive-review");yield return game.GetComponent<RuntimeSmoke>().RunImmersiveReview(output,false,checkWaterline:true);
            var result=JsonUtility.FromJson<Result>(File.ReadAllText(Path.Combine(output,"immersive.json")));Assert.That(result.status,Is.EqualTo("PASS"),string.Join("\n",result.failures));Assert.That(result.catchesAfter,Is.EqualTo(result.catchesBefore+1));Assert.That(result.unfinishedExitPreservedLedger&&result.settingsRestored,Is.True);
        }
        [UnityTest,Timeout(60000)] public IEnumerator RestAcknowledgementResetsOnlyAfterExplicitContinueAndKeepsMinimalHud()
        {
            var args=LaunchArguments.Get();int index=Array.IndexOf(args,"--ocean-save-root");Assert.That(index,Is.GreaterThanOrEqualTo(0));root=Path.GetFullPath(args[index+1]);Assert.That(root,Does.Contain("unity-test-saves"));
            archive=Path.Combine(root,"rest-before-after");Assert.That(Directory.Exists(archive),Is.False);Directory.CreateDirectory(archive);
            foreach(string suffix in new[]{"",".bak",".tmp"}){string file=Path.Combine(root,"ocean-progress.json")+suffix;if(File.Exists(file)){string saved=Path.Combine(archive,"before"+suffix+".json");File.Move(file,saved);originals.Add(file,saved);}}
            yield return SceneManager.LoadSceneAsync("Bay",LoadSceneMode.Single);scene=SceneManager.GetSceneByName("Bay");yield return null;yield return null;game=UnityEngine.Object.FindFirstObjectByType<GameController>();
            var clock=typeof(GameController).GetField("activeTime",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);Assert.That(clock,Is.Not.Null);
            foreach(bool left in new[]{false,true})
            {
                game.Pause(true);game.Configure(s=>s.leftHanded=left);yield return null;yield return null;
                byte[] saved=File.ReadAllBytes(Path.Combine(root,"ocean-progress.json"));
                // Set only the in-memory boundary clock. Real Update, pause,
                // navigation and pointer events drive the reminder itself.
                clock.SetValue(game,899.99f);yield return new WaitForSecondsRealtime(.12f);Assert.That(game.RestReminder,Is.False,"Paused time must not trigger the reminder");
                yield return RestTap("继续探索");
                float deadline=Time.realtimeSinceStartup+2;while(!game.RestReminder&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(game.RestReminder,Is.True);Assert.That(game.Session.Paused,Is.False);Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Ready));
                yield return null;Assert.That(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="和家人休息一下吧。"),Is.True);RestHud();
                yield return RestTap("暂停");Assert.That(game.Session.Paused,Is.True);
                Assert.That(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="和家人休息一下，准备好再继续。"),Is.True,"Pause menu must carry the pending reminder");
                yield return RestTap("海域与装备");Assert.That(game.RestReminder,Is.True);yield return RestTap("开始钓鱼");Assert.That(game.RestReminder,Is.True,"Preparing another cast must not acknowledge rest");
                // A learned-player fixture exercises quiet waiting. Restore
                // this in-memory flag before any settings or save operation.
                bool learned=game.Progress.Data.tutorialComplete;
                bool checkedWaiting=false;try
                {
                    game.Progress.Data.tutorialComplete=true;game.Cast(.6f);deadline=Time.realtimeSinceStartup+2;
                    while(game.Session.Phase==FishingPhase.Casting&&Time.realtimeSinceStartup<deadline)yield return null;
                    Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Waiting));
                    yield return null;yield return null; // Wait for the phase-triggered UI rebuild before aging its presentation clock.
                    typeof(GameUI).GetField("hintBegan",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(game.GetComponent<GameUI>(),Time.unscaledTime-7);
                    yield return null;yield return null;Assert.That(game.RestReminder,Is.True);Assert.That(UnityEngine.Object.FindObjectsByType<HudFootprint>(FindObjectsSortMode.None).Any(m=>m.name=="PhaseHUD"),Is.False,"Pending rest must not pin a quiet waiting hint");
                    checkedWaiting=true;
                }
                finally{if(!checkedWaiting)game.Pause(true);game.Session.Cancel();game.Progress.Data.tutorialComplete=learned;}
                yield return null;yield return null;
                yield return RestTap("暂停");
                Assert.That(game.Session.Paused,Is.True);Assert.That(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="和家人休息一下，准备好再继续。"),Is.True,"Pause menu must carry the pending reminder");
                yield return RestTap("设置");Assert.That(game.RestReminder,Is.True,"Navigation alone must not acknowledge rest");yield return RestTap("出海");
                yield return RestTap("休息好了，继续");Assert.That(game.RestReminder,Is.False);Assert.That((float)clock.GetValue(game),Is.LessThan(2));Assert.That(game.Session.Paused||game.Reeling,Is.False);RestHud();
                CollectionAssert.AreEqual(saved,File.ReadAllBytes(Path.Combine(root,"ocean-progress.json")),"Rest and navigation must not rewrite progress or settings");
            }
            File.WriteAllText(Path.Combine(archive,"rest.json"),"{\"status\":\"PASS\",\"scope\":\"headless active-clock boundary and real UI pointer regression; not elapsed 15-minute or child/device acceptance\",\"hands\":2,\"saveBytesUnchanged\":true}");
        }
        [UnityTest,Timeout(120000)] public IEnumerator DedicatedPagesKeepModelClearAndBackReturnsToTheirActualSource()
        {
            var args=LaunchArguments.Get();int index=Array.IndexOf(args,"--ocean-save-root");Assert.That(index,Is.GreaterThanOrEqualTo(0));root=Path.GetFullPath(args[index+1]);Assert.That(root,Does.Contain("unity-test-saves"));
            archive=Path.Combine(root,"dedicated-pages-before-after");Directory.CreateDirectory(archive);
            foreach(string suffix in new[]{"",".bak",".tmp"}){string file=Path.Combine(root,"ocean-progress.json")+suffix;if(File.Exists(file)){string saved=Path.Combine(archive,"before"+suffix+".json");File.Move(file,saved);originals.Add(file,saved);}}
            yield return SceneManager.LoadSceneAsync("Bay",LoadSceneMode.Single);scene=SceneManager.GetSceneByName("Bay");yield return null;yield return null;game=UnityEngine.Object.FindFirstObjectByType<GameController>();game.Configure(_=>{});yield return null;yield return null;
            string savedData=JsonUtility.ToJson(game.Progress.Data);var ui=game.GetComponent<GameUI>();int pages=0,critical=0;
            IEnumerator TapExact(string label,string expected)
            {
                var b=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(x=>x.isActiveAndEnabled&&x.interactable&&x.GetComponentInChildren<Text>()?.text==label);
                var scroll=b.GetComponentInParent<ScrollRect>();
                if(scroll!=null&&scroll.content.rect.height>scroll.viewport.rect.height)
                {
                    Canvas.ForceUpdateCanvases();var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.content,b.GetComponent<RectTransform>());float range=scroll.content.rect.height-scroll.viewport.rect.height;scroll.verticalNormalizedPosition=1-Mathf.Clamp(scroll.content.rect.yMax-bounds.center.y-scroll.viewport.rect.height/2,0,range)/range;
                }
                yield return null;yield return null;Canvas.ForceUpdateCanvases();
                var rt=b.GetComponent<RectTransform>();var point=RectTransformUtility.WorldToScreenPoint(null,rt.TransformPoint(rt.rect.center));var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
                Assert.That(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,Is.True,label+" must be the top visible target");
                ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerClickHandler);yield return null;yield return null;
                Assert.That(ui.PageName,Is.EqualTo(expected),label+" back/context route");Assert.That(game.Session.Paused&&!game.Reeling,Is.True,"Pages must never resume a held action");
                if(expected!="首页"&&expected!="出海")
                {
                    var rects=UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
                    Assert.That(rects.Any(t=>t.name=="Navigation"||t.name=="Footer"),Is.False,"No duplicate global tab bar or unrelated footer");
                    foreach(var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(x=>x.GetComponentInParent<ScrollRect>()==null))
                    {
                        var r=button.GetComponent<RectTransform>();var corners=new Vector3[4];r.GetWorldCorners(corners);var safe=DisplayMetrics.SafeArea;float unit=DisplayMetrics.PixelsPerPoint;
                        Assert.That(r.rect.width,Is.GreaterThanOrEqualTo(44));Assert.That(r.rect.height,Is.GreaterThanOrEqualTo(44));
                        Assert.That(corners[0].x,Is.GreaterThanOrEqualTo(safe.xMin-.1f));Assert.That(corners[0].y,Is.GreaterThanOrEqualTo(safe.yMin-.1f));Assert.That(corners[2].x,Is.LessThanOrEqualTo(safe.xMax+.1f));Assert.That(corners[2].y,Is.LessThanOrEqualTo(safe.yMax+.1f));
                        var text=button.GetComponentInChildren<Text>();Assert.That(text.fontSize,Is.GreaterThanOrEqualTo(17));Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+.1f),expected+"/"+text.text);critical++;
                    }
                    pages++;
                }
            }
            yield return TapExact("海域与装备","准备");yield return TapExact("更换钓具","钓具");yield return TapExact("上一级","准备");
            yield return TapExact("看看海湾","海湾观察");Assert.That(UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).Any(t=>t.name=="HabitatDetails"),Is.False);yield return TapExact("上一级","准备");yield return TapExact("上一级","首页");
            yield return TapExact("图鉴","图鉴");Assert.That(ui.FocusedFish,Is.Null);yield return TapExact("未发现 · 蓝鳍鲹","鱼类详情");
            Assert.That(ui.TryJournalModelRect(out var model),Is.True);Assert.That(model.width/DisplayMetrics.SafeArea.width,Is.GreaterThan(.85f));
            Assert.That(UnityEngine.Object.FindFirstObjectByType<AuthoredSpinningRig>(FindObjectsInactive.Include).gameObject.activeInHierarchy,Is.False,"Fish appreciation must hide the unrelated fishing rig");
            Assert.That(UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Length,Is.Zero,"3D model has no large reading side panel");
            yield return TapExact("认识它","鱼类资料");Assert.That(ui.FocusedFish,Is.Null);Assert.That(UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Any(s=>s.content.name=="SpeciesContent"),Is.True);
            yield return TapExact("上一级","鱼类详情");Assert.That(ui.FocusedFish,Is.EqualTo("trevally"));yield return TapExact("上一级","图鉴");yield return TapExact("上一级","首页");
            yield return TapExact("设置","设置");
            foreach(string group in new[]{"操作与辅助","声音与触觉","画面与动态"}){yield return TapExact(group,group);yield return TapExact("上一级","设置");}
            yield return TapExact("上一级","首页");yield return TapExact("钓具","钓具");yield return TapExact("观察入门钓组","钓组预览");
            Assert.That(UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).Any(t=>t.name=="GearDetails"),Is.False);
            yield return TapExact("上一级","钓具");yield return TapExact("上一级","首页");
            Assert.That(JsonUtility.ToJson(game.Progress.Data),Is.EqualTo(savedData));File.WriteAllText(Path.Combine(archive,"pages.json"),"{\"status\":\"PASS\",\"scope\":\"strict visible hierarchy/back/safe-target checks, no automatic destination routing; not native or OS touch\",\"pages\":"+pages+",\"criticalButtons\":"+critical+"}");
        }
        IEnumerator RestTap(string text)
        {
            yield return UnityEngine.Object.FindFirstObjectByType<RuntimeSmoke>().ResolveControlForReview(text,true);
            Canvas.ForceUpdateCanvases();var b=UnityEngine.Object.FindFirstObjectByType<RuntimeSmoke>().FindForReview(text,true);
            var rt=b.GetComponent<RectTransform>();var point=RectTransformUtility.WorldToScreenPoint(null,rt.TransformPoint(rt.rect.center));var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Assert.That(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,Is.True,"Top pointer target must be "+text);
            ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerClickHandler);yield return null;yield return null;
        }
        void RestHud()
        {
            Canvas.ForceUpdateCanvases();Assert.That(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Length,Is.EqualTo(2));var areas=UnityEngine.Object.FindObjectsByType<HudFootprint>(FindObjectsSortMode.None);Assert.That(areas.Length,Is.EqualTo(3));var safe=DisplayMetrics.SafeArea;float area=0;var center=new Rect(safe.xMin+safe.width*.25f,safe.yMin+safe.height*.45f,safe.width*.5f,safe.height*.4f);
            foreach(var mark in areas){var corners=new Vector3[4];mark.GetComponent<RectTransform>().GetWorldCorners(corners);var rect=Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);area+=rect.width*rect.height;Assert.That(rect.Overlaps(center),Is.False);}
            Assert.That(area/(safe.width*safe.height),Is.LessThanOrEqualTo(.15f));
        }
    }
}
