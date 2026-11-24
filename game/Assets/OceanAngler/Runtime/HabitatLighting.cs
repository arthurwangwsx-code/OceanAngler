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
        public void Restore()
        {
            if(!captured)return;
            if(sun!=null){sun.color=originalSunColor;sun.intensity=originalSunIntensity;}
            RenderSettings.ambientMode=originalMode;RenderSettings.ambientLight=originalAmbient;captured=false;sun=null;
        }
        void OnDisable()=>Restore();
    }
}
