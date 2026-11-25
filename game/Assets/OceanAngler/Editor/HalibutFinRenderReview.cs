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
    // Isolated imported animation sampling. Native journal motion is separate.
    public static class HalibutFinRenderReview
    {
        [Serializable] sealed class View {public string name;public int repeatPixels;public string cpuPoseBefore,cpuPoseAfter;}
        [Serializable] sealed class Motion {public string action;public int adjacentChanges,minimumChangedPixels=int.MaxValue,endpointPixels;}
        [Serializable] sealed class Receipt
        {
            public string status="RUNNING",graphics,scope="Imported halibut seven two-second cycles at 30 authored samples/sec; endpoint repeats, distance/edge controls. Not runtime FPS, native UI, science or final art acceptance";
            public int images,cycleSamplesPerAction=61,samples,wholeMeshFramingChecks;public List<View> repeats=new List<View>();public List<string> actions=new List<string>();public List<Motion> motion=new List<Motion>();public List<int> ablationPixels=new List<int>();
        }
        static IEnumerator job;
        public static void Capture(){job=Run();EditorApplication.update+=Step;}
        static void Step()
        {
            try{if(job.MoveNext())return;EditorApplication.update-=Step;job=null;EditorApplication.Exit(0);}
            catch(Exception e){EditorApplication.update-=Step;(job as IDisposable)?.Dispose();job=null;Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static int Changed(Color32[] a,Color32[] b)=>a.Where((p,i)=>p.r!=b[i].r||p.g!=b[i].g||p.b!=b[i].b).Count();
        static IEnumerator Run()
        {
            string folder=Environment.GetEnvironmentVariable("OCEAN_HALIBUT_FIN_OUTPUT");if(string.IsNullOrEmpty(folder)||SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("GPU and output required");
            var receipt=new Receipt{graphics=SystemInfo.graphicsDeviceType.ToString()};int priorQuality=QualitySettings.GetQualityLevel(),medium=Array.IndexOf(QualitySettings.names,"Medium");var priorDefault=GraphicsSettings.defaultRenderPipeline;QualitySettings.SetQualityLevel(medium,true);var priorMedium=QualitySettings.renderPipeline;
            GameObject root=null;Camera camera=null;Light sun=null;RenderTexture target=null;Mesh posedMesh=null;
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OceanAngler/Generated/OceanURP.asset");if(pipeline.msaaSampleCount!=4)throw new InvalidOperationException("Production MSAA required");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
                RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.32f,.4f,.43f);
                camera=new GameObject("MedianFinReviewCamera").AddComponent<Camera>();camera.enabled=false;camera.aspect=1.6f;camera.orthographic=true;camera.orthographicSize=.95f;camera.nearClipPlane=.02f;camera.farClipPlane=30;camera.allowHDR=true;camera.allowMSAA=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.075f,.085f);camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                sun=new GameObject("MedianFinReviewSun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;sun.transform.rotation=Quaternion.Euler(45,-30,0);
                target=new RenderTexture(1280,800,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};target.Create();camera.targetTexture=target;receipt.samples=target.antiAliasing;var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
                root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Halibut"));root.GetComponent<AuthoredFishVisual>().InitializePresentation();var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>()){renderer.updateWhenOffscreen=true;renderer.forceMatrixRecalculationPerRender=true;}
                var group=root.GetComponent<LODGroup>();var fins=root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("dorsal_")||t.name.StartsWith("anal_")).ToArray();if(fins.Length!=34)throw new InvalidOperationException("Expected imported independent fin controls");var rest=fins.Select(t=>t.localRotation).ToArray();var clips=animator.runtimeAnimatorController.animationClips;posedMesh=new Mesh();int currentLod=0;string cpuPoseHash=null;
                Color32[] Draw(string name)
                {
                    // Complete the actual pose and inspect its deformed vertices
                    // before the first request. No rendered warm frame is hidden.
                    var renderer=root.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="Halibut_LOD"+currentLod);renderer.BakeMesh(posedMesh,true);
                    var values=posedMesh.vertices.Concat(posedMesh.normals).SelectMany(v=>new[]{v.x,v.y,v.z}).ToArray();var bytes=new byte[values.Length*sizeof(float)];Buffer.BlockCopy(values,0,bytes,0,bytes.Length);using(var sha=System.Security.Cryptography.SHA256.Create())cpuPoseHash=Convert.ToBase64String(sha.ComputeHash(bytes));
                    foreach(var vertex in posedMesh.vertices){var p=camera.WorldToViewportPoint(renderer.transform.TransformPoint(vertex));if(p.z<=camera.nearClipPlane||p.x<.01f||p.x>.99f||p.y<.01f||p.y>.99f)throw new InvalidOperationException("Whole fish leaves review frame: "+name);}receipt.wholeMeshFramingChecks++;
                    RenderPipeline.SubmitRenderRequest(camera,request);receipt.images++;var prior=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                    try{RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());return image.GetPixels32();}
                    finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(image);}
                }
                void Repeat(string name,Color32[] first){string before=cpuPoseHash;var repeated=Draw(name+"-repeat");receipt.repeats.Add(new View{name=name,repeatPixels=Changed(first,repeated),cpuPoseBefore=before,cpuPoseAfter=cpuPoseHash});}
                void PoseCamera(bool edge){var center=new Vector3(.15f,0,0);var eye=edge?new Vector3(0,.2f,-3):new Vector3(-.4f,2.2f,-2.8f);camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(center-eye));camera.orthographicSize=edge?.9f:.95f;}
                group.ForceLOD(0);PoseCamera(false);yield return null;yield return null;
                foreach(string semantic in new[]{"idle","swim","turn","burst","bite","hooked","release"})
                {
                    var clip=clips.Single(c=>c.name==semantic);receipt.actions.Add(semantic);
                    var measured=new Motion{action=semantic};receipt.motion.Add(measured);Color32[] previous=null,firstFrame=null;
                    for(int frame=0;frame<=60;frame++)
                    {
                        clip.SampleAnimation(animator.gameObject,clip.length*frame/60);EditorApplication.QueuePlayerLoopUpdate();yield return null;EditorApplication.QueuePlayerLoopUpdate();yield return null;
                        string name=semantic+"-"+frame.ToString("D3");var first=Draw(name);if(frame==0||frame==60)Repeat(name,first);
                        if(previous!=null){int changed=Changed(previous,first);if(changed>0)measured.adjacentChanges++;measured.minimumChangedPixels=Math.Min(measured.minimumChangedPixels,changed);}else firstFrame=first;
                        previous=first;if(frame==60)measured.endpointPixels=Changed(firstFrame,first);
                    }
                }
                var swim=clips.Single(c=>c.name=="swim");
                foreach(int lod in new[]{1,2})
                {
                    currentLod=lod;group.ForceLOD(lod);yield return null;yield return null;
                    for(int frame=0;frame<4;frame++){swim.SampleAnimation(animator.gameObject,swim.length*frame/4);EditorApplication.QueuePlayerLoopUpdate();yield return null;EditorApplication.QueuePlayerLoopUpdate();yield return null;string name="lod"+lod+"-"+frame;Repeat(name,Draw(name));}
                }
                currentLod=0;group.ForceLOD(0);PoseCamera(true);yield return null;yield return null;
                for(int frame=0;frame<4;frame++)
                {
                    swim.SampleAnimation(animator.gameObject,swim.length*frame/4);EditorApplication.QueuePlayerLoopUpdate();yield return null;EditorApplication.QueuePlayerLoopUpdate();yield return null;string name="edge"+frame;var active=Draw(name);Repeat(name,active);var posed=fins.Select(t=>t.localRotation).ToArray();for(int i=0;i<fins.Length;i++)fins[i].localRotation=rest[i];EditorApplication.QueuePlayerLoopUpdate();yield return null;EditorApplication.QueuePlayerLoopUpdate();yield return null;var held=Draw(name+"-held-fin");Repeat(name+"-held-fin",held);receipt.ablationPixels.Add(Changed(active,held));for(int i=0;i<fins.Length;i++)fins[i].localRotation=posed[i];
                }
                receipt.status=receipt.repeats.All(v=>v.repeatPixels==0)&&receipt.motion.All(m=>m.adjacentChanges==60)&&receipt.ablationPixels.All(n=>n>0)?"PASS":"PARTIAL";
            }
            finally
            {
                if(receipt.status=="RUNNING")receipt.status="FAIL";File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));if(posedMesh!=null)UnityEngine.Object.DestroyImmediate(posedMesh);if(root!=null){root.GetComponent<AuthoredFishVisual>().ReleasePresentation();UnityEngine.Object.DestroyImmediate(root);}if(camera!=null){camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(camera.gameObject);}if(sun!=null)UnityEngine.Object.DestroyImmediate(sun.gameObject);if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}QualitySettings.renderPipeline=priorMedium;GraphicsSettings.defaultRenderPipeline=priorDefault;QualitySettings.SetQualityLevel(priorQuality,true);
            }
            if(receipt.status!="PASS")throw new InvalidOperationException("Retain and inspect actual first/repeat draw differences");
        }
    }
}
