using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace OceanAngler
{
    public sealed partial class RuntimeSmoke
    {
        [Serializable] sealed class ContourView
        {
            public string habitat,stage;public int waterOrder,waterQueue;public float cameraY,seaLevel;public JournalFrameSample model;
        }
        [Serializable] sealed class ContourResult
        {
            public string status="RUNNING",scope="Explicit QA flag in developer review; actual visible controls, camera, original materials and production culling. Not OS touch, science, physical performance or final art qualification.";
            public List<ContourView> views=new List<ContourView>();public bool zeroPointsPreserved;
        }
        IEnumerator ReviewFinContours()
        {
            var ui=GetComponent<GameUI>();var world=GetComponent<OceanWorld>();var result=new ContourResult();
            JourneyRequire(game.DeveloperMode&&game.Progress.Data.totalPoints==0&&game.Progress.Data.catches.Count==0,"contour-explicit-zero-point-developer-session");
            foreach(var habitat in game.Content.habitats)
            {
                yield return JourneyClick("海域与装备",true);yield return null;yield return null;yield return PageTap(habitat.name,"准备");
                yield return PageTap(habitat.id=="bay"?"看看海湾":"看看海域","海湾观察");
                if(!ui.HabitatSurfaceView)yield return PageTap("回到水面","海湾观察");
                foreach(bool above in new[]{true,false})
                {
                    if(!above)yield return PageTap("潜入水下","海湾观察");
                    var active=world.ActiveHabitat;var camera=Camera.main;var sample=new ContourView{habitat=habitat.id,stage=above?"surface":"underwater",waterOrder=active.waterSurface.sortingOrder,waterQueue=active.waterSurface.sharedMaterial.renderQueue,cameraY=camera.transform.position.y,seaLevel=active.SeaLevel};result.views.Add(sample);
                    JourneyRequire((above?sample.cameraY>sample.seaLevel:sample.cameraY<sample.seaLevel)&&(above?sample.waterOrder==0:sample.waterOrder<0),"contour-actual-camera-water-order-"+habitat.id+"-"+sample.stage);
                    yield return PageFrame("contour-"+habitat.id+"-"+sample.stage,true);
                }
                yield return PageTap("上一级","准备");yield return JourneyClick("图鉴",true);yield return null;yield return null;
                yield return PageTap("未发现 · 蓝鳍鲹","鱼类详情");yield return PageTap("重置","鱼类详情");yield return PageTap("静止","鱼类详情");
                for(int angle=0;angle<4;angle++)
                {
                    if(angle>0)for(int step=0;step<3;step++)yield return PageTap("旋转","鱼类详情");
                    var active=world.ActiveHabitat;var camera=Camera.main;var frame=ProbeJournalFrame(ui,world.TargetFish,camera);var sample=new ContourView{habitat=habitat.id,stage="model-"+(angle*90),waterOrder=active.waterSurface.sortingOrder,waterQueue=active.waterSurface.sharedMaterial.renderQueue,cameraY=camera.transform.position.y,seaLevel=active.SeaLevel,model=frame};result.views.Add(sample);
                    JourneyRequire(ui.ModelPaused&&frame.outside==0&&frame.behindCamera==0&&frame.vertices>1000&&sample.waterOrder<world.TargetFish.GetComponentsInChildren<SkinnedMeshRenderer>(true).Min(r=>r.sortingOrder),"contour-complete-paused-model-"+habitat.id+"-"+(angle*90));
                    yield return PageFrame("contour-"+habitat.id+"-model-"+(angle*90),true);
                }
                yield return PageTap("上一级","图鉴");
            }
            result.zeroPointsPreserved=game.Progress.Data.totalPoints==0&&game.Progress.Data.catches.Count==0;JourneyRequire(result.views.Count==18&&result.zeroPointsPreserved,"contour-all-habitats-no-fabricated-points-or-catches");result.status="PASS";
            File.WriteAllText(Path.Combine(folder,"fin-contour.json"),JsonUtility.ToJson(result,true));
        }
    }
}
