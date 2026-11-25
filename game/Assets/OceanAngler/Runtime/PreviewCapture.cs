using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
namespace OceanAngler
{
    // 仅在显式命令行预览模式写本地截图；不捕获或上传用户屏幕。
    public sealed class PreviewCapture : MonoBehaviour
    {
        [Serializable] sealed class SimulatorProbe
        {
            public string scope="explicit development-player diagnostic, not device qualification",graphics,deviceType,quality;
            public int nativeSamples,desiredSamples,ldrSamples,packedHdrSamples,halfHdrSamples;public bool hdrBefore,hdrAfter,ldrControl;
            public string[] heroMotionClips;public bool[] heroMotionLoops;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ProbeSimulatorRendering()
        {
#if UNITY_IOS && DEVELOPMENT_BUILD
            var args=LaunchArguments.Get();int index=Array.IndexOf(args,"--ocean-simulator-probe");
            if(index<0||index+1>=args.Length)return;
            var pipeline=(QualitySettings.renderPipeline??GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
            if(pipeline==null)return;
            int Samples(GraphicsFormat format)=>SystemInfo.GetRenderTextureSupportedMSAASampleCount(new RenderTextureDescriptor(64,64){graphicsFormat=format,depthBufferBits=24,msaaSamples=pipeline.msaaSampleCount});
            var receipt=new SimulatorProbe{graphics=SystemInfo.graphicsDeviceType.ToString(),deviceType=SystemInfo.deviceType.ToString(),quality=QualitySettings.names[QualitySettings.GetQualityLevel()],nativeSamples=QualitySettings.antiAliasing,desiredSamples=pipeline.msaaSampleCount,ldrSamples=Samples(GraphicsFormat.R8G8B8A8_UNorm),packedHdrSamples=Samples(GraphicsFormat.B10G11R11_UFloatPack32),halfHdrSamples=Samples(GraphicsFormat.R16G16B16A16_SFloat),hdrBefore=pipeline.supportsHDR,ldrControl=Array.IndexOf(args,"--ocean-simulator-ldr-control")>=0};
            var hero=Resources.Load<GameObject>("Models/Bluefin");var animator=hero==null?null:hero.GetComponentInChildren<Animator>();
            if(animator!=null&&animator.runtimeAnimatorController!=null)
            {
                var clips=animator.runtimeAnimatorController.animationClips;
                receipt.heroMotionClips=Array.ConvertAll(clips,clip=>clip.name);receipt.heroMotionLoops=Array.ConvertAll(clips,clip=>clip.isLooping);
            }
            if(receipt.ldrControl)pipeline.supportsHDR=false;
            receipt.hdrAfter=pipeline.supportsHDR;string path=args[index+1];Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(receipt,true));
#endif
        }
        IEnumerator Start()
        {
            var args=LaunchArguments.Get();int index=Array.IndexOf(args,"--ocean-capture");
            if(index<0||index+1>=args.Length)yield break;
            Application.runInBackground=true;
            yield return new WaitForSecondsRealtime(3);
            yield return new WaitForEndOfFrame();
            string path=args[index+1];Directory.CreateDirectory(Path.GetDirectoryName(path));
            var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path,image.EncodeToPNG());Destroy(image);
            Debug.Log("OCEAN_CAPTURE_OK "+path);
            Application.Quit();
        }
    }
}
