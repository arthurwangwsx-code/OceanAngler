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
    // Imported prefab/material/pose diagnostics; no player UI or device claims.
    public static class FishRenderReview
    {
        static IEnumerator run;
        public static void Capture(){run=RunCapture();EditorApplication.update+=Step;}
        static void Step()
        {
            try{if(run.MoveNext())return;EditorApplication.update-=Step;run=null;EditorApplication.Exit(0);}
            catch(Exception e){EditorApplication.update-=Step;(run as IDisposable)?.Dispose();run=null;Debug.LogException(e);EditorApplication.Exit(1);}
        }
        [Serializable] sealed class Receipt {public string status="PASS",scope="isolated imported-fish Editor GPU fixture; no player UI/device acceptance",graphics,initialPassScope="Production material initialization; first/repeated isolated draw checked; no player-startup claim";public int width=1440,height=960,images,initialPassImages,irisTextureWidth;public float importedJawAngle,idleInitialDifference,biteInitialDifference;public bool singleSidedLining,firstDrawStable=true;}
        static IEnumerator RunCapture()
        {
            string folder=Environment.GetEnvironmentVariable("OCEAN_FISH_REVIEW_OUTPUT");if(string.IsNullOrEmpty(folder))throw new InvalidOperationException("Review output required");
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("GPU required");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OceanAngler/Generated/OceanURP.asset");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
            RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.32f,.40f,.43f);
            var camera=new GameObject("FishReviewCamera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.aspect=1.5f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.075f,.085f);camera.nearClipPlane=.02f;camera.farClipPlane=30;
            camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            var sun=new GameObject("FishReviewSun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;sun.transform.rotation=Quaternion.Euler(45,-30,0);
            var target=new RenderTexture(1440,960,24,RenderTextureFormat.ARGB32){antiAliasing=4};target.Create();camera.targetTexture=target;
            var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};var receipt=new Receipt{graphics=SystemInfo.graphicsDeviceType.ToString()};Quaternion closed=Quaternion.identity;
            try
            {
                if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new InvalidOperationException("URP request unavailable");
                foreach(string pose in new[]{"idle","bite"})
                {
                    // Fresh renderer per pose avoids reusing skinning results from
                    // two samples within the same Editor frame.
                    var root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin"));
                    try
                    {
                        root.GetComponent<LODGroup>().ForceLOD(0);var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;
                        root.GetComponent<AuthoredFishVisual>().InitializePresentation();
                        foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())renderer.updateWhenOffscreen=true;
                        var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/OceanAngler/Art/Bluefin/Bluefin.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).Single(c=>c.name==pose||c.name.EndsWith("|"+pose));clip.SampleAnimation(animator.gameObject,pose=="bite"?.5f:0);
                        var jaw=root.GetComponentsInChildren<Transform>().Single(t=>t.name=="jaw");
                        if(pose=="idle")closed=jaw.localRotation;else receipt.importedJawAngle=Quaternion.Angle(closed,jaw.localRotation);
                        var lining=root.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m.name.Replace(" (Instance)","")=="Bluefin_Oral");
                        if(lining==null)throw new InvalidOperationException("Imported oral material missing");receipt.singleSidedLining=lining.GetFloat("_Cull")==2;
                        if(!receipt.singleSidedLining)throw new InvalidOperationException("Lining must retain back-face culling");
                        var iris=root.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m.name=="Bluefin_Iris");
                        var irisMap=iris!=null?iris.GetTexture("_BaseMap"):null;
                        if(irisMap==null||irisMap.width!=256)throw new InvalidOperationException("Imported iris texture missing or unexpected size");receipt.irisTextureWidth=irisMap.width;
                        // Sampling during executeMethod can precede the Editor's
                        // renderer/material update. Advance real Editor updates,
                        // without drawing and discarding a defective first image.
                        yield return null;yield return null;
                        View("side",new Vector3(0,.08f,-5),new Vector3(.15f,0,0),.92f);
                        View("mouth",new Vector3(-1.3f,.05f,-5),new Vector3(-.94f,-.045f,0),.22f);
                        View("fin",new Vector3(-.1f,.55f,-5),new Vector3(-.05f,.38f,0),.43f);
                        if(pose=="idle")
                        {
                            View("eye",new Vector3(-.825f,.105f,-5),new Vector3(-.825f,.105f,0),.067f);
                            View("eye-opposite",new Vector3(-.825f,.105f,5),new Vector3(-.825f,.105f,0),.067f);
                        }
                        void View(string name,Vector3 from,Vector3 look,float size)
                        {
                            camera.transform.SetPositionAndRotation(from,Quaternion.LookRotation(look-from,Vector3.up));camera.orthographicSize=size;
                            Color32[] first=null;
                            if(name=="side"){RenderPipeline.SubmitRenderRequest(camera,request);first=Save(target,Path.Combine(folder,"initial-"+pose+"-side.png"));receipt.initialPassImages++;}
                            RenderPipeline.SubmitRenderRequest(camera,request);var pixels=Save(target,Path.Combine(folder,pose+"-"+name+".png"));receipt.images++;
                            if(first!=null)
                            {
                                double difference=0;for(int i=0;i<pixels.Length;i++)difference+=Math.Abs(pixels[i].r-first[i].r)+Math.Abs(pixels[i].g-first[i].g)+Math.Abs(pixels[i].b-first[i].b);
                                float value=(float)(difference/(pixels.Length*765d));if(pose=="idle")receipt.idleInitialDifference=value;else receipt.biteInitialDifference=value;
                                receipt.firstDrawStable&=value<.00001f;
                            }
                        }
                    }
                    finally{root.GetComponent<AuthoredFishVisual>().ReleasePresentation();UnityEngine.Object.DestroyImmediate(root);}
                }
                if(receipt.importedJawAngle<5)throw new InvalidOperationException("Imported bite was not actually sampled");
                if(!receipt.firstDrawStable){receipt.status="FAIL";File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));throw new InvalidOperationException("First draw differs from same-pose repeat; do not replace it with a warm capture");}
                File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));Debug.Log("FISH_GPU_REVIEW_PASS");
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
