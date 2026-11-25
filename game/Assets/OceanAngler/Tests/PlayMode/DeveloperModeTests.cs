using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace OceanAngler.Tests
{
    public sealed class DeveloperModeTests
    {
        [Serializable] sealed class Result { public string status; public string[] failures; }
        Scene scene;
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(scene.IsValid()&&scene.isLoaded){var empty=SceneManager.CreateScene("AfterDeveloperMode");SceneManager.SetActiveScene(empty);yield return SceneManager.UnloadSceneAsync(scene);}
        }
        [UnityTest,Timeout(240000)] public IEnumerator ExplicitUiSwitchesUnlockWithoutPointsAndRestartWithIsolatedProgress()
        {
            var args=LaunchArguments.Get();int index=Array.IndexOf(args,"--ocean-save-root");Assert.That(index,Is.GreaterThanOrEqualTo(0));string root=Path.GetFullPath(args[index+1]);Assert.That(root,Does.Contain("unity-test-saves"));
            Assert.That(File.Exists(Path.Combine(root,"ocean-progress.json")),Is.False,"Requires a fresh isolated test root");
            foreach(string phase in new[]{"enable","disable","normal-restart"})
            {
                yield return SceneManager.LoadSceneAsync("Bay",LoadSceneMode.Single);scene=SceneManager.GetSceneByName("Bay");yield return null;yield return null;
                var game=UnityEngine.Object.FindFirstObjectByType<GameController>();string output=Path.Combine(root,"developer-"+phase);
                yield return game.GetComponent<RuntimeSmoke>().RunDeveloperReview(output,phase,false);
                var result=JsonUtility.FromJson<Result>(File.ReadAllText(Path.Combine(output,"receipt.json")));
                Assert.That(result.status,Is.EqualTo("PASS"),phase+": "+string.Join("\n",result.failures));
            }
        }
    }
}
