using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace OceanAngler.Tests
{
    public sealed class SlopeHabitatTests
    {
        GameObject root,cameraObject,sunObject;MarineHabitat habitat;Camera camera;Material sky;Color ambient,fog;AmbientMode mode;Light sun;
        [SetUp] public void Setup()
        {
            sky=RenderSettings.skybox;ambient=RenderSettings.ambientLight;mode=RenderSettings.ambientMode;fog=RenderSettings.fogColor;sun=RenderSettings.sun;
            sunObject=new GameObject("SlopeTestSun");var light=sunObject.AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(.9f,.8f,.7f);light.intensity=1.3f;RenderSettings.sun=light;
            root=Object.Instantiate(Resources.Load<GameObject>("Models/ContinentalSlope"));habitat=root.GetComponent<MarineHabitat>();cameraObject=new GameObject("SlopeTestCamera");camera=cameraObject.AddComponent<Camera>();
        }
        [TearDown] public void Cleanup(){Object.DestroyImmediate(root);Object.DestroyImmediate(cameraObject);Object.DestroyImmediate(sunObject);RenderSettings.sun=sun;RenderSettings.skybox=sky;RenderSettings.ambientLight=ambient;RenderSettings.ambientMode=mode;RenderSettings.fogColor=fog;}
        [Test] public void AllThreeHabitatsHaveAuthoredResourcesAndSlopeRetainsFiveLodAssemblies()
        {
            foreach(string id in new[]{"bay","channel","slope"})Assert.That(Resources.Load<GameObject>(MarineHabitat.ResourceFor(id)).GetComponent<MarineHabitat>(),Is.Not.Null);
            Assert.That(root.GetComponent<BayHabitat>(),Is.Null);var groups=root.GetComponentsInChildren<LODGroup>();Assert.That(groups.Length,Is.EqualTo(5));var renderers=groups.SelectMany(g=>g.GetLODs()).SelectMany(l=>l.renderers).ToArray();Assert.That(renderers.Length,Is.EqualTo(15));Assert.That(renderers.Distinct().Count(),Is.EqualTo(15));Assert.That(renderers.Contains(habitat.waterSurface),Is.False);Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty);
            foreach(var group in groups){Assert.That(group.lodCount,Is.EqualTo(3));foreach(var renderer in group.GetLODs().SelectMany(l=>l.renderers))Assert.That(renderer.transform.IsChildOf(group.transform),Is.True);}
            Assert.That(habitat.SeaLevel,Is.EqualTo(200));Assert.That(habitat.waterSurface.bounds.center.y,Is.EqualTo(200).Within(.001f));Assert.That(Vector3.Distance(habitat.underwaterCamera.position,new Vector3(0,1.25f,-6.5f)),Is.LessThan(.001f));Assert.That(habitat.surfaceCamera.position.y,Is.GreaterThan(200));Assert.That(habitat.lighting.inspectionLights.Length,Is.EqualTo(3));Assert.That(habitat.causticLight,Is.Null);
        }
        [Test] public void SedimentBlendAndHullMaskRetainTheirImportedPbrInputs()
        {
            foreach(var mesh in root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.StartsWith("Terrain_LOD")))
            {
                var colors=mesh.sharedMesh.colors;Assert.That(colors.Length,Is.EqualTo(mesh.sharedMesh.vertexCount));Assert.That(colors.All(c=>!float.IsNaN(c.r)&&c.r>=0&&c.r<=1),Is.True);Assert.That(colors.Min(c=>c.r),Is.LessThan(.01f));Assert.That(colors.Max(c=>c.r),Is.GreaterThan(.5f));Assert.That(colors.Count(c=>c.r>.01f&&c.r<.99f),Is.GreaterThan(100));Assert.That(mesh.sharedMesh.tangents.Length,Is.EqualTo(mesh.sharedMesh.vertexCount));
            }
            var terrain=root.GetComponentsInChildren<Renderer>(true).First(r=>r.name=="Terrain_LOD0").sharedMaterial;Assert.That(terrain.shader.name,Is.EqualTo("OceanAngler/CoastalTerrain"));foreach(string property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap","_SoilMap","_SoilNormal","_SoilMetallicGlossMap"})Assert.That(terrain.GetTexture(property),Is.Not.Null);
            foreach(string name in new[]{"Strata","Silt"}){string path="Assets/OceanAngler/Art/ContinentalSlope/Slope_"+name;Assert.That(((TextureImporter)AssetImporter.GetAtPath(path+"_Normal.png")).textureType,Is.EqualTo(TextureImporterType.NormalMap));Assert.That(((TextureImporter)AssetImporter.GetAtPath(path+"_MetallicSmoothness.png")).sRGBTexture,Is.False);}
            var water=habitat.waterSurface.sharedMaterial;Assert.That(water.GetFloat("_SeaLevel"),Is.EqualTo(200));Assert.That(water.GetVector("_BoatOrigin"),Is.EqualTo(new Vector4(-5,200,8,.48f)));
            foreach(var material in root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials)){Assert.That(material,Is.Not.Null);Assert.That(ShaderUtil.ShaderHasError(material.shader),Is.False);}
        }
        [Test] public void DeepLightingWaterLevelAndPausedViewsRestoreExactSceneBaseline()
        {
            var light=RenderSettings.sun;Color originalColor=light.color;float intensity=light.intensity;var originalAmbient=RenderSettings.ambientLight;var originalMode=RenderSettings.ambientMode;
            habitat.Present(camera,true,false,0,true,false);Assert.That(light.intensity,Is.EqualTo(intensity*.13f).Within(.0001f));Assert.That(habitat.lighting.inspectionLights.All(l=>l.enabled),Is.True);Assert.That(habitat.suspendedParticles.main.simulationSpeed,Is.Zero);var block=new MaterialPropertyBlock();habitat.waterSurface.GetPropertyBlock(block);Assert.That(block.GetFloat("_SeaLevel"),Is.EqualTo(200));Assert.That(block.GetFloat("_OceanTime"),Is.Zero);
            habitat.Present(camera,true,true,0,true,false);Assert.That(light.color,Is.EqualTo(originalColor));Assert.That(light.intensity,Is.EqualTo(intensity));Assert.That(RenderSettings.ambientMode,Is.EqualTo(originalMode));Assert.That(RenderSettings.ambientLight,Is.EqualTo(originalAmbient));Assert.That(habitat.lighting.inspectionLights.All(l=>!l.enabled),Is.True);Assert.That(habitat.suspendedParticles.gameObject.activeSelf,Is.False);
            habitat.Present(camera,true,false,1,true,true);Assert.That(Vector3.Distance(camera.transform.position,habitat.reefCamera.position),Is.LessThan(.001f));Assert.That(habitat.suspendedParticles.gameObject.activeSelf,Is.False);habitat.lighting.Restore();habitat.lighting.Restore();root.SetActive(false);Assert.That(light.intensity,Is.EqualTo(intensity));Assert.That(light.color,Is.EqualTo(originalColor));Assert.That(RenderSettings.ambientLight,Is.EqualTo(originalAmbient));
        }
        [Test] public void ResearchLampFollowsFinalViewWithoutAddingLightsOrLeakingToSurface()
        {
            var lighting=habitat.lighting;Assert.That(lighting.cameraFill,Is.Not.Null);Assert.That(lighting.inspectionLights.Count(l=>l==lighting.cameraFill),Is.EqualTo(1));Assert.That(root.GetComponentsInChildren<Light>(true).Length,Is.EqualTo(3));
            var lamp=lighting.cameraFill;Assert.That(lamp.type,Is.EqualTo(LightType.Spot));Assert.That(lamp.shadows,Is.EqualTo(LightShadows.None));
            var originalSun=RenderSettings.sun.color;float originalIntensity=RenderSettings.sun.intensity;var originalAmbient=RenderSettings.ambientLight;
            habitat.Present(camera,false,false,0,true,true);
            var subject=new Vector3(0,.3f,1.2f);var eye=subject+new Vector3(0,2.8f,-3.8f);camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(subject-eye));lighting.AimCameraFill(camera);
            Assert.That(Vector3.Dot((lamp.transform.position-subject).normalized,Vector3.up),Is.GreaterThan(.5f),"The eyed side receives light from above");
            Assert.That(Vector3.Angle(lamp.transform.forward,subject-lamp.transform.position),Is.LessThan(lamp.spotAngle*.5f-5),"Close subject remains inside the beam");
            var heldPosition=lamp.transform.position;var heldRotation=lamp.transform.rotation;lighting.AimCameraFill(camera);Assert.That(lamp.transform.position,Is.EqualTo(heldPosition));Assert.That(lamp.transform.rotation,Is.EqualTo(heldRotation));
            camera.transform.position+=new Vector3(3,1,-2);camera.transform.LookAt(subject);lighting.AimCameraFill(camera);Assert.That(Vector3.Distance(lamp.transform.position,heldPosition),Is.GreaterThan(2));
            habitat.Present(camera,true,true,0,true,true);Assert.That(lamp.enabled,Is.False);Assert.That(RenderSettings.sun.color,Is.EqualTo(originalSun));Assert.That(RenderSettings.sun.intensity,Is.EqualTo(originalIntensity));Assert.That(RenderSettings.ambientLight,Is.EqualTo(originalAmbient));
            habitat.Present(camera,false,false,0,true,true);Assert.That(lamp.enabled,Is.True);lighting.Restore();Assert.That(lighting.inspectionLights.All(l=>!l.enabled),Is.True);Assert.That(RenderSettings.sun.intensity,Is.EqualTo(originalIntensity));
        }
        [Test] public void PreviewProjectionClearsRovFromRightPanelAndRestoresCenteredPlay()
        {
            camera.aspect=2.17f;var bounds=root.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="ROV_LOD0").bounds;
            float RightEdge()
            {
                float right=float.NegativeInfinity;for(int i=0;i<8;i++)right=Mathf.Max(right,camera.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))).x);return right;
            }
            habitat.Present(camera,true,false,1,true,false);Assert.That(RightEdge(),Is.GreaterThan(.62f),"Old centered view reproduces the panel obstruction");
            habitat.Present(camera,true,false,1,true,false,true,.33f);Assert.That(RightEdge(),Is.LessThan(.62f));Assert.That(camera.WorldToViewportPoint(habitat.reefTarget.position).x,Is.EqualTo(.335f).Within(.001f));
            habitat.Present(camera,false,false,0,true,false);Assert.That(camera.WorldToViewportPoint(habitat.underwaterTarget.position).x,Is.EqualTo(.5f).Within(.001f));
        }
        [Test] public void ImportedSlopeClearsFishingAndWhaleSharkVisitVolumesAtAllLods()
        {
            var triangles=new List<Bounds>();
            foreach(var mesh in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!mesh.name.Contains("_LOD")||mesh.name.StartsWith("Water_"))continue;var vertices=mesh.sharedMesh.vertices.Select(mesh.transform.TransformPoint).ToArray();var indices=mesh.sharedMesh.triangles;
                for(int i=0;i<indices.Length;i+=3){var b=new Bounds(vertices[indices[i]],Vector3.zero);b.Encapsulate(vertices[indices[i+1]]);b.Encapsulate(vertices[indices[i+2]]);triangles.Add(b);}
            }
            int fishing=0,observed=0;
            foreach(float power in new[]{.3f,.6f,1f})foreach(float lane in new[]{-1.3f,0,1.3f})foreach(var phase in new[]{FishingPhase.Waiting,FishingPhase.Fighting,FishingPhase.Landed})for(int step=0;step<=10;step++)
            {
                var pose=FishingEncounter.Evaluate(phase,step/10f,power,Vector3.left,Vector3.up*2,lane,0,false);var body=new Bounds(pose.fishPosition,Vector3.zero);for(int corner=0;corner<8;corner++)body.Encapsulate(pose.fishPosition+pose.fishRotation*new Vector3((corner&1)==0?-2.4f:2.4f,(corner&2)==0?-1.6f:1.6f,(corner&4)==0?-1.5f:1.5f));foreach(var triangle in triangles)if(body.Intersects(triangle))Assert.Fail("Slope intersects conservative fishing body");fishing++;
            }
            var visit=new GameObject("SlopeVisitBounds").AddComponent<ObservationEncounter>();
            try
            {
                foreach(bool compact in new[]{false,true}){visit.Present("whale-shark",camera,compact,false,0);if(!visit.Playing)visit.Toggle();for(int step=0;step<96;step++){visit.Present("whale-shark",camera,compact,false,.25f);var renderers=visit.Visual.GetComponentsInChildren<Renderer>();var body=renderers[0].bounds;foreach(var renderer in renderers)body.Encapsulate(renderer.bounds);foreach(var triangle in triangles)if(body.Intersects(triangle))Assert.Fail("Slope intersects A0 whale-shark visit body");observed++;}}
            }
            finally{Object.DestroyImmediate(visit.gameObject);}
            Debug.Log("SLOPE_CORRIDOR_CLEAR fishing="+fishing+" observations="+observed+" triangles="+triangles.Count);
        }
    }
}
