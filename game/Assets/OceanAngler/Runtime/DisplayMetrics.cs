using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
namespace OceanAngler
{
    public static class DisplayMetrics
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern double OceanLandscapePointWidth();
#endif
        public static float PixelsPerPoint
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                double points=OceanLandscapePointWidth();
                return points>0?(float)(Screen.width/points):1;
#else
                var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--ocean-point-scale");
                return index>=0&&index+1<args.Length&&float.TryParse(args[index+1],NumberStyles.Float,CultureInfo.InvariantCulture,out float scale)?Mathf.Clamp(scale,1,4):1;
#endif
            }
        }
        public static Rect SafeArea
        {
            get
            {
#if !UNITY_IOS || UNITY_EDITOR
                var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--ocean-safe-insets");
                if(index>=0&&index+1<args.Length)
                {
                    var v=args[index+1].Split(',');
                    if(v.Length==4&&float.TryParse(v[0],out float left)&&float.TryParse(v[1],out float bottom)&&float.TryParse(v[2],out float right)&&float.TryParse(v[3],out float top))
                        return new Rect(left,bottom,Screen.width-left-right,Screen.height-bottom-top);
                }
#endif
                return Screen.safeArea;
            }
        }
    }
}
