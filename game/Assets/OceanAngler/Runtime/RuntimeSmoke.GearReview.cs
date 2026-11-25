using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace OceanAngler
{
    public sealed partial class RuntimeSmoke
    {
        [Serializable] sealed class GearCheck { public string id,notice;public float scrollBefore,scrollAfter;public bool selectedVisible,topHitMatches; }
        bool medianFinReview;
        bool journalFramingReview;bool pageReview;
        public IEnumerator RunGearReview(string outputFolder,bool captureFrames,bool quitProcess=false,bool reviewMedianFins=false,bool reviewJournalFraming=false,bool reviewPages=false)
        {
            folder=Path.GetFullPath(outputFolder);Directory.CreateDirectory(folder);
            if(File.Exists(Path.Combine(folder,"receipt.json")))throw new InvalidOperationException("Use a new review folder");
            game=GetComponent<GameController>();began=Time.realtimeSinceStartup;finished=false;journeyMode=gearReviewMode=true;journeyQuit=quitProcess;completeContent=false;medianFinReview=reviewMedianFins;
            receipt=new Receipt{captureEnabled=captureFrames,scope=captureFrames?"native focused gear/model-status UI review from a genuine isolated save copy; not fresh progression or OS touch":"headless focused gear/model-status UI review; logical fixture, no rendered acceptance"};
            pageReview=reviewPages;journalFramingReview=reviewJournalFraming;journalFramingSamples.Clear();
            var stack=new Stack<IEnumerator>();stack.Push(GearReview());
            while(stack.Count>0&&!finished)
            {
                object next=null;bool moved=false;Exception failure=null;
                try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){receipt.failures.Add("review-exception:"+failure.GetType().Name+":"+failure.Message);break;}
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(next is IEnumerator nested)stack.Push(nested);else yield return next;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
            if(!finished)FinishJourney();
        }
        IEnumerator GearReview()
        {
            var args=LaunchArguments.Get();int index=Array.IndexOf(args,"--ocean-save-root");
            JourneyRequire(index>=0&&index+1<args.Length,"gear-explicit-isolated-save-root");
            string saveFile=Path.Combine(Path.GetFullPath(args[index+1]),"ocean-progress.json");
            JourneyRequire(!game.Progress.RequiresRecovery&&game.Session.Phase==FishingPhase.Ready&&game.Content.gear.All(g=>g.unlockPoints<=game.Progress.Data.totalPoints),"gear-valid-save-with-all-equipment-unlocked");
            var before=game.Progress.Data.Clone();
            if(pageReview){yield return null;yield return null;yield return ReviewDedicatedPages();}
            yield return null;yield return null;yield return JourneyClick("钓具",true);yield return null;yield return null;
            receipt.gearEntryHintPresent=FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="选择已解锁的钓具，研究积分不会扣除。");
            string[] categories={"rod","reel","line","lure"},labels={"鱼竿","卷线器","鱼线","拟饵"};
            for(int category=0;category<categories.Length;category++)
            {
                yield return JourneyClick(labels[category],true);yield return null;yield return null;
                foreach(var item in game.Content.gear.Where(g=>g.category==categories[category]).Reverse())
                {
                    var button=Find(item.name);yield return null;yield return null;Canvas.ForceUpdateCanvases();
                    var scroll=button.GetComponentInParent<ScrollRect>();float position=scroll!=null?scroll.verticalNormalizedPosition:1;
                    Click(item.name);yield return null;yield return null;Canvas.ForceUpdateCanvases();
                    // Do not call Find here: it scrolls and would conceal the bug.
                    button=FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.isActiveAndEnabled&&b.GetComponentInChildren<Text>().text.StartsWith(item.name+"  已装备"));
                    scroll=button.GetComponentInParent<ScrollRect>();var rect=button.GetComponent<RectTransform>();var corners=new Vector3[4];rect.GetWorldCorners(corners);
                    var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
                    bool visible=scroll==null||RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport,corners[0])&&RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport,corners[2]);
                    bool top=hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button;
                    receipt.gearChecks.Add(new GearCheck{id=item.id,notice=game.Notice,scrollBefore=position,scrollAfter=scroll!=null?scroll.verticalNormalizedPosition:1,selectedVisible=visible,topHitMatches=top});
                    if(item==game.Content.gear.Last(g=>g.category==categories[category]))yield return JourneyFrame("gear-selected-"+categories[category]);
                    JourneyRequire(visible&&top&&Mathf.Abs(position-(scroll!=null?scroll.verticalNormalizedPosition:1))<.005f,"gear-selection-stays-visible-"+item.id);
                    JourneyRequire(game.Notice=="已装备"+item.name+"，研究积分未扣除。","gear-success-feedback-"+item.id);
                    JourneyRequire(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text==game.Notice),"gear-rendered-feedback-"+item.id);
                    receipt.equippedGear.Add(item.id);
                    var loaded=new SaveStore(saveFile,game.Content).Load();JourneyRequire(!loaded.requiresRecovery&&new[]{loaded.data.rodId,loaded.data.reelId,loaded.data.lineId,loaded.data.lureId}.Contains(item.id),"gear-selected-on-disk-"+item.id);
                }
            }
            JourneyRequire(receipt.gearEntryHintPresent,"gear-contextual-entry-hint");
            foreach(var fish in game.Content.fish)
            {
                yield return JourneyClick("图鉴",true);yield return null;yield return null;
                yield return JourneyClick(before.catches.Any(c=>c.fishId==fish.id)?fish.name:"未发现 · "+fish.name,true);yield return null;yield return null;
                var status=FindObjectsByType<Text>(FindObjectsSortMode.None).SingleOrDefault(t=>t.name=="ModelStatus");
                JourneyRequire(status!=null&&status.text=="开发版模型 · 还在制作","prototype-status-visible-"+fish.id);
                var corners=new Vector3[4];status.rectTransform.GetWorldCorners(corners);var safe=DisplayMetrics.SafeArea;
                JourneyRequire(!status.canvasRenderer.cull&&corners[0].x>=safe.xMin&&corners[0].y>=safe.yMin&&corners[2].x<=safe.xMax&&corners[2].y<=safe.yMax,"prototype-status-in-safe-area-"+fish.id);
                receipt.modelStatuses.Add(fish.id);
                if(fish.id=="trevally"||fish.id=="halibut")yield return JourneyFrame("prototype-"+fish.id);
                if(fish.id=="halibut"&&medianFinReview)
                {
                    JourneyRequire(Debug.isDebugBuild&&before.catches.Any(c=>c.fishId=="halibut")&&!before.settings.reducedMotion,"fin-review-development-build-and-discovered-moving-halibut");
                    yield return ReviewMedianFinMotion(saveFile);
                }
                if(journalFramingReview)yield return ReviewJournalFraming(fish.id,saveFile);
            }
            if(journalFramingReview)yield return ReviewJournalReturn();
            var after=game.Progress.Data;
            JourneyRequire(after.totalPoints==before.totalPoints&&after.catches.Select(JsonUtility.ToJson).SequenceEqual(before.catches.Select(JsonUtility.ToJson))&&after.observations.SequenceEqual(before.observations)&&after.habitatId==before.habitatId&&after.tutorialComplete==before.tutorialComplete&&JsonUtility.ToJson(after.settings)==JsonUtility.ToJson(before.settings),"gear-review-preserves-progress-and-settings");
            receipt.pixelsPerPoint=DisplayMetrics.PixelsPerPoint;receipt.safeWidthPoints=DisplayMetrics.SafeArea.width/DisplayMetrics.PixelsPerPoint;receipt.safeHeightPoints=DisplayMetrics.SafeArea.height/DisplayMetrics.PixelsPerPoint;
        }
    }
}
