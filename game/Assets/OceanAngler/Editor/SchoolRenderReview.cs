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
    public static class SchoolRenderReview
    {
        static IEnumerator run;
        [Serializable] sealed class Receipt
        {
            public string status="PASS",scope="isolated Editor Metal scene with sampled swim poses; no player/UI/device qualification",graphics;
            public int members,images,width=1440,height=900;public float visibleDifference,minimumMovement;
        }
        public static void Capture(){run=CaptureSteps();EditorApplication.update+=Step;}
        static void Step()
        {
            try{if(run.MoveNext())return;EditorApplication.update-=Step;run=null;EditorApplication.Exit(0);}
            catch(Exception e){EditorApplication.update-=Step;(run as IDisposable)?.Dispose();run=null;Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static IEnumerator CaptureSteps()
        {
            string folder=Environment.GetEnvironmentVariable("OCEAN_SCHOOL_REVIEW_OUTPUT");if(string.IsNullOrEmpty(folder))throw new InvalidOperationException("Output required");
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("GPU required");
            EditorSceneManager.OpenScene("Assets/OceanAngler/Generated/Bay.unity");var camera=Camera.main;camera.enabled=false;camera.aspect=1.6f;
            var data=camera.GetComponent<UniversalAdditionalCameraData>()??camera.gameObject.AddComponent<UniversalAdditionalCameraData>();data.requiresColorTexture=true;data.requiresDepthTexture=true;
            RenderSettings.ambientLight=new Color(.2f,.32f,.38f);
            var root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/CoralBay"));var bay=root.GetComponent<BayHabitat>();var school=bay.school;school.Initialize();
            var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/OceanAngler/Art/Bluefin/Bluefin.fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__")&&(c.name=="swim"||c.name.EndsWith("|swim")));
            var start=school.Members.Select(f=>f.transform.localPosition).ToArray();
            foreach(var fish in school.Members){fish.GetComponent<LODGroup>().ForceLOD(0);fish.GetComponentInChildren<Animator>().enabled=false;foreach(var renderer in fish.GetComponentsInChildren<SkinnedMeshRenderer>())renderer.updateWhenOffscreen=true;}
            var target=new RenderTexture(1440,900,24,RenderTextureFormat.ARGB32){antiAliasing=4};target.Create();camera.targetTexture=target;
            var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};var receipt=new Receipt{graphics=SystemInfo.graphicsDeviceType.ToString(),members=school.Members.Length,minimumMovement=float.PositiveInfinity};
            try
            {
                if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new InvalidOperationException("URP unavailable");
                for(int pose=0;pose<2;pose++)
                {
                    if(pose==1)for(int frame=0;frame<180;frame++)school.Present(1f/30,false,false,true);
                    for(int i=0;i<school.Members.Length;i++)clip.SampleAnimation(school.Members[i].GetComponentInChildren<Animator>().gameObject,(pose*.7f+i*.4f)%clip.length);
                    school.Present(0,true,false,true);yield return null;yield return null;
                    for(int view=0;view<3;view++)
                    {
                        bay.Present(camera,true,view==0,view==2?1:0,true,false);yield return null;
                        RenderPipeline.SubmitRenderRequest(camera,request);var pixels=Save(target,Path.Combine(folder,(pose==0?"start-":"later-")+(view==0?"surface":view==1?"underwater":"reef")+".png"));receipt.images++;
                        if(pose==0&&view==1)
                        {
                            school.gameObject.SetActive(false);RenderPipeline.SubmitRenderRequest(camera,request);var control=Save(target,Path.Combine(folder,"underwater-no-school.png"));receipt.images++;
                            double diff=0;for(int i=0;i<pixels.Length;i++)diff+=Math.Abs(pixels[i].r-control[i].r)+Math.Abs(pixels[i].g-control[i].g)+Math.Abs(pixels[i].b-control[i].b);receipt.visibleDifference=(float)(diff/(pixels.Length*765d));school.gameObject.SetActive(true);yield return null;yield return null;
                        }
                    }
                }
                for(int i=0;i<start.Length;i++)receipt.minimumMovement=Mathf.Min(receipt.minimumMovement,Vector3.Distance(start[i],school.Members[i].transform.localPosition));
                if(receipt.members!=3||receipt.visibleDifference<.00001f||receipt.minimumMovement<.5f)throw new InvalidOperationException("School was not visibly rendered/moving in the sampled scene");
                File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));Debug.Log("SCHOOL_GPU_PASS");
            }
            finally{school.ReleasePresentation();camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(root);}
        }
        static Color32[] Save(RenderTexture source,string path)
        {
            var prior=RenderTexture.active;var image=new Texture2D(source.width,source.height,TextureFormat.RGB24,false);
            try{RenderTexture.active=source;image.ReadPixels(new Rect(0,0,source.width,source.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());return image.GetPixels32();}
            finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
