using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace OceanAngler.Tests
{
    // Explicitly selected species and rule-valid bootstrap catches in an isolated
    // test save. This is integration coverage, not natural progression or OS touch.
    public sealed class HalibutWorldTests
    {
        Scene scene;GameController game;string root,archive;readonly Dictionary<string,string> originals=new Dictionary<string,string>();
        IEnumerator Tap(string text)
        {
            var button=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.isActiveAndEnabled&&b.interactable&&b.GetComponentInChildren<Text>()?.text==text);
            var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);yield return null;yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(game!=null)game.Pause(true);
            if(scene.IsValid()&&scene.isLoaded){var empty=SceneManager.CreateScene("AfterHalibutWorld");SceneManager.SetActiveScene(empty);yield return SceneManager.UnloadSceneAsync(scene);}
            if(root!=null)foreach(string suffix in new[]{"",".bak",".tmp"}){string file=Path.Combine(root,"ocean-progress.json")+suffix;if(File.Exists(file))File.Move(file,Path.Combine(archive,"after"+suffix+".json"));}
            foreach(var entry in originals)File.Move(entry.Value,entry.Key);originals.Clear();
        }
        [UnityTest,Timeout(120000)] public IEnumerator AuthoredHalibutSharesProductionEncounterHookReleaseAndJournal()
        {
            var args=LaunchArguments.Get();int index=Array.IndexOf(args,"--ocean-save-root");Assert.That(index,Is.GreaterThanOrEqualTo(0));root=Path.GetFullPath(args[index+1]);Assert.That(root,Does.Contain("unity-test-saves"));archive=Path.Combine(root,"halibut-world-before-after");Assert.That(Directory.Exists(archive),Is.False);Directory.CreateDirectory(archive);
            foreach(string suffix in new[]{"",".bak",".tmp"}){string file=Path.Combine(root,"ocean-progress.json")+suffix;if(File.Exists(file)){string copy=Path.Combine(archive,"before"+suffix+".json");File.Move(file,copy);originals[file]=copy;}}
            yield return SceneManager.LoadSceneAsync("Bay",LoadSceneMode.Single);scene=SceneManager.GetSceneByName("Bay");yield return null;yield return null;game=UnityEngine.Object.FindFirstObjectByType<GameController>();game.enabled=false;var ui=game.GetComponent<GameUI>();var world=game.GetComponent<OceanWorld>();var first=game.Content.Fish("trevally");
            while(game.Progress.Data.totalPoints<game.Content.Habitat("slope").unlockPoints)game.Progress.Settle(new CatchRecord{catchId=Guid.NewGuid().ToString(),fishId=first.id,habitatId=first.habitatId,mode="assisted",weight=first.minWeight,length=first.minLength,skillPoints=0});
            game.SelectHabitat("slope");Assert.That(game.Progress.Data.habitatId,Is.EqualTo("slope"));yield return Tap("继续探索");
            game.Session.Cast(game.Content.Fish("halibut"),game.Progress.Data,game.Content,new System.Random(971));yield return null;yield return null;
            var visual=world.TargetFish as AuthoredFishVisual;Assert.That(visual,Is.Not.Null);Assert.That(visual.GetComponentsInChildren<SkinnedMeshRenderer>().Any(r=>r.name=="Halibut_LOD0"),Is.True);Assert.That(Vector3.Dot(visual.transform.up,Vector3.up),Is.GreaterThan(.98f));
            for(int i=0;i<1200&&game.Session.Phase!=FishingPhase.Bite;i++)game.Session.Tick(1f/30,false);Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Bite));game.Strike();yield return null;yield return null;
            var rig=UnityEngine.Object.FindFirstObjectByType<AuthoredSpinningRig>();var endpoint=rig.fishingLine.GetPosition(rig.fishingLine.positionCount-1);if(!rig.fishingLine.useWorldSpace)endpoint=rig.fishingLine.transform.TransformPoint(endpoint);Assert.That(Vector3.Distance(endpoint,visual.HookPosition),Is.LessThan(.002f));
            for(int i=0;i<9000&&game.Session.Phase!=FishingPhase.Landed;i++)game.Session.Tick(1f/30,game.Session.Tension<.65f);Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Landed));yield return null;yield return null;
            var caught=game.Session.Catch;int points=game.Progress.Data.totalPoints;game.Release();Assert.That(game.Progress.Data.catches.Any(c=>c.catchId==caught.catchId),Is.True);int releasedPoints=game.Progress.Data.totalPoints;Assert.That(releasedPoints,Is.GreaterThan(points));game.Release();Assert.That(game.Progress.Data.totalPoints,Is.EqualTo(releasedPoints));yield return new WaitForSeconds(.5f);Assert.That(world.TargetFish,Is.SameAs(visual));Assert.That(visual.semantic,Is.EqualTo("release"));
            yield return Tap("暂停");yield return Tap("图鉴");yield return Tap("大西洋庸鲽");Assert.That(ui.FocusedFish,Is.EqualTo("halibut"));
            // Opening the journal first selects its default species, so the
            // world legitimately replaces the previous instance. Check the same
            // authored species asset on return, not object identity across species.
            visual=world.TargetFish as AuthoredFishVisual;Assert.That(visual,Is.Not.Null);Assert.That(visual.GetComponentsInChildren<SkinnedMeshRenderer>().Any(r=>r.name=="Halibut_LOD0"),Is.True);Assert.That(ui.PageName,Is.EqualTo(ui.Compact?"鱼类详情":"图鉴"));
            var expected=Quaternion.Euler(0,-25,-3)*Quaternion.Euler(-55,0,0);Assert.That(Quaternion.Angle(visual.transform.rotation,expected),Is.LessThan(.1f));Assert.That(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Single(t=>t.name=="ModelStatus").text,Does.Contain("还在制作"));
            byte[] before=File.ReadAllBytes(Path.Combine(root,"ocean-progress.json"));yield return Tap("旋转");Assert.That(ui.ModelYaw,Is.EqualTo(30));yield return Tap("放大");yield return Tap("静止");Assert.That(visual.paused,Is.True);yield return Tap("重置");Assert.That(ui.ModelYaw,Is.Zero);Assert.That(ui.ModelZoom,Is.EqualTo(1));CollectionAssert.AreEqual(before,File.ReadAllBytes(Path.Combine(root,"ocean-progress.json")));
        }
    }
}
