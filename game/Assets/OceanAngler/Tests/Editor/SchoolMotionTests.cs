using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace OceanAngler.Tests
{
    public sealed class SchoolMotionTests
    {
        [Test] public void TwentyMinutesRemainSeparatedBoundedAndDeterministic()
        {
            var school=Resources.Load<GameObject>("Models/CoralBay").GetComponent<BayHabitat>().school;
            Assert.That(school,Is.Not.Null);var a=new SchoolMotion(school.centres);var b=new SchoolMotion(school.centres);float minimum=float.PositiveInfinity;
            for(int frame=0;frame<36000;frame++)
            {
                a.Advance(1f/30,false,false);b.Advance(1f/60,false,false);b.Advance(1f/60,false,false);
                for(int i=0;i<SchoolMotion.Count;i++)
                {
                    Assert.That(school.centres.Contains(a.Position(i)),Is.True);Assert.That(a.Velocity(i).magnitude,Is.LessThanOrEqualTo(.651f));
                    Assert.That(Vector3.Distance(a.Position(i),b.Position(i)),Is.LessThan(.0001f));
                    for(int j=i+1;j<SchoolMotion.Count;j++)minimum=Mathf.Min(minimum,Vector3.Distance(a.Position(i),a.Position(j)));
                }
            }
            Assert.That(a.BoundaryCorrections,Is.Zero,"No hidden clamp/teleport during the reviewed trajectory");
            Assert.That(a.NeighbourInteractions,Is.GreaterThan(0));Assert.That(a.SeparationResponses,Is.GreaterThan(0));
            Assert.That(minimum,Is.GreaterThan(.96f),"Conservative two-body safety spheres must stay apart");Debug.Log("SCHOOL_SIM_PASS minSeparation="+minimum+" neighbours="+a.NeighbourInteractions+" separation="+a.SeparationResponses);
        }
        [Test] public void PausedReducedAndInvalidTimeCannotMoveTheSchool()
        {
            var motion=new SchoolMotion(new Bounds(new Vector3(2,3,13),new Vector3(7,1,10)));var before=Enumerable.Range(0,3).Select(motion.Position).ToArray();
            motion.Advance(10,true,false);motion.Advance(10,false,true);motion.Advance(float.NaN,false,false);motion.Advance(float.PositiveInfinity,false,false);motion.Advance(-1,false,false);
            CollectionAssert.AreEqual(before,Enumerable.Range(0,3).Select(motion.Position).ToArray());
            motion.Advance(10,false,false);for(int i=0;i<3;i++)Assert.That(Vector3.Distance(before[i],motion.Position(i)),Is.LessThanOrEqualTo(.0651f));
            Assert.Throws<ArgumentException>(()=>new SchoolMotion(new Bounds(Vector3.zero,new Vector3(2,1,10))));
            Assert.Throws<ArgumentException>(()=>new SchoolMotion(new Bounds(new Vector3(float.NaN,0,0),new Vector3(7,1,10))));
        }
        [Test] public void SweptBodyVolumeIsClearOfAllThreeEnvironmentLods()
        {
            var root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/CoralBay"));
            try
            {
                var bay=root.GetComponent<BayHabitat>();var school=bay.school;Assert.That(school,Is.Not.Null);var swept=school.centres;swept.Expand(BayFishSchool.SafetyRadius*2);
                Assert.That(swept.max.y,Is.LessThan(bay.SeaLevel));Assert.That(swept.min.z,Is.GreaterThan(6));int triangles=0;
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(!filter.name.Contains("_LOD")||filter.name.StartsWith("Water_"))continue;
                    var vertices=filter.sharedMesh.vertices;var indices=filter.sharedMesh.triangles;
                    for(int i=0;i<indices.Length;i+=3)
                    {
                        var box=new Bounds(bay.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[indices[i]])),Vector3.zero);
                        for(int j=1;j<3;j++)box.Encapsulate(bay.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[indices[i+j]])));
                        Assert.That(box.Intersects(swept),Is.False,"Swimming body volume intersects "+filter.name);triangles++;
                    }
                }
                Assert.That(triangles,Is.GreaterThan(100000));Debug.Log("SCHOOL_GEOMETRY_PASS checkedTriangles="+triangles+" bounds="+school.centres);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void ActualSwimSkinFitsTheConservativeBodyRadius()
        {
            var root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin"));root.transform.localScale=Vector3.one*.26f;
            try
            {
                var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;
                var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/OceanAngler/Art/Bluefin/Bluefin.fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__")&&(c.name=="swim"||c.name.EndsWith("|swim")));float radius=0;
                for(int pose=0;pose<7;pose++)
                {
                    clip.SampleAnimation(animator.gameObject,pose*clip.length/7);
                    foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var mesh=new Mesh();try{renderer.BakeMesh(mesh,true);if(pose==0)Debug.Log("SCHOOL_BAKE scale="+renderer.transform.lossyScale+" root="+root.transform.localScale+" baked="+mesh.bounds+" world="+renderer.bounds);foreach(var v in mesh.vertices)radius=Mathf.Max(radius,Vector3.Distance(root.transform.position,renderer.transform.TransformPoint(v)));}finally{UnityEngine.Object.DestroyImmediate(mesh);}
                    }
                }
                Assert.That(radius,Is.GreaterThan(.25f));Assert.That(radius,Is.LessThan(BayFishSchool.SafetyRadius-.05f));Debug.Log("SCHOOL_BODY_RADIUS "+radius);
            }
            finally{root.GetComponent<AuthoredFishVisual>().ReleasePresentation();UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
