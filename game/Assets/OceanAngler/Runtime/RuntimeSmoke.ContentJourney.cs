using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace OceanAngler
{
    public sealed partial class RuntimeSmoke
    {
        [Serializable] sealed class TaskCheckpoint { public int round,points,species,completedTasks; }
        [Serializable] sealed class TaskResult { public string id;public int value,target;public bool complete; }
        void RecordTaskCheckpoint(ProgressData data)
        {
            receipt.taskCheckpoints.Add(new TaskCheckpoint{round=receipt.completedRounds,points=data.totalPoints,species=data.catches.Select(c=>c.fishId).Distinct().Count(),completedTasks=game.Content.tasks.Count(t=>TaskProgress.Complete(t,data))});
        }
        IEnumerator ReviewCompleteContent()
        {
            var data=game.Progress.Data;int points=data.totalPoints,count=data.catches.Count;
            JourneyRequire(game.Content.tasks.All(t=>TaskProgress.Complete(t,data)),"content-all-eighteen-tasks-from-normal-catches");
            JourneyRequire(data.catches.Select(c=>c.fishId).Distinct().Count()==12,"content-all-twelve-random-species");
            JourneyRequire(game.Content.gear.All(g=>g.unlockPoints<=points),"content-all-twenty-gear-naturally-unlocked");
            // Inspect every gear item via its visible, raycast-checked button.
            yield return JourneyClick("钓具",true);yield return null;yield return null;
            string[] categories={"rod","reel","line","lure"},labels={"鱼竿","卷线器","鱼线","拟饵"};
            for(int i=0;i<categories.Length;i++)
            {
                yield return JourneyClick(labels[i],true);yield return null;yield return null;
                foreach(var gear in game.Content.gear.Where(g=>g.category==categories[i]))
                {
                    yield return JourneyClick(gear.name);yield return null;yield return null;
                    data=game.Progress.Data;
                    JourneyRequire(new[]{data.rodId,data.reelId,data.lineId,data.lureId}.Contains(gear.id),"content-equipped-"+gear.id);
                    JourneyRequire(data.totalPoints==points&&data.catches.Count==count,"content-equipping-preserves-ledger-"+gear.id);
                    receipt.equippedGear.Add(gear.id);
                }
                yield return JourneyFrame("content-gear-"+categories[i]);
            }
            foreach(var fish in game.Content.fish)
            {
                yield return JourneyClick("图鉴",true);yield return null;yield return null;
                JourneyRequire(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("12 / 12")),"content-journal-twelve-of-twelve");
                yield return JourneyClick(fish.name,true);yield return null;yield return null;
                var ui=GetComponent<GameUI>();
                JourneyRequire(ui.FocusedFish==fish.id,"content-journal-focused-"+fish.id);
                var texts=FindObjectsByType<Text>(FindObjectsSortMode.None);
                JourneyRequire(texts.Any(t=>t.name=="CatchSummary"&&t.text.StartsWith("我的记录")),"content-journal-record-"+fish.id);
                JourneyRequire(texts.Any(t=>t.name=="ScientificName"&&t.text==fish.scientificName),"content-journal-identity-"+fish.id);
                receipt.inspectedSpecies.Add(fish.id);yield return JourneyFrame("content-species-"+fish.id);
            }
            yield return JourneyClick("任务",true);yield return null;yield return null;
            JourneyRequire(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="探索手册  ·  18 / 18"),"content-task-page-eighteen-of-eighteen");
            yield return JourneyFrame("content-tasks-header");
            var scroll=FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Single(s=>s.name=="Tasks");
            var rows=scroll.content.GetComponentsInChildren<RectTransform>().Where(t=>t.name=="Task").OrderByDescending(t=>t.anchoredPosition.y).ThenBy(t=>t.anchoredPosition.x).ToArray();
            JourneyRequire(rows.Length==18,"content-task-page-has-eighteen-cards");
            float lastY=float.NaN;int image=0;
            foreach(var row in rows)
            {
                var labelsInRow=row.GetComponentsInChildren<Text>();
                var task=game.Content.tasks.Single(t=>labelsInRow.Any(l=>l.text=="已完成  "+t.name));
                int value=TaskProgress.Value(task,data);
                JourneyRequire(labelsInRow.Any(l=>l.text==TaskProgress.GoalText(task,game.Content))&&labelsInRow.Any(l=>l.text=="进度  "+task.target+" / "+task.target),"content-task-visible-labels-"+task.id);
                receipt.tasks.Add(new TaskResult{id=task.id,value=value,target=task.target,complete=value>=task.target});
                if(float.IsNaN(lastY)||Mathf.Abs(lastY-row.anchoredPosition.y)>.1f)
                {
                    Canvas.ForceUpdateCanvases();var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.content,row);
                    float max=scroll.content.rect.height-scroll.viewport.rect.height;
                    if(max>0)scroll.verticalNormalizedPosition=1-Mathf.Clamp(scroll.content.rect.yMax-bounds.center.y-scroll.viewport.rect.height/2,0,max)/max;
                    yield return null;yield return null;
                    var corners=new Vector3[4];row.GetWorldCorners(corners);
                    JourneyRequire(RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport,corners[0])&&RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport,corners[2]),"content-task-card-in-viewport-"+task.id);
                    yield return JourneyFrame("content-tasks-row-"+(++image).ToString("D2"));lastY=row.anchoredPosition.y;
                }
            }
            JourneyRequire(data.totalPoints==points&&data.catches.Count==count,"content-inspections-preserve-catch-points");
            WriteJourney();
        }
    }
}
