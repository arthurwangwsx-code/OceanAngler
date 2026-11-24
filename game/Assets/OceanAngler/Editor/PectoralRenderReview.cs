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
    // Material controls are private diagnostic clones, never candidate assets.
    public static class PectoralRenderReview
    {
        [Serializable] sealed class Sample {public int lod,repeatChangedPixels;public Vector3 center,size;public List<Control> controls=new List<Control>();}
        [Serializable] sealed class Control {public string name;public int changedPixels;}
        [Serializable] sealed class Receipt {public string status="IN_PROGRESS",scope="imported pectoral shading controls; not Player, scientific or art acceptance",graphics,quality,target,platform;public int samples,images;public List<Sample> measurements=new List<Sample>();}
        static IEnumerator job;
        public static void Capture(){job=Run();EditorApplication.update+=Step;}
        static void Step()
        {
            try{if(job.MoveNext())return;EditorApplication.update-=Step;job=null;EditorApplication.Exit(0);}
            catch(Exception e){EditorApplication.update-=Step;(job as IDisposable)?.Dispose();job=null;Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static int Difference(Color32 a,Color32 b)=>Mathf.Max(Mathf.Abs(a.r-b.r),Mathf.Max(Mathf.Abs(a.g-b.g),Mathf.Abs(a.b-b.b)));
        static Bounds PectoralBounds(SkinnedMeshRenderer renderer)
        {
            var mesh=new Mesh();renderer.BakeMesh(mesh,true);
            try
            {
                var candidates=new List<Bounds>();var vertices=mesh.vertices;var weights=renderer.sharedMesh.boneWeights;
                for(int bone=0;bone<renderer.bones.Length;bone++)
                {
                    if(!renderer.bones[bone].name.StartsWith("pectoral"))continue;
                    bool found=false;var bounds=new Bounds();
                    for(int i=0;i<weights.Length;i++)
                    {
                        var w=weights[i];float weight=(w.boneIndex0==bone?w.weight0:0)+(w.boneIndex1==bone?w.weight1:0)+(w.boneIndex2==bone?w.weight2:0)+(w.boneIndex3==bone?w.weight3:0);
                        if(weight<.8f)continue;var point=renderer.transform.TransformPoint(vertices[i]);if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);
                    }
                    if(found)candidates.Add(bounds);
                }
                if(candidates.Count!=2)throw new InvalidOperationException("Expected two independently weighted pectoral fins");
                return candidates.OrderBy(b=>b.center.z).First();
            }
            finally{UnityEngine.Object.DestroyImmediate(mesh);}
        }
        static IEnumerator Run()
        {
            string folder=Environment.GetEnvironmentVariable("OCEAN_PECTORAL_REVIEW_OUTPUT");if(string.IsNullOrEmpty(folder))throw new InvalidOperationException("Review folder required");
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("GPU required");
            int quality=Array.IndexOf(QualitySettings.names,"Medium"),previousQuality=QualitySettings.GetQualityLevel();if(quality<0)throw new InvalidOperationException("Production Medium quality missing");
            var previousDefault=GraphicsSettings.defaultRenderPipeline;QualitySettings.SetQualityLevel(quality,true);var previousMedium=QualitySettings.renderPipeline;
            var receipt=new Receipt{graphics=SystemInfo.graphicsDeviceType.ToString(),quality=QualitySettings.names[quality],platform=EditorUserBuildSettings.activeBuildTarget.ToString()};
            GameObject root=null;Camera camera=null;Light sun=null;RenderTexture target=null;
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OceanAngler/Generated/OceanURP.asset");if(pipeline==null||pipeline.msaaSampleCount!=4)throw new InvalidOperationException("Production HDR/four-sample pipeline required");
                GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
                RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.32f,.40f,.43f);
                camera=new GameObject("PectoralReviewCamera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.aspect=1.5f;camera.allowHDR=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.03f,.06f,.07f);camera.nearClipPlane=.02f;camera.farClipPlane=30;camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                sun=new GameObject("PectoralReviewSun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;sun.shadows=LightShadows.Soft;sun.shadowBias=2;sun.shadowNormalBias=0;sun.transform.rotation=Quaternion.Euler(45,-30,0);
                target=new RenderTexture(1440,960,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};target.Create();camera.targetTexture=target;receipt.target=target.graphicsFormat.ToString();receipt.samples=target.antiAliasing;
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
                Color32[] Draw(string name)
                {
                    RenderPipeline.SubmitRenderRequest(camera,request);receipt.images++;var previous=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                    try{RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());return image.GetPixels32();}
                    finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
                }
                root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin"));root.GetComponent<AuthoredFishVisual>().InitializePresentation();var renderers=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;
                foreach(var renderer in renderers)renderer.updateWhenOffscreen=true;
                AssetDatabase.LoadAllAssetsAtPath("Assets/OceanAngler/Art/Bluefin/Bluefin.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).Single(c=>c.name=="idle"||c.name.EndsWith("|idle")).SampleAnimation(animator.gameObject,0);
                for(int lod=0;lod<3;lod++)
                {
                    root.GetComponent<LODGroup>().ForceLOD(lod);yield return null;yield return null;
                    var renderer=renderers.Single(r=>r.name.EndsWith("_LOD"+lod));var bounds=PectoralBounds(renderer);var sample=new Sample{lod=lod,center=bounds.center,size=bounds.size};receipt.measurements.Add(sample);
                    camera.orthographicSize=Mathf.Max(bounds.extents.y,bounds.extents.x/camera.aspect)*1.5f;camera.transform.position=bounds.center+new Vector3(0,.12f,-4);camera.transform.LookAt(bounds.center);
                    var original=renderer.sharedMaterials;var baseline=Draw("lod"+lod+"-production");
                    foreach(string variant in new[]{"no-normal","no-ray-shadow","no-shadow","opaque-membrane","no-membrane"})
                    {
                        var clones=original.Select(m=>new Material(m)).ToArray();var casting=renderer.shadowCastingMode;
                        try
                        {
                            foreach(var m in clones)
                            {
                                if(variant=="no-ray-shadow"&&m.name.StartsWith("Bluefin_Rays"))m.SetShaderPassEnabled("ShadowCaster",false);
                                if(!m.name.StartsWith("Bluefin_Fins"))continue;
                                if(variant=="no-normal"){m.SetFloat("_BumpScale",0);m.DisableKeyword("_NORMALMAP");}
                                if(variant=="opaque-membrane"){m.SetFloat("_Surface",0);BaseShaderGUI.SetupMaterialBlendMode(m);}
                                if(variant=="no-membrane")m.SetShaderPassEnabled("UniversalForward",false);
                            }
                            if(variant=="no-shadow")renderer.shadowCastingMode=ShadowCastingMode.Off;
                            renderer.sharedMaterials=clones;yield return null;yield return null;var pixels=Draw("lod"+lod+"-diagnostic-"+variant);sample.controls.Add(new Control{name=variant,changedPixels=pixels.Where((p,i)=>Difference(p,baseline[i])>2).Count()});
                        }
                        finally{renderer.sharedMaterials=original;renderer.shadowCastingMode=casting;foreach(var m in clones)UnityEngine.Object.DestroyImmediate(m);}
                    }
                    yield return null;yield return null;var repeated=Draw("lod"+lod+"-production-repeat");sample.repeatChangedPixels=repeated.Where((p,i)=>Difference(p,baseline[i])!=0).Count();
                    if(sample.repeatChangedPixels!=0)throw new InvalidOperationException("Production restore/repeat differs at LOD"+lod);
                    if(sample.controls.Single(c=>c.name=="no-membrane").changedPixels<300)throw new InvalidOperationException("Pectoral membrane is not sufficiently visible for diagnosis at LOD"+lod);
                    if(lod==0)
                    {
                        var whole=renderer.bounds;camera.orthographicSize=Mathf.Max(whole.extents.y,whole.extents.x/camera.aspect)*1.2f;
                        foreach(var view in new[]{("side",new Vector3(0,.1f,-6)),("quarter",new Vector3(-2.5f,1.1f,-5)),("reverse",new Vector3(0,.1f,6))})
                        {camera.transform.position=whole.center+view.Item2;camera.transform.LookAt(whole.center);Draw("production-whole-"+view.Item1);}
                    }
                }
                receipt.status="PASS";
            }
            finally
            {
                if(receipt.status!="PASS")receipt.status="FAIL";File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));
                if(root!=null){root.GetComponent<AuthoredFishVisual>().ReleasePresentation();UnityEngine.Object.DestroyImmediate(root);}if(camera!=null){camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(camera.gameObject);}if(sun!=null)UnityEngine.Object.DestroyImmediate(sun.gameObject);if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
                QualitySettings.renderPipeline=previousMedium;GraphicsSettings.defaultRenderPipeline=previousDefault;QualitySettings.SetQualityLevel(previousQuality,true);
            }
        }
    }
}
