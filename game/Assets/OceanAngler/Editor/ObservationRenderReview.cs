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
    public static class ObservationRenderReview
    {
        static IEnumerator run;
        [Serializable] sealed class Sample{public string id;public int triangles,parts;public bool hasCollider,catchVisual;}
        [Serializable] sealed class Receipt{public string status="PASS",scope="A0 interaction models, isolated Editor Metal samples, no UI/player/device/art acceptance",graphics;public float maximumInitialDifference;public int initialDrawSamples;public int images,width=1440,height=900;public Sample[] species;}
        public static void Capture(){run=CaptureSteps();EditorApplication.update+=Step;}
        static void Step(){try{if(run.MoveNext())return;EditorApplication.update-=Step;run=null;EditorApplication.Exit(0);}catch(Exception e){EditorApplication.update-=Step;(run as IDisposable)?.Dispose();run=null;Debug.LogException(e);EditorApplication.Exit(1);}}
        static IEnumerator CaptureSteps()
        {
            string folder=Environment.GetEnvironmentVariable("OCEAN_OBSERVATION_REVIEW_OUTPUT");if(string.IsNullOrEmpty(folder)||SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("GPU and output required");
            EditorSceneManager.OpenScene("Assets/OceanAngler/Generated/Bay.unity");var camera=Camera.main;camera.enabled=false;camera.aspect=1.6f;
            var data=camera.GetComponent<UniversalAdditionalCameraData>()??camera.gameObject.AddComponent<UniversalAdditionalCameraData>();data.requiresColorTexture=true;data.requiresDepthTexture=true;
            var root=new GameObject("ObservationFixture");var encounter=root.AddComponent<ObservationEncounter>();
            var target=new RenderTexture(1440,900,24,RenderTextureFormat.ARGB32){antiAliasing=4};target.Create();var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};camera.targetTexture=target;
            var receipt=new Receipt{graphics=SystemInfo.graphicsDeviceType.ToString(),species=new Sample[3]};var ids=new[]{"green-turtle","bottlenose-dolphin","whale-shark"};GameObject bayRoot=null;
            try
            {
                RenderSettings.fog=false;camera.backgroundColor=new Color(.035f,.13f,.17f);RenderSettings.ambientLight=new Color(.45f,.55f,.6f);
                for(int i=0;i<ids.Length;i++)
                {
                    encounter.Present(ids[i],camera,false,false,0);encounter.Toggle();for(int step=0;step<12;step++)encounter.Present(ids[i],camera,false,false,.1f);
                    var visual=encounter.Visual;var meshes=visual.GetComponentsInChildren<MeshFilter>();receipt.species[i]=new Sample{id=ids[i],triangles=meshes.Sum(m=>m.sharedMesh.triangles.Length/3),parts=meshes.Length,hasCollider=visual.GetComponentInChildren<Collider>()!=null,catchVisual=visual.GetComponent<FishVisual>()!=null};
                    if(receipt.species[i].hasCollider||receipt.species[i].catchVisual)throw new InvalidOperationException("Observation target must be separate");
                    foreach(var mesh in meshes)foreach(var vertex in mesh.sharedMesh.vertices)if(!float.IsFinite(vertex.x)||!float.IsFinite(vertex.y)||!float.IsFinite(vertex.z))throw new InvalidOperationException("Invalid geometry");
                    var center=visual.transform.position;
                    foreach(var view in new[]{("side",new Vector3(0,.4f,-5)),("three-quarter",new Vector3(-3,1,-4)),("top",new Vector3(0,5,-.01f))})
                    {
                        camera.transform.SetPositionAndRotation(center+view.Item2,Quaternion.LookRotation(-view.Item2));camera.fieldOfView=38;yield return null;yield return null;
                        RenderPipeline.SubmitRenderRequest(camera,request);var first=Save(target,Path.Combine(folder,ids[i]+"-"+view.Item1+".png"));receipt.images++;
                        if(view.Item1=="side")
                        {
                            yield return null;yield return null;RenderPipeline.SubmitRenderRequest(camera,request);var repeated=Save(target,Path.Combine(folder,ids[i]+"-side-repeat.png"));receipt.images++;receipt.initialDrawSamples++;
                            float maximum=0;for(int pixel=0;pixel<first.Length;pixel++)maximum=Mathf.Max(maximum,Mathf.Max(Mathf.Abs(first[pixel].r-repeated[pixel].r)/255f,Mathf.Max(Mathf.Abs(first[pixel].g-repeated[pixel].g)/255f,Mathf.Abs(first[pixel].b-repeated[pixel].b)/255f)));
                            receipt.maximumInitialDifference=Mathf.Max(receipt.maximumInitialDifference,maximum);
                        }
                    }
                }
                encounter.Present("green-turtle",camera,false,false,0);bayRoot=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/CoralBay"));var bay=bayRoot.GetComponent<BayHabitat>();bay.Present(camera,false,false,0,true,false,false);encounter.Present("green-turtle",camera,false,false,0);
                yield return null;yield return null;RenderPipeline.SubmitRenderRequest(camera,request);Save(target,Path.Combine(folder,"green-turtle-bay.png"));receipt.images++;
                if(receipt.images!=13||receipt.initialDrawSamples!=3||receipt.maximumInitialDifference>0)throw new InvalidOperationException("Missing samples");File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));Debug.Log("OBSERVATION_GPU_PASS");
            }
            finally{camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(root);if(bayRoot!=null)UnityEngine.Object.DestroyImmediate(bayRoot);}
        }
        static Color32[] Save(RenderTexture source,string path){var previous=RenderTexture.active;var image=new Texture2D(source.width,source.height,TextureFormat.RGB24,false);try{RenderTexture.active=source;image.ReadPixels(new Rect(0,0,source.width,source.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());return image.GetPixels32();}finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}}
    }
}
