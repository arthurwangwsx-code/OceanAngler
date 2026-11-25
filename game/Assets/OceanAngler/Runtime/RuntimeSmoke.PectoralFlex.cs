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
        [Serializable] sealed class PectoralSample {public float seconds;public Quaternion leftMid,leftTip,rightMid,rightTip;}
        [Serializable] sealed class PectoralResult
        {
            public string status="RUNNING",scope="Actual discovered Bluefin journal swim/pause/resume, native production culling. Not all seven native actions, a new catch, OS touch, measured fish motion or final art acceptance";
            public int controls;public float maximumLocalTravel,pauseDrift,resumeTravel;public bool saveUnchanged;public List<PectoralSample> samples=new List<PectoralSample>();
        }
        IEnumerator ReviewPectoralFlex(string saveFile)
        {
            var ui=GetComponent<GameUI>();var fish=GetComponent<OceanWorld>().TargetFish as AuthoredFishVisual;
            JourneyRequire(ui.FocusedFish=="trevally"&&fish!=null&&!ui.ModelPaused&&game.Progress.Data.catches.Any(c=>c.fishId=="trevally"),"pectoral-review-discovered-authored-bluefin");
            var names=new[]{"pectoralLMid","pectoralLTip","pectoralRMid","pectoralRTip"};var controls=names.Select(n=>fish.GetComponentsInChildren<Transform>(true).Single(t=>t.name==n)).ToArray();
            var before=File.ReadAllBytes(saveFile);var result=new PectoralResult{controls=controls.Length};var original=controls.Select(t=>t.localRotation).ToArray();float started=Time.realtimeSinceStartup;
            yield return JourneyFrame("bluefin-flex-start");
            for(int i=0;i<17;i++)
            {
                if(i>0)yield return new WaitForSecondsRealtime(.25f);
                result.samples.Add(new PectoralSample{seconds=Time.realtimeSinceStartup-started,leftMid=controls[0].localRotation,leftTip=controls[1].localRotation,rightMid=controls[2].localRotation,rightTip=controls[3].localRotation});
                result.maximumLocalTravel=Mathf.Max(result.maximumLocalTravel,controls.Select((t,j)=>Quaternion.Angle(original[j],t.localRotation)).Max());
            }
            JourneyRequire(result.maximumLocalTravel>.1f,"pectoral-review-visible-local-flex-controls-animate");yield return JourneyFrame("bluefin-flex-end");
            yield return JourneyClick("静止",true);yield return null;yield return null;var held=controls.Select(t=>t.localRotation).ToArray();yield return new WaitForSecondsRealtime(.55f);
            result.pauseDrift=controls.Select((t,i)=>Quaternion.Angle(held[i],t.localRotation)).Max();JourneyRequire(ui.ModelPaused&&result.pauseDrift<.001f,"pectoral-review-visible-pause-freezes-new-controls");yield return JourneyFrame("bluefin-flex-paused");
            yield return JourneyClick("游动",true);yield return new WaitForSecondsRealtime(.35f);result.resumeTravel=controls.Select((t,i)=>Quaternion.Angle(held[i],t.localRotation)).Max();
            JourneyRequire(!ui.ModelPaused&&result.resumeTravel>.01f,"pectoral-review-visible-resume-restores-flex");yield return JourneyFrame("bluefin-flex-resumed");yield return JourneyClick("重置",true);
            result.saveUnchanged=before.SequenceEqual(File.ReadAllBytes(saveFile));JourneyRequire(result.saveUnchanged,"pectoral-review-model-controls-preserve-save");result.status="PASS";File.WriteAllText(Path.Combine(folder,"pectoral-flex.json"),JsonUtility.ToJson(result,true));
        }
    }
}
