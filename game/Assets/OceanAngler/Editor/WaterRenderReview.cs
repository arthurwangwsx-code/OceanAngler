using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace OceanAngler.Editor
{
    // Isolated GPU material review. No player saves, desktop capture or UI claims.
    public static class WaterRenderReview
    {
        [Serializable] sealed class EnvironmentSample
        {
            public string phase,buildTarget,colorSpace,quality,pipeline,ambientMode,skyShader,sun,reflectionMode,defaultReflectionFormat;
            public int qualityIndex,pipelineSamples,textureLimit;public float ambientIntensity,reflectionIntensity,lodBias;
            public Color ambient,sky,equator,ground,fog,sunColor;public float fogDensity,sunIntensity;
            public Vector3 sunEuler,cameraPosition;public Quaternion cameraRotation;public float fieldOfView;
            public float[] ambientProbe;public Vector4 reflectionDecode;public string[] materialTextureFormats;
        }
        [Serializable] sealed class EnvironmentTrace {public List<EnvironmentSample> samples=new List<EnvironmentSample>();}
        static void RecordEnvironment(EnvironmentTrace trace,string phase,Camera camera,string folder)
        {
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var sun=RenderSettings.sun;
            if(sun==null)foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional&&light.enabled){sun=light;break;}
            var sample=new EnvironmentSample{phase=phase,buildTarget=EditorUserBuildSettings.activeBuildTarget.ToString(),colorSpace=QualitySettings.activeColorSpace.ToString(),qualityIndex=QualitySettings.GetQualityLevel(),quality=QualitySettings.names[QualitySettings.GetQualityLevel()],pipeline=pipeline==null?"none":pipeline.name,pipelineSamples=pipeline==null?0:pipeline.msaaSampleCount,textureLimit=QualitySettings.globalTextureMipmapLimit,lodBias=QualitySettings.lodBias,ambientMode=RenderSettings.ambientMode.ToString(),ambientIntensity=RenderSettings.ambientIntensity,ambient=RenderSettings.ambientLight,sky=RenderSettings.ambientSkyColor,equator=RenderSettings.ambientEquatorColor,ground=RenderSettings.ambientGroundColor,fog=RenderSettings.fogColor,fogDensity=RenderSettings.fogDensity,skyShader=RenderSettings.skybox==null?"none":RenderSettings.skybox.shader.name,sun=sun==null?"none":sun.name,sunColor=sun==null?Color.black:sun.color,sunIntensity=sun==null?0:sun.intensity,sunEuler=sun==null?Vector3.zero:sun.transform.eulerAngles,reflectionMode=RenderSettings.defaultReflectionMode.ToString(),reflectionIntensity=RenderSettings.reflectionIntensity,cameraPosition=camera.transform.position,cameraRotation=camera.transform.rotation,fieldOfView=camera.fieldOfView,ambientProbe=new float[27]};
            var sh=RenderSettings.ambientProbe;for(int rgb=0;rgb<3;rgb++)for(int coefficient=0;coefficient<9;coefficient++)sample.ambientProbe[rgb*9+coefficient]=sh[rgb,coefficient];
            sample.defaultReflectionFormat=ReflectionProbe.defaultTexture==null?"none":ReflectionProbe.defaultTexture.graphicsFormat.ToString();sample.reflectionDecode=ReflectionProbe.defaultTextureHDRDecodeValues;
            var formats=new List<string>();foreach(string name in new[]{"Channel_Stone","Channel_CoastalSoil"})foreach(string suffix in new[]{"","_Normal","_MetallicSmoothness"})
            {
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OceanAngler/Art/NearshoreChannel/"+name+suffix+".png");if(texture!=null)formats.Add(texture.name+":"+texture.graphicsFormat+":"+texture.width+"x"+texture.height);
            }
            sample.materialTextureFormats=formats.ToArray();
            trace.samples.Add(sample);File.WriteAllText(Path.Combine(folder,"environment.json"),JsonUtility.ToJson(trace,true));
        }
        [Serializable] sealed class Receipt {public string status="IN_PROGRESS",scope="isolated Editor GPU water fixture; no player UI/device acceptance",staticMatchCheck="NOT_RUN";public int width=1440,height=900,reflectionPasses,validatedMaterials,initialChangedPixels,repeatChangedPixels,samples=4,staticDraws;public float reflectionVariance,waveDifference;public string graphics,buildTarget,colorSpace;public bool released,representedEachDraw=true,exactStaticMatch,boatMaterialBlockControl;public float initialDifference,repeatDifference;}
        public static void Capture()=>CapturePrefab("OCEAN_WATER_REVIEW_OUTPUT","Models/CoralBay",false);
        public static void CaptureChannel()=>CapturePrefab("OCEAN_CHANNEL_REVIEW_OUTPUT","Models/NearshoreChannel",true);
        static void CapturePrefab(string outputVariable,string resource,bool channel)
        {
            string folder=Environment.GetEnvironmentVariable(outputVariable);if(string.IsNullOrEmpty(folder))throw new InvalidOperationException("Review output required");
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("A graphics device is required for GPU review");
            string expectedTarget=Environment.GetEnvironmentVariable("OCEAN_REVIEW_TARGET");if(!string.IsNullOrEmpty(expectedTarget)&&EditorUserBuildSettings.activeBuildTarget.ToString()!=expectedTarget)throw new InvalidOperationException("Review build target mismatch");
            EditorSceneManager.OpenScene("Assets/OceanAngler/Generated/Bay.unity");var camera=Camera.main;camera.enabled=false;camera.aspect=1.6f;
            var data=camera.GetComponent<UniversalAdditionalCameraData>()??camera.gameObject.AddComponent<UniversalAdditionalCameraData>();data.requiresColorTexture=true;data.requiresDepthTexture=true;
            RenderSettings.ambientLight=new Color(.2f,.32f,.38f);
            var fill=new GameObject("ReviewRuntimeWaterFill").AddComponent<Light>();fill.type=LightType.Point;fill.color=new Color(.37f,.79f,.86f);fill.intensity=channel?0:2.5f;fill.range=15;fill.transform.position=new Vector3(0,3,0);
            var root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>(resource));var bay=root.GetComponent<MarineHabitat>();var reflection=bay.reflection;
            bool boatBlockControl=channel&&Environment.GetEnvironmentVariable("OCEAN_CHANNEL_REVIEW_BOAT_BLOCKS")=="1";
            if(boatBlockControl)foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))if(renderer.name.StartsWith("Boat_LOD",StringComparison.Ordinal))
            {
                var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++){var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,i);block.SetColor("_BaseColor",materials[i].GetColor("_BaseColor"));block.SetFloat("_Smoothness",materials[i].GetFloat("_Smoothness"));block.SetFloat("_Metallic",materials[i].GetFloat("_Metallic"));renderer.SetPropertyBlock(block,i);}
            }
            int samples=channel&&Environment.GetEnvironmentVariable("OCEAN_CHANNEL_REVIEW_MSAA")=="1"?1:4;
            var target=new RenderTexture(1440,900,24,RenderTextureFormat.ARGB32){antiAliasing=samples};target.Create();camera.targetTexture=target;
            var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};var receipt=new Receipt{buildTarget=EditorUserBuildSettings.activeBuildTarget.ToString(),colorSpace=QualitySettings.activeColorSpace.ToString(),graphics=SystemInfo.graphicsDeviceType.ToString(),samples=target.antiAliasing,boatMaterialBlockControl=boatBlockControl};
            try
            {
                if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new InvalidOperationException("URP render request not supported");
                var environment=new EnvironmentTrace();RecordEnvironment(environment,"scene-loaded",camera,folder);
                bay.Present(camera,true,true,0,true,false);RecordEnvironment(environment,"presented",camera,folder);RenderPipeline.SubmitRenderRequest(camera,request);var first=Read(target,Path.Combine(folder,"surface.png"));RecordEnvironment(environment,"first-draw",camera,folder);
                if(channel)
                {
                    receipt.staticDraws=3;
                    receipt.representedEachDraw=Environment.GetEnvironmentVariable("OCEAN_CHANNEL_REVIEW_REPRESENT")!="0";
                    if(receipt.representedEachDraw)bay.Present(camera,true,true,0,true,false);RenderPipeline.SubmitRenderRequest(camera,request);var repeated=Read(target,Path.Combine(folder,"surface-repeat.png"));
                    RecordEnvironment(environment,"repeat-draw",camera,folder);
                    if(receipt.representedEachDraw)bay.Present(camera,true,true,0,true,false);RenderPipeline.SubmitRenderRequest(camera,request);var third=Read(target,Path.Combine(folder,"surface-third.png"));
                    RecordEnvironment(environment,"third-draw",camera,folder);
                    for(int i=0;i<first.Length;i++)
                    {
                        float initial=Mathf.Max(Mathf.Abs(first[i].r-repeated[i].r),Mathf.Max(Mathf.Abs(first[i].g-repeated[i].g),Mathf.Abs(first[i].b-repeated[i].b)));receipt.initialDifference=Mathf.Max(receipt.initialDifference,initial);if(initial>0)receipt.initialChangedPixels++;
                        float next=Mathf.Max(Mathf.Abs(repeated[i].r-third[i].r),Mathf.Max(Mathf.Abs(repeated[i].g-third[i].g),Mathf.Abs(repeated[i].b-third[i].b)));receipt.repeatDifference=Mathf.Max(receipt.repeatDifference,next);if(next>0)receipt.repeatChangedPixels++;
                    }
                    receipt.exactStaticMatch=receipt.initialDifference==0&&receipt.repeatDifference==0;
                    receipt.staticMatchCheck=receipt.exactStaticMatch?"PASS":"PARTIAL_STATIC_RGB_DIFFERENCE";
                    File.WriteAllText(Path.Combine(folder,"initial-diagnostics.json"),JsonUtility.ToJson(receipt,true));
                }
                bay.waterSurface.enabled=false;RenderPipeline.SubmitRenderRequest(camera,request);Read(target,Path.Combine(folder,"no-water-control.png"));bay.waterSurface.enabled=true;
                if(!reflection.Ready)throw new InvalidOperationException("Planar reflection missing");
                var pixels=Read(reflection.Texture,Path.Combine(folder,"reflection.png"));float mean=0,squared=0;
                foreach(var p in pixels){float value=(p.r+p.g+p.b)/3;mean+=value;squared+=value*value;}mean/=pixels.Length;receipt.reflectionVariance=squared/pixels.Length-mean*mean;
                int captures=reflection.RenderCount;bay.Present(camera,true,true,0,true,false);
                if(reflection.RenderCount!=captures)throw new InvalidOperationException("Static reflection was not reused");
                var properties=new MaterialPropertyBlock();bay.waterSurface.GetPropertyBlock(properties);properties.SetFloat("_OceanTime",1.4f);bay.waterSurface.SetPropertyBlock(properties);
                RenderPipeline.SubmitRenderRequest(camera,request);var second=Read(target,Path.Combine(folder,"surface-time2.png"));
                float difference=0;for(int i=0;i<first.Length;i++)difference+=Mathf.Abs(first[i].r-second[i].r)+Mathf.Abs(first[i].g-second[i].g)+Mathf.Abs(first[i].b-second[i].b);receipt.waveDifference=difference/(first.Length*3);
                bay.Present(camera,true,true,1,true,false);RenderPipeline.SubmitRenderRequest(camera,request);Read(target,Path.Combine(folder,"pier.png"));
                captures=reflection.RenderCount;bay.Present(camera,true,false,0,true,false);RenderPipeline.SubmitRenderRequest(camera,request);Read(target,Path.Combine(folder,"underwater.png"));
                if(reflection.Ready||reflection.RenderCount!=captures)throw new InvalidOperationException("Underwater reflection request was not skipped");
                bay.Present(camera,true,false,1,true,false);RenderPipeline.SubmitRenderRequest(camera,request);Read(target,Path.Combine(folder,"reef.png"));receipt.reflectionPasses=reflection.RenderCount;
                if(channel)
                {
                    bay.Present(camera,true,true,0,true,false);camera.transform.SetPositionAndRotation(new Vector3(-14,8,58),Quaternion.LookRotation(new Vector3(-10,3,14)));camera.fieldOfView=50;reflection.Prepare(camera,true,bay.SeaLevel);
                    var coastalWater=new MaterialPropertyBlock();bay.waterSurface.GetPropertyBlock(coastalWater);coastalWater.SetTexture("_PlanarReflection",reflection.Texture);coastalWater.SetMatrix("_ReflectionVP",reflection.ViewProjection);bay.waterSurface.SetPropertyBlock(coastalWater);RenderPipeline.SubmitRenderRequest(camera,request);Read(target,Path.Combine(folder,"terrain-close.png"));
                    bay.Present(camera,true,false,0,true,false);camera.transform.SetPositionAndRotation(new Vector3(-14,3,58),Quaternion.LookRotation(new Vector3(-10,-7,14)));camera.fieldOfView=50;RenderPipeline.SubmitRenderRequest(camera,request);Read(target,Path.Combine(folder,"terrain-wet-edge.png"));receipt.reflectionPasses=reflection.RenderCount;
                }
                if(receipt.reflectionVariance<.0005f||receipt.waveDifference<.0001f)throw new InvalidOperationException("Rendered water/reflection lacks scene or wave detail");
                reflection.enabled=false;bay.Present(camera,true,true,0,true,false);receipt.released=reflection.Texture==null;
                if(!receipt.released)throw new InvalidOperationException("Disabled reflection retained/recreated its texture");
                foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)
                {
                    if(material==null||material.shader==null||!material.shader.isSupported||ShaderUtil.ShaderHasError(material.shader))throw new InvalidOperationException("Rendered habitat has a missing/unsupported/failed shader");receipt.validatedMaterials++;
                }
                receipt.status=!channel||receipt.exactStaticMatch?"PASS":"PARTIAL_STATIC_RGB_DIFFERENCE";
                File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));Debug.Log("WATER_GPU_REVIEW_"+receipt.status);
            }
            finally{camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(fill.gameObject);}
        }
        static Color[] Read(RenderTexture source,string path)
        {
            var prior=RenderTexture.active;var image=new Texture2D(source.width,source.height,TextureFormat.RGB24,false);
            try{RenderTexture.active=source;image.ReadPixels(new Rect(0,0,source.width,source.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());return image.GetPixels();}
            finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
