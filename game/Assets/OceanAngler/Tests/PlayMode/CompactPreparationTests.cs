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
    public sealed class CompactPreparationTests
    {
        [Serializable] sealed class Hit { public bool leftHanded,topTargetMatches;public Vector2 point,screen; }
        [Serializable] sealed class Receipt { public string status="PASS",scope="headless compact UI raycasts; no device rendering";public List<Hit> hands=new List<Hit>(); }
        Scene scene;GameController game;string root,archive;
        readonly Dictionary<string,string> originals=new Dictionary<string,string>();
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(game!=null)game.Pause(true);
            if(scene.IsValid()&&scene.isLoaded){var empty=SceneManager.CreateScene("AfterCompactPreparation");SceneManager.SetActiveScene(empty);yield return SceneManager.UnloadSceneAsync(scene);}
            if(root!=null)foreach(string suffix in new[]{"",".bak",".tmp"})
            {
                string file=Path.Combine(root,"ocean-progress.json")+suffix;
                if(File.Exists(file))File.Move(file,Path.Combine(archive,"after"+suffix+".json"));
            }
            foreach(var pair in originals)File.Move(pair.Value,pair.Key);
        }
        IEnumerator Tap(string text)
        {
            yield return UnityEngine.Object.FindFirstObjectByType<RuntimeSmoke>().ResolveControlForReview(text,false);
            if(UnityEngine.Object.FindFirstObjectByType<GameUI>().Playing&&new[]{"出海","图鉴","钓具","任务","设置","海域与装备"}.Contains(text))yield return Tap("暂停");
            var b=UnityEngine.Object.FindFirstObjectByType<RuntimeSmoke>().FindForReview(text);
            var scroll=b.GetComponentInParent<ScrollRect>();Canvas.ForceUpdateCanvases();
            if(scroll!=null&&scroll.content.rect.height>scroll.viewport.rect.height)
            {
                var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.content,b.GetComponent<RectTransform>());
                float range=scroll.content.rect.height-scroll.viewport.rect.height,offset=Mathf.Clamp(scroll.content.rect.yMax-bounds.center.y-scroll.viewport.rect.height/2,0,range);
                scroll.verticalNormalizedPosition=1-offset/range;
            }
            yield return null;yield return null;Canvas.ForceUpdateCanvases();
            var rect=b.GetComponent<RectTransform>();var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Assert.That(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,Is.True,"Top UI hit must be "+text);
            if(text=="看看海湾")
            {
                evidence.hands.Add(new Hit{leftHanded=game.Progress.Data.settings.leftHanded,point=point,screen=new Vector2(Screen.width,Screen.height),topTargetMatches=true});
                File.WriteAllText(Path.Combine(archive,"hit-receipt.json"),JsonUtility.ToJson(evidence,true));
            }
            ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerClickHandler);
            yield return null;yield return null;
        }
        readonly Receipt evidence=new Receipt();
        [UnityTest] public IEnumerator PausedInspectionRemainsTappableForEitherHand()
        {
            var args=LaunchArguments.Get();int index=Array.IndexOf(args,"--ocean-save-root");Assert.That(index,Is.GreaterThanOrEqualTo(0));Assert.That(index+1,Is.LessThan(args.Length));
            root=Path.GetFullPath(args[index+1]);Assert.That(root,Does.Contain("unity-test-saves"));archive=Path.Combine(root,"compact-preparation-before-after");Assert.That(Directory.Exists(archive),Is.False);Directory.CreateDirectory(archive);
            foreach(string suffix in new[]{"",".bak",".tmp"})
            {
                string file=Path.Combine(root,"ocean-progress.json")+suffix;
                if(File.Exists(file)){string saved=Path.Combine(archive,"before"+suffix+".json");File.Move(file,saved);originals.Add(file,saved);}
            }
            yield return SceneManager.LoadSceneAsync("Bay",LoadSceneMode.Single);scene=SceneManager.GetSceneByName("Bay");yield return null;yield return null;
            game=UnityEngine.Object.FindFirstObjectByType<GameController>();var ui=game.GetComponent<GameUI>();Assert.That(ui.Compact,Is.True,"Run this regression with a compact viewport");
            foreach(bool left in new[]{false,true})
            {
                yield return Tap("设置");if(game.Progress.Data.settings.leftHanded!=left)yield return Tap("左手操作");
                yield return Tap("钓具");yield return Tap("出海");Assert.That(game.Session.Paused,Is.True);
                yield return Tap("海域与装备");yield return Tap("看看海湾");Assert.That(ui.InspectingHabitat,Is.True);Assert.That(game.Session.Paused,Is.True);
                yield return Tap("返回钓鱼");
            }
            Assert.That(evidence.hands.Count,Is.EqualTo(2));Assert.That(evidence.hands.Select(h=>h.leftHanded),Is.EquivalentTo(new[]{false,true}));
        }
    }
}
