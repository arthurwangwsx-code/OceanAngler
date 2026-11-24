using UnityEngine;
namespace OceanAngler
{
    public class MarineHabitat : MonoBehaviour
    {
        public Transform underwaterCamera,underwaterTarget,surfaceCamera,surfaceTarget,reefCamera,reefTarget,pierCamera,pierTarget;
        public Renderer waterSurface;
        public Material sky;public BayPlanarReflection reflection;
        public Light causticLight;public ParticleSystem suspendedParticles;
        public HabitatLighting lighting;
        public float SeaLevel=4.5f;
        public Color underwaterColor=new Color(.075f,.42f,.49f),surfaceColor=new Color(.53f,.73f,.75f);
        public float underwaterDensity=.035f,surfaceDensity=.012f;
        public static string ResourceFor(string id)=>id=="bay"?"Models/CoralBay":id=="channel"?"Models/NearshoreChannel":id=="slope"?"Models/ContinentalSlope":null;
        protected virtual void PresentAmbient(bool paused,bool reduced,bool visible){}
        float waterTime;MaterialPropertyBlock waterProperties;
        public void Present(Camera camera,bool observing,bool above,int angle,bool paused,bool reduced,bool showAmbient=true,float compositionOffset=0)
        {
            PresentAmbient(paused,reduced,showAmbient);
            if(lighting!=null)lighting.Present(above);
            if(!paused&&!reduced)waterTime+=Time.deltaTime;
            if(waterProperties==null)waterProperties=new MaterialPropertyBlock();
            waterProperties.SetFloat("_OceanTime",reduced?0:waterTime);
            waterProperties.SetFloat("_SeaLevel",SeaLevel);
            if(causticLight!=null){causticLight.enabled=!above;causticLight.transform.localRotation=Quaternion.Euler(90,reduced?0:waterTime*1.5f,0);}
            if(suspendedParticles!=null){var main=suspendedParticles.main;main.simulationSpeed=paused||reduced?0:1;suspendedParticles.gameObject.SetActive(!above&&!reduced);}
            var from=above?(angle%2==0?surfaceCamera:pierCamera):(observing&&angle%2!=0?reefCamera:underwaterCamera);
            var target=above?(angle%2==0?surfaceTarget:pierTarget):(observing&&angle%2!=0?reefTarget:underwaterTarget);
            // Explicit destinations make mode changes reversible and keep the fishing camera stable.
            camera.transform.SetPositionAndRotation(from.position,Quaternion.LookRotation(target.position-from.position,Vector3.up));
            camera.fieldOfView=observing?58:42;camera.depthTextureMode|=DepthTextureMode.Depth;
            // Reserve the actual preview panel width without moving the subject
            // or adding more play HUD. Reflection uses this same projection.
            camera.ResetProjectionMatrix();
            if(compositionOffset!=0){var projection=camera.projectionMatrix;projection.m02=compositionOffset;camera.projectionMatrix=projection;}
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogDensity=above?surfaceDensity:underwaterDensity;
            RenderSettings.fogColor=above?surfaceColor:underwaterColor;
            camera.backgroundColor=RenderSettings.fogColor;
            camera.clearFlags=above?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;
            if(above&&sky!=null)RenderSettings.skybox=sky;
            if(reflection!=null)reflection.Prepare(camera,above,SeaLevel);
            waterProperties.SetFloat("_HasReflection",reflection!=null&&reflection.Ready?1:0);
            if(reflection!=null&&reflection.Ready){waterProperties.SetTexture("_PlanarReflection",reflection.Texture);waterProperties.SetMatrix("_ReflectionVP",reflection.ViewProjection);}
            else waterProperties.SetTexture("_PlanarReflection",Texture2D.blackTexture);
            waterSurface.SetPropertyBlock(waterProperties);
        }
    }
}
