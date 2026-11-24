using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace OceanAngler.Tests
{
    public sealed class ContinuousJourneyTests
    {
        [Serializable] sealed class Round { public int number;public string habitatId,mode; }
        [Serializable] sealed class Summary { public string status,scope;public string[] failures;public int completedRounds,catches,capturedFrames;public bool captureEnabled;public Round[] rounds; }
        Scene scene;GameController game;string root,archive;
        readonly Dictionary<string,string> originals=new Dictionary<string,string>();
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(game!=null)game.Pause(true);
            if(scene.IsValid()&&scene.isLoaded)
            {
                var empty=SceneManager.CreateScene("AfterContinuousJourney");SceneManager.SetActiveScene(empty);yield return SceneManager.UnloadSceneAsync(scene);
            }
            if(root!=null)foreach(string suffix in new[]{"",".bak",".tmp"})
            {
                string file=Path.Combine(root,"ocean-progress.json")+suffix;
                if(File.Exists(file))File.Move(file,Path.Combine(archive,"after"+suffix+".json"));
            }
            foreach(var pair in originals)File.Move(pair.Value,pair.Key);
        }
        [UnityTest,Timeout(1800000)] public IEnumerator TwentyUiCatchesPersistAcrossNaturalUnlocksAndSceneRestart()
        {
            var args=LaunchArguments.Get();int index=Array.IndexOf(args,"--ocean-save-root");
            Assert.That(index,Is.GreaterThanOrEqualTo(0));Assert.That(index+1,Is.LessThan(args.Length));root=Path.GetFullPath(args[index+1]);Assert.That(root,Does.Contain("unity-test-saves"));
            archive=Path.Combine(root,"continuous-journey-before-after");Assert.That(Directory.Exists(archive),Is.False);Directory.CreateDirectory(archive);
            foreach(string suffix in new[]{"",".bak",".tmp"})
            {
                string file=Path.Combine(root,"ocean-progress.json")+suffix;
                if(File.Exists(file)){string saved=Path.Combine(archive,"before"+suffix+".json");File.Move(file,saved);originals.Add(file,saved);}
            }
            yield return SceneManager.LoadSceneAsync("Bay",LoadSceneMode.Single);scene=SceneManager.GetSceneByName("Bay");yield return null;yield return null;
            game=UnityEngine.Object.FindFirstObjectByType<GameController>();
            string output=Path.Combine(root,"continuous-journey");
            yield return game.GetComponent<RuntimeSmoke>().RunJourney20(output,false);
            var result=JsonUtility.FromJson<Summary>(File.ReadAllText(Path.Combine(output,"receipt.json")));
            Assert.That(result.status,Is.EqualTo("PASS"),string.Join("\n",result.failures));Assert.That(result.failures,Is.Empty);
            Assert.That(result.scope,Does.StartWith("headless PlayMode"));Assert.That(result.captureEnabled,Is.False);Assert.That(result.capturedFrames,Is.Zero);
            Assert.That(result.completedRounds,Is.EqualTo(20));Assert.That(result.catches,Is.EqualTo(20));CollectionAssert.AreEqual(Enumerable.Range(1,20),result.rounds.Select(r=>r.number));
            Assert.That(result.rounds.Any(r=>r.mode=="assisted")&&result.rounds.Any(r=>r.mode=="standard"),Is.True);
            var data=game.Progress.Data;int points=data.totalPoints;string[] ids=data.catches.Select(c=>c.catchId).ToArray();
            Assert.That(ids.Distinct().Count(),Is.EqualTo(20));Assert.That(data.observations.Count,Is.EqualTo(3));
            // Recreate the actual scene/controller. A SaveStore.Load alone does
            // not prove the startup path restores the player's progress.
            yield return SceneManager.LoadSceneAsync("Bay",LoadSceneMode.Single);scene=SceneManager.GetSceneByName("Bay");yield return null;yield return null;
            game=UnityEngine.Object.FindFirstObjectByType<GameController>();Assert.That(game.Progress.RequiresRecovery,Is.False);
            Assert.That(game.Progress.Data.totalPoints,Is.EqualTo(points));CollectionAssert.AreEqual(ids,game.Progress.Data.catches.Select(c=>c.catchId));Assert.That(game.Progress.Data.observations.Count,Is.EqualTo(3));
            Assert.That(game.Session.Phase,Is.EqualTo(FishingPhase.Ready));Assert.That(game.Reeling,Is.False);
            File.WriteAllText(Path.Combine(output,"scene-restart.json"),JsonUtility.ToJson(new RestartReceipt{points=points,catches=20,observations=3},true));
        }
        [Serializable] sealed class RestartReceipt { public string status="PASS",scope="headless scene/controller reload, not OS process restart";public int points,catches,observations; }
    }
}
