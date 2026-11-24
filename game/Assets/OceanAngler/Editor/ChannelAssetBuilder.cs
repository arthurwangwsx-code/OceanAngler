using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace OceanAngler.Editor
{
    public static class ChannelAssetBuilder
    {
        const string Folder="Assets/OceanAngler/Art/NearshoreChannel",Bay="Assets/OceanAngler/Art/CoralBay";
        [Serializable] sealed class Station{public float z,width,keel;}
        [Serializable] sealed class Profile{public float[] origin;public float rimHeight;public Station[] stations;}
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            foreach(var path in Directory.GetFiles(Folder,"*.png"))
            {
                var texture=(TextureImporter)AssetImporter.GetAtPath(path);bool normal=path.Contains("_Normal"),linear=normal||path.Contains("_Roughness")||path.Contains("_MetallicSmoothness");texture.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;texture.sRGBTexture=!linear;texture.maxTextureSize=512;texture.textureCompression=TextureImporterCompression.CompressedHQ;texture.wrapMode=TextureWrapMode.Repeat;texture.SaveAndReimport();
            }
            var materials=new[]{Create("Sediment",Color.white,true),Create("Stone",Color.white,true),Create("Kelp",Color.white,true),Create("CoastalSoil",Color.white,true),Create("Buoy",new Color(.62f,.22f,.12f),false),Create("Mooring",new Color(.38f,.34f,.25f),false),Water()}.Concat(new[]{"BoatHull","BoatTrim","Metal","Wood","Rope"}.Select(name=>AssetDatabase.LoadAssetAtPath<Material>(Bay+"/Bay_"+name+".mat"))).ToArray();
            if(materials.Any(m=>m==null))throw new InvalidOperationException("Missing owned material dependency");
            var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"/NearshoreChannel.fbx");importer.importAnimation=false;importer.importVisibility=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var material in materials)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),material.name),material);importer.SaveAndReimport();
            var root=new GameObject("NearshoreChannelCandidate");var align=new GameObject("CoordinateAlignment").transform;align.SetParent(root.transform,false);var imported=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/NearshoreChannel.fbx"));imported.transform.SetParent(align,false);PrefabUtility.UnpackPrefabInstance(imported,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            try
            {
                Transform Find(string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
                var origin=Find("OriginAnchor").position;align.rotation=Quaternion.Inverse(Quaternion.LookRotation(Find("NorthAnchor").position-origin,Find("UpAnchor").position-origin));align.position=-Find("OriginAnchor").position;
                foreach(var group in imported.GetComponentsInChildren<LODGroup>(true))UnityEngine.Object.DestroyImmediate(group);
                foreach(var part in new[]{"Seabed","Islands","Outcrops","Kelp","Boat","Mooring"})
                {
                    var holder=new GameObject(part+"DistanceGroup");holder.transform.SetParent(root.transform,false);var levels=new LOD[3];
                    for(int i=0;i<3;i++){var renderer=Find(part+"_LOD"+i).GetComponent<Renderer>();renderer.transform.SetParent(holder.transform,true);renderer.gameObject.SetActive(true);renderer.enabled=true;renderer.shadowCastingMode=part=="Kelp"?ShadowCastingMode.Off:ShadowCastingMode.On;levels[i]=new LOD(i==0?.3f:i==1?.1f:.015f,new[]{renderer});}
                    var group=holder.AddComponent<LODGroup>();group.SetLODs(levels);group.RecalculateBounds();
                }
                var habitat=root.AddComponent<MarineHabitat>();habitat.underwaterCamera=Find("UnderwaterCamera");habitat.underwaterTarget=Find("UnderwaterTarget");habitat.surfaceCamera=Find("SurfaceCamera");habitat.surfaceTarget=Find("SurfaceTarget");habitat.reefCamera=Find("ReefCamera");habitat.reefTarget=Find("ReefTarget");habitat.pierCamera=Find("BoatCamera");habitat.pierTarget=Find("BoatTarget");
                habitat.underwaterColor=new Color(.045f,.24f,.35f);habitat.surfaceColor=new Color(.51f,.70f,.79f);habitat.underwaterDensity=.022f;habitat.surfaceDensity=.006f;
                habitat.waterSurface=Find("Water_LOD0").GetComponent<Renderer>();habitat.waterSurface.gameObject.SetActive(true);habitat.waterSurface.gameObject.layer=4;habitat.waterSurface.shadowCastingMode=ShadowCastingMode.Off;habitat.waterSurface.receiveShadows=false;habitat.sky=AssetDatabase.LoadAssetAtPath<Material>(Bay+"/Bay_Sky.mat");habitat.reflection=root.AddComponent<BayPlanarReflection>();
                if(Mathf.Abs(habitat.waterSurface.bounds.center.y-4.5f)>.001f)throw new InvalidOperationException("Channel coordinate alignment failed");
                var caustic=new GameObject("ChannelCaustics").AddComponent<Light>();caustic.transform.SetParent(root.transform,false);caustic.transform.localPosition=new Vector3(0,4.2f,16);caustic.transform.localRotation=Quaternion.Euler(90,0,0);caustic.type=LightType.Spot;caustic.spotAngle=110;caustic.range=30;caustic.intensity=10;caustic.color=new Color(.35f,.65f,.8f);caustic.cookie=AssetDatabase.LoadAssetAtPath<Texture2D>(Bay+"/Bay_Caustic.png");caustic.shadows=LightShadows.None;habitat.causticLight=caustic;
                var ps=new GameObject("ChannelSuspendedParticles").AddComponent<ParticleSystem>();ps.transform.SetParent(root.transform,false);ps.transform.localPosition=new Vector3(0,0,14);var main=ps.main;main.maxParticles=48;main.startLifetime=16;main.startSpeed=.012f;main.startSize=new ParticleSystem.MinMaxCurve(.006f,.018f);main.startColor=new Color(.55f,.74f,.85f,.15f);main.simulationSpace=ParticleSystemSimulationSpace.World;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(18,7,24);var emission=ps.emission;emission.rateOverTime=3;var pr=ps.GetComponent<ParticleSystemRenderer>();pr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Bay+"/Bay_Particles.mat");pr.shadowCastingMode=ShadowCastingMode.Off;pr.receiveShadows=false;habitat.suspendedParticles=ps;
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/OceanAngler/Resources/Models/NearshoreChannel.prefab");AssetDatabase.SaveAssets();Debug.Log("CHANNEL_UNITY_OK groups=6 lods=3 water=separate");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static Material Create(string name,Color color,bool textured)
        {
            string path=Folder+"/Channel_"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}material.color=color;material.SetColor("_Color",color);material.SetFloat("_Smoothness",name=="Buoy"?.5f:.1f);
            if(textured){material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Channel_"+name+".png"));material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Channel_"+name+"_Normal.png"));material.SetFloat("_BumpScale",.35f);material.EnableKeyword("_NORMALMAP");material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Channel_"+name+"_MetallicSmoothness.png"));material.SetFloat("_Smoothness",1);material.EnableKeyword("_METALLICSPECGLOSSMAP");}EditorUtility.SetDirty(material);return material;
        }
        static Material Water()
        {
            string path=Folder+"/Channel_Water.mat";var shader=Shader.Find("OceanAngler/BayWater");var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}material.SetColor("_DeepColor",new Color(.025f,.22f,.31f));material.SetColor("_ShallowColor",new Color(.075f,.38f,.48f));
            var project=Directory.GetParent(Application.dataPath).Parent.FullName;var profile=JsonUtility.FromJson<Profile>(File.ReadAllText(Path.Combine(project,"art/source/nearshore-channel/boat-profile.json")));if(profile.origin.Length!=3||profile.stations.Length!=7)throw new InvalidOperationException("Channel hull profile mismatch");material.SetVector("_BoatOrigin",new Vector4(profile.origin[0],profile.origin[2],profile.origin[1],profile.rimHeight));for(int i=0;i<7;i++){var s=profile.stations[i];material.SetVector("_Boat"+i,new Vector4(s.z,s.width,s.keel,0));}EditorUtility.SetDirty(material);return material;
        }
    }
}
