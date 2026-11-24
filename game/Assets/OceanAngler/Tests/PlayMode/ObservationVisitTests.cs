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
    public sealed class ObservationVisitTests
    {
        Scene scene;GameController game;string root,archive;readonly Dictionary<string,string> originals=new Dictionary<string,string>();
        void Click(string text)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.isActiveAndEnabled&&b.interactable&&b.GetComponentInChildren<Text>().text==text).onClick.Invoke();
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(game!=null)game.Pause(true);
            if(scene.IsValid()&&scene.isLoaded){var empty=SceneManager.CreateScene("AfterObservations");SceneManager.SetActiveScene(empty);yield return SceneManager.UnloadSceneAsync(scene);}
            if(root!=null)foreach(string suffix in new[]{"",".bak",".tmp"})
            {
                string file=Path.Combine(root,"ocean-progress.json")+suffix;
                if(File.Exists(file))File.Move(file,Path.Combine(archive,"result"+suffix+".json"));
                if(originals.TryGetValue(file,out var saved))File.Move(saved,file);
            }
        }
        [UnityTest] public IEnumerator ProtectedVisitsRecordSeparatelyPersistAndStopOnNavigationOrBackground()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--ocean-save-root");Assert.That(index,Is.GreaterThanOrEqualTo(0));root=Path.GetFullPath(args[index+1]);Assert.That(root,Does.Contain("unity-test-saves"));
            archive=Path.Combine(root,"observation-fixture");Assert.That(Directory.Exists(archive),Is.False);Directory.CreateDirectory(archive);
            foreach(string suffix in new[]{"",".bak",".tmp"}){string file=Path.Combine(root,"ocean-progress.json")+suffix;if(File.Exists(file)){string saved=Path.Combine(archive,"before"+suffix+".json");File.Move(file,saved);originals.Add(file,saved);}}
            yield return SceneManager.LoadSceneAsync("Bay",LoadSceneMode.Single);scene=SceneManager.GetSceneByName("Bay");yield return null;yield return null;
            game=UnityEngine.Object.FindFirstObjectByType<GameController>();var ui=game.GetComponent<GameUI>();var world=game.GetComponent<OceanWorld>();var encounter=world.Observations;game.gameObject.SendMessage("OnApplicationFocus",true);
            game.Cast(.5f);for(int i=0;i<1200&&game.Session.Phase!=FishingPhase.Bite;i++)game.Session.Tick(1f/30,false);game.Strike();game.HoldReel(true);Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Fighting));
            Click("图鉴");yield return null;yield return null;var activeCatch=game.Session.Catch;float heldTension=game.Session.Tension;Click("保护观察  0 / 3");yield return null;yield return null;
            Assert.That(game.Progress.Data.observations,Is.Empty);Assert.That(game.Session.Paused&&!game.Reeling,Is.True);
            Assert.That(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Count(b=>!b.interactable&&b.GetComponentInChildren<Text>().text.Contains("探索积分")),Is.EqualTo(2));
            Click("观察绿海龟");yield return null;yield return null;
            Assert.That(ui.InspectingObservation,Is.True);Assert.That(world.TargetFish.gameObject.activeSelf,Is.False);Assert.That(UnityEngine.Object.FindFirstObjectByType<AuthoredSpinningRig>(FindObjectsInactive.Include).gameObject.activeSelf,Is.False);
            Assert.That(encounter.Visual.GetComponentsInChildren<Collider>(),Is.Empty);Assert.That(encounter.Visual.GetComponent<FishVisual>(),Is.Null);
            game.RecordObservation();Assert.That(game.Progress.Data.observations,Is.Empty);Assert.That(encounter.CanRecord,Is.False);
            Click("安静观察");yield return new WaitForSeconds(2.3f);yield return null;Assert.That(encounter.CanRecord,Is.True);Assert.That(game.Session.Paused,Is.True);
            string save=Path.Combine(root,"ocean-progress.json");Directory.CreateDirectory(save);
            LogAssert.Expect(LogType.Warning,new Regex("Progress operation failed: (IOException|UnauthorizedAccessException)"));Click("记入观察图鉴");yield return null;yield return null;
            Assert.That(game.Progress.Data.observations,Is.Empty);Assert.That(encounter.CanRecord,Is.True);Directory.Delete(save);
            Click("记入观察图鉴");yield return null;yield return null;CollectionAssert.AreEqual(new[]{"green-turtle"},game.Progress.Data.observations);Assert.That(game.Progress.Data.schemaVersion,Is.EqualTo(2));
            string once=File.ReadAllText(save);game.RecordObservation();Assert.That(File.ReadAllText(save),Is.EqualTo(once));Assert.That(game.Progress.Data.totalPoints,Is.Zero);Assert.That(game.Progress.Data.catches,Is.Empty);
            Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Fighting));Assert.That(game.Session.Catch,Is.SameAs(activeCatch));Assert.That(game.Session.Tension,Is.EqualTo(heldTension));
            Click("返回观察图鉴");yield return null;yield return null;Assert.That(encounter.Visual.gameObject.activeSelf,Is.False);Assert.That(encounter.Playing||encounter.CanRecord,Is.False);game.RecordObservation();Assert.That(game.Progress.Data.observations.Count,Is.EqualTo(1));
            // Seed the real point ledger, rather than assigning an invalid points total.
            while(game.Progress.Data.totalPoints<900)
            {
                var f=game.Content.Fish("trevally");game.Progress.Settle(new CatchRecord{catchId=Guid.NewGuid().ToString(),fishId=f.id,habitatId=f.habitatId,mode="assisted",weight=f.minWeight,length=f.minLength,skillPoints=30});
            }
            Click("图鉴");yield return null;yield return null;Click("保护观察  1 / 3");yield return null;yield return null;
            int points=game.Progress.Data.totalPoints,catches=game.Progress.Data.catches.Count;var tasks=game.Content.tasks.Select(t=>TaskProgress.Value(t,game.Progress.Data)).ToArray();
            Assert.That(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Count(b=>!b.interactable&&b.GetComponentInChildren<Text>().text=="返回准备后前往"),Is.EqualTo(2));
            game.ReturnToPreparation();Click("图鉴");yield return null;yield return null;Click("保护观察  1 / 3");yield return null;yield return null;
            Click("观察宽吻海豚");yield return null;yield return null;Assert.That(game.Progress.Data.habitatId,Is.EqualTo("channel"));Assert.That(encounter.SpeciesId,Is.EqualTo("bottlenose-dolphin"));
            Click("安静观察");yield return new WaitForSeconds(2.3f);Click("记入观察图鉴");yield return null;yield return null;
            Click("返回观察图鉴");yield return null;yield return null;Click("观察鲸鲨");yield return null;yield return null;
            game.Configure(s=>s.reducedMotion=true);yield return null;yield return null;Click("安静观察");var visual=encounter.Visual;var before=visual.GetComponentsInChildren<Transform>().Select(t=>t.localRotation).ToArray();yield return new WaitForSeconds(2.3f);
            CollectionAssert.AreEqual(before,visual.GetComponentsInChildren<Transform>().Select(t=>t.localRotation).ToArray());Assert.That(encounter.CanRecord,Is.True);
            game.gameObject.SendMessage("OnApplicationPause",true);yield return null;Assert.That(encounter.Playing||encounter.CanRecord,Is.False);
            game.gameObject.SendMessage("OnApplicationPause",false);yield return null;yield return null;Assert.That(encounter.Playing||encounter.CanRecord,Is.False);
            game.gameObject.SendMessage("OnApplicationFocus",false);game.gameObject.SendMessage("OnApplicationPause",false);Click("安静观察");yield return null;Assert.That(encounter.Playing,Is.False);game.gameObject.SendMessage("OnApplicationFocus",true);
            Click("安静观察");yield return new WaitForSeconds(2.3f);Click("记入观察图鉴");yield return null;yield return null;
            var loaded=new SaveStore(save,game.Content).Load();Assert.That(loaded.requiresRecovery,Is.False);Assert.That(loaded.data.observations.Count,Is.EqualTo(3));Assert.That(loaded.data.totalPoints,Is.EqualTo(points));Assert.That(loaded.data.catches.Count,Is.EqualTo(catches));CollectionAssert.AreEqual(tasks,game.Content.tasks.Select(t=>TaskProgress.Value(t,loaded.data)).ToArray());
            var layoutChecks=new List<string>();
            foreach(var species in game.Content.observations)
            {
                Click("返回观察图鉴");yield return null;yield return null;Click("观察"+species.name);yield return null;yield return null;
                var detail=UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Single(s=>s.content.name=="ObservationContent");
            foreach(float width in new[]{240f,310f,500f})
            {
                var viewport=detail.viewport;viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width);viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,90);Canvas.ForceUpdateCanvases();yield return null;Canvas.ForceUpdateCanvases();
                foreach(var text in detail.content.GetComponentsInChildren<Text>()){Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+.1f),text.name);var gen=new TextGenerator();Assert.That(gen.Populate(text.text,text.GetGenerationSettings(text.rectTransform.rect.size)),Is.True);Assert.That(gen.characterCountVisible,Is.GreaterThanOrEqualTo(text.text.Length),species.id+"/"+width+"/"+text.name);}
                detail.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();yield return null;var last=detail.content.GetComponentsInChildren<Text>().Single(t=>t.name=="ObservationCare");var corners=new Vector3[4];last.rectTransform.GetWorldCorners(corners);Assert.That(viewport.InverseTransformPoint(corners[0]).y,Is.GreaterThanOrEqualTo(viewport.rect.yMin-.2f));
            }
                layoutChecks.Add(species.id+",three-widths,PASS");
            }
            File.WriteAllLines(Path.Combine(archive,"layout.csv"),layoutChecks);
            // Both exits must be independently reachable; invoking onClick alone
            // would hide a visit-exit button covering the wide navigation bar.
            Canvas.ForceUpdateCanvases();
            foreach(string label in new[]{"出海","图鉴","钓具","任务","设置","返回观察图鉴"})
            {
                var button=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.isActiveAndEnabled&&b.interactable&&b.GetComponentInChildren<Text>().text==label);
                var rect=button.GetComponent<RectTransform>();var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
                Assert.That(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,Is.True,"Independent observation exit/navigation hit: "+label);
            }
            Click("返回观察图鉴");yield return null;yield return null;Assert.That(ui.ObservationJournal&&!ui.InspectingObservation,Is.True);
            Click("观察鲸鲨");yield return null;yield return null;
            Click("出海");yield return null;yield return null;Assert.That(game.Session.Paused,Is.True);Assert.That(encounter.Visual.gameObject.activeSelf,Is.False);Assert.That(world.TargetFish.gameObject.activeSelf,Is.True);
            Assert.That(UnityEngine.Object.FindObjectsByType<ObservationVisual>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,Is.EqualTo(1));
        }
    }
}
