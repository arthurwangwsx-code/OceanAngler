using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace OceanAngler.Tests
{
    public sealed class AuthoredFishMaterialTests
    {
        [Test] public void PresentationColorsAreIsolatedIdempotentAndReleased()
        {
            var prefab=Resources.Load<GameObject>("Models/Bluefin");var source=prefab.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
            var original=source.Select(m=>m.color).ToArray();var instance=Object.Instantiate(prefab);var visual=instance.GetComponent<AuthoredFishVisual>();
            try
            {
                visual.InitializePresentation();
                Material[] Current()=>instance.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(r=>r.sharedMaterials).ToArray();
                var shared=Current();var ids=shared.Select(m=>m.GetInstanceID()).ToArray();
                Assert.That(shared.Length,Is.GreaterThan(0));Assert.That(shared.All(m=>EditorUtility.IsPersistent(m)&&source.Contains(m)),Is.True,"Presentation must reuse source materials without cloning them");
                visual.InitializePresentation();CollectionAssert.AreEqual(ids,Current().Select(m=>m.GetInstanceID()).ToArray(),"Repeated preparation must not allocate more materials");
                visual.SetSilhouette(true);visual.InitializePresentation();visual.SetSilhouette(false);
                foreach(var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                for(int i=0;i<renderer.sharedMaterials.Length;i++)
                {var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,i);Assert.That(block.GetColor("_BaseColor"),Is.EqualTo(renderer.sharedMaterials[i].color));Assert.That(block.GetFloat("_Smoothness"),Is.EqualTo(renderer.sharedMaterials[i].GetFloat("_Smoothness")));}
                CollectionAssert.AreEqual(original,source.Select(m=>m.color).ToArray());
                visual.ReleasePresentation();visual.ReleasePresentation();Assert.That(instance.GetComponentsInChildren<SkinnedMeshRenderer>().All(r=>!r.HasPropertyBlock()),Is.True);
                visual.InitializePresentation();CollectionAssert.AreEqual(ids,Current().Select(m=>m.GetInstanceID()).ToArray());
            }
            finally{visual.ReleasePresentation();Object.DestroyImmediate(instance);}
            Assert.That(source.All(m=>m!=null),Is.True);
        }
    }
}
