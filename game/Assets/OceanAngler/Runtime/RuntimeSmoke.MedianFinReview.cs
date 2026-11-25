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
        [Serializable] sealed class FinSample {public float seconds;public Quaternion front,rear;}
        [Serializable] sealed class FinResult
        {
            public string status,scope="Actual discovered journal swim/pause/resume through shared UI on an isolated save. Not seven native actions, a new catch, OS touch, scientific or final art acceptance";
            public int bones;public float maximumLocalTravel,pauseDrift,resumeTravel,inspectionZoom;public bool modelOnlyControlsPreserveSave;public List<FinSample> samples=new List<FinSample>();
        }
        IEnumerator ReviewMedianFinMotion(string saveFile)
        {
            var ui=GetComponent<GameUI>();var visual=GetComponent<OceanWorld>().TargetFish as AuthoredFishVisual;
            JourneyRequire(ui.FocusedFish=="halibut"&&visual!=null&&!ui.ModelPaused,"fin-review-actual-authored-journal");
            var fins=visual.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("dorsal_")||t.name.StartsWith("anal_")).OrderBy(t=>t.name).ToArray();JourneyRequire(fins.Length==34,"fin-review-imported-34-controls");
            var animator=visual.GetComponentInChildren<Animator>();var renderers=visual.GetComponentsInChildren<SkinnedMeshRenderer>();var priorCulling=animator.cullingMode;var priorOffscreen=renderers.Select(r=>r.updateWhenOffscreen).ToArray();
            // A no-graphics fixture has no visible renderer. Bypass culling only
            // for that fixture; native captures retain the production settings.
            if(!receipt.captureEnabled){animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;foreach(var renderer in renderers)renderer.updateWhenOffscreen=true;}
            try
            {
            var front=fins.Single(t=>t.name=="dorsal_05");var rear=fins.Single(t=>t.name=="dorsal_10");var result=new FinResult{status="RUNNING",bones=fins.Length};var before=File.ReadAllBytes(saveFile);var original=fins.Select(t=>t.localRotation).ToArray();float started=Time.realtimeSinceStartup;
            if(!receipt.captureEnabled)result.scope="Headless discovered-journal logical fixture with explicit AlwaysAnimate; no rendered, native or physical acceptance";
            yield return JourneyClick("放大");yield return JourneyClick("放大");result.inspectionZoom=ui.ModelZoom;
            yield return JourneyFrame("halibut-wave-start");
            for(int step=0;step<17;step++)
            {
                if(step>0)yield return new WaitForSecondsRealtime(.25f);
                result.samples.Add(new FinSample{seconds=Time.realtimeSinceStartup-started,front=front.localRotation,rear=rear.localRotation});
                result.maximumLocalTravel=Mathf.Max(result.maximumLocalTravel,fins.Select((t,i)=>Quaternion.Angle(original[i],t.localRotation)).Max());
            }
            JourneyRequire(result.maximumLocalTravel>.1f,"fin-review-independent-local-controls-animate");yield return JourneyFrame("halibut-wave-end");
            yield return JourneyClick("静止");yield return null;yield return null;var held=fins.Select(t=>t.localRotation).ToArray();yield return new WaitForSecondsRealtime(.55f);
            result.pauseDrift=fins.Select((t,i)=>Quaternion.Angle(held[i],t.localRotation)).Max();JourneyRequire(ui.ModelPaused&&result.pauseDrift<.001f,"fin-review-visible-pause-freezes-fins");yield return JourneyFrame("halibut-wave-paused");
            yield return JourneyClick("游动");yield return new WaitForSecondsRealtime(.35f);result.resumeTravel=fins.Select((t,i)=>Quaternion.Angle(held[i],t.localRotation)).Max();JourneyRequire(!ui.ModelPaused&&result.resumeTravel>.01f,"fin-review-visible-resume-restores-motion");yield return JourneyFrame("halibut-wave-resumed");
            yield return JourneyClick("重置");
            result.modelOnlyControlsPreserveSave=before.SequenceEqual(File.ReadAllBytes(saveFile));JourneyRequire(result.modelOnlyControlsPreserveSave,"fin-review-model-controls-do-not-write-save");
            result.status=result.bones==34&&result.maximumLocalTravel>.1f&&result.pauseDrift<.001f&&result.resumeTravel>.01f&&result.modelOnlyControlsPreserveSave?"PASS":"FAIL";File.WriteAllText(Path.Combine(folder,"fin-motion.json"),JsonUtility.ToJson(result,true));
            }
            finally{if(animator!=null)animator.cullingMode=priorCulling;for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)renderers[i].updateWhenOffscreen=priorOffscreen[i];}
        }
    }
}
