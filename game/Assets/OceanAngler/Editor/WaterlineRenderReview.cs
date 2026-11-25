using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace OceanAngler.Editor
{
    public static class WaterlineRenderReview
    {
        [Serializable] sealed class Sample {public string name;public float seaLevel,cameraY,rigY,lureY,mechanismMargin;public int repeatChangedPixels;}
        [Serializable] sealed class Receipt {public string status="RUNNING",scope="Static actual imported three-habitat waterline poses; not Player transition, natural motion or physical qualification",graphics,quality,target;public int images,samples;public List<Sample> views=new List<Sample>();}
        static IEnumerator job;
        public static void Capture(){job=Run();EditorApplication.update+=Step;}
        public static void CaptureReflection(){job=RunReflection();EditorApplication.update+=Step;}
        static void Step()
        {
            try{if(job.MoveNext())return;EditorApplication.update-=Step;job=null;EditorApplication.Exit(0);}
            catch(Exception e){EditorApplication.update-=Step;(job as IDisposable)?.Dispose();job=null;Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static Color32[] Save(RenderTexture source,string path)
        {
            var prior=RenderTexture.active;var image=new Texture2D(source.width,source.height,TextureFormat.RGB24,false);
            try{RenderTexture.active=source;image.ReadPixels(new Rect(0,0,source.width,source.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());return image.GetPixels32();}
            finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(image);}
        }
        [Serializable] sealed class ReflectionReceipt
        {
            public string status="RUNNING",graphics,scope="Fixed-camera actual planar reflection with an owned moving diagnostic marker under the authored rig; not native performance or art acceptance";
            public bool trackingAvailable,staticCacheReused,underwaterSkipped;public int beforeRenders,afterRenders,changedPixels,width,height;
            public List<ReflectionChange> changes=new List<ReflectionChange>();
        }
        [Serializable] sealed class ReflectionChange {public string name;public int renderDelta,changedPixels,minimumPixels;}
        static IEnumerator RunReflection()
        {
            string folder=Environment.GetEnvironmentVariable("OCEAN_REFLECTION_REVIEW_OUTPUT");if(string.IsNullOrEmpty(folder)||SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("Output and real GPU required");
            EditorSceneManager.OpenScene("Assets/OceanAngler/Generated/Bay.unity");var camera=Camera.main;camera.enabled=false;
            int previousQuality=QualitySettings.GetQualityLevel(),medium=Array.IndexOf(QualitySettings.names,"Medium");var previousDefault=GraphicsSettings.defaultRenderPipeline;QualitySettings.SetQualityLevel(medium,true);var previousMedium=QualitySettings.renderPipeline;
            RenderTexture target=null;MarineHabitat habitat=null;AuthoredSpinningRig rig=null;Material material=null;
            var receipt=new ReflectionReceipt{graphics=SystemInfo.graphicsDeviceType.ToString()};
            try
            {
                var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OceanAngler/Generated/OceanURP.asset");QualitySettings.renderPipeline=pipeline;GraphicsSettings.defaultRenderPipeline=pipeline;
                var data=camera.GetComponent<UniversalAdditionalCameraData>()??camera.gameObject.AddComponent<UniversalAdditionalCameraData>();data.requiresColorTexture=true;data.requiresDepthTexture=true;
                target=new RenderTexture(1280,800,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};target.Create();camera.targetTexture=target;camera.aspect=1.6f;
                habitat=UnityEngine.Object.Instantiate(Resources.Load<GameObject>(MarineHabitat.ResourceFor("bay"))).GetComponent<MarineHabitat>();
                rig=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/SpinningRig")).GetComponent<AuthoredSpinningRig>();rig.InitializeMechanisms();OceanWorld.PoseGearAtWaterline(rig,habitat.SeaLevel,false);
                rig.Drive(0,false,true,true,false,rig.CastOrigin+Vector3.down*.3f,false);rig.Present(0);
                // This large owned marker makes stale reflected geometry observable
                // independently of subpixel changes on the thin fishing line.
                var marker=GameObject.CreatePrimitive(PrimitiveType.Cube);marker.name="ReflectionDiagnosticMarker";marker.transform.SetParent(rig.transform,false);marker.transform.position=new Vector3(1.3f,habitat.SeaLevel+1,-.2f);marker.transform.localScale=Vector3.one*.6f;
                material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(.9f,.08f,.035f);marker.GetComponent<Renderer>().sharedMaterial=material;
                var lineObject=new GameObject("ReflectionDiagnosticLine");lineObject.transform.SetParent(rig.transform,false);var line=lineObject.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=true;line.positionCount=3;line.startWidth=line.endWidth=.065f;
                line.SetPositions(new[]{new Vector3(-1.2f,habitat.SeaLevel+.8f,1.5f),new Vector3(-.5f,habitat.SeaLevel+1.6f,2),new Vector3(.1f,habitat.SeaLevel+.9f,2.5f)});
                // Optional lookup retains an executable red fixture against the
                // previous cache implementation, rather than a compiler error.
                var track=typeof(BayPlanarReflection).GetMethod("TrackSubject");receipt.trackingAvailable=track!=null;track?.Invoke(habitat.reflection,new object[]{rig.transform});
                habitat.Present(camera,false,true,0,true,true,false,0,true);yield return null;yield return null;
                var reflection=habitat.reflection;receipt.beforeRenders=reflection.RenderCount;receipt.width=reflection.Texture.width;receipt.height=reflection.Texture.height;
                var before=Save(reflection.Texture,Path.Combine(folder,"reflection-before.png"));
                habitat.Present(camera,false,true,0,true,true,false,0,true);receipt.staticCacheReused=reflection.RenderCount==receipt.beforeRenders;
                marker.transform.position+=Vector3.left*1.6f;
                habitat.Present(camera,false,true,0,true,true,false,0,true);
                var after=Save(reflection.Texture,Path.Combine(folder,"reflection-after.png"));receipt.afterRenders=reflection.RenderCount;
                receipt.changedPixels=before.Where((p,i)=>p.r!=after[i].r||p.g!=after[i].g||p.b!=after[i].b).Count();
                receipt.status=receipt.staticCacheReused&&receipt.afterRenders==receipt.beforeRenders+1&&receipt.changedPixels>128?"PASS":"FAIL";
                File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));
                if(receipt.status!="PASS")throw new InvalidOperationException("Moving geometry retained a stale planar reflection: "+JsonUtility.ToJson(receipt));
                Color32[] CheckChange(string name,Color32[] prior,int priorRenders,int minimum)
                {
                    var pixels=Save(reflection.Texture,Path.Combine(folder,"reflection-"+name+".png"));int changed=prior.Where((p,i)=>p.r!=pixels[i].r||p.g!=pixels[i].g||p.b!=pixels[i].b).Count();
                    var change=new ReflectionChange{name=name,renderDelta=reflection.RenderCount-priorRenders,changedPixels=changed,minimumPixels=minimum};receipt.changes.Add(change);receipt.status=change.renderDelta==1&&changed>=minimum?"RUNNING":"FAIL";
                    File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));if(receipt.status=="FAIL")throw new InvalidOperationException("Reflection change failed: "+JsonUtility.ToJson(change));return pixels;
                }
                int count=reflection.RenderCount;marker.transform.position+=Vector3.right*.8f;EditorApplication.QueuePlayerLoopUpdate();yield return null;yield return null;
                after=CheckChange("late-update",after,count,128);
                count=reflection.RenderCount;line.SetPosition(1,line.GetPosition(1)+new Vector3(.9f,.8f,0));habitat.Present(camera,false,true,0,true,true,false,0,true);after=CheckChange("line-only",after,count,64);
                // Only the bone changes here: renderer transforms and line points
                // stay fixed. A render-count check covers the offscreen rod joint.
                count=reflection.RenderCount;rig.rodBones[rig.rodBones.Length-1].localRotation*=Quaternion.Euler(20,0,0);habitat.Present(camera,false,true,0,true,true,false,0,true);after=CheckChange("bone-only",after,count,0);
                count=reflection.RenderCount;marker.GetComponent<Renderer>().enabled=false;habitat.Present(camera,false,true,0,true,true,false,0,true);after=CheckChange("visibility",after,count,128);
                count=reflection.RenderCount;reflection.Prepare(camera,false,habitat.SeaLevel);marker.GetComponent<Renderer>().enabled=true;marker.transform.position+=Vector3.left*.5f;EditorApplication.QueuePlayerLoopUpdate();yield return null;yield return null;
                receipt.underwaterSkipped=!reflection.Ready&&reflection.RenderCount==count;if(!receipt.underwaterSkipped)throw new InvalidOperationException("Underwater reflection must stay inactive");
                habitat.Present(camera,false,true,0,true,true,false,0,true);after=CheckChange("resurface",after,count,128);
                count=reflection.RenderCount;habitat.Present(camera,false,true,0,true,true,false,0,true);EditorApplication.QueuePlayerLoopUpdate();yield return null;yield return null;
                if(reflection.RenderCount!=count)throw new InvalidOperationException("Static subject repeatedly invalidated its reflection");
                receipt.status="PASS";File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));
            }
            finally
            {
                if(habitat!=null){if(habitat.lighting!=null)habitat.lighting.Restore();UnityEngine.Object.DestroyImmediate(habitat.gameObject);}if(rig!=null){rig.ReleasePresentation();UnityEngine.Object.DestroyImmediate(rig.gameObject);}if(material!=null)UnityEngine.Object.DestroyImmediate(material);
                camera.targetTexture=null;if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}QualitySettings.renderPipeline=previousMedium;QualitySettings.SetQualityLevel(previousQuality,true);GraphicsSettings.defaultRenderPipeline=previousDefault;
            }
        }
        static IEnumerator Run()
        {
            string folder=Environment.GetEnvironmentVariable("OCEAN_WATERLINE_REVIEW_OUTPUT");if(string.IsNullOrEmpty(folder)||SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("Output and real GPU required");
            EditorSceneManager.OpenScene("Assets/OceanAngler/Generated/Bay.unity");var camera=Camera.main;camera.enabled=false;
            int previousQuality=QualitySettings.GetQualityLevel(),medium=Array.IndexOf(QualitySettings.names,"Medium");var previousDefault=GraphicsSettings.defaultRenderPipeline;QualitySettings.SetQualityLevel(medium,true);var previousMedium=QualitySettings.renderPipeline;
            RenderTexture target=null;
            try
            {
                var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OceanAngler/Generated/OceanURP.asset");if(pipeline.msaaSampleCount!=4)throw new InvalidOperationException("Production four-sample pipeline required");QualitySettings.renderPipeline=pipeline;GraphicsSettings.defaultRenderPipeline=pipeline;
                var data=camera.GetComponent<UniversalAdditionalCameraData>()??camera.gameObject.AddComponent<UniversalAdditionalCameraData>();data.requiresColorTexture=true;data.requiresDepthTexture=true;
                target=new RenderTexture(1280,800,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};target.Create();camera.targetTexture=target;camera.aspect=1.6f;
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};var receipt=new Receipt{graphics=SystemInfo.graphicsDeviceType.ToString(),quality=QualitySettings.names[medium],target=target.graphicsFormat.ToString(),samples=target.antiAliasing};
                foreach(string id in new[]{"bay","channel","slope"})
                {
                    var habitat=UnityEngine.Object.Instantiate(Resources.Load<GameObject>(MarineHabitat.ResourceFor(id))).GetComponent<MarineHabitat>();
                    var rig=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/SpinningRig")).GetComponent<AuthoredSpinningRig>();rig.InitializeMechanisms();
                    habitat.reflection.TrackSubject(rig.transform);
                    var fish=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin")).GetComponent<AuthoredFishVisual>();fish.InitializePresentation();var animator=fish.GetComponentInChildren<Animator>();animator.enabled=false;
                    var hook=fish.transform.InverseTransformPoint(fish.HookPosition);var clips=animator.runtimeAnimatorController.animationClips;
                    try
                    {
                        foreach(bool left in new[]{false,true})foreach(var frame in new[]{("ready",FishingPhase.Ready,0f,true),("cast",FishingPhase.Casting,.5f,true),("impact",FishingPhase.Waiting,0f,true),("underwater",FishingPhase.Fighting,.5f,false)})
                        {
                            OceanWorld.PoseGearAtWaterline(rig,habitat.SeaLevel,left);var pose=FishingEncounter.Evaluate(frame.Item2,frame.Item3,.6f,hook,rig.CastOrigin,0,0,false,habitat.SeaLevel);
                            fish.transform.SetPositionAndRotation(pose.fishPosition,pose.fishRotation);clips.Single(c=>c.name==(frame.Item2==FishingPhase.Fighting?"hooked":"swim")).SampleAnimation(animator.gameObject,.2f);
                            rig.Drive(frame.Item2==FishingPhase.Fighting?.45f:0,false,false,false,frame.Item2==FishingPhase.Casting,pose.lurePosition,false,frame.Item2==FishingPhase.Fighting,frame.Item2==FishingPhase.Fighting?fish:null);rig.Present(.1f);
                            habitat.SetFishingSplash(pose.lurePosition,frame.Item1=="impact"?.15f:frame.Item1=="ready"?float.PositiveInfinity:-1);habitat.Present(camera,false,frame.Item4,0,true,false,true,0,frame.Item4);
                            yield return null;yield return null;
                            string name=id+"-"+(left?"left":"right")+"-"+frame.Item1;RenderPipeline.SubmitRenderRequest(camera,request);var first=Save(target,Path.Combine(folder,name+".png"));receipt.images++;
                            RenderPipeline.SubmitRenderRequest(camera,request);var second=Save(target,Path.Combine(folder,name+"-repeat.png"));receipt.images++;
                            float margin=1;
                            if(frame.Item4)foreach(var mechanism in new[]{rig.rotor,rig.spool,rig.crank,rig.roller})
                            {
                                var point=camera.WorldToViewportPoint(mechanism.position);margin=Mathf.Min(margin,point.x,1-point.x,point.y,1-point.y);
                                if(point.z<=camera.nearClipPlane||margin<.04f)throw new InvalidOperationException(name+" reel mechanism leaves framing "+mechanism.name+" "+point);
                            }
                            int changed=first.Where((p,i)=>p.r!=second[i].r||p.g!=second[i].g||p.b!=second[i].b).Count();receipt.views.Add(new Sample{name=name,seaLevel=habitat.SeaLevel,cameraY=camera.transform.position.y,rigY=rig.transform.position.y,lureY=rig.lureNose.position.y,repeatChangedPixels=changed,mechanismMargin=margin});
                        }
                    }
                    finally{if(habitat.lighting!=null)habitat.lighting.Restore();fish.ReleasePresentation();rig.ReleasePresentation();UnityEngine.Object.DestroyImmediate(fish.gameObject);UnityEngine.Object.DestroyImmediate(rig.gameObject);UnityEngine.Object.DestroyImmediate(habitat.gameObject);}
                }
                receipt.status=receipt.views.All(s=>s.repeatChangedPixels==0)?"PASS":"PARTIAL";File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));
                if(receipt.status!="PASS")throw new InvalidOperationException("Initial/repeat pixel differences retained");
            }
            finally{camera.targetTexture=null;if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}QualitySettings.renderPipeline=previousMedium;QualitySettings.SetQualityLevel(previousQuality,true);GraphicsSettings.defaultRenderPipeline=previousDefault;}
        }
    }
}
