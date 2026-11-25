using UnityEngine;
using UnityEngine.Rendering;
namespace OceanAngler
{
    // An authored deep habitat temporarily owns its illumination. Switching
    // away restores the exact scene baseline before another habitat presents.
    public sealed class HabitatLighting : MonoBehaviour
    {
        public Color underwaterAmbient=new Color(.28f,.38f,.5f),sunTint=new Color(.3f,.5f,.8f);
        public float underwaterSunScale=.13f;
        public Light[] inspectionLights;
        public Light cameraFill;
        Light sun;Color originalSunColor,originalAmbient;float originalSunIntensity;AmbientMode originalMode;bool captured;
        public void Present(bool above)
        {
            if(!captured)
            {
                sun=RenderSettings.sun;
                if(sun==null)foreach(var candidate in FindObjectsByType<Light>(FindObjectsSortMode.None))if(candidate.isActiveAndEnabled&&candidate.type==LightType.Directional){sun=candidate;break;}
                if(sun!=null){originalSunColor=sun.color;originalSunIntensity=sun.intensity;}
                originalMode=RenderSettings.ambientMode;originalAmbient=RenderSettings.ambientLight;captured=true;
            }
            if(sun!=null){sun.intensity=originalSunIntensity*(above?1:underwaterSunScale);sun.color=above?originalSunColor:originalSunColor*sunTint;}
            RenderSettings.ambientMode=above?originalMode:AmbientMode.Flat;RenderSettings.ambientLight=above?originalAmbient:underwaterAmbient;
            if(inspectionLights!=null)foreach(var light in inspectionLights)if(light!=null)light.enabled=!above;
        }
        // The existing research camera lamp follows the final view, including
        // elevated flatfish views. A small offset preserves shape and specular
        // separation; the sun, ambient and fog retain the deep-water treatment.
        public void AimCameraFill(Camera camera)
        {
            if(cameraFill==null||!cameraFill.enabled||camera==null)return;
            var frame=camera.transform;
            var position=frame.position+frame.right*.7f+frame.up*.7f;
            var target=frame.position+frame.forward*8;
            cameraFill.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position,frame.up));
        }
        public void Restore()
        {
            if(!captured)return;
            if(sun!=null){sun.color=originalSunColor;sun.intensity=originalSunIntensity;}
            if(inspectionLights!=null)foreach(var light in inspectionLights)if(light!=null)light.enabled=false;
            RenderSettings.ambientMode=originalMode;RenderSettings.ambientLight=originalAmbient;captured=false;sun=null;
        }
        void OnDisable()=>Restore();
    }
}
