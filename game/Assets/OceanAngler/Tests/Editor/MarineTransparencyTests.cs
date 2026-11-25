using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace OceanAngler.Tests
{
    public sealed class MarineTransparencyTests
    {
        [TestCase("CoralBay"),TestCase("NearshoreChannel"),TestCase("ContinentalSlope")]
        public void WaterBehindUnderwaterFishDrawsBeforeTheirTransparentMembranes(string resource)
        {
            var root=Object.Instantiate(Resources.Load<GameObject>("Models/"+resource));var subject=Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin"));var cameraRoot=new GameObject("TransparencyCamera");var camera=cameraRoot.AddComponent<Camera>();var priorSky=RenderSettings.skybox;
            try
            {
                var habitat=root.GetComponent<MarineHabitat>();var water=habitat.waterSurface;var source=water.sharedMaterial;int queue=source.renderQueue,original=water.sortingOrder;bool enabled=water.enabled,hidden=water.forceRenderingOff;var fish=subject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                for(int visit=0;visit<3;visit++)
                {
                    habitat.Present(camera,false,false,0,true,true,false);
                    Assert.That(camera.transform.position.y,Is.LessThan(habitat.SeaLevel));
                    Assert.That(water.sortingOrder,Is.LessThan(fish.Min(r=>r.sortingOrder)),"The large water bounds must not cover nearer transparent fins above the horizon");
                    Assert.That(water.sharedMaterial,Is.SameAs(source));Assert.That(source.renderQueue,Is.EqualTo(queue));Assert.That(water.enabled,Is.EqualTo(enabled));Assert.That(water.forceRenderingOff,Is.EqualTo(hidden));
                    habitat.Present(camera,true,true,0,true,true,false);
                    Assert.That(camera.transform.position.y,Is.GreaterThan(habitat.SeaLevel));Assert.That(water.sortingOrder,Is.EqualTo(original),"Above-water views restore the authored order");
                }
            }
            finally{Object.DestroyImmediate(subject);Object.DestroyImmediate(root);Object.DestroyImmediate(cameraRoot);RenderSettings.skybox=priorSky;}
        }
    }
}
