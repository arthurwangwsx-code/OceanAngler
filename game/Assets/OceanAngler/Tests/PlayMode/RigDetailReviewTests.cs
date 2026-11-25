using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace OceanAngler.Tests
{
    public sealed class RigDetailReviewTests
    {
        [Serializable] sealed class Result {public string status;public string[] failures;public int catchesBefore,catchesAfter;public float rollerTravel;}
        Scene scene;GameController game;string root,archive;readonly Dictionary<string,string> originals=new Dictionary<string,string>();
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(game!=null)game.Pause(true);
            if(scene.IsValid()&&scene.isLoaded){var empty=SceneManager.CreateScene("AfterRigDetailReview");SceneManager.SetActiveScene(empty);yield return SceneManager.UnloadSceneAsync(scene);}
            if(root!=null)foreach(string suffix in new[]{"",".bak",".tmp"}){string file=Path.Combine(root,"ocean-progress.json")+suffix;if(File.Exists(file))File.Move(file,Path.Combine(archive,"after"+suffix+".json"));}
            foreach(var pair in originals)File.Move(pair.Value,pair.Key);originals.Clear();
        }
        [UnityTest,Timeout(240000)] public IEnumerator ExistingPreviewAndNormalTutorialUseTheCompleteRigWithoutSyntheticProgress()
        {
            var args=LaunchArguments.Get();int index=Array.IndexOf(args,"--ocean-save-root");Assert.That(index,Is.GreaterThanOrEqualTo(0));root=Path.GetFullPath(args[index+1]);Assert.That(root,Does.Contain("unity-test-saves"));
            archive=Path.Combine(root,"rig-detail-before-after");Assert.That(Directory.Exists(archive),Is.False);Directory.CreateDirectory(archive);
            foreach(string suffix in new[]{"",".bak",".tmp"}){string file=Path.Combine(root,"ocean-progress.json")+suffix;if(File.Exists(file)){string before=Path.Combine(archive,"before"+suffix+".json");File.Move(file,before);originals.Add(file,before);}}
            yield return SceneManager.LoadSceneAsync("Bay",LoadSceneMode.Single);scene=SceneManager.GetSceneByName("Bay");yield return null;yield return null;
            game=UnityEngine.Object.FindFirstObjectByType<GameController>();string output=Path.Combine(root,"rig-detail-review");yield return game.GetComponent<RuntimeSmoke>().RunRigDetailReview(output,false);
            var result=JsonUtility.FromJson<Result>(File.ReadAllText(Path.Combine(output,"rig-detail.json")));Assert.That(result.status,Is.EqualTo("PASS"),string.Join("\n",result.failures));Assert.That(result.catchesBefore,Is.Zero);Assert.That(result.catchesAfter,Is.EqualTo(1));Assert.That(result.rollerTravel,Is.GreaterThan(5));
        }
    }
}
