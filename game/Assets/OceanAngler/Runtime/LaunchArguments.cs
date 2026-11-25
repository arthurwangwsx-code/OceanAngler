using System;
using System.Runtime.InteropServices;
namespace OceanAngler
{
    // iOS launch services supplies arguments through NSProcessInfo; IL2CPP's
    // managed main-argument list can be empty in a Unity iOS player.
    public static class LaunchArguments
    {
        static readonly Lazy<string[]> arguments=new Lazy<string[]>(Read);
        public static string[] Get()=>arguments.Value;
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int OceanLaunchArgumentCount();
        [DllImport("__Internal")] static extern IntPtr OceanLaunchArgumentAt(int index);
#endif
        static string[] Read()
        {
#if UNITY_IOS && !UNITY_EDITOR
            int count=OceanLaunchArgumentCount();
            if(count>0)
            {
                var result=new string[count];
                for(int i=0;i<count;i++)result[i]=Marshal.PtrToStringAnsi(OceanLaunchArgumentAt(i))??string.Empty;
                return result;
            }
#endif
            return Environment.GetCommandLineArgs();
        }
    }
}
