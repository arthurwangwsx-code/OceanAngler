#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;
namespace OceanAngler.Editor
{
    public static class IOSPostprocess
    {
        [PostProcessBuild(90)] public static void Configure(BuildTarget target,string output)
        {
            if(target!=BuildTarget.iOS)return;
            string projectPath=PBXProject.GetPBXProjectPath(output);var p=new PBXProject();p.ReadFromFile(projectPath);
            string app=p.GetUnityMainTargetGuid(),framework=p.GetUnityFrameworkTargetGuid();
            p.SetBuildProperty(app,"CODE_SIGN_STYLE","Manual");p.SetBuildProperty(app,"PROVISIONING_PROFILE_SPECIFIER","$(OCEAN_PROFILE_NAME)");p.SetBuildProperty(app,"PROVISIONING_PROFILE","$(OCEAN_PROFILE_UUID)");
            p.SetBuildProperty(framework,"CODE_SIGN_STYLE","Manual");p.SetBuildProperty(framework,"PROVISIONING_PROFILE_SPECIFIER","");p.SetBuildProperty(framework,"PROVISIONING_PROFILE","");
            p.WriteToFile(projectPath);
            var plist=new PlistDocument();string path=Path.Combine(output,"Info.plist");plist.ReadFromFile(path);plist.root.SetBoolean("ITSAppUsesNonExemptEncryption",false);plist.WriteToFile(path);
        }
    }
}
#endif
