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
    public static class RigRenderReview
    {
        [Serializable] sealed class Shot {public string name;public int changedPixels;}
        [Serializable] sealed class Receipt {public string status="IN_PROGRESS",scope="isolated imported rig Metal HDR/manual presentation; not Player UI or device qualification",graphics,target,quality;public int width=1440,height=1000,samples,images;public List<Shot> shots=new List<Shot>();}
        static IEnumerator job;
        public static void Capture(){job=Run();EditorApplication.update+=Step;}
        static void Step()
        {
            try{if(job.MoveNext())return;EditorApplication.update-=Step;job=null;EditorApplication.Exit(0);}
            catch(Exception e){EditorApplication.update-=Step;(job as IDisposable)?.Dispose();job=null;Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static IEnumerator Run()
        {
            string folder=Environment.GetEnvironmentVariable("OCEAN_RIG_REVIEW_OUTPUT");if(string.IsNullOrEmpty(folder)||SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("GPU and output folder required");
            int quality=Array.IndexOf(QualitySettings.names,"Medium"),previousQuality=QualitySettings.GetQualityLevel();if(quality<0)throw new InvalidOperationException("Medium quality missing");var previousDefault=GraphicsSettings.defaultRenderPipeline;QualitySettings.SetQualityLevel(quality,true);var previousMedium=QualitySettings.renderPipeline;
            GameObject root=null,camObject=null,sunObject=null;RenderTexture target=null;var receipt=new Receipt{graphics=SystemInfo.graphicsDeviceType.ToString(),quality="Medium",samples=4};
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OceanAngler/Generated/OceanURP.asset");if(pipeline.msaaSampleCount!=4)throw new InvalidOperationException("Preserve four-sample production pipeline");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
                RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.38f,.43f,.46f);
                camObject=new GameObject("RigReviewCamera");var camera=camObject.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.aspect=1.44f;camera.allowHDR=true;camera.nearClipPlane=.005f;camera.farClipPlane=30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.03f,.06f,.07f);camObject.AddComponent<UniversalAdditionalCameraData>();
                sunObject=new GameObject("RigReviewSun");var sun=sunObject.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.4f;sun.transform.rotation=Quaternion.Euler(40,-35,0);
                target=new RenderTexture(1440,1000,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};target.Create();camera.targetTexture=target;receipt.target=target.graphicsFormat.ToString();receipt.samples=target.antiAliasing;var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
                if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new InvalidOperationException("URP render request unavailable");
                Color32[] Draw(string name)
                {
                    RenderPipeline.SubmitRenderRequest(camera,request);var previous=RenderTexture.active;var tex=new Texture2D(1440,1000,TextureFormat.RGB24,false);
                    try{RenderTexture.active=target;tex.ReadPixels(new Rect(0,0,1440,1000),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),tex.EncodeToPNG());receipt.images++;return tex.GetPixels32();}
                    finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(tex);}
                }
                int Difference(Color32 a,Color32 b)=>Mathf.Max(Mathf.Abs(a.r-b.r),Mathf.Max(Mathf.Abs(a.g-b.g),Mathf.Abs(a.b-b.b)));
                void Pair(string name)
                {
                    var first=Draw(name+"-first");var repeat=Draw(name+"-repeat");int changes=0;for(int i=0;i<first.Length;i++)if(Difference(first[i],repeat[i])!=0)changes++;
                    receipt.shots.Add(new Shot{name=name,changedPixels=changes});if(changes!=0)throw new InvalidOperationException("Rig first/repeat mismatch: "+name+"="+changes);
                }
                for(int lod=0;lod<3;lod++)
                {
                    root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/SpinningRig"));var rig=root.GetComponent<AuthoredSpinningRig>();rig.enabled=false;rig.InitializeMechanisms();root.GetComponent<LODGroup>().ForceLOD(lod);rig.Drive(0,false,false,false,false,Vector3.zero,true);rig.Present(0);
                    var mechanical=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.EndsWith("_LOD"+lod)&&!r.name.StartsWith("Rod_")&&!r.name.StartsWith("Lure_")).ToArray();var bounds=mechanical[0].bounds;foreach(var r in mechanical)bounds.Encapsulate(r.bounds);
                    var center=bounds.center;float size=Mathf.Max(bounds.extents.y,bounds.extents.x/camera.aspect)*1.4f;camera.orthographicSize=size;camera.transform.position=center+new Vector3(-.32f,.2f,-.55f);camera.transform.LookAt(center);yield return null;yield return null;
                    var first=Draw("lod"+lod+"-rest-first");var repeat=Draw("lod"+lod+"-rest-repeat");int changes=0;for(int i=0;i<first.Length;i++)if(Difference(first[i],repeat[i])!=0)changes++;receipt.shots.Add(new Shot{name="lod"+lod+"-rest",changedPixels=changes});if(changes!=0)throw new InvalidOperationException("Initial rig draw differs");
                    if(lod==0)
                    {
                        root.SetActive(false);yield return null;yield return null;var empty=Draw("diagnostic-empty");int coverage=0;for(int i=0;i<first.Length;i++)if(Difference(first[i],empty[i])>3)coverage++;if(coverage<1000)throw new InvalidOperationException("Rig pixels missing");root.SetActive(true);yield return null;yield return null;
                    }
                    rig.Drive(.4f,true,false,false,false,Vector3.zero,true);rig.Present(.07f);yield return null;yield return null;Pair("lod"+lod+"-winding-a");
                    rig.Present(.083f);yield return null;yield return null;Pair("lod"+lod+"-winding-b");
                    var rotation=rig.roller.localRotation;rig.Drive(.4f,true,true,false,false,Vector3.zero,true);rig.Present(.1f);if(Quaternion.Angle(rotation,rig.roller.localRotation)>.01f)throw new InvalidOperationException("Paused roller moved");
                    rig.Drive(0,false,false,false,true,Vector3.zero,true);for(int i=0;i<3;i++)rig.Present(.1f);if(rig.LinePassesRoller||Mathf.Abs(rig.BailOpenDegrees-82)>.01f)throw new InvalidOperationException("Open-bail payout not active");yield return null;yield return null;Pair("lod"+lod+"-open-bail");
                    rig.Drive(0,false,false,false,false,Vector3.zero,true);for(int i=0;i<3;i++)rig.Present(.1f);yield return null;yield return null;
                    if(lod==0)
                    {
                        camera.transform.position=center+new Vector3(.32f,.1f,.55f);camera.transform.LookAt(center);Pair("production-reverse");
                        var axis=(rig.rollerAxis.position-rig.roller.position).normalized;camera.orthographicSize=.024f;camera.transform.position=rig.roller.position+axis*.07f+new Vector3(0,.035f,-.10f);camera.transform.LookAt(rig.roller.position);Pair("production-roller");
                        camera.orthographicSize=1.5f;camera.transform.position=new Vector3(-1.3f,1.6f,-3.5f);camera.transform.LookAt(new Vector3(0,1.17f,0));Pair("production-whole");
                    }
                    rig.ReleasePresentation();UnityEngine.Object.DestroyImmediate(root);root=null;
                }
                receipt.status="PASS";Debug.Log("RIG_DETAIL_GPU_PASS");
            }
            finally
            {
                if(receipt.status!="PASS")receipt.status="FAIL";File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));
                if(root!=null){root.GetComponent<AuthoredSpinningRig>().ReleasePresentation();UnityEngine.Object.DestroyImmediate(root);}if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(camObject!=null)UnityEngine.Object.DestroyImmediate(camObject);if(sunObject!=null)UnityEngine.Object.DestroyImmediate(sunObject);
                QualitySettings.renderPipeline=previousMedium;GraphicsSettings.defaultRenderPipeline=previousDefault;QualitySettings.SetQualityLevel(previousQuality,true);
            }
        }
    }
}
