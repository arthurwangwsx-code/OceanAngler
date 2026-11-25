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
    // Head macro views deliberately crop the fish. They diagnose the actual
    // runtime mesh/materials; they do not replace whole-fish framing/art review.
    public static class BluefinHeadRenderReview
    {
        [Serializable] sealed class View
        {public int lod,side,repeatPixels,noBumpPixels,noSpecularPixels,skinGillPixels;public string motion;public float phase;}
        [Serializable] sealed class Receipt
        {
            public string status="RUNNING",graphics,target,platform,scope="Imported runtime head macro, private material ablations only. No natural motion, Player, device, science or final art acceptance.";
            public int samples,images;public List<View> views=new List<View>();
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
            string folder=Environment.GetEnvironmentVariable("OCEAN_HEAD_REVIEW_OUTPUT");
            if(string.IsNullOrEmpty(folder)||SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("Fresh output and GPU required");
            var receipt=new Receipt{graphics=SystemInfo.graphicsDeviceType.ToString(),platform=EditorUserBuildSettings.activeBuildTarget.ToString()};
            int prior=QualitySettings.GetQualityLevel(),medium=Array.IndexOf(QualitySettings.names,"Medium");if(medium<0)throw new InvalidOperationException("Production Medium required");
            var priorDefault=GraphicsSettings.defaultRenderPipeline;QualitySettings.SetQualityLevel(medium,true);var priorMedium=QualitySettings.renderPipeline;
            GameObject root=null;Camera camera=null;Light light=null;RenderTexture target=null;Material noBump=null,noSpecular=null;
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OceanAngler/Generated/OceanURP.asset");if(pipeline==null||pipeline.msaaSampleCount!=4)throw new InvalidOperationException("Production four-sample HDR pipeline required");
                GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.32f,.4f,.43f);
                camera=new GameObject("HeadReviewCamera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=.37f;camera.aspect=1.5f;camera.allowHDR=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.12f,.17f);camera.nearClipPlane=.02f;camera.farClipPlane=30;camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                light=new GameObject("HeadReviewLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(45,-30,0);
                target=new RenderTexture(1200,800,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};target.Create();camera.targetTexture=target;receipt.samples=target.antiAliasing;receipt.target=target.graphicsFormat.ToString();var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
                Color32[] Draw(string name)
                {
                    RenderPipeline.SubmitRenderRequest(camera,request);var previous=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                    try{RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());receipt.images++;return image.GetPixels32();}
                    finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
                }
                int Changed(Color32[] a,Color32[] b)=>a.Where((p,i)=>!p.Equals(b[i])).Count();
                foreach(int lod in new[]{0,1,2})foreach(string motion in new[]{"swim","bite"})foreach(int side in new[]{-1,1})
                {
                    root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin"));root.GetComponent<AuthoredFishVisual>().InitializePresentation();root.GetComponent<LODGroup>().ForceLOD(lod);
                    var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;var renderers=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);foreach(var r in renderers)r.updateWhenOffscreen=true;
                    var original=renderers.Select(r=>r.sharedMaterials).ToArray();var skin=original.SelectMany(m=>m).First(m=>m.name=="Bluefin_Skin");var gill=original.SelectMany(m=>m).First(m=>m.name=="Bluefin_Gill");
                    noBump=new Material(skin);noBump.SetFloat("_BumpScale",0);
                    noSpecular=new Material(skin);noSpecular.SetFloat("_SpecularHighlights",0);noSpecular.SetFloat("_EnvironmentReflections",0);noSpecular.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");noSpecular.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                    void Materials(Material skinControl,bool replaceGill)
                    {
                        for(int i=0;i<renderers.Length;i++)
                        {
                            renderers[i].sharedMaterials=original[i].Select(m=>m==skin?(skinControl??m):m==gill&&replaceGill?skin:m).ToArray();
                            // Presentation uses per-slot property blocks. A gill
                            // material swap alone would retain its dark tint.
                            for(int slot=0;slot<original[i].Length;slot++)if(original[i][slot]==gill)
                            {
                                var material=replaceGill?skin:gill;var block=new MaterialPropertyBlock();renderers[i].GetPropertyBlock(block,slot);block.SetColor("_BaseColor",material.color);block.SetFloat("_Smoothness",material.GetFloat("_Smoothness"));renderers[i].SetPropertyBlock(block,slot);
                            }
                        }
                    }
                    float phase=motion=="swim"?.125f:.25f;var clip=animator.runtimeAnimatorController.animationClips.Single(c=>c.name==motion);clip.SampleAnimation(animator.gameObject,clip.length*phase);
                    var look=root.transform.TransformPoint(new Vector3(-.69f,-.025f,0));camera.transform.SetPositionAndRotation(look+new Vector3(-.05f,.05f,side*5),Quaternion.LookRotation(new Vector3(.05f,-.05f,-side*5),Vector3.up));
                    yield return null;yield return null;
                    string name="lod"+lod+"-"+motion+"-side"+side;var view=new View{lod=lod,motion=motion,phase=phase,side=side};receipt.views.Add(view);
                    var production=Draw(name+"-production");view.repeatPixels=Changed(production,Draw(name+"-production-repeat"));
                    Materials(noBump,false);yield return null;yield return null;view.noBumpPixels=Changed(production,Draw(name+"-control-no-bump"));
                    Materials(noSpecular,false);yield return null;yield return null;view.noSpecularPixels=Changed(production,Draw(name+"-control-no-specular"));
                    Materials(null,true);yield return null;yield return null;view.skinGillPixels=Changed(production,Draw(name+"-control-skin-gill"));
                    Materials(null,false);foreach(var m in new[]{noBump,noSpecular})UnityEngine.Object.DestroyImmediate(m);noBump=noSpecular=null;
                    root.GetComponent<AuthoredFishVisual>().ReleasePresentation();UnityEngine.Object.DestroyImmediate(root);root=null;
                }
                if(receipt.views.Count!=12||receipt.images!=60)throw new InvalidOperationException("Incomplete diagnosis set");receipt.status=receipt.views.All(v=>v.repeatPixels==0)?"PASS":"PARTIAL";
            }
            finally
            {
                if(receipt.status=="RUNNING")receipt.status="FAIL";File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));
                if(root!=null){root.GetComponent<AuthoredFishVisual>().ReleasePresentation();UnityEngine.Object.DestroyImmediate(root);}foreach(var m in new[]{noBump,noSpecular})if(m!=null)UnityEngine.Object.DestroyImmediate(m);
                if(camera!=null)camera.targetTexture=null;if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(camera!=null)UnityEngine.Object.DestroyImmediate(camera.gameObject);if(light!=null)UnityEngine.Object.DestroyImmediate(light.gameObject);QualitySettings.renderPipeline=priorMedium;GraphicsSettings.defaultRenderPipeline=priorDefault;QualitySettings.SetQualityLevel(prior,true);
            }
            if(receipt.status!="PASS")throw new InvalidOperationException("Keep all first/repeated frames and inspect the differences");
        }
    }
}
