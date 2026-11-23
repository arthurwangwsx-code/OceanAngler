using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace OceanAngler
{
    public sealed class BayFishSchool : MonoBehaviour
    {
        public const float SafetyRadius=.48f;
        public GameObject fishPrefab;
        public Bounds centres;
        public float solidCeiling;
        SchoolMotion motion;
        AuthoredFishVisual[] members;
        public SchoolMotion Motion=>motion;
        public AuthoredFishVisual[] Members=>members??Array.Empty<AuthoredFishVisual>();
        public void Initialize()
        {
            if(motion!=null)return;
            if(fishPrefab==null)throw new InvalidOperationException("Ambient bluefin source missing");
            motion=new SchoolMotion(centres);members=new AuthoredFishVisual[SchoolMotion.Count];
            for(int i=0;i<members.Length;i++)
            {
                var root=Instantiate(fishPrefab,transform);root.name="AmbientBluefin-"+i;root.transform.localScale=Vector3.one*(.18f+i*.04f);
                foreach(var collider in root.GetComponentsInChildren<Collider>())collider.enabled=false;
                var visual=root.GetComponent<AuthoredFishVisual>();visual.InitializePresentation();members[i]=visual;
                foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>()){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.updateWhenOffscreen=false;}
                root.GetComponentInChildren<Animator>().cullingMode=AnimatorCullingMode.CullUpdateTransforms;
            }
            Apply(false,false);
        }
        public void Present(float delta,bool paused,bool reduced,bool visible)
        {
            Initialize();gameObject.SetActive(visible);
            if(!visible)return;
            motion.Advance(delta,paused,reduced);Apply(paused,reduced);
        }
        void Apply(bool paused,bool reduced)
        {
            for(int i=0;i<members.Length;i++)
            {
                var fish=members[i];fish.transform.localPosition=motion.Position(i);
                var velocity=motion.Velocity(i);if(velocity.sqrMagnitude>.0001f)fish.transform.localRotation=Quaternion.LookRotation(velocity,Vector3.up)*Quaternion.Euler(0,90,0);
                fish.paused=paused;fish.reducedMotion=reduced;fish.semantic="swim";fish.speed=Mathf.Clamp(velocity.magnitude/.5f,.45f,1.3f)*(1+i*.06f);
            }
        }
        public void ReleasePresentation(){foreach(var fish in Members)if(fish!=null)fish.ReleasePresentation();}
        void OnDestroy(){ReleasePresentation();}
    }
}
