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
    // Explicit old-lamp controls on identical imported poses. No game save,
    // target selection, native UI, performance or art qualification is implied.
    public static class DeepWaterLightingReview
    {
        [Serializable] sealed class Sample
        {
            public string name;public int oldRepeatPixels,newRepeatPixels,changedPixels;
            public Vector3 camera,subject,oldLamp,newLamp;public float fov;
        }
        [Serializable] sealed class Receipt
        {
            public string status="RUNNING",graphics,scope="Isolated same-pose Editor GPU old/new research lamp; not native UI, scientific or final art acceptance";
            public int images,samples;public List<Sample> views=new List<Sample>();
        }
        static IEnumerator job;
        public static void Capture(){job=Run();EditorApplication.update+=Step;}
        static void Step()
        {
            try{if(job.MoveNext())return;EditorApplication.update-=Step;job=null;EditorApplication.Exit(0);}
            catch(Exception e){EditorApplication.update-=Step;(job as IDisposable)?.Dispose();job=null;Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static Color32[] Save(RenderTexture target,string path)
        {
            var prior=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try{RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());return image.GetPixels32();}
            finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(image);}
        }
        static int Changed(Color32[] a,Color32[] b)=>a.Where((p,i)=>p.r!=b[i].r||p.g!=b[i].g||p.b!=b[i].b).Count();
        static IEnumerator Run()
        {
            string folder=Environment.GetEnvironmentVariable("OCEAN_DEEP_LIGHTING_OUTPUT");if(string.IsNullOrEmpty(folder)||SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("GPU and fresh output required");
            var receipt=new Receipt{graphics=SystemInfo.graphicsDeviceType.ToString()};
            int priorQuality=QualitySettings.GetQualityLevel(),medium=Array.IndexOf(QualitySettings.names,"Medium");var priorDefault=GraphicsSettings.defaultRenderPipeline;
            QualitySettings.SetQualityLevel(medium,true);var priorMedium=QualitySettings.renderPipeline;
            MarineHabitat habitat=null;GameObject subject=null;RenderTexture target=null;Camera camera=null;
            try
            {
                EditorSceneManager.OpenScene("Assets/OceanAngler/Generated/Bay.unity");camera=Camera.main;camera.enabled=false;camera.aspect=16f/9;camera.allowHDR=true;camera.allowMSAA=true;
                var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OceanAngler/Generated/OceanURP.asset");if(pipeline==null||pipeline.msaaSampleCount!=4)throw new InvalidOperationException("Production HDR/MSAA pipeline required");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
                RenderSettings.ambientLight=new Color(.2f,.32f,.38f);
                habitat=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/ContinentalSlope")).GetComponent<MarineHabitat>();var lighting=habitat.lighting;var lamp=lighting.cameraFill;if(lamp==null)throw new InvalidOperationException("Authored camera lamp missing");
                var newColor=lamp.color;float newIntensity=lamp.intensity,newAngle=lamp.spotAngle,newRange=lamp.range;
                var catalog=JsonUtility.FromJson<ContentCatalog>(Resources.Load<TextAsset>("ContentCatalog").text);
                target=new RenderTexture(1440,810,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};target.Create();camera.targetTexture=target;receipt.samples=target.antiAliasing;
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
                Color32[] Draw(string name){RenderPipeline.SubmitRenderRequest(camera,request);receipt.images++;return Save(target,Path.Combine(folder,name+".png"));}
                foreach(string id in new[]{"halibut","bigeye","alfonsino","cod"})
                {
                    if(id=="halibut")
                    {
                        subject=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Halibut"));var authored=subject.GetComponent<AuthoredFishVisual>();authored.InitializePresentation();var animator=subject.GetComponentInChildren<Animator>();animator.enabled=false;
                        animator.runtimeAnimatorController.animationClips.Single(c=>c.name=="hooked").SampleAnimation(animator.gameObject,.24f);
                        foreach(var renderer in subject.GetComponentsInChildren<SkinnedMeshRenderer>())renderer.updateWhenOffscreen=true;
                    }
                    else{subject=new GameObject("LightingControl-"+id);subject.AddComponent<FishModel>().Initialize(catalog.Fish(id));}
                    try
                    {
                        var visual=subject.GetComponent<FishVisual>();visual.paused=true;visual.reducedMotion=true;
                        foreach(string mode in id=="halibut"?new[]{"fight-20","fight-80","landed","journal"}:new[]{"fight-20"})
                        {
                            habitat.Present(camera,false,false,0,true,true,false);
                            var pose=FishingEncounter.Evaluate(mode=="landed"?FishingPhase.Landed:FishingPhase.Fighting,mode=="fight-80"?.8f:.2f,.6f,subject.transform.InverseTransformPoint(visual.HookPosition),Vector3.zero,0,0,false,habitat.SeaLevel);
                            subject.transform.SetPositionAndRotation(pose.fishPosition,pose.fishRotation);
                            if(id=="halibut")
                            {
                                var look=subject.transform.position;var eye=look+new Vector3(0,2.8f,-3.8f);camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(look-eye));
                                if(mode=="journal"){subject.transform.SetPositionAndRotation(new Vector3(2.35f,1.35f,1.2f),Quaternion.Euler(0,-25,-3)*Quaternion.Euler(-55,0,0));camera.transform.SetPositionAndRotation(habitat.underwaterCamera.position,Quaternion.LookRotation(habitat.underwaterTarget.position-habitat.underwaterCamera.position));}
                            }
                            var sample=new Sample{name=id+"-"+mode,camera=camera.transform.position,subject=subject.transform.position,fov=camera.fieldOfView};receipt.views.Add(sample);
                            lamp.color=new Color(.6f,.73f,1);lamp.intensity=6;lamp.spotAngle=90;lamp.range=35;lamp.transform.SetPositionAndRotation(new Vector3(0,2,-6),Quaternion.LookRotation(new Vector3(0,-3,14)-new Vector3(0,2,-6)));sample.oldLamp=lamp.transform.position;
                            yield return null;yield return null;var old=Draw(sample.name+"-old");sample.oldRepeatPixels=Changed(old,Draw(sample.name+"-old-repeat"));
                            lamp.color=newColor;lamp.intensity=newIntensity;lamp.spotAngle=newAngle;lamp.range=newRange;lighting.AimCameraFill(camera);sample.newLamp=lamp.transform.position;
                            yield return null;yield return null;var changed=Draw(sample.name+"-new");sample.newRepeatPixels=Changed(changed,Draw(sample.name+"-new-repeat"));sample.changedPixels=Changed(old,changed);
                        }
                    }
                    finally{var authored=subject.GetComponent<AuthoredFishVisual>();if(authored!=null)authored.ReleasePresentation();UnityEngine.Object.DestroyImmediate(subject);subject=null;}
                }
                receipt.status=receipt.views.All(s=>s.oldRepeatPixels==0&&s.newRepeatPixels==0&&s.changedPixels>0)?"PASS":"PARTIAL";
            }
            finally
            {
                if(receipt.status=="RUNNING")receipt.status="FAIL";File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));
                if(subject!=null)UnityEngine.Object.DestroyImmediate(subject);if(habitat!=null){habitat.lighting.Restore();UnityEngine.Object.DestroyImmediate(habitat.gameObject);}if(camera!=null)camera.targetTexture=null;if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}QualitySettings.renderPipeline=priorMedium;GraphicsSettings.defaultRenderPipeline=priorDefault;QualitySettings.SetQualityLevel(priorQuality,true);
            }
            if(receipt.status!="PASS")throw new InvalidOperationException("Keep first-draw differences and inspect the controls");
        }
    }
}
