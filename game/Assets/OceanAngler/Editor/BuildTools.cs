using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace OceanAngler.Editor
{
    public static class BuildTools
    {
        [Serializable] sealed class ReleaseConfig { public string bundleId,appId,teamId,version,build,displayName; }
        public static void Prepare()
        {
            AudioAssetBuilder.Prepare();
            const string folder = "Assets/OceanAngler/Generated";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/OceanAngler", "Generated");
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(folder + "/OceanURP.asset");
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, folder + "/OceanRenderer.asset");
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, folder + "/OceanURP.asset");
            }
            const string surfacePath = "Assets/OceanAngler/Resources/Surface.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(surfacePath) == null)
                AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Lit")), surfacePath);
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            QualitySettings.antiAliasing=4;
            pipeline.msaaSampleCount=4;EditorUtility.SetDirty(pipeline);
            var waterRenderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(folder+"/OceanRenderer.asset");waterRenderer.copyDepthMode=CopyDepthMode.AfterOpaques;EditorUtility.SetDirty(waterRenderer);
            pipeline.shadowDistance=35;pipeline.mainLightShadowmapResolution=1024;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientLight = new Color(0.35f, 0.55f, 0.65f);
            // Keep the runtime EXP2 fog variant in scene-based shader stripping.
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.035f;RenderSettings.fogColor=new Color(.075f,.42f,.49f);
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 3, -7);
            camera.transform.LookAt(new Vector3(0, 0, 2));
            camera.backgroundColor = new Color(0.1f, 0.45f, 0.65f); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.gameObject.AddComponent<AudioListener>();
            var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.3f;
            light.shadows=LightShadows.Hard;light.shadowStrength=.7f;
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            var root = new GameObject("OceanGame");
            root.AddComponent<GameController>();
            root.AddComponent<GameUI>();
            root.AddComponent<OceanWorld>();
            root.AddComponent<PreviewCapture>();
            root.AddComponent<RuntimeSmoke>();
            root.AddComponent<FeedbackPlayer>();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), folder + "/Bay.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(folder + "/Bay.unity", true) };
            var release=JsonUtility.FromJson<ReleaseConfig>(System.IO.File.ReadAllText(System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).Parent.FullName,"publication/release.json")));
            PlayerSettings.companyName = "OceanAngler"; PlayerSettings.productName = release.displayName;
            PlayerSettings.bundleVersion = release.version;PlayerSettings.iOS.buildNumber=release.build;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, release.bundleId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
            PlayerSettings.iOS.targetOSVersionString = "17.0";PlayerSettings.iOS.targetDevice=iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;PlayerSettings.iOS.requiresFullScreen=true;
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;PlayerSettings.iOS.appleDeveloperTeamID=release.teamId;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            var icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OceanAngler/Art/AppIcon.png");
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown,new[]{icon},IconKind.Any);
            AssetDatabase.SaveAssets();
            Debug.Log("OCEAN_PREPARE_OK");
        }
        static void Primitive(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color color)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name; obj.transform.position = pos; obj.transform.localScale = scale;
            var path = "Assets/OceanAngler/Generated/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.color = color; EditorUtility.SetDirty(mat);
            obj.GetComponent<Renderer>().sharedMaterial = mat;
        }
        public static void ApplyRenderQuality()
        {
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OceanAngler/Generated/OceanURP.asset");
            if(pipeline==null)throw new InvalidOperationException("Missing canonical URP asset");
            pipeline.msaaSampleCount=4;EditorUtility.SetDirty(pipeline);
        }
        public static void ExportMac()
        {
            AudioAssetBuilder.Validate();
            var output = Environment.GetEnvironmentVariable("OCEAN_MAC_OUTPUT");
            if (string.IsNullOrEmpty(output)) throw new Exception("Mac output required");
            var result = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Select(x => x.path).ToArray(), output, BuildTarget.StandaloneOSX, BuildOptions.Development);
            if (result.summary.result != BuildResult.Succeeded) throw new Exception("Mac build failed");
        }
        public static void ExportIOS()
        {
            AudioAssetBuilder.Validate();
            var number = Environment.GetEnvironmentVariable("OCEAN_BUILD_NUMBER");
            if (!int.TryParse(number, out int n) || n < 1) throw new Exception("Positive build number required");
            if(PlayerSettings.iOS.buildNumber!=number)throw new Exception("Build number must match committed release configuration; run prepare first");
            var output = Environment.GetEnvironmentVariable("OCEAN_IOS_OUTPUT");
            if (string.IsNullOrEmpty(output) || System.IO.Directory.Exists(output)) throw new Exception("A new export path is required");
            var result = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Select(x => x.path).ToArray(), output, BuildTarget.iOS, BuildOptions.None);
            if (result.summary.result != BuildResult.Succeeded) throw new Exception("iOS export failed");
        }
    }
}
