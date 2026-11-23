using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        void LandCurrentFish()
        {
            game.HoldReel(false);
            for(int i=0;i<9000&&game.Session.Phase!=FishingPhase.Landed;i++)
            {
                if(game.Session.Phase==FishingPhase.Bite)game.Strike();
                game.Session.Tick(1f/30,game.Session.Tension<.65f);
            }
            Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Landed));
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
            var rodRest=gear.rodBones.Last().localRotation;
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

            // A failed disk commit must not release or reward; the same fish can be retried.
            LandCurrentFish();yield return null;yield return null;
            int released=0;game.Released+=_=>released++;
            string saveFile=Path.Combine(save,"ocean-progress.json");Assert.That(Directory.Exists(saveFile)||File.Exists(saveFile),Is.False);
            Directory.CreateDirectory(saveFile);float heldLoad=game.Session.Tension;
            LogAssert.Expect(LogType.Warning,new Regex("Progress operation failed: (IOException|UnauthorizedAccessException)"));
            Click("记录并放流");yield return null;yield return null;
            Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Landed));Assert.That(game.Progress.Data.catches.Count,Is.Zero);Assert.That(released,Is.Zero);
            Assert.That(game.Session.Tension,Is.EqualTo(heldLoad));
            Assert.That(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("轻柔控线已达标")),Is.True);
            Assert.That(fish.semantic,Is.EqualTo("idle"));Directory.Delete(saveFile);
            Click("记录并放流");yield return null;yield return null;
            Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Ready));Assert.That(released,Is.EqualTo(1));Assert.That(game.Reeling,Is.False);
            Assert.That(game.Session.Tension,Is.Zero);Assert.That(game.Session.Progress,Is.Zero);
            Assert.That(Quaternion.Angle(rodRest,gear.rodBones.Last().localRotation),Is.LessThan(.001f));
            Assert.That(fish.semantic,Is.EqualTo("turn"));
            Assert.That(game.Progress.Data.catches.Single().released,Is.True);
            var persisted=new SaveStore(saveFile,game.Content).Load();Assert.That(persisted.requiresRecovery,Is.False);Assert.That(persisted.data.catches.Count,Is.EqualTo(1));
            int points=game.Progress.Data.totalPoints;var start=fish.transform.position;
            yield return new WaitForSeconds(.5f);Assert.That(Vector3.Distance(start,fish.transform.position),Is.GreaterThan(.05f));Assert.That(fish.semantic,Is.EqualTo("release"));
            game.Release();Assert.That(game.Progress.Data.totalPoints,Is.EqualTo(points));Assert.That(released,Is.EqualTo(1));
            game.Pause(true);yield return null;yield return null;var frozen=fish.transform.position;
            yield return new WaitForSeconds(.2f);Assert.That(Vector3.Distance(frozen,fish.transform.position),Is.LessThan(.0001f));
            game.Pause(false);yield return new WaitForSeconds(2.5f);Assert.That(fish.gameObject.activeSelf,Is.False);

            // New casts can immediately replace the departing fish without a presentation lock.
            game.Cast(.5f);yield return null;yield return null;LandCurrentFish();yield return null;yield return null;
            Click("记录并放流");yield return null;yield return null;Assert.That(released,Is.EqualTo(2));
            game.Cast(.5f);yield return null;yield return null;
            var current=UnityEngine.Object.FindFirstObjectByType<FishVisual>();Assert.That(current,Is.Not.Null);
            Assert.That(current.semantic,Is.EqualTo("burst"));Assert.That(current.transform.position.z,Is.EqualTo(1.2f).Within(.001f));
            game.Configure(s=>s.reducedMotion=true);LandCurrentFish();yield return null;yield return null;
            Click("记录并放流");yield return null;yield return null;
            Assert.That(released,Is.EqualTo(3));Assert.That(current.gameObject.activeSelf,Is.False);
            Assert.That(new SaveStore(saveFile,game.Content).Load().data.catches.Count,Is.EqualTo(3));

            // Opening the journal must cancel the departure without moving its inspected fish.
            game.Configure(s=>s.reducedMotion=false);game.Cast(.5f);LandCurrentFish();yield return null;yield return null;
            Click("记录并放流");yield return null;yield return null;Assert.That(released,Is.EqualTo(4));
            Click("图鉴");yield return null;yield return null;
            Assert.That(game.Session.Paused,Is.True);Assert.That(ui.FocusedFish,Is.Not.Null);
            var inspected=UnityEngine.Object.FindFirstObjectByType<FishVisual>();Assert.That(inspected.semantic,Is.EqualTo("swim"));
            Click("出海");yield return null;yield return null;Assert.That(inspected.gameObject.activeSelf,Is.False);
            Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Ready));Assert.That(game.Session.Paused,Is.True);
            Click("继续探索");game.Cast(.5f);yield return null;yield return null;
            Assert.That(UnityEngine.Object.FindFirstObjectByType<FishVisual>().semantic,Is.EqualTo("burst"));
            Assert.That(new SaveStore(saveFile,game.Content).Load().data.catches.Count,Is.EqualTo(4));
            Click("任务");yield return null;yield return null;Canvas.ForceUpdateCanvases();
            var rows=UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).Where(t=>t.name=="Task").ToArray();Assert.That(rows.Length,Is.EqualTo(18));
            foreach(var row in rows)
            {
                var labels=row.GetComponentsInChildren<Text>();Assert.That(labels.Length,Is.EqualTo(3));
                Assert.That(labels[1].text.Length,Is.GreaterThan(10));
                // Exercise the minimum two-column card width with actual bundled font metrics.
                row.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,282);Canvas.ForceUpdateCanvases();
                Assert.That(labels[1].preferredHeight,Is.LessThanOrEqualTo(labels[1].rectTransform.rect.height+.5f),labels[1].text);
            }
            var five=rows.Single(r=>r.GetComponentsInChildren<Text>()[0].text.EndsWith("海湾新朋友"));
            Assert.That(five.GetComponentsInChildren<Text>()[2].text,Is.EqualTo("进度  4 / 5"));

            // Actual journal cards select all species without recording catches.
            // The same detail tree must reflow in a narrow card and reach its last
            // paragraph by scrolling, independently of the current screen profile.
            int beforeJournalPoints=game.Progress.Data.totalPoints,beforeJournalCount=game.Progress.Data.catches.Count;
            var journalChecks=new System.Collections.Generic.List<string>();
            Click("图鉴");yield return null;yield return null;
            foreach(var species in game.Content.fish)
            {
                if(ui.Compact&&ui.CompactViewer){Click("返回图鉴");yield return null;yield return null;}
                bool known=game.Progress.Data.catches.Any(c=>c.fishId==species.id);
                Click((known?"":"未发现 · ")+species.name);yield return null;yield return null;
                Assert.That(ui.FocusedFish,Is.EqualTo(species.id));Assert.That(game.Session.Paused&&!game.Reeling,Is.True);
                var detail=UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Single(s=>s.content.name=="SpeciesContent");
                foreach(float width in new[]{240f,310f,500f})
                {
                    var panel=detail.GetComponent<RectTransform>();panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width);panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,230);
                    Canvas.ForceUpdateCanvases();yield return null;Canvas.ForceUpdateCanvases();
                    var labels=detail.content.GetComponentsInChildren<Text>();
                    Assert.That(labels.Single(t=>t.name=="ScientificName").text,Is.EqualTo(species.scientificName));
                    Assert.That(labels.Single(t=>t.name=="Recognition").text,Does.Contain(species.note));
                    Assert.That(labels.Single(t=>t.name=="NaturalHabitat").text,Does.Contain(species.naturalHabitat));
                    Assert.That(labels.Single(t=>t.name=="CatchSummary").text.Contains("尚未记录"),Is.EqualTo(!known));
                    foreach(var text in labels)
                    {
                        Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+.01f),species.id+"/"+width+"/"+text.name);
                        var generator=new TextGenerator();Assert.That(generator.Populate(text.text,text.GetGenerationSettings(text.rectTransform.rect.size)),Is.True);
                        Assert.That(generator.characterCountVisible,Is.GreaterThanOrEqualTo(text.text.Length),species.id+"/"+width+"/"+text.name);
                    }
                    Assert.That(detail.content.rect.height,Is.GreaterThan(detail.viewport.rect.height));
                    detail.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();yield return null;
                    var last=labels.Single(t=>t.name=="ExplorationNote");var corners=new Vector3[4];last.rectTransform.GetWorldCorners(corners);
                    float lower=detail.viewport.InverseTransformPoint(corners[0]).y,upper=detail.viewport.InverseTransformPoint(corners[1]).y;
                    Assert.That(lower,Is.GreaterThanOrEqualTo(detail.viewport.rect.yMin-.1f));Assert.That(upper,Is.LessThanOrEqualTo(detail.viewport.rect.yMax+.1f));
                    journalChecks.Add(species.id+","+width+",PASS");
                }
            }
            Click("旋转");Click("放大");Click("静止");yield return null;yield return null;
            Assert.That(ui.ModelYaw,Is.Not.Zero);Assert.That(ui.ModelZoom,Is.GreaterThan(1));Assert.That(ui.ModelPaused,Is.True);
            Click("重置");yield return null;yield return null;
            Assert.That(ui.ModelYaw,Is.Zero);Assert.That(ui.ModelZoom,Is.EqualTo(1));Assert.That(ui.ModelPaused,Is.False);
            Assert.That(game.Progress.Data.totalPoints,Is.EqualTo(beforeJournalPoints));Assert.That(game.Progress.Data.catches.Count,Is.EqualTo(beforeJournalCount));
            File.WriteAllLines(Path.Combine(save,"journal-layout.csv"),journalChecks);

            Click("设置");yield return null;yield return null;
            var effects=game.GetComponent<AudioSource>();var ambient=game.transform.Find("OceanAmbience").GetComponent<AudioSource>();var music=game.transform.Find("ExploreMusic").GetComponent<AudioSource>();
            Assert.That(ambient.clip.name,Is.EqualTo("OceanBed"));Assert.That(music.clip.name,Is.EqualTo("ExploreTheme"));Assert.That(ambient.loop&&music.loop,Is.True);
            Click("探索音乐    已关闭");yield return null;yield return null;
            Assert.That(new SaveStore(saveFile,game.Content).Load().data.settings.music,Is.True);Assert.That(music.volume,Is.Zero);
            Click("出海");yield return null;yield return null;Click("继续探索");yield return null;yield return null;
            Assert.That(ambient.volume,Is.GreaterThan(0));Assert.That(music.volume,Is.GreaterThan(0));
            game.Configure(s=>s.sound=false);yield return null;yield return null;
            Assert.That(effects.volume,Is.Zero);Assert.That(ambient.volume,Is.Zero);Assert.That(music.volume,Is.GreaterThan(0));
            game.gameObject.SendMessage("OnApplicationPause",true,SendMessageOptions.RequireReceiver);yield return null;
            Assert.That(game.Session.Paused,Is.True);Assert.That(ambient.volume+music.volume+effects.volume,Is.Zero);
            game.gameObject.SendMessage("OnApplicationPause",false,SendMessageOptions.RequireReceiver);yield return null;
            Assert.That(game.Session.Paused,Is.True);Assert.That(music.volume,Is.Zero);
            game.Configure(s=>{s.sound=true;s.music=false;});game.Pause(false);yield return null;yield return null;
            Assert.That(ambient.volume,Is.GreaterThan(0));Assert.That(music.volume,Is.Zero);
            foreach(string name in new[]{"Bite","Discovery","Retry"})
            {
                var cue=Resources.FindObjectsOfTypeAll<AudioClip>().Single(c=>c.name==name);var samples=new float[cue.samples];Assert.That(cue.GetData(samples,0),Is.True);
                Assert.That(samples.Max(Mathf.Abs),Is.GreaterThan(.3f));
                for(int i=1;i<samples.Length;i++)Assert.That(Mathf.Abs(samples[i]-samples[i-1]),Is.LessThan(.15f),name+" continuous cue waveform");
            }
        }
    }
}
