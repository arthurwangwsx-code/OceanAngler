using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace OceanAngler.Tests
{
    public sealed class BluefinPectoralAnimationTests
    {
        static double RotationDistance(Quaternion a,Quaternion b)
        {
            // atan2 retains small real rotations that float acos/dot rounds to zero.
            Quaternion q=Quaternion.Inverse(a)*b;
            return 2*Math.Atan2(Math.Sqrt((double)q.x*q.x+(double)q.y*q.y+(double)q.z*q.z),Math.Abs(q.w))*180/Math.PI;
        }
        [UnityTest] public IEnumerator ActualControllerAnimatesTheFlexibleTipsAndHonorsPauseAndReducedMotion()
        {
            var root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin"));
            try
            {
                var visual=root.GetComponent<AuthoredFishVisual>();var animator=root.GetComponentInChildren<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())renderer.updateWhenOffscreen=true;
                var tips=root.GetComponentsInChildren<Transform>().Where(t=>t.name=="pectoralLTip"||t.name=="pectoralRTip").ToArray();Assert.That(tips.Length,Is.EqualTo(2));
                foreach(string semantic in new[]{"idle","swim","turn","burst","bite","hooked","release"})
                {
                    visual.semantic=semantic;float deadline=Time.realtimeSinceStartup+2;
                    do{yield return null;}while((animator.IsInTransition(0)||!animator.GetCurrentAnimatorStateInfo(0).IsName(semantic))&&Time.realtimeSinceStartup<deadline);
                    Assert.That(!animator.IsInTransition(0)&&animator.GetCurrentAnimatorStateInfo(0).IsName(semantic),Is.True,semantic);
                    var before=tips.Select(t=>t.localRotation).ToArray();var travel=new double[tips.Length];
                    for(int sample=0;sample<10;sample++)
                    {
                        yield return new WaitForSeconds(.1f);
                        for(int i=0;i<tips.Length;i++)travel[i]=Math.Max(travel[i],RotationDistance(before[i],tips[i].localRotation));
                    }
                    for(int i=0;i<tips.Length;i++)Assert.That(travel[i],Is.GreaterThan(.01),semantic+"/"+tips[i].name);
                }
                visual.paused=true;yield return null;yield return null;var held=tips.Select(t=>t.localRotation).ToArray();yield return new WaitForSeconds(.25f);
                for(int i=0;i<tips.Length;i++)Assert.That(RotationDistance(held[i],tips[i].localRotation),Is.LessThan(.001f));
                visual.paused=false;visual.reducedMotion=true;yield return null;yield return null;held=tips.Select(t=>t.localRotation).ToArray();yield return new WaitForSeconds(.25f);
                for(int i=0;i<tips.Length;i++)Assert.That(RotationDistance(held[i],tips[i].localRotation),Is.LessThan(.001f));
                visual.reducedMotion=false;yield return null;yield return null;held=tips.Select(t=>t.localRotation).ToArray();yield return new WaitForSeconds(.17f);
                Assert.That(tips.Select((t,i)=>RotationDistance(held[i],t.localRotation)).Max(),Is.GreaterThan(.01f));
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
