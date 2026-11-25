using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace OceanAngler.Editor
{
    public static class SlopeAssetBuilder
    {
        const string Folder="Assets/OceanAngler/Art/ContinentalSlope",Bay="Assets/OceanAngler/Art/CoralBay";
        [Serializable] sealed class Station {public float z,width,keel;}
        [Serializable] sealed class Profile {public float[] origin;public float rimHeight;public Station[] stations;}
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            foreach(var path in Directory.GetFiles(Folder,"*.png"))
            {
                var texture=(TextureImporter)AssetImporter.GetAtPath(path);bool normal=path.Contains("_Normal");texture.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;texture.sRGBTexture=!normal&&!path.Contains("_Roughness")&&!path.Contains("_MetallicSmoothness");texture.maxTextureSize=1024;texture.textureCompression=TextureImporterCompression.CompressedHQ;texture.wrapMode=TextureWrapMode.Repeat;texture.SaveAndReimport();
            }
            var materials=new[]{Terrain(),Solid("SurveyYellow",new Color(.8f,.48f,.065f),.48f,.05f),Solid("FrameMetal",new Color(.22f,.25f,.29f),.33f,.65f),Solid("Optics",new Color(.018f,.045f,.07f),.15f,.1f),Solid("Rope",new Color(.12f,.18f,.2f),.8f,0),Solid("InstrumentWhite",new Color(.78f,.79f,.73f),.6f,0),Solid("Marker",new Color(.68f,.23f,.065f),.6f,0),Water()}.Concat(new[]{"BoatHull","BoatTrim","Metal","Wood","Rope"}.Select(name=>AssetDatabase.LoadAssetAtPath<Material>(Bay+"/Bay_"+name+".mat"))).ToArray();
            if(materials.Any(m=>m==null))throw new InvalidOperationException("Missing owned slope material dependency");
            var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"/ContinentalSlope.fbx");importer.importAnimation=false;importer.importVisibility=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var material in materials)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),material.name),material);importer.SaveAndReimport();
            var root=new GameObject("ContinentalSlopeCandidate");var align=new GameObject("CoordinateAlignment").transform;align.SetParent(root.transform,false);var imported=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/ContinentalSlope.fbx"));imported.transform.SetParent(align,false);PrefabUtility.UnpackPrefabInstance(imported,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            try
            {
                Transform Find(string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
                var origin=Find("OriginAnchor").position;align.rotation=Quaternion.Inverse(Quaternion.LookRotation(Find("NorthAnchor").position-origin,Find("UpAnchor").position-origin));align.position=-Find("OriginAnchor").position;
                foreach(var group in imported.GetComponentsInChildren<LODGroup>(true))UnityEngine.Object.DestroyImmediate(group);
                foreach(var part in new[]{"Terrain","Scree","ROV","Boat","Survey"})
                {
                    var holder=new GameObject(part+"DistanceGroup");holder.transform.SetParent(root.transform,false);var lods=new LOD[3];
                    for(int i=0;i<3;i++)
                    {
                        var renderer=Find(part+"_LOD"+i).GetComponent<Renderer>();renderer.transform.SetParent(holder.transform,true);renderer.gameObject.SetActive(true);renderer.enabled=true;renderer.shadowCastingMode=part=="Survey"?ShadowCastingMode.Off:ShadowCastingMode.On;lods[i]=new LOD(i==0?.3f:i==1?.1f:.015f,new[]{renderer});
                    }
                    var group=holder.AddComponent<LODGroup>();group.SetLODs(lods);group.RecalculateBounds();
                }
                var habitat=root.AddComponent<MarineHabitat>();habitat.SeaLevel=200;habitat.underwaterCamera=Find("UnderwaterCamera");habitat.underwaterTarget=Find("UnderwaterTarget");habitat.surfaceCamera=Find("SurfaceCamera");habitat.surfaceTarget=Find("SurfaceTarget");habitat.reefCamera=Find("ReefCamera");habitat.reefTarget=Find("ReefTarget");habitat.pierCamera=Find("BoatCamera");habitat.pierTarget=Find("BoatTarget");
                habitat.underwaterColor=new Color(.025f,.10f,.20f);habitat.surfaceColor=new Color(.48f,.66f,.77f);habitat.underwaterDensity=.018f;habitat.surfaceDensity=.004f;
                habitat.waterSurface=Find("Water_LOD0").GetComponent<Renderer>();habitat.waterSurface.gameObject.SetActive(true);habitat.waterSurface.gameObject.layer=4;habitat.waterSurface.shadowCastingMode=ShadowCastingMode.Off;habitat.waterSurface.receiveShadows=false;habitat.sky=AssetDatabase.LoadAssetAtPath<Material>(Bay+"/Bay_Sky.mat");habitat.reflection=root.AddComponent<BayPlanarReflection>();
                if(Mathf.Abs(habitat.waterSurface.bounds.center.y-habitat.SeaLevel)>.001f)throw new InvalidOperationException("Slope metre/sea-level alignment failed");
                Light Spot(string name,Vector3 position,Vector3 target,Color color,float intensity,float angle,float range)
                {
                    var light=new GameObject(name).AddComponent<Light>();light.transform.SetParent(root.transform,false);light.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));light.type=LightType.Spot;light.color=color;light.intensity=intensity;light.spotAngle=angle;light.range=range;light.shadows=LightShadows.None;return light;
                }
                habitat.lighting=root.AddComponent<HabitatLighting>();habitat.lighting.inspectionLights=new[]{Spot("RovWarmLampA",Find("LampA").position,new Vector3(-1,-3,8),new Color(1,.72f,.42f),4,85,24),Spot("RovWarmLampB",Find("LampB").position,new Vector3(-6,-7,20),new Color(1,.72f,.42f),4,85,20),Spot("ResearchCameraFill",new Vector3(0,2,-6),new Vector3(0,-3,14),new Color(1,.92f,.8f),36,80,20)};
                habitat.lighting.cameraFill=habitat.lighting.inspectionLights[2];
                var ps=new GameObject("SlopeMarineSnow").AddComponent<ParticleSystem>();ps.transform.SetParent(root.transform,false);ps.transform.localPosition=new Vector3(0,-1,18);ps.transform.localRotation=Quaternion.Euler(180,0,0);var main=ps.main;main.maxParticles=72;main.startLifetime=22;main.startSpeed=.012f;main.startSize=new ParticleSystem.MinMaxCurve(.006f,.016f);main.startColor=new Color(.55f,.68f,.82f,.12f);main.simulationSpace=ParticleSystemSimulationSpace.World;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(28,16,45);var emission=ps.emission;emission.rateOverTime=3;var particles=ps.GetComponent<ParticleSystemRenderer>();particles.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Bay+"/Bay_Particles.mat");particles.shadowCastingMode=ShadowCastingMode.Off;particles.receiveShadows=false;habitat.suspendedParticles=ps;
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/OceanAngler/Resources/Models/ContinentalSlope.prefab");AssetDatabase.SaveAssets();Debug.Log("SLOPE_UNITY_OK groups=5 lods=3 water=200 researchLights=3");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static Material Solid(string name,Color color,float roughness,float metallic)
        {
            string path=Folder+"/Slope_"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}material.color=color;material.SetColor("_Color",color);material.SetFloat("_Smoothness",1-roughness);material.SetFloat("_Metallic",metallic);EditorUtility.SetDirty(material);return material;
        }
        static Material Terrain()
        {
            string path=Folder+"/Slope_Terrain.mat";var shader=Shader.Find("OceanAngler/CoastalTerrain");if(shader==null)throw new InvalidOperationException("Terrain shader missing");var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}material.shader=shader;material.SetColor("_BaseColor",Color.white);material.SetFloat("_BumpScale",.4f);material.SetFloat("_TextureScale",.32f);
            foreach(var map in new[]{("_BaseMap","Strata"),("_BumpMap","Strata_Normal"),("_MetallicGlossMap","Strata_MetallicSmoothness"),("_SoilMap","Silt"),("_SoilNormal","Silt_Normal"),("_SoilMetallicGlossMap","Silt_MetallicSmoothness")})material.SetTexture(map.Item1,AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Slope_"+map.Item2+".png"));EditorUtility.SetDirty(material);return material;
        }
        static Material Water()
        {
            string path=Folder+"/Slope_Water.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(Shader.Find("OceanAngler/BayWater"));AssetDatabase.CreateAsset(material,path);}material.SetColor("_DeepColor",new Color(.015f,.08f,.18f));material.SetColor("_ShallowColor",new Color(.04f,.2f,.3f));material.SetFloat("_SeaLevel",200);
            string project=Directory.GetParent(Application.dataPath).Parent.FullName;var profile=JsonUtility.FromJson<Profile>(File.ReadAllText(Path.Combine(project,"art/source/continental-slope/boat-profile.json")));if(profile.origin.Length!=3||profile.stations.Length!=7||profile.origin[2]!=200)throw new InvalidOperationException("Slope hull profile mismatch");material.SetVector("_BoatOrigin",new Vector4(profile.origin[0],profile.origin[2],profile.origin[1],profile.rimHeight));for(int i=0;i<7;i++){var station=profile.stations[i];material.SetVector("_Boat"+i,new Vector4(station.z,station.width,station.keel,0));}EditorUtility.SetDirty(material);return material;
        }
    }
}
