using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace OceanAngler.Tests
{
    public sealed class BayHabitatTests
    {
        GameObject instance,cameraObject;BayHabitat bay;Camera camera;
        [SetUp] public void SetUp()
        {
            var prefab=Resources.Load<GameObject>("Models/CoralBay");Assert.That(prefab,Is.Not.Null);
            instance=Object.Instantiate(prefab);bay=instance.GetComponent<BayHabitat>();cameraObject=new GameObject("BayTestCamera");camera=cameraObject.AddComponent<Camera>();
        }
        [TearDown] public void TearDown(){Object.DestroyImmediate(instance);Object.DestroyImmediate(cameraObject);}
        [Test] public void ImportedSceneHasDistinctLodOwnersAndSeparateWater()
        {
            var groups=instance.GetComponentsInChildren<LODGroup>();Assert.That(groups.Length,Is.EqualTo(8));
            var renderers=groups.SelectMany(g=>g.GetLODs()).SelectMany(l=>l.renderers).ToArray();Assert.That(renderers.Length,Is.EqualTo(24));Assert.That(renderers.Distinct().Count(),Is.EqualTo(24));
            foreach(var group in groups)
            {
                Assert.That(group.lodCount,Is.EqualTo(3));foreach(var renderer in group.GetLODs().SelectMany(l=>l.renderers))Assert.That(renderer.transform.IsChildOf(group.transform),Is.True);
            }
            Assert.That(renderers.Contains(bay.waterSurface),Is.False);Assert.That(bay.waterSurface.bounds.center.y,Is.EqualTo(4.5f).Within(.001f));
            Assert.That(Vector3.Distance(bay.underwaterCamera.position,new Vector3(0,1.25f,-6.5f)),Is.LessThan(.001f));
        }
        [Test] public void SurfaceReefAndFishingDestinationsAreReversible()
        {
            bay.Present(camera,true,true,0,true,false);Assert.That(Vector3.Distance(camera.transform.position,bay.surfaceCamera.position),Is.LessThan(.001f));Assert.That(camera.transform.position.y,Is.GreaterThan(bay.SeaLevel));Assert.That(bay.suspendedParticles.gameObject.activeSelf,Is.False);Assert.That(bay.causticLight.enabled,Is.False);
            bay.Present(camera,true,false,1,true,false);Assert.That(Vector3.Distance(camera.transform.position,bay.reefCamera.position),Is.LessThan(.001f));Assert.That(camera.transform.position.y,Is.LessThan(bay.SeaLevel));Assert.That(bay.causticLight.enabled,Is.True);
            bay.Present(camera,false,false,1,true,false);Assert.That(Vector3.Distance(camera.transform.position,bay.underwaterCamera.position),Is.LessThan(.001f));Assert.That(camera.fieldOfView,Is.EqualTo(42));
        }
        [Test] public void PauseAndReducedMotionConstrainAmbientEffects()
        {
            bay.Present(camera,false,false,0,true,false);Assert.That(bay.suspendedParticles.main.simulationSpeed,Is.EqualTo(0));
            bay.Present(camera,false,false,0,false,true);Assert.That(bay.suspendedParticles.gameObject.activeSelf,Is.False);
            var properties=new MaterialPropertyBlock();bay.waterSurface.GetPropertyBlock(properties);Assert.That(properties.GetFloat("_OceanTime"),Is.EqualTo(0));
            bay.Present(camera,false,false,0,false,false);Assert.That(bay.suspendedParticles.gameObject.activeSelf,Is.True);Assert.That(bay.suspendedParticles.main.maxParticles,Is.EqualTo(72));Assert.That(bay.suspendedParticles.main.simulationSpeed,Is.EqualTo(1));
        }
        [Test] public void WaterInputsAndOriginalPbrMapsAreConfigured()
        {
            Assert.That(bay.waterSurface.sharedMaterial.shader.name,Is.EqualTo("OceanAngler/BayWater"));Assert.That(ShaderUtil.ShaderHasError(bay.waterSurface.sharedMaterial.shader),Is.False);
            foreach(string name in new[]{"Sand","Limestone","Wood","BranchCoral","PlateCoral","DomeCoral"})
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/OceanAngler/Art/CoralBay/Bay_"+name+".mat");Assert.That(mat.GetTexture("_BaseMap"),Is.Not.Null);Assert.That(mat.GetTexture("_BumpMap"),Is.Not.Null);Assert.That(mat.GetTexture("_MetallicGlossMap"),Is.Not.Null);
                var packed="Assets/OceanAngler/Art/CoralBay/Bay_"+name+"_MetallicSmoothness.png";
                Assert.That(((TextureImporter)AssetImporter.GetAtPath(packed)).sRGBTexture,Is.False);
                Assert.That(System.IO.File.ReadAllBytes(Application.dataPath+packed.Substring(6))[25],Is.EqualTo(6),"RGBA smoothness must retain alpha");
            }
            Assert.That(System.IO.File.ReadAllBytes(Application.dataPath+"/OceanAngler/Art/CoralBay/Bay_Particle.png")[25],Is.EqualTo(6));
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OceanAngler/Generated/OceanURP.asset");Assert.That(pipeline.supportsCameraOpaqueTexture&&pipeline.supportsCameraDepthTexture,Is.True);Assert.That(bay.causticLight.cookie,Is.Not.Null);
        }
    }
}
