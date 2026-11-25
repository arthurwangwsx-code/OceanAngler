using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace OceanAngler.Editor
{
    // Isolated imported model review. No game UI, native device or art approval.
    public static class HalibutRenderReview
    {
        static IEnumerator run;
        public static void Capture(){run=RunCapture();EditorApplication.update+=Step;}
        static void Step()
        {
            try{if(run.MoveNext())return;EditorApplication.update-=Step;run=null;EditorApplication.Exit(0);}
            catch(Exception e){EditorApplication.update-=Step;(run as IDisposable)?.Dispose();run=null;Debug.LogException(e);EditorApplication.Exit(1);}
        }
        [Serializable] sealed class Receipt
        {
            public string status="PASS",scope="isolated imported halibut Editor Metal; not Player, science, visual or device acceptance",graphics;
            public int images,pairs;public int maxChannelDifference;public bool firstDrawStable=true;public float biteHookTravel;
        }
        static IEnumerator RunCapture()
        {
            string folder=Environment.GetEnvironmentVariable("OCEAN_HALIBUT_REVIEW_OUTPUT");if(string.IsNullOrEmpty(folder))throw new InvalidOperationException("Output required");
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("GPU required");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OceanAngler/Generated/OceanURP.asset");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
            RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.32f,.40f,.43f);
            var camera=new GameObject("HalibutReviewCamera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.aspect=1.5f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.075f,.085f);camera.nearClipPlane=.02f;camera.farClipPlane=30;camera.allowHDR=true;camera.allowMSAA=true;camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            var sun=new GameObject("HalibutReviewSun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;sun.transform.rotation=Quaternion.Euler(45,-30,0);
            var target=new RenderTexture(1440,960,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};target.Create();camera.targetTexture=target;var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};var receipt=new Receipt{graphics=SystemInfo.graphicsDeviceType.ToString()};
            try
            {
                if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new InvalidOperationException("URP request unavailable");
                for(int lod=0;lod<3;lod++)
                {
                    var root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Halibut"));
                    try
                    {
                        root.GetComponent<LODGroup>().ForceLOD(lod);var visual=root.GetComponent<AuthoredFishVisual>();visual.InitializePresentation();var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>())r.updateWhenOffscreen=true;
                        var idle=animator.runtimeAnimatorController.animationClips.Single(c=>c.name=="idle");idle.SampleAnimation(animator.gameObject,0);yield return null;yield return null;
                        View("lod"+lod+"-eyed",new Vector3(.15f,5,0),new Vector3(.15f,0,0),Vector3.forward,.98f);
                        View("lod"+lod+"-blind",new Vector3(.15f,-5,0),new Vector3(.15f,0,0),Vector3.back,.98f);
                        View("lod"+lod+"-oblique",new Vector3(-1,3,-4),new Vector3(.15f,0,0),Vector3.up,1.05f);
                        if(lod==0)
                        {
                            View("eyes",new Vector3(-.74f,5,-.12f),new Vector3(-.74f,.06f,-.12f),Vector3.forward,.15f);
                            Vector3 closed=visual.HookPosition;animator.runtimeAnimatorController.animationClips.Single(c=>c.name=="bite").SampleAnimation(animator.gameObject,.5f);receipt.biteHookTravel=Vector3.Distance(closed,visual.HookPosition);yield return null;yield return null;
                            View("bite-oblique",new Vector3(-1,3,-4),new Vector3(.15f,0,0),Vector3.up,1.05f);
                            View("bite-mouth",new Vector3(-4,1,-2),new Vector3(-1,.02f,0),Vector3.up,.18f);
                        }
                        void View(string name,Vector3 from,Vector3 look,Vector3 up,float size)
                        {
                            camera.transform.SetPositionAndRotation(from,Quaternion.LookRotation(look-from,up));camera.orthographicSize=size;
                            RenderPipeline.SubmitRenderRequest(camera,request);var first=Save(target,Path.Combine(folder,"first-"+name+".png"));
                            RenderPipeline.SubmitRenderRequest(camera,request);var second=Save(target,Path.Combine(folder,name+".png"));int max=0;
                            for(int i=0;i<first.Length;i++)max=Math.Max(max,Math.Max(Math.Abs(first[i].r-second[i].r),Math.Max(Math.Abs(first[i].g-second[i].g),Math.Abs(first[i].b-second[i].b))));
                            receipt.images+=2;receipt.pairs++;receipt.maxChannelDifference=Math.Max(receipt.maxChannelDifference,max);receipt.firstDrawStable&=max==0;
                        }
                    }
                    finally{root.GetComponent<AuthoredFishVisual>().ReleasePresentation();UnityEngine.Object.DestroyImmediate(root);}
                }
                if(!receipt.firstDrawStable||receipt.biteHookTravel<.05f)receipt.status="FAIL";File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));if(receipt.status!="PASS")throw new InvalidOperationException("Imported pose or first draw failed");Debug.Log("HALIBUT_GPU_REVIEW_PASS");
            }
            finally{camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(sun.gameObject);}
        }
        static Color32[] Save(RenderTexture source,string path)
        {
            var prior=RenderTexture.active;var image=new Texture2D(source.width,source.height,TextureFormat.RGB24,false);
            try{RenderTexture.active=source;image.ReadPixels(new Rect(0,0,source.width,source.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());return image.GetPixels32();}
            finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
