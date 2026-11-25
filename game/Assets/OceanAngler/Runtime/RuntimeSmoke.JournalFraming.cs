using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace OceanAngler
{
    public sealed partial class RuntimeSmoke
    {
        [Serializable] public sealed class JournalFrameSample
        {
            public string fishId;public float zoom,yaw;public Rect available,projected;
            public int vertices,outside,behindCamera;
        }
        [Serializable] sealed class JournalFramingResult
        {
            public string status="PASS",scope="Actual native journal UI/model geometric clearance at 12 species, zoom limits and quarter rotations; two authored fish have extra natural motion samples. Not OS touch, all animation actions, art/science or physical qualification";
            public bool modelControlsPreserveSave,returnedToFishing;public Vector3 returnedScale;public bool returnedSurfaceCamera,returnedHudCentralClear;public int returnedHudButtons;public float returnedHudRatio;public List<JournalFrameSample> samples;
        }
        readonly List<JournalFrameSample> journalFramingSamples=new List<JournalFrameSample>();
        [Serializable] public sealed class JournalStabilitySample
        {
            public int countBefore,countAfter;public Vector3 positionBefore,positionAfter,scaleBefore,scaleAfter;
            public float requestedYawBefore,requestedYawAfter;
            public OceanWorld.JournalFitState inputsBefore,inputsAfter;
        }
        public static JournalStabilitySample BeginJournalStability(OceanWorld world)=>new JournalStabilitySample{countBefore=world.JournalFitCalculations,positionBefore=world.TargetFish.transform.position,scaleBefore=world.TargetFish.transform.localScale,inputsBefore=world.CurrentJournalFit,requestedYawBefore=world.GetComponent<GameUI>().ModelYaw};
        public static void EndJournalStability(OceanWorld world,JournalStabilitySample sample)
        {sample.countAfter=world.JournalFitCalculations;sample.positionAfter=world.TargetFish.transform.position;sample.scaleAfter=world.TargetFish.transform.localScale;sample.inputsAfter=world.CurrentJournalFit;sample.requestedYawAfter=world.GetComponent<GameUI>().ModelYaw;}
        IEnumerator ReviewJournalFraming(string fishId,string saveFile)
        {
            JourneyRequire(Debug.isDebugBuild,"framing-review-development-only");
            var ui=GetComponent<GameUI>();var world=GetComponent<OceanWorld>();byte[] before=File.ReadAllBytes(saveFile);bool authored=fishId=="halibut"||fishId=="trevally";
            yield return JourneyClick("重置");
            foreach(float zoom in authored?new[]{.7f,1f,1.3f,1.5f}:new[]{1.5f})
            {
                while(ui.ModelZoom>zoom+.01f)yield return JourneyClick("缩小");while(ui.ModelZoom<zoom-.01f)yield return JourneyClick("放大");
                for(int turn=0;turn<4;turn++)
                {
                    if(receipt.captureEnabled)yield return new WaitForEndOfFrame();else{yield return null;world.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);}
                    var sample=ProbeJournalFrame(ui,world.TargetFish,Camera.main);journalFramingSamples.Add(sample);
                    JourneyRequire(sample.outside==0&&sample.behindCamera==0&&sample.vertices>100,"journal-framing-clear-"+fishId+"-"+zoom+"-"+turn);
                    if(authored)
                    {
                        var stability=BeginJournalStability(world);
                        yield return new WaitForSecondsRealtime(.16f);if(receipt.captureEnabled)yield return new WaitForEndOfFrame();else world.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);
                        sample=ProbeJournalFrame(ui,world.TargetFish,Camera.main);journalFramingSamples.Add(sample);
                        EndJournalStability(world,stability);File.WriteAllText(Path.Combine(folder,"journal-stability-last.json"),JsonUtility.ToJson(stability,true));
                        JourneyRequire(sample.outside==0&&sample.behindCamera==0,"journal-framing-moving-clear-"+fishId);
                        JourneyRequire(stability.countAfter==stability.countBefore&&Vector3.Distance(stability.positionBefore,stability.positionAfter)<.00001f&&Vector3.Distance(stability.scaleBefore,stability.scaleAfter)<.00001f,"journal-framing-stable-during-motion-"+fishId);
                        if(turn==0||turn==1&&zoom>1.4f)yield return JourneyFrame("journal-"+fishId+"-zoom"+Mathf.RoundToInt(zoom*100)+"-turn"+turn);
                    }
                    for(int i=0;i<3;i++)yield return JourneyClick("旋转");
                }
            }
            yield return JourneyClick("重置");JourneyRequire(before.SequenceEqual(File.ReadAllBytes(saveFile)),"journal-framing-controls-preserve-save-"+fishId);
            File.WriteAllText(Path.Combine(folder,"journal-framing.json"),JsonUtility.ToJson(new JournalFramingResult{modelControlsPreserveSave=true,samples=journalFramingSamples},true));
        }
        IEnumerator ReviewJournalReturn()
        {
            var ui=GetComponent<GameUI>();var world=GetComponent<OceanWorld>();yield return JourneyClick("出海",true);yield return null;yield return null;
            JourneyRequire(game.Session.Paused&&!ui.Playing,"journal-return-keeps-explicit-pause");yield return JourneyClick("继续探索",true);yield return null;yield return null;
            bool surface=!game.Progress.Data.settings.underwaterOnly&&!game.Progress.Data.settings.reducedMotion;
            JourneyRequire(ui.FocusedFish==null&&ui.Playing&&!game.Reeling&&game.Session.Phase==FishingPhase.Ready&&Vector3.Distance(world.TargetFish.transform.localScale,Vector3.one)<.00001f,"journal-return-normal-playing-scale-and-fresh-input");
            JourneyRequire(world.FishingSurfaceView==surface&&(surface?Camera.main.transform.position.y>world.ActiveHabitat.SeaLevel:Camera.main.transform.position.y<world.ActiveHabitat.SeaLevel),"journal-return-actual-fishing-camera");
            CheckImmersiveHud();yield return JourneyFrame("journal-back-to-fishing");
            var hud=hudSamples.Last();File.WriteAllText(Path.Combine(folder,"journal-framing.json"),JsonUtility.ToJson(new JournalFramingResult{modelControlsPreserveSave=true,returnedToFishing=true,returnedScale=world.TargetFish.transform.localScale,returnedSurfaceCamera=world.FishingSurfaceView,returnedHudCentralClear=hud.centralClear,returnedHudButtons=hud.buttons,returnedHudRatio=hud.occupiedRatio,samples=journalFramingSamples},true));game.Pause(true);
        }
        // Deliberately derives the empty region from real controls, not the
        // production preview marker or its framing solver. QA only.
        public static JournalFrameSample ProbeJournalFrame(GameUI ui,FishVisual fish,Camera camera)
        {
            var buttons=FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.isActiveAndEnabled).ToArray();
            Button Find(string name)=>buttons.Single(b=>b.GetComponentInChildren<Text>()?.text==name);
            Rect Pixels(RectTransform rt)
            {
                var corners=new Vector3[4];rt.GetWorldCorners(corners);var points=corners.Select(p=>RectTransformUtility.WorldToScreenPoint(null,p)).ToArray();
                return Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));
            }
            var controls=new[]{"旋转","放大","缩小",ui.ModelPaused?"游动":"静止","重置"}.Select(n=>Pixels(Find(n).GetComponent<RectTransform>())).ToArray();
            float unit=DisplayMetrics.PixelsPerPoint;var safe=DisplayMetrics.SafeArea;
            var status=FindObjectsByType<Text>(FindObjectsSortMode.None).Single(t=>t.name=="ModelStatus");
            float top=Pixels(status.rectTransform).yMin-6*unit;
            var result=new JournalFrameSample{fishId=ui.FocusedFish,zoom=ui.ModelZoom,yaw=ui.ModelYaw,available=Rect.MinMaxRect(safe.xMin+12*unit,controls.Max(r=>r.yMax)+10*unit,safe.xMax-12*unit,top)};
            var min=new Vector2(float.PositiveInfinity,float.PositiveInfinity);var max=-min;var scratch=new Mesh();
            void Point(Vector3 world)
            {
                var p=camera.WorldToScreenPoint(world);result.vertices++;if(p.z<=camera.nearClipPlane)result.behindCamera++;
                if(p.x<result.available.xMin-.01f||p.x>result.available.xMax+.01f||p.y<result.available.yMin-.01f||p.y>result.available.yMax+.01f)result.outside++;
                min=Vector2.Min(min,p);max=Vector2.Max(max,p);
            }
            try
            {
                foreach(var renderer in fish.GetComponentsInChildren<Renderer>(true))
                {
                    if(renderer is SkinnedMeshRenderer skin){skin.BakeMesh(scratch,true);foreach(var v in scratch.vertices)Point(skin.transform.TransformPoint(v));}
                    else if(renderer is MeshRenderer mesh){var filter=mesh.GetComponent<MeshFilter>();if(filter!=null)foreach(var v in filter.sharedMesh.vertices)Point(mesh.transform.TransformPoint(v));}
                    else if(renderer is LineRenderer line)for(int i=0;i<line.positionCount;i++)Point(line.useWorldSpace?line.GetPosition(i):line.transform.TransformPoint(line.GetPosition(i)));
                }
            }
            finally{Destroy(scratch);}
            result.projected=Rect.MinMaxRect(min.x,min.y,max.x,max.y);return result;
        }
    }
}
