using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;
namespace OceanAngler.Tests
{
    public sealed class JournalEnvelopeAssetTests
    {
        [Test] public void SavedEnvelopesCoverAllCurrentSpeciesAndBindToActualSourceFiles()
        {
            var asset=Resources.Load<TextAsset>("JournalEnvelopes");Assert.That(asset,Is.Not.Null);var data=JsonUtility.FromJson<JournalEnvelopeData>(asset.text);
            Assert.That(data.version,Is.EqualTo(1));Assert.That(data.unityVersion,Is.EqualTo(Application.unityVersion));Assert.That(data.sections,Is.EqualTo(32));Assert.That(data.padding,Is.EqualTo(.012f).Within(.000001));
            CollectionAssert.AreEquivalent(ContentCatalog.Load().fish.Select(f=>f.id),data.shapes.Select(s=>s.fishId));
            var expected=new[]{"Assets/OceanAngler/Runtime/FishModel.cs","Assets/OceanAngler/Runtime/FishVisual.cs","Assets/OceanAngler/Runtime/AuthoredFishVisual.cs","Assets/OceanAngler/Resources/ContentCatalog.json"}.Concat(new[]{"Bluefin","Halibut"}.SelectMany(name=>new[]{"Assets/OceanAngler/Resources/Models/"+name+".prefab","Assets/OceanAngler/Art/"+name+"/"+name+".fbx","Assets/OceanAngler/Art/"+name+"/"+name+".fbx.meta","Assets/OceanAngler/Art/"+name+"/"+name+".controller","Assets/OceanAngler/Art/"+name+"/Motions/idle.anim","Assets/OceanAngler/Art/"+name+"/Motions/swim.anim"}));
            CollectionAssert.AreEquivalent(expected,data.inputs.Select(i=>i.path));
            foreach(var input in data.inputs)using(var sha=SHA256.Create())Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(input.path))).Replace("-","").ToLowerInvariant(),Is.EqualTo(input.sha256),input.path+" stale envelope");
            foreach(var shape in data.shapes)
            {
                Assert.That(shape.sections.Length,Is.InRange(16,32));Assert.That(shape.sourceVertices,Is.GreaterThan(3000));Assert.That(shape.poses,Is.EqualTo(shape.fishId=="halibut"||shape.fishId=="trevally"?130:65));
                foreach(var section in shape.sections)
                {
                    Assert.That(section.size.x,Is.GreaterThan(0));Assert.That(section.size.y,Is.GreaterThan(0));Assert.That(section.size.z,Is.GreaterThan(0));
                    foreach(float value in new[]{section.min.x,section.min.y,section.min.z,section.max.x,section.max.y,section.max.z})Assert.That(float.IsFinite(value),Is.True);
                    Assert.That(section.min.x,Is.GreaterThanOrEqualTo(shape.bounds.min.x-.0001f));Assert.That(section.max.x,Is.LessThanOrEqualTo(shape.bounds.max.x+.0001f));
                }
            }
        }
    }
}
