using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace OceanAngler.Tests
{
    public sealed class GearFeedbackTests
    {
        [Serializable] sealed class Summary { public string status;public string[] failures,equippedGear,modelStatuses; }
        Scene scene;GameController game;string root,archive;
        readonly Dictionary<string,string> originals=new Dictionary<string,string>();
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(game!=null)game.Pause(true);
            if(scene.IsValid()&&scene.isLoaded){var empty=SceneManager.CreateScene("AfterGearFeedback");SceneManager.SetActiveScene(empty);yield return SceneManager.UnloadSceneAsync(scene);}
            if(root!=null)foreach(string suffix in new[]{"",".bak",".tmp"})
            {
                string file=Path.Combine(root,"ocean-progress.json")+suffix;
                if(File.Exists(file))File.Move(file,Path.Combine(archive,"after"+suffix+".json"));
                if(Directory.Exists(file))Directory.Move(file,Path.Combine(archive,"after-temp-directory"));
            }
            foreach(var pair in originals)File.Move(pair.Value,pair.Key);originals.Clear();
        }
        IEnumerator Tap(string text,bool exact=false)
        {
            yield return UnityEngine.Object.FindFirstObjectByType<RuntimeSmoke>().ResolveControlForReview(text,exact);
            if(UnityEngine.Object.FindFirstObjectByType<GameUI>().Playing&&new[]{"出海","图鉴","钓具","任务","设置","海域与装备"}.Contains(text))yield return Tap("暂停",true);
            var b=UnityEngine.Object.FindFirstObjectByType<RuntimeSmoke>().FindForReview(text,exact);
            var scroll=b.GetComponentInParent<ScrollRect>();Canvas.ForceUpdateCanvases();
            if(scroll!=null&&scroll.content.rect.height>scroll.viewport.rect.height)
            {
                var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.content,b.GetComponent<RectTransform>());float range=scroll.content.rect.height-scroll.viewport.rect.height;
                scroll.verticalNormalizedPosition=1-Mathf.Clamp(scroll.content.rect.yMax-bounds.center.y-scroll.viewport.rect.height/2,0,range)/range;
            }
            yield return null;yield return null;var rect=b.GetComponent<RectTransform>();var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));var data=new PointerEventData(EventSystem.current){position=point};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Assert.That(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,Is.True,"Visible target "+text);
            ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerClickHandler);yield return null;yield return null;
        }
        [UnityTest,Timeout(240000)] public IEnumerator SelectionRemainsVisibleAndOnlyCommittedEquipmentGetsSuccessFeedback()
        {
            var args=LaunchArguments.Get();int index=Array.IndexOf(args,"--ocean-save-root");Assert.That(index,Is.GreaterThanOrEqualTo(0));root=Path.GetFullPath(args[index+1]);Assert.That(root,Does.Contain("unity-test-saves"));
            archive=Path.Combine(root,"gear-feedback-before-after");Assert.That(Directory.Exists(archive),Is.False);Directory.CreateDirectory(archive);
            foreach(string suffix in new[]{"",".bak",".tmp"})
            {
                string file=Path.Combine(root,"ocean-progress.json")+suffix;
                if(File.Exists(file)){string saved=Path.Combine(archive,"before"+suffix+".json");File.Move(file,saved);originals.Add(file,saved);}
            }
            var catalog=ContentCatalog.Load();var store=new SaveStore(Path.Combine(root,"ocean-progress.json"),catalog);var service=new ProgressService(catalog,store);
            // Logical fixture uses real simulation/settlement, not injected points.
            // Native review instead uses provenanced copies of actual UI catches.
            for(int cycle=0;service.Data.totalPoints<catalog.gear.Max(g=>g.unlockPoints)&&cycle<80;cycle++)
            {
                var session=new FishingSession();session.Cast(catalog.fish[0],service.Data,catalog,new System.Random(cycle));
                for(int frame=0;frame<5400&&session.Phase!=FishingPhase.Landed;frame++){if(session.Phase==FishingPhase.Bite)session.Strike();session.Tick(1f/30,session.Tension<.65f);}
                Assert.That(session.Phase,Is.EqualTo(FishingPhase.Landed));service.Settle(session.Catch);
            }
            Assert.That(service.Data.totalPoints,Is.GreaterThanOrEqualTo(catalog.gear.Max(g=>g.unlockPoints)));
            yield return SceneManager.LoadSceneAsync("Bay",LoadSceneMode.Single);scene=SceneManager.GetSceneByName("Bay");yield return null;yield return null;
            game=UnityEngine.Object.FindFirstObjectByType<GameController>();string output=Path.Combine(root,"gear-review");yield return game.GetComponent<RuntimeSmoke>().RunGearReview(output,false);
            var result=JsonUtility.FromJson<Summary>(File.ReadAllText(Path.Combine(output,"receipt.json")));
            Assert.That(result.status,Is.EqualTo("PASS"),string.Join("\n",result.failures));CollectionAssert.AreEquivalent(catalog.gear.Select(g=>g.id),result.equippedGear);CollectionAssert.AreEquivalent(catalog.fish.Select(f=>f.id),result.modelStatuses);
            yield return Tap("钓具",true);yield return Tap("鱼竿",true);
            string originalGear=game.Progress.Data.rodId;int points=game.Progress.Data.totalPoints;string before=File.ReadAllText(store.PathName);string blocked=store.PathName+".tmp";Directory.CreateDirectory(blocked);
            LogAssert.Expect(LogType.Warning,new Regex("Progress operation failed: (IOException|UnauthorizedAccessException)"));yield return Tap("深水竿");
            Assert.That(game.Notice,Is.EqualTo("暂时无法保存装备，请重试。"));Assert.That(game.Progress.Data.rodId,Is.EqualTo(originalGear));Assert.That(File.ReadAllText(store.PathName),Is.EqualTo(before));Assert.That(game.Progress.Data.totalPoints,Is.EqualTo(points));
            Directory.Move(blocked,Path.Combine(archive,"write-blocker"));yield return Tap("深水竿");
            Assert.That(game.Notice,Is.EqualTo("已装备深水竿，研究积分未扣除。"));Assert.That(game.Progress.Data.rodId,Is.EqualTo("rod-6"));Assert.That(store.Load().data.rodId,Is.EqualTo("rod-6"));Assert.That(game.Progress.Data.totalPoints,Is.EqualTo(points));
            File.WriteAllText(Path.Combine(output,"write-failure-retry.json"),"{\"status\":\"PASS\",\"failedWriteDidNotShowSuccess\":true,\"retryPersisted\":true}");
        }
    }
}
