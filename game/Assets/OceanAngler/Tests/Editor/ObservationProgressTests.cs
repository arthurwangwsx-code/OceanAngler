using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEngine;
namespace OceanAngler.Tests
{
    public sealed class ObservationProgressTests
    {
        ContentCatalog catalog;string folder;SaveStore store;
        [SetUp] public void Setup(){catalog=ContentCatalog.Load();folder=Path.Combine(Path.GetTempPath(),"OceanObservation-"+Guid.NewGuid());Directory.CreateDirectory(folder);store=new SaveStore(Path.Combine(folder,"progress.json"),catalog);}
        [TearDown] public void Cleanup(){Directory.Delete(folder,true);}
        ProgressService Service()=>new ProgressService(catalog,store);
        void LegacyPayload(string payload)
        {
            string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-","");
            File.WriteAllText(store.PathName,JsonUtility.ToJson(new FixtureEnvelope{payload=payload,checksum=hash}));
        }
        [Serializable] class FixtureEnvelope{public int version=1;public string payload,checksum;}
        [Test] public void TurtleViewingVolumeClearsAllAuthoredBayLodsInBothLayouts()
        {
            var bay=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/CoralBay"));var root=new GameObject("ObservationGeometry");var view=new GameObject("ObservationCamera").AddComponent<Camera>();view.aspect=1.6f;
            try
            {
                var triangles=new System.Collections.Generic.List<(string,Bounds)>();
                foreach(var filter in bay.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(!filter.name.Contains("_LOD")||filter.name.StartsWith("Water_"))continue;
                    var vertices=filter.sharedMesh.vertices.Select(filter.transform.TransformPoint).ToArray();var indices=filter.sharedMesh.triangles;
                    for(int i=0;i<indices.Length;i+=3){var bounds=new Bounds(vertices[indices[i]],Vector3.zero);bounds.Encapsulate(vertices[indices[i+1]]);bounds.Encapsulate(vertices[indices[i+2]]);triangles.Add((filter.name,bounds));}
                }
                var encounter=root.AddComponent<ObservationEncounter>();encounter.Present("green-turtle",view,false,false,0);encounter.Toggle();int samples=0;
                foreach(bool compact in new[]{false,true})for(int pose=0;pose<256;pose++)
                {
                    encounter.Present("green-turtle",view,compact,false,.25f);var renderers=encounter.Visual.GetComponentsInChildren<Renderer>();var volume=renderers[0].bounds;foreach(var renderer in renderers)volume.Encapsulate(renderer.bounds);
                    foreach(var triangle in triangles)if(volume.Intersects(triangle.Item2))Assert.Fail("Turtle volume intersects "+triangle.Item1+" layout="+compact+" pose="+pose);
                    samples++;
                }
                Debug.Log("OBSERVATION_BAY_CLEAR poses="+samples+" environmentTriangles="+triangles.Count);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(view.gameObject);UnityEngine.Object.DestroyImmediate(bay);}
        }
        [Test] public void LegacySaveLoadsWithoutObservationFieldAndOnlyObservationUpgradesIt()
        {
            var legacy=new ProgressData();legacy.settings.leftHanded=true;string payload=JsonUtility.ToJson(legacy).Replace(",\"observations\":[]","");Assert.That(payload,Does.Not.Contain("observations"));LegacyPayload(payload);
            var service=Service();Assert.That(service.RequiresRecovery,Is.False);Assert.That(service.Data.settings.leftHanded,Is.True);Assert.That(service.Data.schemaVersion,Is.EqualTo(1));Assert.That(service.Data.observations,Is.Empty);
            service.Configure(s=>s.music=true);Assert.That(Service().Data.schemaVersion,Is.EqualTo(1));
            var before=service.Data;Assert.That(service.Observe("green-turtle"),Is.True);var loaded=Service();
            Assert.That(loaded.Data.schemaVersion,Is.EqualTo(2));CollectionAssert.AreEqual(new[]{"green-turtle"},loaded.Data.observations);
            Assert.That(loaded.Data.settings.music&&loaded.Data.settings.leftHanded,Is.True);Assert.That(loaded.Data.totalPoints,Is.EqualTo(before.totalPoints));Assert.That(loaded.Data.catches,Is.Empty);
            CollectionAssert.AreEqual(catalog.tasks.Select(t=>TaskProgress.Value(t,before)),catalog.tasks.Select(t=>TaskProgress.Value(t,loaded.Data)));
        }
        [Test] public void DuplicateObservationDoesNotRewriteOrAwardAndInvalidTargetsAreRejected()
        {
            var service=Service();Assert.That(service.Observe("green-turtle"),Is.True);string saved=File.ReadAllText(store.PathName);
            Assert.That(service.Observe("green-turtle"),Is.False);Assert.That(File.ReadAllText(store.PathName),Is.EqualTo(saved));Assert.That(File.Exists(store.PathName+".bak"),Is.False);
            Assert.Throws<ArgumentException>(()=>service.Observe("trevally"));Assert.Throws<InvalidOperationException>(()=>service.Observe("bottlenose-dolphin"));
            Assert.That(service.Data.totalPoints,Is.Zero);Assert.That(service.Data.catches,Is.Empty);
        }
        [Test] public void FailedObservationWriteKeepsVersionAndRecordsAndCanRetry()
        {
            var service=Service();Directory.CreateDirectory(store.PathName);
            var error=Assert.Catch<Exception>(()=>service.Observe("green-turtle"));Assert.That(error,Is.InstanceOf<IOException>().Or.InstanceOf<UnauthorizedAccessException>());Assert.That(service.Data.schemaVersion,Is.EqualTo(1));Assert.That(service.Data.observations,Is.Empty);
            Directory.Delete(store.PathName);Assert.That(service.Observe("green-turtle"),Is.True);Assert.That(Service().Data.observations.Count,Is.EqualTo(1));
        }
        [Test] public void CorruptObservationLedgerAndFuturePayloadCannotBeSilentlyAccepted()
        {
            foreach(var data in new[]{new ProgressData{schemaVersion=1,observations=new System.Collections.Generic.List<string>{"green-turtle"}},new ProgressData{schemaVersion=2,observations=new System.Collections.Generic.List<string>{"green-turtle","green-turtle"}},new ProgressData{schemaVersion=2,observations=new System.Collections.Generic.List<string>{"trevally"}}})Assert.That(()=>data.Validate(catalog),Throws.Exception);
            var future=JsonUtility.ToJson(new ProgressData{schemaVersion=3});LegacyPayload(future);Assert.That(Service().RequiresRecovery,Is.True);Assert.Throws<InvalidOperationException>(()=>store.Write(new ProgressData()));Assert.That(File.ReadAllText(store.PathName),Does.Contain("schemaVersion"));
        }
    }
}
