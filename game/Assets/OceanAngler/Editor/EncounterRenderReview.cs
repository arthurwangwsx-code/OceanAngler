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
    public static class EncounterRenderReview
    {
        static IEnumerator run;
        [Serializable] sealed class Receipt
        {
            public string status="PASS",scope="isolated Editor Metal phase samples; no player UI, continuous runtime or device qualification",graphics;
            public int images,inspectionImages,diagnosticImages,width=1440,height=900;public float maximumLineError,maximumLureError,approachDistance;
        }
        public static void Capture(){run=CaptureSteps();EditorApplication.update+=Step;}
        static void Step()
        {
            try{if(run.MoveNext())return;EditorApplication.update-=Step;run=null;EditorApplication.Exit(0);}
            catch(Exception e){EditorApplication.update-=Step;(run as IDisposable)?.Dispose();run=null;Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static IEnumerator CaptureSteps()
        {
            string folder=Environment.GetEnvironmentVariable("OCEAN_ENCOUNTER_REVIEW_OUTPUT");if(string.IsNullOrEmpty(folder))throw new InvalidOperationException("Output required");
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("GPU required");
            EditorSceneManager.OpenScene("Assets/OceanAngler/Generated/Bay.unity");var camera=Camera.main;camera.enabled=false;camera.aspect=1.6f;
            var data=camera.GetComponent<UniversalAdditionalCameraData>()??camera.gameObject.AddComponent<UniversalAdditionalCameraData>();data.requiresColorTexture=true;data.requiresDepthTexture=true;
            RenderSettings.ambientLight=new Color(.2f,.32f,.38f);
            var root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/CoralBay"));var bay=root.GetComponent<BayHabitat>();
            var fish=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin")).GetComponent<AuthoredFishVisual>();fish.InitializePresentation();fish.GetComponent<LODGroup>().ForceLOD(0);
            var animator=fish.GetComponentInChildren<Animator>();animator.enabled=false;foreach(var renderer in fish.GetComponentsInChildren<SkinnedMeshRenderer>())renderer.updateWhenOffscreen=true;
            var hookLocal=fish.transform.InverseTransformPoint(fish.HookPosition);
            var rig=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/SpinningRig")).GetComponent<AuthoredSpinningRig>();rig.InitializeMechanisms();rig.GetComponent<LODGroup>().ForceLOD(0);
            var start=new Vector3(1.75f,-.3f,-2);var direction=(new Vector3(.7f,2.6f,1.3f)-start).normalized;rig.transform.SetPositionAndRotation(start,Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.up,direction).normalized,direction));
            var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/OceanAngler/Art/Bluefin/Bluefin.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            var frames=new[]{("01-cast",FishingPhase.Casting,.5f,"burst"),("02-wait-start",FishingPhase.Waiting,0f,"swim"),("03-approach",FishingPhase.Waiting,.5f,"swim"),("04-bite",FishingPhase.Bite,0f,"bite"),("05-fight-start",FishingPhase.Fighting,0f,"hooked"),("06-fight-middle",FishingPhase.Fighting,.5f,"hooked"),("07-landed",FishingPhase.Landed,1f,"idle")};
            var target=new RenderTexture(1440,900,24,RenderTextureFormat.ARGB32){antiAliasing=4};target.Create();camera.targetTexture=target;
            var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};var receipt=new Receipt{graphics=SystemInfo.graphicsDeviceType.ToString()};Vector3 approachStart=Vector3.zero;
            try
            {
                if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new InvalidOperationException("URP unavailable");
                foreach(var frame in frames)
                {
                    var pose=FishingEncounter.Evaluate(frame.Item2,frame.Item3,.6f,hookLocal,rig.CastOrigin,0,3,false);
                    fish.transform.SetPositionAndRotation(pose.fishPosition,pose.fishRotation);
                    clips.Single(c=>c.name==frame.Item4||c.name.EndsWith("|"+frame.Item4)).SampleAnimation(animator.gameObject,.2f);
                    bool attached=frame.Item2==FishingPhase.Bite||frame.Item2==FishingPhase.Fighting||frame.Item2==FishingPhase.Landed;
                    bool hooked=frame.Item2==FishingPhase.Fighting||frame.Item2==FishingPhase.Landed;
                    rig.Drive(hooked?.45f:0,false,false,false,frame.Item2==FishingPhase.Casting,pose.lurePosition,false,hooked,attached?fish:null);for(int step=0;step<3;step++)rig.Present(.1f);
                    var lineEnd=rig.fishingLine.GetPosition(rig.fishingLine.positionCount-1);
                    receipt.maximumLineError=Mathf.Max(receipt.maximumLineError,Vector3.Distance(lineEnd,attached?fish.HookPosition:rig.lureNose.position));
                    if(attached&&!hooked)receipt.maximumLureError=Mathf.Max(receipt.maximumLureError,Vector3.Distance(rig.lureNose.position,fish.HookPosition));
                    if(frame.Item1=="02-wait-start")approachStart=fish.transform.position;
                    if(frame.Item1=="04-bite")receipt.approachDistance=Vector3.Distance(approachStart,fish.transform.position);
                    bay.Present(camera,false,false,0,true,false);yield return null;yield return null;
                    RenderPipeline.SubmitRenderRequest(camera,request);Save(target,Path.Combine(folder,frame.Item1+".png"));receipt.images++;
                    if(frame.Item2==FishingPhase.Bite)
                    {
                        var mouth=fish.HookPosition;camera.transform.SetPositionAndRotation(mouth+new Vector3(-.3f,.12f,-1.3f),Quaternion.LookRotation(new Vector3(.3f,-.12f,1.3f)));camera.fieldOfView=42;yield return null;yield return null;
                        RenderPipeline.SubmitRenderRequest(camera,request);Save(target,Path.Combine(folder,"04-bite-close.png"));receipt.images++;
                        if(Environment.GetEnvironmentVariable("OCEAN_ENCOUNTER_LIGHT_DIAGNOSTICS")=="1")
                        {
                            var skins=fish.GetComponentsInChildren<SkinnedMeshRenderer>();var cast=skins.Select(s=>s.shadowCastingMode).ToArray();var receive=skins.Select(s=>s.receiveShadows).ToArray();
                            foreach(var skin in skins)skin.shadowCastingMode=ShadowCastingMode.Off;yield return null;
                            RenderPipeline.SubmitRenderRequest(camera,request);Save(target,Path.Combine(folder,"diagnostic-no-cast-shadow.png"));receipt.diagnosticImages++;
                            for(int i=0;i<skins.Length;i++)skins[i].shadowCastingMode=cast[i];
                            foreach(var skin in skins)skin.receiveShadows=false;yield return null;
                            RenderPipeline.SubmitRenderRequest(camera,request);Save(target,Path.Combine(folder,"diagnostic-no-receive-shadow.png"));receipt.diagnosticImages++;
                            for(int i=0;i<skins.Length;i++)skins[i].receiveShadows=receive[i];
                            bay.causticLight.enabled=false;yield return null;
                            RenderPipeline.SubmitRenderRequest(camera,request);Save(target,Path.Combine(folder,"diagnostic-no-caustics.png"));receipt.diagnosticImages++;bay.causticLight.enabled=true;
                            var blocks=new System.Collections.Generic.List<(SkinnedMeshRenderer,int,MaterialPropertyBlock)>();
                            foreach(var skin in skins)for(int i=0;i<skin.sharedMaterials.Length;i++)
                            {
                                var old=new MaterialPropertyBlock();skin.GetPropertyBlock(old,i);blocks.Add((skin,i,old));var flat=new MaterialPropertyBlock();skin.GetPropertyBlock(flat,i);flat.SetFloat("_BumpScale",0);skin.SetPropertyBlock(flat,i);
                            }
                            yield return null;RenderPipeline.SubmitRenderRequest(camera,request);Save(target,Path.Combine(folder,"diagnostic-flat-normal.png"));receipt.diagnosticImages++;
                            foreach(var item in blocks)item.Item1.SetPropertyBlock(item.Item3,item.Item2);
                            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                            float oldDepth=pipeline.shadowDepthBias,oldNormal=pipeline.shadowNormalBias;int oldResolution=pipeline.mainLightShadowmapResolution;
                            try
                            {
                                foreach(var setting in new[]{(1024,2f,0f),(2048,2f,0f),(2048,4f,0f),(2048,6f,0f),(2048,4f,.5f)})
                                {
                                    pipeline.mainLightShadowmapResolution=setting.Item1;pipeline.shadowDepthBias=setting.Item2;pipeline.shadowNormalBias=setting.Item3;yield return null;
                                    RenderPipeline.SubmitRenderRequest(camera,request);Save(target,Path.Combine(folder,"diagnostic-shadow-"+setting.Item1+"-bias"+setting.Item2+"-normal"+setting.Item3+".png"));receipt.diagnosticImages++;
                                }
                            }
                            finally{pipeline.shadowDepthBias=oldDepth;pipeline.shadowNormalBias=oldNormal;pipeline.mainLightShadowmapResolution=oldResolution;}
                        }
                    }
                }
                // Four rotations at the desktop model-view position. This is
                // still a scene-only fixture, without the actual journal UI.
                rig.gameObject.SetActive(false);bay.school.gameObject.SetActive(false);
                fish.transform.position=new Vector3(2.35f,1.35f,1.2f);clips.Single(c=>c.name=="idle"||c.name.EndsWith("|idle")).SampleAnimation(animator.gameObject,0);
                for(int angle=0;angle<4;angle++)
                {
                    fish.transform.rotation=Quaternion.Euler(0,-25+angle*90,-3);bay.Present(camera,false,false,0,true,false,false);yield return null;yield return null;
                    RenderPipeline.SubmitRenderRequest(camera,request);Save(target,Path.Combine(folder,"inspect-"+(angle*90)+".png"));receipt.inspectionImages++;
                }
                if(receipt.images!=8||receipt.maximumLineError>.00001f||receipt.maximumLureError>.00001f||receipt.approachDistance<4)throw new InvalidOperationException("Encounter anchors/approach failed");
                File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));Debug.Log("ENCOUNTER_GPU_PASS");
            }
            finally
            {
                fish.ReleasePresentation();if(bay.school!=null)bay.school.ReleasePresentation();camera.targetTexture=null;target.Release();
                UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(rig.gameObject);UnityEngine.Object.DestroyImmediate(fish.gameObject);UnityEngine.Object.DestroyImmediate(root);
            }
        }
        static void Save(RenderTexture source,string path)
        {
            var prior=RenderTexture.active;var image=new Texture2D(source.width,source.height,TextureFormat.RGB24,false);
            try{RenderTexture.active=source;image.ReadPixels(new Rect(0,0,source.width,source.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
