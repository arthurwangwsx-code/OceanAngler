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
    public sealed class JournalFramingTests
    {
        [Serializable] sealed class Result {public string status="RUNNING",scope="Headless actual UI and deformed geometry, controlled animation samples; not native/OS touch or final art";public List<RuntimeSmoke.JournalFrameSample> samples=new List<RuntimeSmoke.JournalFrameSample>();}
        Scene scene;GameController game;string root,archive;readonly Dictionary<string,string> originals=new Dictionary<string,string>();
        IEnumerator Tap(string label)
        {
            yield return UnityEngine.Object.FindFirstObjectByType<RuntimeSmoke>().ResolveControlForReview(label,true);
            var b=UnityEngine.Object.FindFirstObjectByType<RuntimeSmoke>().FindForReview(label,true);
            ExecuteEvents.Execute(b.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);yield return null;yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(game!=null)game.Pause(true);
            if(scene.IsValid()&&scene.isLoaded){var empty=SceneManager.CreateScene("AfterJournalFraming");SceneManager.SetActiveScene(empty);yield return SceneManager.UnloadSceneAsync(scene);}
            if(root!=null)foreach(string suffix in new[]{"",".bak",".tmp"}){string file=Path.Combine(root,"ocean-progress.json")+suffix;if(File.Exists(file))File.Move(file,Path.Combine(archive,"after"+suffix+".json"));}
            foreach(var entry in originals)File.Move(entry.Value,entry.Key);originals.Clear();
        }
        [UnityTest,Timeout(180000)] public IEnumerator WholeFishStaysBetweenActualControlsThroughZoomAndFullRotation()
        {
            var args=LaunchArguments.Get();int index=Array.IndexOf(args,"--ocean-save-root");Assert.That(index,Is.GreaterThanOrEqualTo(0));root=Path.GetFullPath(args[index+1]);Assert.That(root,Does.Contain("unity-test-saves"));archive=Path.Combine(root,"journal-framing-before-after");Directory.CreateDirectory(archive);
            foreach(string suffix in new[]{"",".bak",".tmp"}){string file=Path.Combine(root,"ocean-progress.json")+suffix;if(File.Exists(file)){string copy=Path.Combine(archive,"before"+suffix+".json");File.Move(file,copy);originals[file]=copy;}}
            yield return SceneManager.LoadSceneAsync("Bay",LoadSceneMode.Single);scene=SceneManager.GetSceneByName("Bay");yield return null;yield return null;game=UnityEngine.Object.FindFirstObjectByType<GameController>();game.Configure(_=>{});yield return null;yield return null;
            var ui=game.GetComponent<GameUI>();var world=game.GetComponent<OceanWorld>();byte[] saved=File.ReadAllBytes(Path.Combine(root,"ocean-progress.json"));var result=new Result();
            try
            {
                foreach(var definition in game.Content.fish.OrderBy(f=>f.id=="halibut"?0:1))
                {
                    yield return Tap("图鉴");yield return Tap("未发现 · "+definition.name);yield return Tap("重置");yield return Tap("静止");var visual=world.TargetFish;var animator=visual.GetComponentInChildren<Animator>();if(animator!=null)animator.enabled=false;
                    try
                    {
                        foreach(float zoom in new[]{.7f,1f,1.5f})
                        {
                            while(ui.ModelZoom>zoom+.01f)yield return Tap("缩小");while(ui.ModelZoom<zoom-.01f)yield return Tap("放大");
                            for(int turn=0;turn<12;turn++)
                            {
                                int stableCount=0;Vector3 stablePosition=default,stableScale=default;bool firstPose=true;
                                // Off-grid phases are independent of the65
                                // samples used to author the saved envelope.
                                foreach(float phase in new[]{.007f,.083f,.173f,.309f,.431f,.557f,.719f,.883f})
                                {
                                    if(animator!=null){var clip=animator.runtimeAnimatorController.animationClips.Single(c=>c.name=="swim");clip.SampleAnimation(animator.gameObject,clip.length*phase);}
                                    else{var prototype=(FishModel)visual;typeof(FishModel).GetField("animationTime",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(prototype,phase*1.8f);prototype.SendMessage("Update");}
                                    // UnityTest resumes before LateUpdate in
                                    // no-graphics runs. Complete the production
                                    // presentation phase before inspecting it.
                                    yield return null;world.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);var sample=RuntimeSmoke.ProbeJournalFrame(ui,visual,Camera.main);result.samples.Add(sample);
                                    Assert.That(sample.vertices,Is.GreaterThan(100));Assert.That(sample.behindCamera,Is.Zero);Assert.That(sample.outside,Is.Zero,definition.id+" zoom="+zoom+" yaw="+ui.ModelYaw+" region="+sample.available+" actual="+sample.projected);
                                    if(firstPose){stableCount=world.JournalFitCalculations;stablePosition=visual.transform.position;stableScale=visual.transform.localScale;firstPose=false;}
                                    else{Assert.That(world.JournalFitCalculations,Is.EqualTo(stableCount),"Reuse fitting while animation changes");Assert.That(Vector3.Distance(visual.transform.position,stablePosition),Is.LessThan(.00001f));Assert.That(Vector3.Distance(visual.transform.localScale,stableScale),Is.LessThan(.00001f),"No animation-driven zoom pumping");}
                                }
                                yield return Tap("旋转");
                            }
                        }
                    }
                    finally{if(animator!=null)animator.enabled=true;}
                }
                foreach(string id in result.samples.Select(s=>s.fishId).Distinct())
                {
                    var samples=result.samples.Where(s=>s.fishId==id).ToArray();float Width(float zoom)=>samples.First(s=>Mathf.Abs(s.zoom-zoom)<.01f&&Mathf.Repeat(s.yaw,360)<.01f).projected.width;
                    Assert.That(Width(1.5f),Is.GreaterThan(Width(1)*1.3f),id+" zoom must actually enlarge the model");Assert.That(Width(.7f),Is.LessThan(Width(1)*.8f));
                    var front=samples.Where(s=>Mathf.Abs(s.zoom-1.5f)<.01f&&Mathf.Repeat(s.yaw,360)<.01f).ToArray();
                    Assert.That(front.Min(s=>Mathf.Max(s.projected.width/s.available.width,s.projected.height/s.available.height)),Is.GreaterThanOrEqualTo(.75f),id+" maximum front view must use at least75% of an available axis");
                }
                yield return Tap("图鉴");yield return Tap("未发现 · "+game.Content.Fish("trevally").name);yield return Tap("重置");yield return Tap("缩小");yield return Tap("缩小");
                var live=world.TargetFish.GetComponentInChildren<Animator>();var priorCulling=live.cullingMode;var skins=world.TargetFish.GetComponentsInChildren<SkinnedMeshRenderer>();var priorOffscreen=skins.Select(r=>r.updateWhenOffscreen).ToArray();
                // Explicit no-graphics live-Animator fixture. Native runs keep
                // production culling and observe the real rendered frame end.
                live.cullingMode=AnimatorCullingMode.AlwaysAnimate;foreach(var skin in skins)skin.updateWhenOffscreen=true;
                try
                {
                    yield return null;yield return null;world.SendMessage("LateUpdate");var stability=RuntimeSmoke.BeginJournalStability(world);yield return new WaitForSecondsRealtime(.16f);world.SendMessage("LateUpdate");RuntimeSmoke.EndJournalStability(world,stability);
                    File.WriteAllText(Path.Combine(root,"journal-live-stability.json"),JsonUtility.ToJson(stability,true));Assert.That(stability.countAfter,Is.EqualTo(stability.countBefore),"Live Animator must reuse framing too");Assert.That(Vector3.Distance(stability.positionAfter,stability.positionBefore),Is.LessThan(.00001f));Assert.That(Vector3.Distance(stability.scaleAfter,stability.scaleBefore),Is.LessThan(.00001f));
                }
                finally{live.cullingMode=priorCulling;for(int i=0;i<skins.Length;i++)skins[i].updateWhenOffscreen=priorOffscreen[i];}
                var rotate=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.isActiveAndEnabled&&b.GetComponentInChildren<Text>()?.text=="旋转");
                // Match a native UI callback after World.Update: this same
                // rendered frame must consume the new angle in LateUpdate.
                ExecuteEvents.Execute(rotate.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);world.SendMessage("LateUpdate");
                Assert.That(Quaternion.Angle(world.CurrentJournalFit.rotation,Quaternion.Euler(0,-25+ui.ModelYaw,-3)),Is.LessThan(.001f),"Use this frame's newly clicked yaw, not the prior Update pose");
                CollectionAssert.AreEqual(saved,File.ReadAllBytes(Path.Combine(root,"ocean-progress.json")));result.status="PASS";
            }
            finally{if(result.status=="RUNNING")result.status="FAIL";File.WriteAllText(Path.Combine(root,"journal-framing.json"),JsonUtility.ToJson(result,true));}
        }
    }
}
