using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace OceanAngler.Tests
{
    public sealed class BluefinMaterialBindingTests
    {
        [Test] public void ReviewedPrefabMatchesCurrentImportedSubmeshMaterialOrder()
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/OceanAngler/Art/Bluefin/Bluefin.fbx");
            var prefab=Resources.Load<GameObject>("Models/Bluefin");
            var imported=model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var reviewed=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Assert.That(reviewed.Length,Is.EqualTo(3));
            foreach(var renderer in reviewed)
            {
                var source=imported.Single(r=>r.name==renderer.name);
                Debug.Log(renderer.name+" imported="+string.Join(",",source.sharedMaterials.Select(m=>m.name))+" reviewed="+string.Join(",",renderer.sharedMaterials.Select(m=>m.name)));
                Assert.That(renderer.sharedMesh,Is.SameAs(source.sharedMesh),renderer.name);
                Assert.That(renderer.sharedMesh.subMeshCount,Is.EqualTo(source.sharedMaterials.Length),renderer.name);
                CollectionAssert.AreEqual(source.sharedMaterials,renderer.sharedMaterials,renderer.name+" must bind the material that belongs to each current imported submesh");
            }
        }
    }
}
