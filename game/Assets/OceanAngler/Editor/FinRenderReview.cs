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
    // Background/opaque controls are diagnostics on private material clones.
    // Only the production material views can be presented as the candidate.
    public static class FinRenderReview
    {
        [Serializable] sealed class Sample {public int lod,interiorPixels,transmittedPixels,opaqueChangedPixels,staticChangedPixels;public float meanBackgroundTransfer;}
        [Serializable] sealed class Receipt {public string status="IN_PROGRESS",scope="isolated imported-hero Metal HDR fixture; not Player UI, biological transmission or device qualification",graphics,target,quality;public int samples,width=1440,height=960,images;public List<Sample> measurements=new List<Sample>();}
        static IEnumerator job;
        public static void Capture(){job=Run();EditorApplication.update+=Step;}
        static void Step()
        {
            try{if(job.MoveNext())return;EditorApplication.update-=Step;job=null;EditorApplication.Exit(0);}
            catch(Exception e){EditorApplication.update-=Step;(job as IDisposable)?.Dispose();job=null;Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static IEnumerator Run()
        {
            string folder=Environment.GetEnvironmentVariable("OCEAN_FIN_REVIEW_OUTPUT");if(string.IsNullOrEmpty(folder))throw new InvalidOperationException("Review folder required");
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("GPU required");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            int quality=Array.IndexOf(QualitySettings.names,"Medium");if(quality<0)throw new InvalidOperationException("Production Medium quality missing");
            int previousQuality=QualitySettings.GetQualityLevel();var previousDefault=GraphicsSettings.defaultRenderPipeline;QualitySettings.SetQualityLevel(quality,true);var previousMedium=QualitySettings.renderPipeline;
            try
            {
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OceanAngler/Generated/OceanURP.asset");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
            if(pipeline.msaaSampleCount!=4)throw new InvalidOperationException("Keep production four-sample pipeline");
            RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.32f,.40f,.43f);
            var camera=new GameObject("FinReviewCamera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.aspect=1.5f;camera.allowHDR=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.nearClipPlane=.02f;camera.farClipPlane=30;camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            var sun=new GameObject("FinReviewSun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;sun.transform.rotation=Quaternion.Euler(45,-30,0);
            var target=new RenderTexture(1440,960,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};target.Create();camera.targetTexture=target;
            var receipt=new Receipt{graphics=SystemInfo.graphicsDeviceType.ToString(),target=target.graphicsFormat.ToString(),quality=QualitySettings.names[quality],samples=target.antiAliasing};
            var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};Color dark=new Color(.03f,.06f,.07f),light=new Color(.50f,.55f,.56f);
            Color32[] Draw(string name,Color background)
            {
                camera.backgroundColor=background;RenderPipeline.SubmitRenderRequest(camera,request);receipt.images++;var previous=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                try{RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());return image.GetPixels32();}
                finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
            }
            try
            {
                if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new InvalidOperationException("URP render request unavailable");
                for(int lod=0;lod<3;lod++)
                {
                    var root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin"));Material control=null;var renderers=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);var original=renderers.Select(r=>r.sharedMaterials).ToArray();
                    try
                    {
                        root.GetComponent<LODGroup>().ForceLOD(lod);root.GetComponent<AuthoredFishVisual>().InitializePresentation();var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;
                        foreach(var renderer in renderers)renderer.updateWhenOffscreen=true;
                        var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/OceanAngler/Art/Bluefin/Bluefin.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();clips.Single(c=>c.name=="idle"||c.name.EndsWith("|idle")).SampleAnimation(animator.gameObject,0);
                        yield return null;yield return null;
                        var bounds=renderers.Single(r=>r.name.EndsWith("_LOD"+lod)).bounds;Vector3 center=bounds.center;camera.orthographicSize=Mathf.Max(bounds.extents.y*1.2f,bounds.extents.x/camera.aspect*1.2f);camera.transform.position=center+new Vector3(0,.1f,-6);camera.transform.LookAt(center);
                        var a=Draw("lod"+lod+"-production-dark",dark);var repeated=Draw("lod"+lod+"-production-dark-repeat",dark);var b=Draw("lod"+lod+"-production-light",light);
                        var source=original.SelectMany(m=>m).First(m=>m.name=="Bluefin_Fins");control=new Material(source);control.SetFloat("_Surface",0);BaseShaderGUI.SetupMaterialBlendMode(control);
                        for(int r=0;r<renderers.Length;r++){var materials=original[r].Select(m=>m==source?control:m).ToArray();renderers[r].sharedMaterials=materials;}
                        yield return null;yield return null;
                        var opaqueA=Draw("lod"+lod+"-diagnostic-opaque-dark",dark);var opaqueB=Draw("lod"+lod+"-diagnostic-opaque-light",light);
                        if(control.FindPass("ForwardLit")<0)throw new InvalidOperationException("Expected stock Lit forward pass");control.SetShaderPassEnabled("UniversalForward",false);yield return null;yield return null;var absent=Draw("lod"+lod+"-diagnostic-no-membrane",dark);
                        var sample=new Sample{lod=lod};var mask=new bool[a.Length];
                        int Difference(Color32 x,Color32 y)=>Mathf.Max(Mathf.Abs(x.r-y.r),Mathf.Max(Mathf.Abs(x.g-y.g),Mathf.Abs(x.b-y.b)));
                        for(int i=0;i<a.Length;i++){mask[i]=Difference(opaqueA[i],absent[i])>3;if(Difference(a[i],repeated[i])!=0)sample.staticChangedPixels++;}
                        for(int y=1;y<target.height-1;y++)for(int x=1;x<target.width-1;x++)
                        {
                            int i=y*target.width+x;bool interior=true;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)interior&=mask[i+dy*target.width+dx];if(!interior)continue;sample.interiorPixels++;
                            if(Difference(opaqueA[i],opaqueB[i])>2)sample.opaqueChangedPixels++;
                            if(Difference(a[i],b[i])>8){sample.transmittedPixels++;sample.meanBackgroundTransfer+=Difference(a[i],b[i])/125f;}
                        }
                        sample.meanBackgroundTransfer/=Mathf.Max(1,sample.transmittedPixels);receipt.measurements.Add(sample);
                        if(sample.staticChangedPixels!=0||sample.interiorPixels<300||sample.transmittedPixels<100||sample.opaqueChangedPixels>sample.interiorPixels*.001f)throw new InvalidOperationException("Fin control/stability failed LOD"+lod+": "+JsonUtility.ToJson(sample));
                        for(int r=0;r<renderers.Length;r++)renderers[r].sharedMaterials=original[r];UnityEngine.Object.DestroyImmediate(control);control=null;yield return null;yield return null;
                        if(lod==0)
                        {
                            foreach(var view in new[]{("reverse",new Vector3(0,.1f,6)),("quarter",new Vector3(-3,1.2f,-5)),("edge",new Vector3(-6,.1f,.2f))}){camera.transform.position=center+view.Item2;camera.transform.LookAt(center);Draw("production-"+view.Item1,dark);}
                        }
                    }
                    finally{for(int r=0;r<renderers.Length;r++)renderers[r].sharedMaterials=original[r];if(control!=null)UnityEngine.Object.DestroyImmediate(control);root.GetComponent<AuthoredFishVisual>().ReleasePresentation();UnityEngine.Object.DestroyImmediate(root);}
                }
                receipt.status="PASS";Debug.Log("FIN_TISSUE_GPU_PASS");
            }
            finally
            {
                if(receipt.status!="PASS")receipt.status="FAIL";File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(sun.gameObject);
            }
            }
            finally{QualitySettings.renderPipeline=previousMedium;GraphicsSettings.defaultRenderPipeline=previousDefault;QualitySettings.SetQualityLevel(previousQuality,true);}
        }
    }
}
