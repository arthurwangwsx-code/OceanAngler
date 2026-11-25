using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace OceanAngler.Tests
{
    public sealed class BluefinMotionTests
    {
        [Test] public void RuntimeLoopsKeepClosedValueAndMatchingBoundaryVelocity()
        {
            var prefab=Resources.Load<GameObject>("Models/Bluefin");var clips=prefab.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips;
            CollectionAssert.AreEquivalent(new[]{"idle","swim","turn","burst","bite","hooked","release"},clips.Select(c=>c.name));
            foreach(var clip in clips)
            {
                Assert.That(AssetDatabase.GetAssetPath(clip),Does.StartWith("Assets/OceanAngler/Art/Bluefin/Motions/"));
                Assert.That(clip.isLooping,Is.EqualTo(clip.name!="release"));if(!clip.isLooping)continue;
                int checkedCurves=0;float tailVelocity=0;
                foreach(var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if(binding.type!=typeof(Transform))continue;var curve=AnimationUtility.GetEditorCurve(clip,binding);var keys=curve.keys;
                    if(keys.Length<3||keys.Max(k=>k.value)-keys.Min(k=>k.value)<1e-6f)continue;
                    var first=keys[0];var last=keys[keys.Length-1];string label=clip.name+"/"+binding.path+"/"+binding.propertyName;
                    Assert.That(Mathf.Abs(first.value-last.value),Is.LessThan(1e-4f),label);
                    Assert.That(Mathf.Abs(first.outTangent-last.inTangent),Is.LessThan(1e-5f),label);
                    Assert.That(keys.All(k=>!float.IsNaN(k.value)&&!float.IsInfinity(k.value)),Is.True,label);
                    if(binding.path.EndsWith("/tail"))tailVelocity=Mathf.Max(tailVelocity,Mathf.Abs(first.outTangent));checkedCurves++;
                }
                Assert.That(checkedCurves,Is.GreaterThan(5),clip.name);Assert.That(tailVelocity,Is.GreaterThan(.01f),clip.name+" must retain tail travel through the loop");
            }
        }
    }
}
