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
    // Scene/material controls, not Player captures or production probe assets.
    public static class FishLightingReview
    {
        [Serializable] sealed class ViewSample {public string view;public int initialChangedPixels,baselineRepeatPixels,noEnvironmentPixels,localProbePixels,probeRepeatPixels;}
        [Serializable] sealed class Receipt
        {
            public string status="IN_PROGRESS",scope="CoralBay stable-reference Editor GPU lighting controls; initial draw separately retained and unqualified; not Player,art,science or physical qualification",graphics,quality,target,defaultReflection,ambientMode;
            public int images,samples,probeResolution,probeMips;public Vector4 defaultReflectionDecode;public float reflectionIntensity;
            public List<ViewSample> views=new List<ViewSample>();
        }
        static IEnumerator job;
        public static void Capture(){job=Run();EditorApplication.update+=Step;}
        static void Step()
        {
            try{if(job.MoveNext())return;EditorApplication.update-=Step;job=null;EditorApplication.Exit(0);}
            catch(Exception e){EditorApplication.update-=Step;(job as IDisposable)?.Dispose();job=null;Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static int Changed(Color32[] a,Color32[] b,int threshold=2)=>a.Where((p,i)=>Mathf.Max(Mathf.Abs(p.r-b[i].r),Mathf.Max(Mathf.Abs(p.g-b[i].g),Mathf.Abs(p.b-b[i].b)))>threshold).Count();
        internal static void InitializeSceneBindings(GameObject root)
        {
            // The isolated submit-render path can precede per-submesh material
            // initialization. Bind actual authored values explicitly; no warm
            // frame is discarded or used to hide the first-draw defect.
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if(renderer.gameObject.layer==4)continue;var materials=renderer.sharedMaterials;
                for(int slot=0;slot<materials.Length;slot++)
                {
                    var m=materials[slot];if(m==null)continue;var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,slot);
                    for(int p=0;p<m.shader.GetPropertyCount();p++)
                    {
                        string name=m.shader.GetPropertyName(p);
                        switch(m.shader.GetPropertyType(p))
                        {
                            case ShaderPropertyType.Color:block.SetColor(name,m.GetColor(name));break;
                            case ShaderPropertyType.Vector:block.SetVector(name,m.GetVector(name));break;
                            case ShaderPropertyType.Float:case ShaderPropertyType.Range:block.SetFloat(name,m.GetFloat(name));break;
                            case ShaderPropertyType.Int:block.SetInteger(name,m.GetInteger(name));break;
                            case ShaderPropertyType.Texture:
                                var texture=m.GetTexture(name);if(texture!=null)block.SetTexture(name,texture);var scale=m.GetTextureScale(name);var offset=m.GetTextureOffset(name);block.SetVector(name+"_ST",new Vector4(scale.x,scale.y,offset.x,offset.y));break;
                        }
                    }
                    renderer.SetPropertyBlock(block,slot);
                }
            }
        }
        static IEnumerator Run()
        {
            string folder=Environment.GetEnvironmentVariable("OCEAN_FISH_LIGHTING_REVIEW_OUTPUT");if(string.IsNullOrEmpty(folder))throw new InvalidOperationException("Review folder required");
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("GPU required");
            int prior=QualitySettings.GetQualityLevel(),medium=Array.IndexOf(QualitySettings.names,"Medium");if(medium<0)throw new InvalidOperationException("Medium required");
            var priorDefault=GraphicsSettings.defaultRenderPipeline;QualitySettings.SetQualityLevel(medium,true);var priorMedium=QualitySettings.renderPipeline;bool priorRealtime=QualitySettings.realtimeReflectionProbes;
            var receipt=new Receipt{graphics=SystemInfo.graphicsDeviceType.ToString(),quality=QualitySettings.names[medium]};
            RenderTexture target=null;GameObject habitat=null,fish=null,probeRoot=null;Camera camera=null;
            try
            {
                EditorSceneManager.OpenScene("Assets/OceanAngler/Generated/Bay.unity");camera=Camera.main;camera.enabled=false;camera.aspect=1.6f;
                var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OceanAngler/Generated/OceanURP.asset");if(pipeline==null||pipeline.msaaSampleCount!=4)throw new InvalidOperationException("Production pipeline required");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
                RenderSettings.ambientLight=new Color(.2f,.32f,.38f);
                habitat=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/CoralBay"));var bay=habitat.GetComponent<MarineHabitat>();bay.Present(camera,false,false,0,true,true,false);InitializeSceneBindings(habitat);
                fish=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin"));fish.GetComponent<AuthoredFishVisual>().InitializePresentation();var animator=fish.GetComponentInChildren<Animator>();animator.enabled=false;
                foreach(var t in fish.GetComponentsInChildren<Transform>())t.gameObject.layer=31;foreach(var r in fish.GetComponentsInChildren<SkinnedMeshRenderer>())r.updateWhenOffscreen=true;
                var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/OceanAngler/Art/Bluefin/Bluefin.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).Single(c=>c.name=="idle"||c.name.EndsWith("|idle"));clip.SampleAnimation(animator.gameObject,0);
                var visual=fish.GetComponent<AuthoredFishVisual>();var pose=FishingEncounter.Evaluate(FishingPhase.Fighting,.2f,.6f,fish.transform.InverseTransformPoint(visual.HookPosition),Vector3.zero,0,0,false);fish.transform.SetPositionAndRotation(pose.fishPosition,pose.fishRotation);
                target=new RenderTexture(1440,900,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};target.Create();camera.targetTexture=target;receipt.target=target.graphicsFormat.ToString();receipt.samples=target.antiAliasing;
                receipt.defaultReflection=ReflectionProbe.defaultTexture==null?"none":ReflectionProbe.defaultTexture.name;receipt.defaultReflectionDecode=ReflectionProbe.defaultTextureHDRDecodeValues;receipt.reflectionIntensity=RenderSettings.reflectionIntensity;receipt.ambientMode=RenderSettings.ambientMode.ToString();
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
                Color32[] Draw(string name)
                {
                    RenderPipeline.SubmitRenderRequest(camera,request);receipt.images++;var previous=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                    try{RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());return image.GetPixels32();}
                    finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
                }
                yield return null;yield return null;
                var renderers=fish.GetComponentsInChildren<SkinnedMeshRenderer>(true);var originals=renderers.Select(r=>r.sharedMaterials).ToArray();var views=new Dictionary<string,Color32[]>();var cameraGamePosition=camera.transform.position;var cameraGameRotation=camera.transform.rotation;
                void Pose(string view)
                {
                    if(view=="game"){camera.transform.SetPositionAndRotation(cameraGamePosition,cameraGameRotation);return;}
                    var center=fish.transform.TransformPoint(new Vector3(.12f,.02f,0));camera.transform.position=center+(view=="near"?new Vector3(0,.3f,-4):new Vector3(0,.3f,4));camera.transform.LookAt(center);
                }
                foreach(string view in new[]{"game","near","reverse"})
                {
                    Pose(view);yield return null;yield return null;var initial=Draw(view+"-initial-before");var baseline=Draw(view+"-production-before");views.Add(view,baseline);var repeat=Draw(view+"-production-before-repeat");var sample=new ViewSample{view=view,initialChangedPixels=Changed(initial,baseline),baselineRepeatPixels=Changed(baseline,repeat,0)};receipt.views.Add(sample);
                    var clones=originals.Select(ms=>ms.Select(m=>new Material(m)).ToArray()).ToArray();
                    try
                    {
                        for(int r=0;r<renderers.Length;r++){foreach(var m in clones[r]){m.SetFloat("_EnvironmentReflections",0);m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");}renderers[r].sharedMaterials=clones[r];}
                        yield return null;yield return null;sample.noEnvironmentPixels=Changed(baseline,Draw(view+"-diagnostic-no-environment"));
                    }
                    finally{for(int r=0;r<renderers.Length;r++)renderers[r].sharedMaterials=originals[r];foreach(var ms in clones)foreach(var m in ms)UnityEngine.Object.DestroyImmediate(m);}
                }
                probeRoot=new GameObject("DiagnosticLocalReflection");var probe=probeRoot.AddComponent<ReflectionProbe>();probe.transform.position=fish.transform.position;probe.size=Vector3.one*100;probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.NoTimeSlicing;probe.resolution=128;probe.hdr=true;probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=bay.underwaterColor;probe.cullingMask=~((1<<4)|(1<<5)|(1<<31));probe.nearClipPlane=.1f;probe.farClipPlane=40;probe.intensity=1;
                QualitySettings.realtimeReflectionProbes=true;int id=probe.RenderProbe();double deadline=EditorApplication.timeSinceStartup+45;
                while(!probe.IsFinishedRendering(id)&&EditorApplication.timeSinceStartup<deadline){EditorApplication.QueuePlayerLoopUpdate();yield return null;}
                if(!probe.IsFinishedRendering(id)||probe.texture==null)throw new InvalidOperationException("Diagnostic reflection capture not ready");receipt.probeResolution=probe.texture.width;receipt.probeMips=probe.texture.mipmapCount;
                foreach(string view in new[]{"game","near","reverse"})
                {
                    Pose(view);yield return null;yield return null;var pixels=Draw(view+"-diagnostic-local-probe");var repeat=Draw(view+"-diagnostic-local-probe-repeat");var sample=receipt.views.Single(s=>s.view==view);sample.localProbePixels=Changed(views[view],pixels);sample.probeRepeatPixels=Changed(pixels,repeat,0);
                    if(sample.baselineRepeatPixels!=0||sample.probeRepeatPixels!=0)throw new InvalidOperationException("Static control drift: "+JsonUtility.ToJson(sample));
                }
                if(receipt.views.All(s=>s.localProbePixels==0))throw new InvalidOperationException("Local reflection had no rendered effect; do not claim an effective control");receipt.status="PASS";
            }
            finally
            {
                if(receipt.status!="PASS")receipt.status="FAIL";File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));if(probeRoot!=null)UnityEngine.Object.DestroyImmediate(probeRoot);if(fish!=null){fish.GetComponent<AuthoredFishVisual>().ReleasePresentation();UnityEngine.Object.DestroyImmediate(fish);}if(habitat!=null)UnityEngine.Object.DestroyImmediate(habitat);if(camera!=null)camera.targetTexture=null;if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}QualitySettings.realtimeReflectionProbes=priorRealtime;QualitySettings.renderPipeline=priorMedium;GraphicsSettings.defaultRenderPipeline=priorDefault;QualitySettings.SetQualityLevel(prior,true);
            }
        }
    }
}
