using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace OceanAngler.Tests
{
    public sealed class MarineTransparencyLifecycleTests
    {
        [UnityTest] public IEnumerator RealDisableAndRepeatedVisitsRestoreEveryHabitatOrder()
        {
            var cameraRoot=new GameObject("TransparencyLifecycleCamera");var camera=cameraRoot.AddComponent<Camera>();camera.enabled=false;var priorSky=RenderSettings.skybox;GameObject root=null;
            try
            {
                foreach(string resource in new[]{"CoralBay","NearshoreChannel","ContinentalSlope"})
                {
                    root=Object.Instantiate(Resources.Load<GameObject>("Models/"+resource));var habitat=root.GetComponent<MarineHabitat>();var water=habitat.waterSurface;water.sortingOrder=17;
                    for(int visit=0;visit<3;visit++)
                    {
                        root.SetActive(true);yield return null;habitat.Present(camera,false,false,0,true,true,false);Assert.That(water.sortingOrder,Is.LessThan(17));
                        root.SetActive(false);yield return null;Assert.That(water.sortingOrder,Is.EqualTo(17),resource+" actual lifecycle must restore its original order without accumulating offsets");
                    }
                    Object.Destroy(root);yield return null;root=null;
                }
            }
            finally{if(root!=null)Object.Destroy(root);Object.Destroy(cameraRoot);RenderSettings.skybox=priorSky;}
        }
    }
}
