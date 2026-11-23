using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace OceanAngler.Tests
{
    // Exercises actual scene lifecycle and control events, without claiming rendering or touch acceptance.
    public sealed class IntegratedWorldTests
    {
        Scene scene;GameController game;
        void Click(string text)
        {
            var button=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.isActiveAndEnabled&&b.interactable&&b.GetComponentInChildren<Text>()?.text==text);
            button.onClick.Invoke();
        }
        [UnityTearDown] public IEnumerator CleanUp()
        {
            if(game!=null)game.Pause(true);
            if(scene.IsValid()&&scene.isLoaded)
            {
                var empty=SceneManager.CreateScene("AfterOceanIntegration");SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }
        [UnityTest] public IEnumerator ObservationGearDemoAndFishingShareOnePausedWorld()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--ocean-save-root");
            Assert.That(index,Is.GreaterThanOrEqualTo(0),"Never load the game scene against personal saves");
            Assert.That(index+1,Is.LessThan(args.Length));string save=Path.GetFullPath(args[index+1]);
            Assert.That(save,Does.Contain("unity-test-saves"));Assert.That(File.Exists(Path.Combine(save,"ocean-progress.json")),Is.False);
            yield return SceneManager.LoadSceneAsync("Bay",LoadSceneMode.Single);scene=SceneManager.GetSceneByName("Bay");yield return null;yield return null;
            game=UnityEngine.Object.FindFirstObjectByType<GameController>();var ui=game.GetComponent<GameUI>();game.Pause(true);yield return null;yield return null;
            var gear=UnityEngine.Object.FindFirstObjectByType<AuthoredSpinningRig>();var fish=UnityEngine.Object.FindFirstObjectByType<AuthoredFishVisual>();var bay=UnityEngine.Object.FindFirstObjectByType<BayHabitat>();
            Assert.That(gear,Is.Not.Null);Assert.That(fish,Is.Not.Null);Assert.That(bay,Is.Not.Null);Assert.That(GameObject.Find("OtherHabitatPrototype"),Is.Null);
            if(ui.Compact){Click("海域与装备");yield return null;yield return null;}
            Click("看看海湾");yield return null;yield return null;
            Assert.That(ui.InspectingHabitat&&game.Session.Paused,Is.True);Assert.That(gear.gameObject.activeSelf||fish.gameObject.activeSelf,Is.False);Assert.That(Camera.main.transform.position.y,Is.GreaterThan(bay.SeaLevel));
            Click("潜入水下");yield return null;yield return null;Assert.That(Camera.main.transform.position.y,Is.LessThan(bay.SeaLevel));
            Click("返回钓鱼");yield return null;yield return null;Assert.That(game.Session.Paused,Is.True);Assert.That(gear.gameObject.activeSelf&&fish.gameObject.activeSelf,Is.True);
            Click("钓具");yield return null;yield return null;Click("观察入门钓组");yield return null;yield return null;
            Assert.That(ui.InspectingGear&&game.Session.Paused,Is.True);Assert.That(fish.gameObject.activeSelf,Is.False);Assert.That(gear.gameObject.activeSelf,Is.True);
            var prior=gear.crank.localRotation;Click("试摇卷线器");yield return new WaitForSeconds(.2f);
            Assert.That(Quaternion.Angle(prior,gear.crank.localRotation),Is.GreaterThan(10));Assert.That(game.Reeling,Is.False);
            Click("出海");yield return null;yield return null;var stopped=gear.crank.localRotation;yield return new WaitForSeconds(.15f);
            Assert.That(ui.GearReelingDemo,Is.False);Assert.That(game.Session.Paused,Is.True);Assert.That(Quaternion.Angle(stopped,gear.crank.localRotation),Is.LessThan(.02f));
            Assert.That(Vector3.Distance(Camera.main.transform.position,bay.underwaterCamera.position),Is.LessThan(.001f));
            if(ui.Compact){Click("收起准备");yield return null;yield return null;}
            Click("继续探索");yield return null;yield return null;Assert.That(game.Session.Paused,Is.False);
            game.Cast(.5f);Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Casting));Assert.That(game.Reeling,Is.False);
            for(int i=0;i<600&&game.Session.Phase!=FishingPhase.Bite;i++)game.Session.Tick(1f/30,false);
            Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Bite));game.Strike();game.HoldReel(true);Assert.That(game.Reeling,Is.True);yield return null;yield return null;
            Click("钓具");yield return null;yield return null;Click("观察入门钓组");yield return null;yield return null;
            Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Fighting));Assert.That(game.Session.Paused&&!game.Reeling,Is.True);
            Click("出海");yield return null;yield return null;Click("继续探索");yield return null;yield return null;
            Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Fighting));Assert.That(game.Session.Paused||game.Reeling,Is.False);
        }
    }
}
