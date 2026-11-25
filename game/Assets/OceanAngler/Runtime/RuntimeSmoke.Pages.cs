using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace OceanAngler
{
    public sealed partial class RuntimeSmoke
    {
        IEnumerator PageTap(string label,string expected)
        {
            // Dedicated page review requires the actual visible local route;
            // it must not use the compatibility destination resolver.
            JourneyRequire(FindObjectsByType<Button>(FindObjectsSortMode.None).Count(b=>b.isActiveAndEnabled&&b.interactable&&b.GetComponentInChildren<Text>()?.text==label)==1,"page-local-control-"+label);
            yield return JourneyClick(label,true);yield return null;yield return null;
            JourneyRequire(GetComponent<GameUI>().PageName==expected,"page-destination-"+label+"-"+expected);
            JourneyRequire(game.Session.Paused&&!game.Reeling,"page-navigation-stays-paused-"+expected);
        }
        IEnumerator PageFrame(string name,bool model=false)
        {
            var rects=FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
            JourneyRequire(!rects.Any(t=>t.name=="Navigation"||t.name=="Footer"),"page-no-repeated-global-chrome-"+name);
            if(model)JourneyRequire(!rects.Any(t=>new[]{"Details","FishDetails","GearDetails","HabitatDetails","ObservationDetails"}.Contains(t.name)),"page-no-reading-sidebar-"+name);
            var ui=GetComponent<GameUI>();
            if(ui.FocusedFish!=null||ui.InspectingGear||ui.InspectingObservation)
            {
                var habitat=FindFirstObjectByType<MarineHabitat>();
                JourneyRequire(habitat.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r!=habitat.waterSurface&&r.bounds.max.y>habitat.SeaLevel+.02f).All(r=>r.forceRenderingOff),"page-appreciation-surface-scenery-hidden-"+name);
            }
            yield return JourneyFrame("pages-"+name);
        }
        IEnumerator ReviewDedicatedPages()
        {
            string before=JsonUtility.ToJson(game.Progress.Data);
            yield return PageFrame("home");
            yield return PageTap("海域与装备","准备");yield return PageFrame("preparation");
            yield return PageTap("更换钓具","钓具");yield return PageFrame("gear");
            yield return PageTap("观察入门钓组","钓组预览");yield return PageFrame("whole-rig",true);
            yield return PageTap("近看卷线器","钓组预览");yield return PageTap("试摇卷线器","钓组预览");yield return new WaitForSecondsRealtime(.4f);yield return PageFrame("reel",true);
            yield return PageTap("上一级","钓具");yield return PageTap("上一级","准备");
            yield return PageTap(game.Progress.Data.habitatId=="bay"?"看看海湾":"看看海域","海湾观察");yield return PageFrame("habitat-surface",true);
            if(!GetComponent<GameUI>().HabitatSurfaceView)yield return PageTap("回到水面","海湾观察");
            yield return PageTap("潜入水下","海湾观察");yield return PageFrame("habitat-underwater",true);yield return PageTap("回到水面","海湾观察");
            yield return PageTap("上一级","准备");yield return PageTap("上一级","首页");
            yield return PageTap("设置","设置");yield return PageFrame("settings");
            foreach(string group in new[]{"操作与辅助","声音与触觉","画面与动态"})
            {
                yield return PageTap(group,group);yield return PageFrame(group=="操作与辅助"?"controls":group=="声音与触觉"?"sound":"visual");yield return PageTap("上一级","设置");
            }
            yield return PageTap("家长区与隐私","家长验证");yield return PageFrame("parent-gate");
            string question=FindObjectsByType<Text>(FindObjectsSortMode.None).Single(t=>t.text.StartsWith("9 + ")).text;
            int answer=9+int.Parse(question.Split(' ')[2]);yield return PageTap(answer.ToString(),"家长区");yield return PageFrame("parents");
            yield return PageTap("查看隐私与字体许可","隐私许可");yield return PageFrame("privacy");yield return PageTap("上一级","家长区");yield return PageTap("上一级","设置");yield return PageTap("上一级","首页");
            yield return PageTap("任务","任务");yield return PageFrame("tasks");yield return PageTap("上一级","首页");
            yield return PageTap("图鉴","图鉴");yield return PageFrame("journal");
            var fish=game.Content.Fish("trevally");bool known=game.Progress.Data.catches.Any(c=>c.fishId==fish.id);
            yield return PageTap((known?"":"未发现 · ")+fish.name,"鱼类详情");yield return PageFrame("fish",true);
            yield return PageTap("认识它","鱼类资料");yield return PageFrame("fish-reading");yield return PageTap("上一级","鱼类详情");yield return PageTap("上一级","图鉴");
            string entry=FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.GetComponentInChildren<Text>()?.text.StartsWith("保护观察  ")==true).GetComponentInChildren<Text>().text;
            yield return PageTap(entry,"保护观察");yield return PageFrame("observations");
            var species=game.Content.observations.First(o=>o.habitatId==game.Progress.Data.habitatId);
            yield return PageTap("观察"+species.name,"观察生物");yield return PageFrame("observation",true);
            yield return PageTap("认识它","观察资料");yield return PageFrame("observation-reading");yield return PageTap("上一级","观察生物");yield return PageTap("上一级","保护观察");yield return PageTap("上一级","图鉴");yield return PageTap("上一级","首页");
            yield return PageTap("出海","出海");yield return PageFrame("pause");
            yield return PageTap("结束本次 · 返回准备","退出确认");yield return PageFrame("exit");yield return PageTap("取消，继续留在这里","出海");
            JourneyRequire(JsonUtility.ToJson(game.Progress.Data)==before,"page-review-preserves-progress-and-settings");
        }
    }
}
