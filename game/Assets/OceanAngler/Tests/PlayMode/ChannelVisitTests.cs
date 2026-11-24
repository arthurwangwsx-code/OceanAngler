using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace OceanAngler.Tests
{
    public sealed class ChannelVisitTests
    {
        Scene scene;GameController game;string root,archive;readonly Dictionary<string,string> originals=new Dictionary<string,string>();
        void Click(string text)
        {
            if(UnityEngine.Object.FindFirstObjectByType<GameUI>().Playing&&new[]{"出海","图鉴","钓具","任务","设置","海域与装备"}.Contains(text))
                UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.isActiveAndEnabled&&b.GetComponentInChildren<Text>()?.text=="暂停").onClick.Invoke();
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.isActiveAndEnabled&&b.interactable&&b.GetComponentInChildren<Text>()?.text==text).onClick.Invoke();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(game!=null)game.Pause(true);if(scene.IsValid()&&scene.isLoaded){var empty=SceneManager.CreateScene("AfterChannelVisit");SceneManager.SetActiveScene(empty);yield return SceneManager.UnloadSceneAsync(scene);}
            if(root!=null)foreach(string suffix in new[]{"",".bak",".tmp"}){string file=Path.Combine(root,"ocean-progress.json")+suffix;if(File.Exists(file))File.Move(file,Path.Combine(archive,"result"+suffix+".json"));if(originals.TryGetValue(file,out var saved))File.Move(saved,file);}
        }
        [UnityTest] public IEnumerator SharedViewsAndFishingSurviveRepeatedAuthoredAndPrototypeSwitches()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--ocean-save-root");Assert.That(index,Is.GreaterThanOrEqualTo(0));root=Path.GetFullPath(args[index+1]);Assert.That(root,Does.Contain("unity-test-saves"));archive=Path.Combine(root,"channel-fixture");Assert.That(Directory.Exists(archive),Is.False);Directory.CreateDirectory(archive);
            foreach(string suffix in new[]{"",".bak",".tmp"}){string file=Path.Combine(root,"ocean-progress.json")+suffix;if(File.Exists(file)){string saved=Path.Combine(archive,"before"+suffix+".json");File.Move(file,saved);originals.Add(file,saved);}}
            yield return SceneManager.LoadSceneAsync("Bay",LoadSceneMode.Single);scene=SceneManager.GetSceneByName("Bay");yield return null;yield return null;
            game=UnityEngine.Object.FindFirstObjectByType<GameController>();var world=game.GetComponent<OceanWorld>();var ui=game.GetComponent<GameUI>();game.Pause(true);Assert.That(world.ActiveHabitat,Is.TypeOf<BayHabitat>());Assert.That(GameObject.Find("OtherHabitatPrototype"),Is.Null);
            while(game.Progress.Data.totalPoints<1800){var f=game.Content.Fish("trevally");game.Progress.Settle(new CatchRecord{catchId=Guid.NewGuid().ToString(),fishId=f.id,habitatId=f.habitatId,mode="assisted",weight=f.minWeight,length=f.minLength,skillPoints=30});}
            int initial=game.Progress.Data.catches.Count;
            for(int trip=0;trip<3;trip++)
            {
                game.SelectHabitat("channel");yield return null;yield return null;
                Assert.That(world.ActiveHabitat,Is.TypeOf<MarineHabitat>());Assert.That(UnityEngine.Object.FindObjectsByType<MarineHabitat>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,Is.EqualTo(1));Assert.That(GameObject.Find("OtherHabitatPrototype"),Is.Null);
                Click("海域与装备");yield return null;yield return null;
                Click("看看海域");yield return null;yield return null;Assert.That(ui.InspectingHabitat&&game.Session.Paused,Is.True);
                if(!ui.HabitatSurfaceView){Click("回到水面");yield return null;yield return null;}
                Assert.That(Camera.main.transform.position.y,Is.GreaterThan(world.ActiveHabitat.SeaLevel));Click("潜入水下");yield return null;yield return null;Assert.That(Camera.main.transform.position.y,Is.LessThan(world.ActiveHabitat.SeaLevel));Click("换个角度");yield return null;yield return null;
                var target=ui.HabitatAngle%2==0?world.ActiveHabitat.underwaterCamera:world.ActiveHabitat.reefCamera;Assert.That(Vector3.Distance(Camera.main.transform.position,target.position),Is.LessThan(.001f));
                game.Configure(s=>s.reducedMotion=true);yield return null;yield return null;Assert.That(world.ActiveHabitat.suspendedParticles.gameObject.activeSelf,Is.False);
                Click("返回钓鱼");yield return null;yield return null;Assert.That(game.Session.Paused,Is.True);

                Click("继续探索");game.Cast(.5f);for(int i=0;i<12000&&game.Session.Phase!=FishingPhase.Landed;i++){if(game.Session.Phase==FishingPhase.Bite)game.Strike();game.Session.Tick(1f/30,game.Session.Tension<.65f);}Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Landed));yield return null;yield return null;
                Click("记录并放流");yield return null;yield return null;Assert.That(game.Progress.Data.catches.Count,Is.EqualTo(initial+trip+1));Assert.That(new SaveStore(Path.Combine(root,"ocean-progress.json"),game.Content).Load().data.habitatId,Is.EqualTo("channel"));
                game.Pause(true);game.SelectHabitat("bay");yield return null;yield return null;Assert.That(world.ActiveHabitat,Is.TypeOf<BayHabitat>());Assert.That(world.ActiveHabitat.GetComponent<BayHabitat>().school.Members.Length,Is.EqualTo(3));
                game.SelectHabitat("slope");yield return null;yield return null;Assert.That(world.ActiveHabitat,Is.Null);Assert.That(GameObject.Find("OtherHabitatPrototype"),Is.Not.Null);Assert.That(Camera.main.clearFlags,Is.EqualTo(CameraClearFlags.SolidColor));Assert.That(RenderSettings.fogColor,Is.EqualTo(new Color(.025f,.10f,.22f)));
                game.SelectHabitat("bay");yield return null;yield return null;Assert.That(GameObject.Find("OtherHabitatPrototype"),Is.Null);
            }
            Assert.That(UnityEngine.Object.FindObjectsByType<MarineHabitat>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,Is.EqualTo(1));Assert.That(new SaveStore(Path.Combine(root,"ocean-progress.json"),game.Content).Load().data.catches.Count,Is.EqualTo(initial+3));
        }
    }
}
