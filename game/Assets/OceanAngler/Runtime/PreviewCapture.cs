using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace OceanAngler
{
    // 仅在显式命令行预览模式写本地截图；不捕获或上传用户屏幕。
    public sealed class PreviewCapture : MonoBehaviour
    {
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--ocean-capture");
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
