using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace OceanAngler.Tests
{
    public sealed class HalibutFinAnimationTests
    {
        [UnityTest] public IEnumerator MedianFinControlsPlayAndRespectPauseAndReducedMotion()
        {
            var root=Object.Instantiate(Resources.Load<GameObject>("Models/Halibut"));
            try
            {
                var visual=root.GetComponent<AuthoredFishVisual>();var animator=root.GetComponentInChildren<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())renderer.updateWhenOffscreen=true;
                var fin=root.GetComponentsInChildren<Transform>().Single(t=>t.name=="dorsal_08");
                foreach(string semantic in new[]{"idle","swim","turn","burst","bite","hooked","release"})
                {
                    visual.semantic=semantic;yield return new WaitForSeconds(.3f);Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName(semantic),Is.True);var rotation=fin.localRotation;yield return new WaitForSeconds(.16f);Assert.That(Quaternion.Angle(rotation,fin.localRotation),Is.GreaterThan(.01f),semantic);
                }
                visual.paused=true;yield return null;yield return null;var held=fin.localRotation;yield return new WaitForSeconds(.25f);Assert.That(Quaternion.Angle(held,fin.localRotation),Is.LessThan(.001f));
                visual.paused=false;visual.reducedMotion=true;yield return null;yield return null;held=fin.localRotation;yield return new WaitForSeconds(.25f);Assert.That(Quaternion.Angle(held,fin.localRotation),Is.LessThan(.001f));
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
