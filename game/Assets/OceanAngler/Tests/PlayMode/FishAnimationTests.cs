using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace OceanAngler.Tests
{
    public sealed class FishAnimationTests
    {
        GameObject instance;
        [TearDown] public void CleanUp(){if(instance!=null)Object.DestroyImmediate(instance);}
        [UnityTest] public IEnumerator ReleaseUsesImportedNonLoopingMotionAndFreezesTheActualTail()
        {
            instance=Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin"));
            var visual=instance.GetComponent<AuthoredFishVisual>();var animator=instance.GetComponentInChildren<Animator>();
            // Headless evaluation explicitly bypasses render culling; this is not pixel acceptance.
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())renderer.updateWhenOffscreen=true;
            visual.semantic="turn";yield return new WaitForSeconds(.3f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("turn"),Is.True);
            visual.semantic="release";yield return new WaitForSeconds(.3f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("release"),Is.True);
            Assert.That(animator.GetCurrentAnimatorClipInfo(0).Single().clip.isLooping,Is.False);
            var tail=instance.GetComponentsInChildren<Transform>().Single(t=>t.name=="tail");var prior=tail.localRotation;
            yield return new WaitForSeconds(.25f);Assert.That(Quaternion.Angle(prior,tail.localRotation),Is.GreaterThan(.01f));
            visual.paused=true;yield return null;yield return null;prior=tail.localRotation;
            yield return new WaitForSeconds(.2f);Assert.That(Quaternion.Angle(prior,tail.localRotation),Is.LessThan(.001f));
            visual.paused=false;visual.reducedMotion=true;yield return null;yield return null;prior=tail.localRotation;
            yield return new WaitForSeconds(.2f);Assert.That(Quaternion.Angle(prior,tail.localRotation),Is.LessThan(.001f));
        }
    }
}
