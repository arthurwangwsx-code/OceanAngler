using UnityEngine;
namespace OceanAngler
{
    public abstract class FishVisual : MonoBehaviour
    {
        public bool reducedMotion, paused;
        public float speed=1;
        public string semantic="swim";
        public virtual Vector3 HookPosition=>transform.TransformPoint(new Vector3(-1,0,0));
        public abstract void SetSilhouette(bool hidden);
    }
}
