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
    public static class BluefinPectoralRenderReview
    {
        [Serializable] sealed class View {public string motion,side;public int lod,activeRepeatPixels,heldRepeatPixels,ablationPixels;public float normalizedTime,minViewportMargin;}
        [Serializable] sealed class Receipt
        {
            public string status="RUNNING",graphics,target,platform,scope="Editor GPU actual runtime clips with new-control bind-pose ablation; discrete samples, not continuous Animator/Player, runtime FPS, measured fish biomechanics or final art acceptance";
            public int images,samples;public List<View> views=new List<View>();
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
            string folder=Environment.GetEnvironmentVariable("OCEAN_PECTORAL_MOTION_OUTPUT");
            if(string.IsNullOrEmpty(folder)||SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("New output and GPU required");
            int prior=QualitySettings.GetQualityLevel(),medium=Array.IndexOf(QualitySettings.names,"Medium");if(medium<0)throw new InvalidOperationException("Production Medium required");
            var priorDefault=GraphicsSettings.defaultRenderPipeline;QualitySettings.SetQualityLevel(medium,true);var priorMedium=QualitySettings.renderPipeline;
            GameObject root=null;Camera camera=null;Light sun=null;RenderTexture target=null;Mesh mesh=null;
            var receipt=new Receipt{graphics=SystemInfo.graphicsDeviceType.ToString(),platform=EditorUserBuildSettings.activeBuildTarget.ToString()};
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OceanAngler/Generated/OceanURP.asset");if(pipeline==null||pipeline.msaaSampleCount!=4)throw new InvalidOperationException("Production four-sample HDR pipeline required");
                GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.32f,.4f,.43f);
                camera=new GameObject("PectoralReviewCamera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1.15f;camera.aspect=1.5f;camera.allowHDR=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.075f,.085f);camera.nearClipPlane=.02f;camera.farClipPlane=30;camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                sun=new GameObject("PectoralReviewLight").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;sun.transform.rotation=Quaternion.Euler(45,-30,0);
                target=new RenderTexture(1440,960,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};target.Create();camera.targetTexture=target;receipt.samples=target.antiAliasing;receipt.target=target.graphicsFormat.ToString();var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
                Color32[] Draw(string name)
                {
                    RenderPipeline.SubmitRenderRequest(camera,request);var previous=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                    try{RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());receipt.images++;return image.GetPixels32();}
                    finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
                }
                int Changed(Color32[] a,Color32[] b)=>a.Where((p,i)=>!p.Equals(b[i])).Count();
                foreach(int lod in new[]{0,1,2})foreach(string motion in new[]{"idle","swim","turn","burst","bite","hooked","release"})foreach(float time in new[]{.25f,.75f})
                {
                    root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin"));root.GetComponent<AuthoredFishVisual>().InitializePresentation();root.GetComponent<LODGroup>().ForceLOD(lod);
                    var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;var renderers=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);foreach(var r in renderers)r.updateWhenOffscreen=true;
                    var reference=renderers.Single(r=>r.name.EndsWith("_LOD0"));var active=renderers.Single(r=>r.name.EndsWith("_LOD"+lod));
                    var controls=new[]{"pectoralLMid","pectoralLTip","pectoralRMid","pectoralRTip"}.Select(n=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==n)).ToArray();
                    var rest=controls.Select(b=>
                    {
                        int i=Array.IndexOf(reference.bones,b),p=Array.IndexOf(reference.bones,b.parent);if(i<0||p<0)throw new InvalidOperationException("Actual bind matrices required");return reference.sharedMesh.bindposes[p]*reference.sharedMesh.bindposes[i].inverse;
                    }).ToArray();
                    var clip=animator.runtimeAnimatorController.animationClips.Single(c=>c.name==motion);clip.SampleAnimation(animator.gameObject,clip.length*time);var positions=controls.Select(t=>t.localPosition).ToArray();var rotations=controls.Select(t=>t.localRotation).ToArray();
                    mesh=new Mesh();active.BakeMesh(mesh,true);Vector3 center=active.bounds.center;
                    foreach(int side in new[]{-1,1})
                    {
                        for(int i=0;i<controls.Length;i++){controls[i].localPosition=positions[i];controls[i].localRotation=rotations[i];}
                        camera.transform.position=center+new Vector3(0,.65f,side*5);camera.transform.LookAt(center);yield return null;yield return null;
                        var sample=new View{motion=motion,side=side<0?"near":"opposite",lod=lod,normalizedTime=time,minViewportMargin=1};receipt.views.Add(sample);active.BakeMesh(mesh,true);
                        foreach(var v in mesh.vertices){var p=camera.WorldToViewportPoint(active.transform.TransformPoint(v));if(p.z<=camera.nearClipPlane)throw new InvalidOperationException("Mesh behind camera");sample.minViewportMargin=Mathf.Min(sample.minViewportMargin,p.x,p.y,1-p.x,1-p.y);}
                        if(sample.minViewportMargin<.04f)throw new InvalidOperationException("Full animated fish is cropped");
                        string name="lod"+lod+"-"+motion+"-"+Mathf.RoundToInt(time*100)+"-"+sample.side;var first=Draw(name+"-active-first");sample.activeRepeatPixels=Changed(first,Draw(name+"-active-repeat"));
                        for(int i=0;i<controls.Length;i++){controls[i].localPosition=rest[i].GetColumn(3);controls[i].localRotation=rest[i].rotation;}
                        yield return null;yield return null;var held=Draw(name+"-held-first");sample.heldRepeatPixels=Changed(held,Draw(name+"-held-repeat"));sample.ablationPixels=Changed(first,held);
                        if(sample.activeRepeatPixels!=0||sample.heldRepeatPixels!=0||sample.ablationPixels<20)throw new InvalidOperationException("Static repeat or visible ablation failed: "+name);
                    }
                    UnityEngine.Object.DestroyImmediate(mesh);mesh=null;root.GetComponent<AuthoredFishVisual>().ReleasePresentation();UnityEngine.Object.DestroyImmediate(root);root=null;
                }
                if(receipt.views.Count!=84||receipt.images!=336)throw new InvalidOperationException("Incomplete review set");receipt.status="PASS";
            }
            finally
            {
                if(receipt.status!="PASS")receipt.status="FAIL";File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));
                if(root!=null){root.GetComponent<AuthoredFishVisual>().ReleasePresentation();UnityEngine.Object.DestroyImmediate(root);}if(mesh!=null)UnityEngine.Object.DestroyImmediate(mesh);if(camera!=null)camera.targetTexture=null;if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(camera!=null)UnityEngine.Object.DestroyImmediate(camera.gameObject);if(sun!=null)UnityEngine.Object.DestroyImmediate(sun.gameObject);QualitySettings.renderPipeline=priorMedium;GraphicsSettings.defaultRenderPipeline=priorDefault;QualitySettings.SetQualityLevel(prior,true);
            }
        }
    }
}
