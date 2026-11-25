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
    // Discrete samples of the actual runtime clips and imported prefab. No
    // material substitution, remapping, warm-frame discard or Player claim.
    public static class BluefinGillRenderReview
    {
        [Serializable] sealed class Sample
        {public string motion,side;public int lod,gillVertices,repeatChangedPixels;public float normalizedTime,minGillViewportMargin;}
        [Serializable] sealed class Receipt
        {
            public string status="RUNNING",graphics,platform,target;
            public string scope="Editor GPU discrete runtime-clip head/gill views; not continuous Animator/Player, natural motion, final art, science or physical acceptance";
            public int images,samples;public List<Sample> views=new List<Sample>();
        }
        static IEnumerator job;
        public static void Capture(){job=Run();EditorApplication.update+=Step;}
        static void Step()
        {
            try{if(job.MoveNext())return;EditorApplication.update-=Step;job=null;EditorApplication.Exit(0);}
            catch(Exception e){EditorApplication.update-=Step;(job as IDisposable)?.Dispose();job=null;Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static IEnumerator Run()
        {
            string folder=Environment.GetEnvironmentVariable("OCEAN_GILL_REVIEW_OUTPUT");
            if(string.IsNullOrEmpty(folder)||SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("New output folder and real GPU required");
            int priorQuality=QualitySettings.GetQualityLevel(),medium=Array.IndexOf(QualitySettings.names,"Medium");
            if(medium<0)throw new InvalidOperationException("Production quality required");
            var priorDefault=GraphicsSettings.defaultRenderPipeline;QualitySettings.SetQualityLevel(medium,true);var priorMedium=QualitySettings.renderPipeline;
            Camera camera=null;Light light=null;RenderTexture target=null;GameObject root=null;Mesh baked=null;
            var receipt=new Receipt{graphics=SystemInfo.graphicsDeviceType.ToString(),platform=EditorUserBuildSettings.activeBuildTarget.ToString()};
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OceanAngler/Generated/OceanURP.asset");
                if(pipeline==null||pipeline.msaaSampleCount!=4)throw new InvalidOperationException("Production four-sample HDR pipeline required");
                GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
                RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.32f,.40f,.43f);
                camera=new GameObject("GillReviewCamera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=.58f;camera.aspect=1.5f;camera.allowHDR=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.075f,.085f);camera.nearClipPlane=.02f;camera.farClipPlane=30;camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                light=new GameObject("GillReviewLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(45,-30,0);
                target=new RenderTexture(1440,960,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};target.Create();camera.targetTexture=target;receipt.samples=target.antiAliasing;receipt.target=target.graphicsFormat.ToString();
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
                Color32[] Draw(string name)
                {
                    RenderPipeline.SubmitRenderRequest(camera,request);receipt.images++;var previous=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                    try{RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());return image.GetPixels32();}
                    finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
                }
                var imported=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/OceanAngler/Art/Bluefin/Bluefin.fbx").GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach(int lod in new[]{0,1,2})foreach(string motion in new[]{"idle","swim","turn","burst","bite","hooked","release"})foreach(float time in new[]{.05f,.5f,.95f})
                {
                    root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin"));root.GetComponent<AuthoredFishVisual>().InitializePresentation();root.GetComponent<LODGroup>().ForceLOD(lod);
                    var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;
                    var renderer=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name.EndsWith("_LOD"+lod));
                    var source=imported.Single(r=>r.name==renderer.name&&r.sharedMesh==renderer.sharedMesh);
                    if(!source.sharedMaterials.SequenceEqual(renderer.sharedMaterials))throw new InvalidOperationException("Saved prefab material order differs from current imported submeshes");
                    foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))skin.updateWhenOffscreen=true;
                    var clip=animator.runtimeAnimatorController.animationClips.Single(c=>c.name==motion);clip.SampleAnimation(animator.gameObject,clip.length*time);
                    yield return null;yield return null;
                    baked=new Mesh();renderer.BakeMesh(baked,true);var vertices=baked.vertices;
                    int slot=Array.FindIndex(renderer.sharedMaterials,m=>m.name=="Bluefin_Gill");if(slot<0)throw new InvalidOperationException("Gill material missing");
                    var indices=renderer.sharedMesh.GetTriangles(slot).Distinct().ToArray();
                    foreach(int side in new[]{-1,1})
                    {
                        var look=root.transform.TransformPoint(new Vector3(-.66f,.025f,0));camera.transform.SetPositionAndRotation(look+new Vector3(0,.08f,side*5),Quaternion.LookRotation(new Vector3(0,-.08f,-side*5),Vector3.up));
                        var selected=indices.Select(i=>renderer.transform.TransformPoint(vertices[i])).Where(p=>side*p.z>0).ToArray();
                        var sample=new Sample{motion=motion,side=side<0?"near":"opposite",lod=lod,normalizedTime=time,gillVertices=selected.Length,minGillViewportMargin=1};receipt.views.Add(sample);
                        foreach(var point in selected)
                        {
                            var v=camera.WorldToViewportPoint(point);if(v.z<=camera.nearClipPlane)throw new InvalidOperationException("Gill geometry behind camera");
                            sample.minGillViewportMargin=Mathf.Min(sample.minGillViewportMargin,v.x,v.y,1-v.x,1-v.y);
                        }
                        if(selected.Length<10||sample.minGillViewportMargin<.05f)throw new InvalidOperationException("Gill close-up does not include the complete selected side");
                        string name="lod"+lod+"-"+motion+"-"+Mathf.RoundToInt(time*100).ToString("D2")+"-"+sample.side;
                        var first=Draw(name+"-first");var repeat=Draw(name+"-repeat");sample.repeatChangedPixels=first.Where((p,i)=>!p.Equals(repeat[i])).Count();
                        if(sample.repeatChangedPixels!=0)throw new InvalidOperationException("First/repeated static draw differs: "+name);
                    }
                    UnityEngine.Object.DestroyImmediate(baked);baked=null;root.GetComponent<AuthoredFishVisual>().ReleasePresentation();UnityEngine.Object.DestroyImmediate(root);root=null;
                }
                if(receipt.views.Count!=126||receipt.images!=252)throw new InvalidOperationException("Incomplete view set");receipt.status="PASS";
            }
            finally
            {
                if(receipt.status!="PASS")receipt.status="FAIL";File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));
                if(root!=null){root.GetComponent<AuthoredFishVisual>().ReleasePresentation();UnityEngine.Object.DestroyImmediate(root);}if(baked!=null)UnityEngine.Object.DestroyImmediate(baked);
                if(camera!=null)camera.targetTexture=null;if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(camera!=null)UnityEngine.Object.DestroyImmediate(camera.gameObject);if(light!=null)UnityEngine.Object.DestroyImmediate(light.gameObject);
                QualitySettings.renderPipeline=priorMedium;GraphicsSettings.defaultRenderPipeline=priorDefault;QualitySettings.SetQualityLevel(priorQuality,true);
            }
        }
    }
}
