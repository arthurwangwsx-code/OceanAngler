using UnityEngine;
namespace OceanAngler
{
    public sealed class BayHabitat : MonoBehaviour
    {
        public Transform underwaterCamera,underwaterTarget,surfaceCamera,surfaceTarget,reefCamera,reefTarget,pierCamera,pierTarget;
        public Renderer waterSurface;
        public Light causticLight;public ParticleSystem suspendedParticles;
        public float SeaLevel=4.5f;
        float waterTime;MaterialPropertyBlock waterProperties;
        public void Present(Camera camera,bool observing,bool above,int angle,bool paused,bool reduced)
        {
            if(!paused&&!reduced)waterTime+=Time.deltaTime;
            if(waterProperties==null)waterProperties=new MaterialPropertyBlock();
            waterProperties.SetFloat("_OceanTime",reduced?0:waterTime);waterSurface.SetPropertyBlock(waterProperties);
            if(causticLight!=null){causticLight.enabled=!above;causticLight.transform.localRotation=Quaternion.Euler(90,reduced?0:waterTime*1.5f,0);}
            if(suspendedParticles!=null){var main=suspendedParticles.main;main.simulationSpeed=paused||reduced?0:1;suspendedParticles.gameObject.SetActive(!above&&!reduced);}
            var from=above?(angle%2==0?surfaceCamera:pierCamera):(observing&&angle%2!=0?reefCamera:underwaterCamera);
            var target=above?(angle%2==0?surfaceTarget:pierTarget):(observing&&angle%2!=0?reefTarget:underwaterTarget);
            // Explicit destinations make mode changes reversible and keep the fishing camera stable.
            camera.transform.SetPositionAndRotation(from.position,Quaternion.LookRotation(target.position-from.position,Vector3.up));
            camera.fieldOfView=observing?58:42;camera.depthTextureMode|=DepthTextureMode.Depth;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogDensity=above?.012f:.035f;
            RenderSettings.fogColor=above?new Color(.53f,.73f,.75f):new Color(.075f,.42f,.49f);
            camera.backgroundColor=RenderSettings.fogColor;
        }
    }
}
