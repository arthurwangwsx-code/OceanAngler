using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;
namespace OceanAngler.Tests
{
    public sealed class DeveloperProfileTests
    {
        string root;ContentCatalog catalog;SaveStore normal,test;byte[] original;
        [Serializable] sealed class LegacyEnvelope {public int version=1;public string payload,checksum;}
        [SetUp] public void Setup()
        {
            root=Path.Combine(Path.GetTempPath(),"OceanAnglerDeveloperTests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);catalog=ContentCatalog.Load();
            normal=new SaveStore(Path.Combine(root,"ocean-progress.json"),catalog);test=new SaveStore(Path.Combine(root,"ocean-developer-progress.json"),catalog,true);
            normal.Write(new ProgressData());original=File.ReadAllBytes(normal.PathName);
        }
        [TearDown] public void Cleanup(){if(Directory.Exists(root))Directory.Delete(root,true);}
        [Test] public void ZeroPointsUnlockAllContentOnlyInTheExplicitTestProfile()
        {
            var service=new ProgressService(catalog,test);service.SelectDeveloperProfile(true);
            foreach(var gear in catalog.gear)service.SelectGear(gear.id);
            foreach(var habitat in catalog.habitats)service.SelectHabitat(habitat.id);
            foreach(var species in catalog.observations){service.SelectHabitat(species.habitatId);Assert.That(service.Observe(species.id),Is.True);}
            Assert.That(service.Data.totalPoints,Is.Zero);Assert.That(service.Data.catches,Is.Empty);Assert.That(service.Data.observations.Count,Is.EqualTo(3));
            Assert.That(catalog.gear.Length,Is.EqualTo(20));Assert.That(catalog.habitats.Length,Is.EqualTo(3));
            var loaded=new ProgressService(catalog,test);Assert.That(loaded.RequiresRecovery,Is.False);Assert.That(loaded.DeveloperSelected,Is.True);Assert.That(loaded.Data.totalPoints,Is.Zero);
            var regular=new ProgressService(catalog,normal);
            Assert.Throws<InvalidOperationException>(()=>regular.SelectGear(catalog.gear.First(g=>g.unlockPoints>0).id));
            Assert.Throws<InvalidOperationException>(()=>regular.SelectHabitat(catalog.habitats.First(h=>h.unlockPoints>0).id));
            Assert.That(File.ReadAllBytes(normal.PathName),Is.EqualTo(original));Assert.That(regular.Data.habitatId,Is.EqualTo("bay"));
        }
        [Test] public void TestEnvelopeIsNotAcceptedAsNormalAndModeChoiceIsChecksummed()
        {
            var service=new ProgressService(catalog,test);service.SelectDeveloperProfile(true);
            string raw=File.ReadAllText(test.PathName),wrong=Path.Combine(root,"wrong-role.json");File.WriteAllText(wrong,raw);
            Assert.That(new SaveStore(wrong,catalog).Load().requiresRecovery,Is.True);
            Assert.That(raw,Does.Contain("\"developerSelected\":true"));File.WriteAllText(test.PathName,raw.Replace("\"developerSelected\":true","\"developerSelected\":false"));
            Assert.That(test.Load().requiresRecovery,Is.True);Assert.That(File.ReadAllBytes(normal.PathName),Is.EqualTo(original));
        }
        [Test] public void FailedModeWriteKeepsSelectedStateAndLastGoodTestFile()
        {
            var service=new ProgressService(catalog,test);service.SelectDeveloperProfile(true);byte[] before=File.ReadAllBytes(test.PathName);string data=JsonUtility.ToJson(service.Data);
            Directory.CreateDirectory(test.PathName+".tmp");Assert.That(()=>service.SelectDeveloperProfile(false),Throws.Exception);
            Assert.That(service.DeveloperSelected,Is.True);Assert.That(JsonUtility.ToJson(service.Data),Is.EqualTo(data));Assert.That(File.ReadAllBytes(test.PathName),Is.EqualTo(before));Assert.That(File.ReadAllBytes(normal.PathName),Is.EqualTo(original));
        }
        [Test] public void RestartAndResetKeepTestModeAndRetainTheOriginalTestFile()
        {
            var service=new ProgressService(catalog,test);service.SelectDeveloperProfile(true);service.SelectHabitat(catalog.habitats.Last().id);byte[] previous=File.ReadAllBytes(test.PathName);
            service=new ProgressService(catalog,test);Assert.That(service.DeveloperSelected,Is.True);Assert.That(service.Data.habitatId,Is.EqualTo(catalog.habitats.Last().id));
            service.Reset();Assert.That(service.DeveloperSelected,Is.True);Assert.That(service.Data.totalPoints,Is.Zero);Assert.That(service.Data.tutorialComplete,Is.True);
            Assert.That(new ProgressService(catalog,test).DeveloperSelected,Is.True);
            Assert.That(Directory.GetFiles(root,"ocean-developer-progress.json.preserved-*").Any(p=>File.ReadAllBytes(p).SequenceEqual(previous)),Is.True);
            service.SelectDeveloperProfile(false);Assert.That(new ProgressService(catalog,test).DeveloperSelected,Is.False);Assert.That(File.ReadAllBytes(normal.PathName),Is.EqualTo(original));
        }
        [Test] public void DeveloperProfileStillRejectsUnknownContentAndInvalidLedgers()
        {
            var data=test.CreateData();data.habitatId="missing";Assert.That(()=>test.Write(data),Throws.Exception);
            data=test.CreateData();data.rodId="missing";Assert.That(()=>test.Write(data),Throws.Exception);
            data=test.CreateData();data.observations.Add("missing");Assert.That(()=>test.Write(data),Throws.Exception);
            data=test.CreateData();data.totalPoints=100;Assert.Throws<InvalidOperationException>(()=>test.Write(data));
            Assert.That(test.HasExisting,Is.False);Assert.That(File.ReadAllBytes(normal.PathName),Is.EqualTo(original));
        }
        [Test] public void ExistingVersionOneEnvelopeWithoutProfileFieldsRemainsReadable()
        {
            string payload=JsonUtility.ToJson(new ProgressData()),hash;
            using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-","");
            File.WriteAllText(normal.PathName,JsonUtility.ToJson(new LegacyEnvelope{payload=payload,checksum=hash}));byte[] legacy=File.ReadAllBytes(normal.PathName);
            var result=normal.Load();Assert.That(result.requiresRecovery,Is.False);Assert.That(result.developerSelected,Is.False);Assert.That(result.data.totalPoints,Is.Zero);Assert.That(File.ReadAllBytes(normal.PathName),Is.EqualTo(legacy));
        }
    }
}
