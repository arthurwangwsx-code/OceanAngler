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
    // Material ablations only alter private clones. Production views retain the
    // real imported mesh, materials, runtime clips and four-sample pipeline.
    public static class BluefinContourRenderReview
    {
        [Serializable] sealed class View {public int lod,repeatPixels,opaquePixels,doubleSidedPixels,noRaysPixels,waterEarlyPixels,waterLatePixels,waterLateRepeatPixels,waterAbsentPixels;public float time,yaw;public Vector3 subject,scale,camera;}
        [Serializable] sealed class Receipt
        {
            public string status="RUNNING",graphics,target,platform,scope="Imported runtime asset contour diagnosis. Material controls are private clones, not production candidates or Player/device/art qualification.";
            public int samples,images;public List<View> views=new List<View>();
        }
        static IEnumerator job;
        static bool sceneMode;
        public static void Capture(){job=Run();EditorApplication.update+=Step;}
        public static void CaptureScene(){sceneMode=true;Capture();}
        static void Step()
        {
            try{if(job.MoveNext())return;EditorApplication.update-=Step;job=null;EditorApplication.Exit(0);}
            catch(Exception e){EditorApplication.update-=Step;(job as IDisposable)?.Dispose();job=null;Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static IEnumerator Run()
        {
            string folder=Environment.GetEnvironmentVariable("OCEAN_CONTOUR_OUTPUT");
            if(string.IsNullOrEmpty(folder)||SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("Fresh output and GPU required");
            var receipt=new Receipt{graphics=SystemInfo.graphicsDeviceType.ToString(),platform=EditorUserBuildSettings.activeBuildTarget.ToString()};
            int prior=QualitySettings.GetQualityLevel(),medium=Array.IndexOf(QualitySettings.names,"Medium");if(medium<0)throw new InvalidOperationException("Production Medium required");
            var priorDefault=GraphicsSettings.defaultRenderPipeline;QualitySettings.SetQualityLevel(medium,true);var priorMedium=QualitySettings.renderPipeline;
            GameObject root=null,habitatRoot=null;MarineHabitat habitat=null;Camera camera=null;Light sun=null;RenderTexture target=null;Material opaque=null,twoSided=null,noRays=null;
            try
            {
                if(sceneMode)EditorSceneManager.OpenScene("Assets/OceanAngler/Generated/Bay.unity");else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OceanAngler/Generated/OceanURP.asset");if(pipeline==null||pipeline.msaaSampleCount!=4)throw new InvalidOperationException("Production four-sample HDR pipeline required");
                GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.32f,.4f,.43f);
                camera=sceneMode?Camera.main:new GameObject("ContourReviewCamera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=false;camera.fieldOfView=42;camera.aspect=sceneMode?1440f/662:1.5f;camera.allowHDR=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.12f,.17f);camera.nearClipPlane=.02f;camera.farClipPlane=sceneMode?1000:30;if(camera.GetComponent<UniversalAdditionalCameraData>()==null)camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                if(sceneMode){habitatRoot=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/CoralBay"));habitat=habitatRoot.GetComponent<MarineHabitat>();habitat.Present(camera,false,false,0,true,true,false);FishLightingReview.InitializeSceneBindings(habitatRoot);}
                else{sun=new GameObject("ContourReviewLight").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;sun.transform.rotation=Quaternion.Euler(45,-30,0);}
                target=new RenderTexture(1440,sceneMode?662:960,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};target.Create();camera.targetTexture=target;receipt.samples=target.antiAliasing;receipt.target=target.graphicsFormat.ToString();var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
                Color32[] Draw(string name)
                {
                    RenderPipeline.SubmitRenderRequest(camera,request);var previous=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                    try{RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());receipt.images++;return image.GetPixels32();}
                    finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
                }
                int Changed(Color32[] a,Color32[] b)=>a.Where((p,i)=>!p.Equals(b[i])).Count();
                foreach(int lod in new[]{0,1,2})foreach(float time in new[]{0f,.125f,.25f,.375f})foreach(float yaw in new[]{-25f,155f})
                {
                    root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin"));root.GetComponent<AuthoredFishVisual>().InitializePresentation();root.GetComponent<LODGroup>().ForceLOD(lod);root.transform.rotation=Quaternion.Euler(0,yaw,-3);
                    if(sceneMode){root.transform.position=new Vector3(0,.7f,1.2f);root.transform.localScale=Vector3.one*2.2f;}
                    var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;var renderers=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);foreach(var r in renderers)r.updateWhenOffscreen=true;
                    var original=renderers.Select(r=>r.sharedMaterials).ToArray();var fin=original.SelectMany(m=>m).First(m=>m.name=="Bluefin_Fins");var ray=original.SelectMany(m=>m).First(m=>m.name=="Bluefin_Rays");
                    opaque=new Material(fin);opaque.SetFloat("_Surface",0);BaseShaderGUI.SetupMaterialBlendMode(opaque);
                    twoSided=new Material(fin);twoSided.SetFloat("_Cull",0);
                    noRays=new Material(ray);noRays.SetShaderPassEnabled("UniversalForward",false);noRays.SetShaderPassEnabled("ShadowCaster",false);noRays.SetShaderPassEnabled("DepthOnly",false);
                    void Materials(Material finControl,Material rayControl)
                    {for(int i=0;i<renderers.Length;i++)renderers[i].sharedMaterials=original[i].Select(m=>m==fin?(finControl??m):m==ray?(rayControl??m):m).ToArray();}
                    var clip=animator.runtimeAnimatorController.animationClips.Single(c=>c.name=="swim");clip.SampleAnimation(animator.gameObject,clip.length*time);
                    var active=renderers.Single(r=>r.name.EndsWith("_LOD"+lod));var center=active.bounds.center;if(!sceneMode){camera.transform.position=center+new Vector3(0,.08f,-4.5f);camera.transform.LookAt(center);}
                    yield return null;yield return null;
                    string name="lod"+lod+"-swim"+Mathf.RoundToInt(time*1000)+"-yaw"+Mathf.RoundToInt(yaw);var view=new View{lod=lod,time=time,yaw=yaw,subject=root.transform.position,scale=root.transform.localScale,camera=camera.transform.position};receipt.views.Add(view);
                    var production=Draw(name+"-production");view.repeatPixels=Changed(production,Draw(name+"-production-repeat"));
                    Materials(opaque,null);yield return null;yield return null;view.opaquePixels=Changed(production,Draw(name+"-control-opaque"));
                    Materials(twoSided,null);yield return null;yield return null;view.doubleSidedPixels=Changed(production,Draw(name+"-control-double-sided"));
                    Materials(null,noRays);yield return null;yield return null;view.noRaysPixels=Changed(production,Draw(name+"-control-no-rays"));
                    Materials(null,null);
                    if(sceneMode)
                    {
                        int order=habitat.waterSurface.sortingOrder;bool hidden=habitat.waterSurface.forceRenderingOff;
                        try{habitat.waterSurface.sortingOrder=0;yield return null;yield return null;var late=Draw(name+"-control-water-late");view.waterLatePixels=Changed(production,late);view.waterLateRepeatPixels=Changed(late,Draw(name+"-control-water-late-repeat"));habitat.waterSurface.sortingOrder=-1;yield return null;yield return null;view.waterEarlyPixels=Changed(production,Draw(name+"-control-water-early"));habitat.waterSurface.forceRenderingOff=true;yield return null;yield return null;view.waterAbsentPixels=Changed(production,Draw(name+"-control-water-absent"));}
                        finally{habitat.waterSurface.sortingOrder=order;habitat.waterSurface.forceRenderingOff=hidden;}
                    }
                    foreach(var m in new[]{opaque,twoSided,noRays})UnityEngine.Object.DestroyImmediate(m);opaque=twoSided=noRays=null;
                    root.GetComponent<AuthoredFishVisual>().ReleasePresentation();UnityEngine.Object.DestroyImmediate(root);root=null;
                }
                if(receipt.views.Count!=24||receipt.images!=(sceneMode?216:120))throw new InvalidOperationException("Incomplete diagnosis set");receipt.status=receipt.views.All(v=>v.repeatPixels==0&&v.waterLateRepeatPixels==0)?"PASS":"PARTIAL";
            }
            finally
            {
                if(receipt.status=="RUNNING")receipt.status="FAIL";File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));
                if(root!=null){root.GetComponent<AuthoredFishVisual>().ReleasePresentation();UnityEngine.Object.DestroyImmediate(root);}foreach(var m in new[]{opaque,twoSided,noRays})if(m!=null)UnityEngine.Object.DestroyImmediate(m);
                if(habitatRoot!=null)UnityEngine.Object.DestroyImmediate(habitatRoot);
                if(camera!=null)camera.targetTexture=null;if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(camera!=null)UnityEngine.Object.DestroyImmediate(camera.gameObject);if(sun!=null)UnityEngine.Object.DestroyImmediate(sun.gameObject);QualitySettings.renderPipeline=priorMedium;GraphicsSettings.defaultRenderPipeline=priorDefault;QualitySettings.SetQualityLevel(prior,true);
            }
            if(receipt.status!="PASS")throw new InvalidOperationException("Keep all first/repeated frames and inspect the differences");
        }
    }
}
