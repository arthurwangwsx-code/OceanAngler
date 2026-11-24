using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace OceanAngler.Tests
{
    public sealed class SpinningRigMaterialTests
    {
        [Test] public void PerSlotInitializationRetainsSharedAssetsAndCleansEveryLodAndLineBinding()
        {
            var root=Object.Instantiate(Resources.Load<GameObject>("Models/SpinningRig"));var rig=root.GetComponent<AuthoredSpinningRig>();
            try
            {
                var renderers=root.GetComponentsInChildren<Renderer>(true);var shared=renderers.Select(r=>r.sharedMaterials).ToArray();var colors=shared.Select(a=>a.Select(m=>m.color).ToArray()).ToArray();
                for(int cycle=0;cycle<2;cycle++)
                {
                    rig.InitializeMechanisms();rig.InitializePresentation();
                    for(int r=0;r<renderers.Length;r++)for(int slot=0;slot<shared[r].Length;slot++)
                    {
                        var block=new MaterialPropertyBlock();renderers[r].GetPropertyBlock(block,slot);Assert.That(block.GetColor("_BaseColor"),Is.EqualTo(colors[r][slot]));Assert.That(block.GetFloat("_Smoothness"),Is.EqualTo(shared[r][slot].GetFloat("_Smoothness")));Assert.That(renderers[r].sharedMaterials[slot],Is.SameAs(shared[r][slot]));Assert.That(shared[r][slot].color,Is.EqualTo(colors[r][slot]));
                    }
                    rig.ReleasePresentation();Assert.That(renderers.All(r=>!r.HasPropertyBlock()),Is.True);
                }
            }
            finally{rig.ReleasePresentation();Object.DestroyImmediate(root);}
        }
    }
}
