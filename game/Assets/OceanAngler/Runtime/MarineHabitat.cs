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
        MeshRenderer[] surfaceScenery;bool[] originalSurfaceRendering;bool surfaceSceneryVisible=true;
        bool waterOrderCaptured;int originalWaterOrder;
        void PresentWaterOrder(Camera camera)
        {
            if(!waterOrderCaptured){originalWaterOrder=waterSurface.sortingOrder;waterOrderCaptured=true;}
            // A water plane spans the whole habitat. Sorting its transparent
            // bounds centre by distance can put it over nearer fin membranes,
            // while opaque rays still pass the depth test. Underwater, the
            // surface belongs behind these subjects. Above water, retain the
            // authored order so the surface can cover submerged objects.
            waterSurface.sortingOrder=camera.transform.position.y<SeaLevel?Mathf.Max(short.MinValue,originalWaterOrder-1):originalWaterOrder;
        }
        void PresentSurfaceScenery(bool visible)
        {
            if(surfaceScenery==null)
            {
                // Keep the underwater setting, but remove above-water scenery
                // from appreciation cameras. This also covers each habitat's
                // boat, pier and marker LODs without changing their assets.
                var all=GetComponentsInChildren<MeshRenderer>(true);
                surfaceScenery=System.Array.FindAll(all,r=>r!=waterSurface&&r.bounds.max.y>SeaLevel+.02f);
                originalSurfaceRendering=System.Array.ConvertAll(surfaceScenery,r=>r.forceRenderingOff);
            }
            if(surfaceSceneryVisible==visible)return;
            for(int i=0;i<surfaceScenery.Length;i++)if(surfaceScenery[i]!=null)surfaceScenery[i].forceRenderingOff=!visible||originalSurfaceRendering[i];
            surfaceSceneryVisible=visible;
        }
        void OnDisable(){if(surfaceScenery!=null)PresentSurfaceScenery(true);if(waterOrderCaptured&&waterSurface!=null)waterSurface.sortingOrder=originalWaterOrder;}
        Vector4 fishingSplash=new Vector4(0,0,-1,0);
        public void SetFishingSplash(Vector3 position,float age)
        {
            // An inactive effect still participates in shader arithmetic:
            // cos(infinity)*0 is NaN, so never upload a non-finite sentinel.
            bool active=!float.IsNaN(age)&&!float.IsInfinity(age)&&age>=0&&age<1.4f;
            fishingSplash=active?new Vector4(position.x,position.z,age,1):new Vector4(0,0,-1,0);
        }
        public void Present(Camera camera,bool observing,bool above,int angle,bool paused,bool reduced,bool showAmbient=true,float compositionOffset=0,bool fishingSurface=false)
        {
            PresentAmbient(paused,reduced,showAmbient);
            PresentSurfaceScenery(showAmbient);
            if(lighting!=null)lighting.Present(above);
            if(!paused&&!reduced)waterTime+=Time.deltaTime;
            if(waterProperties==null)waterProperties=new MaterialPropertyBlock();
            waterProperties.SetFloat("_OceanTime",reduced?0:waterTime);
            waterProperties.SetFloat("_SeaLevel",SeaLevel);
            waterProperties.SetVector("_FishingSplash",reduced?new Vector4(0,0,-1,0):fishingSplash);
            if(causticLight!=null){causticLight.enabled=!above;causticLight.transform.localRotation=Quaternion.Euler(90,reduced?0:waterTime*1.5f,0);}
            if(suspendedParticles!=null){var main=suspendedParticles.main;main.simulationSpeed=paused||reduced?0:1;suspendedParticles.gameObject.SetActive(!above&&!reduced);}
            var from=above?(angle%2==0?surfaceCamera:pierCamera):(observing&&angle%2!=0?reefCamera:underwaterCamera);
            var target=above?(angle%2==0?surfaceTarget:pierTarget):(observing&&angle%2!=0?reefTarget:underwaterTarget);
            // Explicit destinations make mode changes reversible and keep the fishing camera stable.
            var position=fishingSurface?new Vector3(underwaterCamera.position.x,SeaLevel+1.25f,underwaterCamera.position.z):from.position;
            var look=fishingSurface?new Vector3(underwaterTarget.position.x,SeaLevel+.05f,underwaterTarget.position.z):target.position;
            camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(look-position,Vector3.up));
            PresentWaterOrder(camera);
            if(lighting!=null)lighting.AimCameraFill(camera);
            camera.fieldOfView=observing||fishingSurface?58:42;camera.depthTextureMode|=DepthTextureMode.Depth;
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
