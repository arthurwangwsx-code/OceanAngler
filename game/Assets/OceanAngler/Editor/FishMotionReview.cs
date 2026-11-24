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
    // Sample real imported clips on the same candidate mesh. This is an
    // offline temporal control, not an Animator/player/performance qualification.
    public static class FishMotionReview
    {
        [Serializable] sealed class JointSample {public string joint;public float endpointDegrees,startDegreesPerSecond,endDegreesPerSecond,velocityJumpDegreesPerSecond,rangeDegrees;}
        [Serializable] sealed class MotionSample {public string variant,motion;public float seconds;public List<JointSample> joints=new List<JointSample>();}
        [Serializable] sealed class Sequence {public string prefix;public int frames,repeatChangedPixels;public float encodedFramesPerSecond=20,minViewportMargin,minRasterMargin;}
        [Serializable] sealed class Receipt {public string status="IN_PROGRESS",scope="Imported clip sampling on shared mesh; offline images, not Animator/Player, biological, natural-motion or physical acceptance",graphics,quality,target;public int samples,images;public float rejectedTopFramingMargin;public List<MotionSample> motions=new List<MotionSample>();public List<Sequence> sequences=new List<Sequence>();}
        static IEnumerator job;
        public static void Capture(){job=Run();EditorApplication.update+=Step;}
        static void Step()
        {
            try{if(job.MoveNext())return;EditorApplication.update-=Step;job=null;EditorApplication.Exit(0);}
            catch(Exception e){EditorApplication.update-=Step;(job as IDisposable)?.Dispose();job=null;Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static AnimationClip Clip(string path,string semantic)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__")&&(c.name==semantic||c.name.EndsWith("|"+semantic)));
        static AnimationClip Playable(Animator animator,string semantic)=>animator.runtimeAnimatorController.animationClips.Single(c=>c.name==semantic||c.name.EndsWith("|"+semantic));
        static Vector3 Velocity(Quaternion from,Quaternion to,float seconds)
        {
            var delta=to*Quaternion.Inverse(from);if(delta.w<0)delta=new Quaternion(-delta.x,-delta.y,-delta.z,-delta.w);
            var axis=new Vector3(delta.x,delta.y,delta.z);float length=axis.magnitude;if(length<1e-9f)return Vector3.zero;
            return axis/length*(2*Mathf.Atan2(length,delta.w)*Mathf.Rad2Deg/seconds);
        }
        static MotionSample Measure(GameObject target,AnimationClip clip,string variant,string semantic)
        {
            var result=new MotionSample{variant=variant,motion=semantic,seconds=clip.length};
            foreach(string name in new[]{"spine1","tail","pectoralL","jaw"})
            {
                var bone=target.GetComponentsInChildren<Transform>().Single(t=>t.name==name);float h=.005f;
                clip.SampleAnimation(target,0);var start=bone.localRotation;
                clip.SampleAnimation(target,h);var after=bone.localRotation;
                clip.SampleAnimation(target,clip.length-h);var before=bone.localRotation;
                clip.SampleAnimation(target,clip.length);var end=bone.localRotation;
                var outgoing=Velocity(start,after,h);var incoming=Velocity(before,end,h);float range=0;
                for(int i=0;i<=120;i++){clip.SampleAnimation(target,clip.length*i/120);range=Mathf.Max(range,Quaternion.Angle(start,bone.localRotation));}
                result.joints.Add(new JointSample{joint=name,endpointDegrees=Quaternion.Angle(start,end),startDegreesPerSecond=outgoing.magnitude,endDegreesPerSecond=incoming.magnitude,velocityJumpDegreesPerSecond=(outgoing-incoming).magnitude,rangeDegrees=range});
            }
            if(result.joints.Single(j=>j.joint=="tail").rangeDegrees<1)throw new InvalidOperationException("Imported clip did not animate candidate bones: "+semantic);
            return result;
        }
        static IEnumerator Run()
        {
            string folder=Environment.GetEnvironmentVariable("OCEAN_MOTION_REVIEW_OUTPUT"),reference=Environment.GetEnvironmentVariable("OCEAN_MOTION_REFERENCE_FBX");
            if(string.IsNullOrEmpty(folder)||!File.Exists(reference))throw new InvalidOperationException("Own reference FBX and new output directory required");
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("GPU required");
            const string fixture="Assets/OceanAngler/Editor/TemporaryMotionReference.fbx",production="Assets/OceanAngler/Art/Bluefin/Bluefin.fbx";
            string fixtureDisk=Path.Combine(Application.dataPath,"OceanAngler/Editor/TemporaryMotionReference.fbx");
            if(File.Exists(fixtureDisk)||File.Exists(fixtureDisk+".meta"))throw new InvalidOperationException("Reconcile existing diagnostic fixture; never overwrite it");
            int prior=QualitySettings.GetQualityLevel(),medium=Array.IndexOf(QualitySettings.names,"Medium");var priorDefault=GraphicsSettings.defaultRenderPipeline;
            if(medium<0)throw new InvalidOperationException("Medium quality required");QualitySettings.SetQualityLevel(medium,true);var priorMedium=QualitySettings.renderPipeline;
            GameObject root=null;Camera camera=null;Light sun=null;RenderTexture texture=null;Mesh baked=null;bool ownsFixture=false;
            var receipt=new Receipt{graphics=SystemInfo.graphicsDeviceType.ToString(),quality=QualitySettings.names[medium]};
            try
            {
                File.Copy(reference,fixtureDisk);ownsFixture=true;AssetDatabase.ImportAsset(fixture,ImportAssetOptions.ForceSynchronousImport);
                var original=(ModelImporter)AssetImporter.GetAtPath(production);var importer=(ModelImporter)AssetImporter.GetAtPath(fixture);
                importer.animationType=original.animationType;importer.avatarSetup=original.avatarSetup;importer.importAnimation=true;importer.resampleCurves=original.resampleCurves;importer.animationCompression=original.animationCompression;importer.animationRotationError=original.animationRotationError;importer.animationPositionError=original.animationPositionError;importer.animationScaleError=original.animationScaleError;importer.SaveAndReimport();
                var definitions=importer.defaultClipAnimations;foreach(var clip in definitions)clip.loopTime=clip.name!="release"&&!clip.name.EndsWith("|release");importer.clipAnimations=definitions;importer.SaveAndReimport();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OceanAngler/Generated/OceanURP.asset");if(pipeline==null||pipeline.msaaSampleCount!=4)throw new InvalidOperationException("Production four-sample HDR pipeline required");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
                RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.32f,.40f,.43f);
                root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin"));root.GetComponent<AuthoredFishVisual>().InitializePresentation();root.GetComponent<LODGroup>().ForceLOD(0);
                var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())renderer.updateWhenOffscreen=true;
                foreach(var variant in new[]{("before",fixture),("candidate",production)})foreach(string semantic in new[]{"idle","swim","turn","burst","bite","hooked","release"})receipt.motions.Add(Measure(animator.gameObject,variant.Item1=="candidate"?Playable(animator,semantic):Clip(variant.Item2,semantic),variant.Item1,semantic));
                camera=new GameObject("MotionReviewCamera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1.15f;camera.aspect=1.5f;camera.allowHDR=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.03f,.06f,.07f);camera.nearClipPlane=.02f;camera.farClipPlane=30;camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                sun=new GameObject("MotionReviewSun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;sun.shadows=LightShadows.Soft;sun.shadowBias=2;sun.shadowNormalBias=0;sun.transform.rotation=Quaternion.Euler(45,-30,0);
                texture=new RenderTexture(960,640,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};texture.Create();camera.targetTexture=texture;receipt.target=texture.graphicsFormat.ToString();receipt.samples=texture.antiAliasing;
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=texture};
                var active=root.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name.EndsWith("_LOD0"));var framingCenter=active.bounds.center;baked=new Mesh();
                float Margin()
                {
                    active.BakeMesh(baked,true);baked.RecalculateBounds();var bounds=baked.bounds;float margin=1;
                    for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                    {
                        var local=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));var point=camera.WorldToViewportPoint(active.transform.TransformPoint(local));
                        if(point.z<=camera.nearClipPlane)return -1;margin=Mathf.Min(margin,point.x,point.y,1-point.x,1-point.y);
                    }
                    return margin;
                }
                Playable(animator,"swim").SampleAnimation(animator.gameObject,0);camera.orthographicSize=.95f;camera.transform.position=new Vector3(0,5,-.05f);camera.transform.LookAt(new Vector3(.1f,0,0));receipt.rejectedTopFramingMargin=Margin();camera.orthographicSize=1.15f;
                Color32[] Draw(string name)
                {
                    RenderPipeline.SubmitRenderRequest(camera,request);var previous=RenderTexture.active;var frame=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);
                    try{RenderTexture.active=texture;frame.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);frame.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),frame.EncodeToPNG());receipt.images++;return frame.GetPixels32();}
                    finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(frame);}
                }
                foreach(var variant in new[]{("before",fixture),("candidate",production)})foreach(string semantic in new[]{"swim","hooked"})foreach(string view in new[]{"side","top"})
                {
                    string prefix=variant.Item1+"-"+semantic+"-"+view;var clip=variant.Item1=="candidate"?Playable(animator,semantic):Clip(variant.Item2,semantic);int frames=Mathf.CeilToInt(clip.length*2*20);
                    camera.transform.position=framingCenter+(view=="side"?new Vector3(0,.12f,-5):new Vector3(0,5,-.05f));camera.transform.LookAt(framingCenter,view=="top"?Vector3.forward:Vector3.up);clip.SampleAnimation(animator.gameObject,0);yield return null;yield return null;Draw(prefix+"-initial-unqualified");
                    Color32[] last=null;float minimum=1,rasterMinimum=1;
                    for(int i=0;i<frames;i++){clip.SampleAnimation(animator.gameObject,(i/20f)%clip.length);yield return null;last=Draw(prefix+"-"+i.ToString("D3"));minimum=Mathf.Min(minimum,Margin());
                        var background=last[0];int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1,visible=0;
                        for(int row=0;row<texture.height;row++)for(int column=0;column<texture.width;column++){var pixel=last[row*texture.width+column];if(Mathf.Abs(pixel.r-background.r)<=2&&Mathf.Abs(pixel.g-background.g)<=2&&Mathf.Abs(pixel.b-background.b)<=2)continue;minX=Mathf.Min(minX,column);maxX=Mathf.Max(maxX,column);minY=Mathf.Min(minY,row);maxY=Mathf.Max(maxY,row);visible++;}
                        if(visible<5000)throw new InvalidOperationException("Motion fish is not sufficiently visible: "+prefix);
                        rasterMinimum=Mathf.Min(rasterMinimum,(float)minX/texture.width,(float)minY/texture.height,(float)(texture.width-1-maxX)/texture.width,(float)(texture.height-1-maxY)/texture.height);
                        if(minimum<.04f||rasterMinimum<.04f)throw new InvalidOperationException("Actual animated mesh is cropped: "+prefix+" frame "+i+" margin "+minimum);}
                    var repeat=Draw(prefix+"-repeat");int changed=repeat.Where((p,i)=>p.r!=last[i].r||p.g!=last[i].g||p.b!=last[i].b).Count();receipt.sequences.Add(new Sequence{prefix=prefix,frames=frames,repeatChangedPixels=changed,minViewportMargin=minimum,minRasterMargin=rasterMinimum});if(changed!=0)throw new InvalidOperationException("Repeated motion pose changed: "+prefix);
                }
                receipt.status="PASS";
            }
            finally
            {
                if(baked!=null)UnityEngine.Object.DestroyImmediate(baked);
                if(texture!=null){texture.Release();UnityEngine.Object.DestroyImmediate(texture);}if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(camera!=null)UnityEngine.Object.DestroyImmediate(camera.gameObject);if(sun!=null)UnityEngine.Object.DestroyImmediate(sun.gameObject);
                if(ownsFixture&&!AssetDatabase.DeleteAsset(fixture))throw new InvalidOperationException("Diagnostic fixture cleanup failed; preserve and reconcile");
                QualitySettings.renderPipeline=priorMedium;QualitySettings.SetQualityLevel(prior,true);GraphicsSettings.defaultRenderPipeline=priorDefault;
                File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));
            }
        }
    }
}
