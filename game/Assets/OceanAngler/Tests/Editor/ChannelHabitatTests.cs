using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace OceanAngler.Tests
{
    public sealed class ChannelHabitatTests
    {
        GameObject root,cameraObject;MarineHabitat habitat;Camera camera;Material priorSky;
        [SetUp] public void Setup(){priorSky=RenderSettings.skybox;root=Object.Instantiate(Resources.Load<GameObject>("Models/NearshoreChannel"));habitat=root.GetComponent<MarineHabitat>();cameraObject=new GameObject("ChannelTestCamera");camera=cameraObject.AddComponent<Camera>();}
        [TearDown] public void Cleanup(){Object.DestroyImmediate(root);Object.DestroyImmediate(cameraObject);RenderSettings.skybox=priorSky;}
        [Test] public void ImportedChannelHasSixIndependentLodGroupsAndMetreMarkers()
        {
            Assert.That(habitat,Is.Not.Null);Assert.That(root.GetComponent<BayHabitat>(),Is.Null);var groups=root.GetComponentsInChildren<LODGroup>();Assert.That(groups.Length,Is.EqualTo(6));var renderers=groups.SelectMany(g=>g.GetLODs()).SelectMany(l=>l.renderers).ToArray();Assert.That(renderers.Length,Is.EqualTo(18));Assert.That(renderers.Distinct().Count(),Is.EqualTo(18));
            foreach(var group in groups){Assert.That(group.lodCount,Is.EqualTo(3));foreach(var renderer in group.GetLODs().SelectMany(l=>l.renderers))Assert.That(renderer.transform.IsChildOf(group.transform),Is.True);}
            Assert.That(renderers.Contains(habitat.waterSurface),Is.False);Assert.That(habitat.waterSurface.bounds.center.y,Is.EqualTo(4.5f).Within(.001f));Assert.That(Vector3.Distance(habitat.underwaterCamera.position,new Vector3(0,1.25f,-6.5f)),Is.LessThan(.001f));Assert.That(Vector3.Distance(habitat.underwaterTarget.position,new Vector3(0,0,6)),Is.LessThan(.001f));
            Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty);
        }
        [Test] public void MarinePresentationRetainsBayDefaultsAndSwitchesChannelViews()
        {
            var bay=Object.Instantiate(Resources.Load<GameObject>("Models/CoralBay"));
            try{var original=bay.GetComponent<BayHabitat>();Assert.That(original.underwaterDensity,Is.EqualTo(.035f));Assert.That(original.surfaceDensity,Is.EqualTo(.012f));Assert.That(original.school,Is.Not.Null);Assert.That(original.waterSurface,Is.Not.Null);}finally{Object.DestroyImmediate(bay);}
            habitat.Present(camera,true,true,0,true,false);Assert.That(camera.transform.position.y,Is.GreaterThan(habitat.SeaLevel));Assert.That(camera.clearFlags,Is.EqualTo(CameraClearFlags.Skybox));Assert.That(habitat.suspendedParticles.gameObject.activeSelf,Is.False);
            habitat.Present(camera,true,false,1,true,false);Assert.That(Vector3.Distance(camera.transform.position,habitat.reefCamera.position),Is.LessThan(.001f));Assert.That(habitat.suspendedParticles.main.simulationSpeed,Is.Zero);
            habitat.Present(camera,false,false,0,true,true);Assert.That(Vector3.Distance(camera.transform.position,habitat.underwaterCamera.position),Is.LessThan(.001f));Assert.That(habitat.suspendedParticles.gameObject.activeSelf,Is.False);var block=new MaterialPropertyBlock();habitat.waterSurface.GetPropertyBlock(block);Assert.That(block.GetFloat("_OceanTime"),Is.Zero);Assert.That(habitat.reflection.Ready,Is.False);
        }
        [Test] public void ChannelPbrAndBoatWaterMaskMatchImportedInputs()
        {
            foreach(var name in new[]{"Sediment","Stone","Kelp","CoastalSoil"})
            {
                string path="Assets/OceanAngler/Art/NearshoreChannel/Channel_"+name;var material=AssetDatabase.LoadAssetAtPath<Material>(path+".mat");Assert.That(material.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));foreach(string property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap"})Assert.That(material.GetTexture(property),Is.Not.Null);
                Assert.That(((TextureImporter)AssetImporter.GetAtPath(path+"_MetallicSmoothness.png")).sRGBTexture,Is.False);Assert.That(System.IO.File.ReadAllBytes(Application.dataPath+path.Substring(6)+"_MetallicSmoothness.png")[25],Is.EqualTo(6));
            }
            var water=habitat.waterSurface.sharedMaterial;Assert.That(water.shader.name,Is.EqualTo("OceanAngler/BayWater"));Assert.That(ShaderUtil.ShaderHasError(water.shader),Is.False);Assert.That(water.GetVector("_BoatOrigin"),Is.EqualTo(new Vector4(-5,4.5f,6,.48f)));
            Assert.That(root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).All(m=>m!=null),Is.True);Assert.That(habitat.causticLight.cookie,Is.Not.Null);
        }
        [Test] public void TerrainBlendSurvivesImportAndAllDistanceLevels()
        {
            var filters=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.StartsWith("Islands_LOD")).ToArray();Assert.That(filters.Length,Is.EqualTo(3));
            foreach(var filter in filters)
            {
                var mesh=filter.sharedMesh;var colors=mesh.colors;Assert.That(colors.Length,Is.EqualTo(mesh.vertexCount));Assert.That(colors.All(c=>!float.IsNaN(c.r)&&!float.IsInfinity(c.r)&&c.r>=0&&c.r<=1),Is.True);Assert.That(colors.Min(c=>c.r),Is.LessThan(.01f));Assert.That(colors.Max(c=>c.r),Is.GreaterThan(.3f));Assert.That(colors.Count(c=>c.r>.01f&&c.r<.99f),Is.GreaterThan(100));Assert.That(mesh.tangents.Length,Is.EqualTo(mesh.vertexCount));
                var materials=filter.GetComponent<Renderer>().sharedMaterials;Assert.That(materials.Length,Is.EqualTo(1));var material=materials[0];Assert.That(material.shader.name,Is.EqualTo("OceanAngler/CoastalTerrain"));Assert.That(ShaderUtil.ShaderHasError(material.shader),Is.False);foreach(string pass in new[]{"ForwardLit","ShadowCaster","DepthOnly"})Assert.That(material.FindPass(pass),Is.GreaterThanOrEqualTo(0));foreach(string map in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap","_SoilMap","_SoilNormal","_SoilMetallicGlossMap"})Assert.That(material.GetTexture(map),Is.Not.Null);
            }
        }
        [Test] public void ChannelOpenCorridorClearsCurrentFishingAndObservationVolumes()
        {
            var triangles=new System.Collections.Generic.List<Bounds>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!filter.name.Contains("_LOD")||filter.name.StartsWith("Water_"))continue;var vertices=filter.sharedMesh.vertices.Select(filter.transform.TransformPoint).ToArray();var indices=filter.sharedMesh.triangles;
                for(int i=0;i<indices.Length;i+=3){var b=new Bounds(vertices[indices[i]],Vector3.zero);b.Encapsulate(vertices[indices[i+1]]);b.Encapsulate(vertices[indices[i+2]]);triangles.Add(b);}
            }
            int fishing=0,observed=0;
            foreach(float power in new[]{.3f,.6f,1f})foreach(float lane in new[]{-1.3f,0,1.3f})foreach(var phase in new[]{FishingPhase.Waiting,FishingPhase.Fighting})for(int step=0;step<=10;step++)
            {
                var pose=FishingEncounter.Evaluate(phase,step/10f,power,Vector3.left,Vector3.up*2,lane,0,false);var volume=new Bounds(pose.fishPosition,Vector3.zero);for(int corner=0;corner<8;corner++)volume.Encapsulate(pose.fishPosition+pose.fishRotation*new Vector3((corner&1)==0?-2.4f:2.4f,(corner&2)==0?-1.6f:1.6f,(corner&4)==0?-1.5f:1.5f));foreach(var triangle in triangles)if(volume.Intersects(triangle))Assert.Fail("Prototype fishing safety volume intersects environment");fishing++;
            }
            var visit=new GameObject("ChannelObservationBounds").AddComponent<ObservationEncounter>();
            try
            {
                foreach(string id in new[]{"bottlenose-dolphin","whale-shark"})foreach(bool compact in new[]{false,true})
                {
                    visit.Present(id,camera,compact,false,0);if(!visit.Playing)visit.Toggle();Assert.That(visit.Playing,Is.True);for(int step=0;step<96;step++){visit.Present(id,camera,compact,false,.25f);var renderers=visit.Visual.GetComponentsInChildren<Renderer>();var volume=renderers[0].bounds;foreach(var renderer in renderers)volume.Encapsulate(renderer.bounds);foreach(var triangle in triangles)if(volume.Intersects(triangle))Assert.Fail("A0 observation volume intersects environment");observed++;}
                }
            }
            finally{Object.DestroyImmediate(visit.gameObject);}
            Debug.Log("CHANNEL_CORRIDOR_CLEAR fishing="+fishing+" observation="+observed+" triangles="+triangles.Count);
        }
    }
}
