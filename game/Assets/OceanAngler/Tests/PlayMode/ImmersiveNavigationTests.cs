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
    public sealed class ImmersiveNavigationTests
    {
        [Serializable] sealed class Result { public string status;public string[] failures;public int catchesBefore,catchesAfter;public bool unfinishedExitPreservedLedger,settingsRestored; }
        Scene scene;GameController game;string root,archive;readonly Dictionary<string,string> originals=new Dictionary<string,string>();
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(game!=null)game.Pause(true);
            if(scene.IsValid()&&scene.isLoaded){var empty=SceneManager.CreateScene("AfterImmersiveNavigation");SceneManager.SetActiveScene(empty);yield return SceneManager.UnloadSceneAsync(scene);}
            if(root!=null)foreach(string suffix in new[]{"",".bak",".tmp"}){string file=Path.Combine(root,"ocean-progress.json")+suffix;if(File.Exists(file))File.Move(file,Path.Combine(archive,"after"+suffix+".json"));}
            foreach(var pair in originals)File.Move(pair.Value,pair.Key);originals.Clear();
        }
        [UnityTest,Timeout(90000)] public IEnumerator EveryMeterValueFitsTheUnchangedHudRegion()
        {
            var args=LaunchArguments.Get();int index=Array.IndexOf(args,"--ocean-save-root");Assert.That(index,Is.GreaterThanOrEqualTo(0));root=Path.GetFullPath(args[index+1]);Assert.That(root,Does.Contain("unity-test-saves"));
            archive=Path.Combine(root,"meter-values-before-after");Assert.That(Directory.Exists(archive),Is.False);Directory.CreateDirectory(archive);
            foreach(string suffix in new[]{"",".bak",".tmp"}){string file=Path.Combine(root,"ocean-progress.json")+suffix;if(File.Exists(file)){string saved=Path.Combine(archive,"before"+suffix+".json");File.Move(file,saved);originals.Add(file,saved);}}
            yield return SceneManager.LoadSceneAsync("Bay",LoadSceneMode.Single);scene=SceneManager.GetSceneByName("Bay");yield return null;yield return null;game=UnityEngine.Object.FindFirstObjectByType<GameController>();
            yield return RestTap("海域与装备");yield return RestTap("开始钓鱼");game.Cast(.6f);float deadline=Time.realtimeSinceStartup+20;
            while(game.Session.Phase!=FishingPhase.Bite&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Bite));game.Strike();yield return null;yield return null;
            var ui=game.GetComponent<GameUI>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var progress=typeof(FishingSession).GetField("<Progress>k__BackingField",flags);var tension=typeof(FishingSession).GetField("<Tension>k__BackingField",flags);Assert.That(progress,Is.Not.Null);Assert.That(tension,Is.Not.Null);
            foreach(bool left in new[]{false,true})
            {
                game.Configure(s=>s.leftHanded=left);yield return null;yield return null;
                float originalProgress=game.Session.Progress,originalTension=game.Session.Tension;bool enabled=game.enabled;byte[] saved=File.ReadAllBytes(Path.Combine(root,"ocean-progress.json"));game.enabled=false;
                try
                {
                    // Headless in-memory boundary fixture only. Never tick the
                    // fishing model or save synthetic progress/tension values.
                    for(int value=0;value<=100;value++)
                    {
                        progress.SetValue(game.Session,value/100f);tension.SetValue(game.Session,value/100f);ui.SendMessage("Update");Canvas.ForceUpdateCanvases();
                        foreach(string field in new[]{"tensionText","progressText"})
                        {
                            var text=(Text)typeof(GameUI).GetField(field,flags).GetValue(ui);Assert.That(text,Is.Not.Null);string label=field+" value="+value+" left="+left;
                            Assert.That(text.fontSize,Is.EqualTo(17),label);Assert.That(text.preferredWidth,Is.LessThanOrEqualTo(text.rectTransform.rect.width+.1f),label+" "+text.text);
                            Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+.1f),label);Assert.That(text.rectTransform.parent.GetComponent<RectTransform>().rect.size,Is.EqualTo(new Vector2(228,48)));
                        }
                    }
                }
                finally{progress.SetValue(game.Session,originalProgress);tension.SetValue(game.Session,originalTension);game.enabled=enabled;ui.SendMessage("Update");}
                CollectionAssert.AreEqual(saved,File.ReadAllBytes(Path.Combine(root,"ocean-progress.json")),"Meter boundary fixture must not rewrite the save");
            }
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
        IEnumerator RestTap(string text)
        {
            Canvas.ForceUpdateCanvases();var b=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(x=>x.isActiveAndEnabled&&x.interactable&&x.GetComponentInChildren<Text>().text==text);
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
