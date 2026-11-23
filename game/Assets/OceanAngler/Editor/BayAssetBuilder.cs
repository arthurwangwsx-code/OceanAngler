using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace OceanAngler.Editor
{
    public static class BayAssetBuilder
    {
        [Serializable] sealed class BoatStation {public float z,width,keel;}
        [Serializable] sealed class BoatProfile {public float[] origin;public float rimHeight;public BoatStation[] stations;}
        const string Folder="Assets/OceanAngler/Art/CoralBay";
        static readonly string[] Parts={"Terrain","Reef","BranchingCoral","PlateCoral","DomeCoral","Seagrass","Pier","Boat"};
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(Folder,"*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);bool normal=path.Contains("_Normal"),linear=path.Contains("_Roughness")||path.Contains("_MetallicSmoothness")||path.Contains("_Caustic");
                importer.textureType=normal?TextureImporterType.NormalMap:path.Contains("_Caustic")?TextureImporterType.Cookie:TextureImporterType.Default;importer.sRGBTexture=!normal&&!linear;importer.wrapMode=TextureWrapMode.Repeat;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.maxTextureSize=512;
                if(path.Contains("_Caustic"))importer.alphaSource=TextureImporterAlphaSource.FromGrayScale;
                importer.SaveAndReimport();
            }
            var mats=new[]{Create("Sand",Color.white,.15f,true),Create("Limestone",Color.white,.16f,true),Create("Wood",Color.white,.3f,true),Create("BranchCoral",Color.white,.22f,true),Create("PlateCoral",Color.white,.2f,true),Create("DomeCoral",Color.white,.14f,true),Create("Seagrass",new Color(.12f,.27f,.12f),.32f),Create("Algae",new Color(.16f,.25f,.12f),.15f),Create("Rope",new Color(.4f,.34f,.23f),.1f),Create("BoatHull",new Color(.67f,.73f,.7f),.62f),Create("BoatTrim",new Color(.15f,.38f,.4f),.64f),Create("Metal",new Color(.28f,.33f,.33f),.62f,false,.72f),Water()};
            var model=(ModelImporter)AssetImporter.GetAtPath(Folder+"/CoralBay.fbx");model.importAnimation=false;model.importVisibility=false;model.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var mat in mats)model.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),mat.name),mat);model.SaveAndReimport();
            var root=new GameObject("CoralBayCandidate");var align=new GameObject("CoordinateAlignment").transform;align.SetParent(root.transform,false);
            var imported=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/CoralBay.fbx"));imported.transform.SetParent(align,false);PrefabUtility.UnpackPrefabInstance(imported,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            try
            {
                Transform Find(string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
                var origin=Find("OriginAnchor").position;var north=Find("NorthAnchor").position-origin;var up=Find("UpAnchor").position-origin;
                align.rotation=Quaternion.Inverse(Quaternion.LookRotation(north,up));align.position=-Find("OriginAnchor").position;
                foreach(var group in imported.GetComponentsInChildren<LODGroup>(true))UnityEngine.Object.DestroyImmediate(group);
                foreach(var animator in imported.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(animator);
                foreach(var part in Parts)
                {
                    var holder=new GameObject(part+"DistanceGroup");holder.transform.SetParent(root.transform,false);var levels=new LOD[3];
                    for(int i=0;i<3;i++)
                    {
                        var renderer=Find(part+"_LOD"+i).GetComponent<Renderer>();renderer.transform.SetParent(holder.transform,true);renderer.gameObject.SetActive(true);renderer.enabled=true;
                        renderer.shadowCastingMode=part=="Seagrass"?ShadowCastingMode.Off:ShadowCastingMode.On;
                        levels[i]=new LOD(i==0?.30f:i==1?.10f:.015f,new[]{renderer});
                    }
                    var group=holder.AddComponent<LODGroup>();group.SetLODs(levels);group.RecalculateBounds();
                }
                var bay=root.AddComponent<BayHabitat>();bay.underwaterCamera=Find("UnderwaterCamera");bay.underwaterTarget=Find("UnderwaterTarget");bay.surfaceCamera=Find("SurfaceCamera");bay.surfaceTarget=Find("SurfaceTarget");
                Transform Marker(string name,Vector3 p){var t=new GameObject(name).transform;t.SetParent(root.transform,false);t.localPosition=p;return t;}
                bay.reefCamera=Marker("ReefCamera",new Vector3(3,1.1f,4));bay.reefTarget=Marker("ReefTarget",new Vector3(5,-.1f,11));bay.pierCamera=Marker("PierCamera",new Vector3(-.3f,6.7f,1));bay.pierTarget=Marker("PierTarget",new Vector3(-4,4.5f,4));
                bay.waterSurface=Find("Water_LOD0").GetComponent<Renderer>();bay.waterSurface.gameObject.SetActive(true);bay.waterSurface.shadowCastingMode=ShadowCastingMode.Off;bay.waterSurface.receiveShadows=false;
                bay.waterSurface.gameObject.layer=4;bay.sky=Sky();bay.reflection=root.AddComponent<BayPlanarReflection>();
                if(Mathf.Abs(bay.waterSurface.bounds.center.y-4.5f)>.001f)throw new InvalidOperationException("Bay water coordinates do not match metres");
                var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OceanAngler/Generated/OceanURP.asset");pipeline.supportsCameraDepthTexture=true;pipeline.supportsCameraOpaqueTexture=true;EditorUtility.SetDirty(pipeline);
                // Transparent water needs this frame's opaque depth, before its own pass.
                var rendererData=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/OceanAngler/Generated/OceanRenderer.asset");rendererData.copyDepthMode=CopyDepthMode.AfterOpaques;EditorUtility.SetDirty(rendererData);
                var caustic=new GameObject("ProjectedCaustics").AddComponent<Light>();caustic.transform.SetParent(root.transform,false);caustic.transform.localPosition=new Vector3(0,4.2f,8);caustic.transform.localRotation=Quaternion.Euler(90,0,0);caustic.type=LightType.Spot;caustic.spotAngle=110;caustic.range=22;caustic.intensity=14;caustic.color=new Color(.44f,.79f,.76f);caustic.shadows=LightShadows.None;caustic.cookie=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Bay_Caustic.png");bay.causticLight=caustic;
                bay.suspendedParticles=Particles(root.transform);
                var fill=new GameObject("WaterAmbientFill").AddComponent<Light>();fill.transform.SetParent(root.transform,false);fill.transform.localPosition=new Vector3(0,3,4);fill.type=LightType.Point;fill.range=18;fill.intensity=1.3f;fill.color=new Color(.2f,.45f,.5f);fill.shadows=LightShadows.None;
                if(!AssetDatabase.IsValidFolder("Assets/OceanAngler/Resources/Models"))AssetDatabase.CreateFolder("Assets/OceanAngler/Resources","Models");
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/OceanAngler/Resources/Models/CoralBay.prefab");AssetDatabase.SaveAssets();
                Debug.Log("CORAL_BAY_UNITY_OK groups=8 lods=3 water=separate");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static Material Water()
        {
            var shader=Shader.Find("OceanAngler/BayWater");if(shader==null)throw new InvalidOperationException("Water shader missing");
            if(ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Water shader has import errors");
            string path=Folder+"/Bay_Water.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}mat.shader=shader;
            mat.SetColor("_DeepColor",new Color(.045f,.29f,.36f));
            var project=Directory.GetParent(Application.dataPath).Parent.FullName;var profile=JsonUtility.FromJson<BoatProfile>(File.ReadAllText(Path.Combine(project,"art/source/coral-bay/boat-profile.json")));
            if(profile.origin.Length!=3||profile.stations.Length!=7)throw new InvalidOperationException("Boat/water profile identity mismatch");
            mat.SetVector("_BoatOrigin",new Vector4(profile.origin[0],profile.origin[2],profile.origin[1],profile.rimHeight));
            for(int i=0;i<7;i++){var s=profile.stations[i];mat.SetVector("_Boat"+i,new Vector4(s.z,s.width,s.keel,0));}
            EditorUtility.SetDirty(mat);return mat;
        }
        static Material Sky()
        {
            const string path=Folder+"/Bay_Sky.mat";var shader=Shader.Find("Skybox/Procedural");if(shader==null)throw new InvalidOperationException("Procedural sky shader missing");
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_SkyTint",new Color(.5f,.5f,.5f));mat.SetColor("_GroundColor",new Color(.369f,.349f,.341f));mat.SetFloat("_Exposure",1);mat.SetFloat("_AtmosphereThickness",1);mat.SetFloat("_SunDisk",2);mat.EnableKeyword("_SUNDISK_HIGH_QUALITY");EditorUtility.SetDirty(mat);return mat;
        }
        static Material Create(string name,Color color,float smooth,bool textured=false,float metal=0)
        {
            string path=Folder+"/Bay_"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.color=color;mat.SetColor("_Color",color);mat.SetFloat("_Smoothness",smooth);mat.SetFloat("_Metallic",metal);
            if(name=="Seagrass")mat.SetFloat("_Cull",0);
            if(textured){mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Bay_"+name+".png"));mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Bay_"+name+"_Normal.png"));mat.SetFloat("_BumpScale",.4f);mat.EnableKeyword("_NORMALMAP");mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Bay_"+name+"_MetallicSmoothness.png"));mat.SetFloat("_Smoothness",1);mat.EnableKeyword("_METALLICSPECGLOSSMAP");}
            EditorUtility.SetDirty(mat);return mat;
        }
        static ParticleSystem Particles(Transform parent)
        {
            const string path=Folder+"/Bay_Particles.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Bay_Particle.png"));mat.SetFloat("_Surface",1);mat.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);mat.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);mat.SetInt("_ZWrite",0);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=3000;EditorUtility.SetDirty(mat);
            var ps=new GameObject("SuspendedParticles").AddComponent<ParticleSystem>();ps.transform.SetParent(parent,false);ps.transform.localPosition=new Vector3(0,1,8);
            var main=ps.main;main.maxParticles=72;main.startLifetime=14;main.startSpeed=.015f;main.startSize=new ParticleSystem.MinMaxCurve(.006f,.02f);main.startColor=new Color(.55f,.8f,.8f,.18f);main.simulationSpace=ParticleSystemSimulationSpace.World;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(16,4,20);var emission=ps.emission;emission.rateOverTime=5;
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return ps;
        }
    }
}
