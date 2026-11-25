using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace OceanAngler.Tests
{
    public sealed class HalibutAssetTests
    {
        [Test] public void ImportedMedianFinsMoveIndependentlyWithoutMovingOtherAnatomy()
        {
            var root=Object.Instantiate(Resources.Load<GameObject>("Models/Halibut"));var moved=new Mesh();var held=new Mesh();
            try
            {
                var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;
                var fins=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("dorsal_")||t.name.StartsWith("anal_")).ToArray();Assert.That(fins.Length,Is.EqualTo(34));var rest=fins.Select(t=>t.localRotation).ToArray();
                foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var mesh=renderer.sharedMesh;Assert.That(mesh.bindposes.Length,Is.LessThanOrEqualTo(42));var weights=mesh.boneWeights;var finBones=new HashSet<int>(renderer.bones.Select((bone,index)=>(bone,index)).Where(x=>x.bone.name.StartsWith("dorsal_")||x.bone.name.StartsWith("anal_")).Select(x=>x.index));
                    bool Influenced(BoneWeight w)=>finBones.Contains(w.boneIndex0)&&w.weight0>0||finBones.Contains(w.boneIndex1)&&w.weight1>0||finBones.Contains(w.boneIndex2)&&w.weight2>0||finBones.Contains(w.boneIndex3)&&w.weight3>0;
                    var moving=Enumerable.Range(0,mesh.vertexCount).Where(i=>Influenced(weights[i])).ToArray();var fixedVertices=Enumerable.Range(0,mesh.vertexCount).Where(i=>!Influenced(weights[i])).ToArray();Assert.That(moving.Length,Is.GreaterThan(100));Assert.That(fixedVertices.Length,Is.GreaterThan(100));
                    foreach(var clip in animator.runtimeAnimatorController.animationClips)
                    {
                        float maximum=0;
                        foreach(float phase in new[]{0,.125f,.25f,.375f,.5f,.625f,.75f,.875f})
                        {
                            clip.SampleAnimation(animator.gameObject,clip.length*phase);renderer.BakeMesh(moved,true);var a=moved.vertices;var rotations=fins.Select(t=>t.localRotation).ToArray();for(int i=0;i<fins.Length;i++)fins[i].localRotation=rest[i];renderer.BakeMesh(held,true);var b=held.vertices;for(int i=0;i<fins.Length;i++)fins[i].localRotation=rotations[i];
                            float Distance(int i)=>renderer.transform.TransformVector(a[i]-b[i]).magnitude;
                            Assert.That(fixedVertices.Max(Distance),Is.LessThan(.00001f),renderer.name+clip.name+" body/eyes/mouth and roots stay fixed under fin-only ablation");maximum=Mathf.Max(maximum,moving.Max(Distance));Assert.That(a.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),Is.True);
                        }
                        Assert.That(maximum,Is.GreaterThan(.0015f),renderer.name+clip.name+" actual imported fin deformation");
                    }
                }
            }
            finally{Object.DestroyImmediate(moved);Object.DestroyImmediate(held);Object.DestroyImmediate(root);}
        }
        [Test] public void ActualImportedToothRootsRemainInVisibleGumThroughAllSevenActions()
        {
            var root=Object.Instantiate(Resources.Load<GameObject>("Models/Halibut"));var baked=new Mesh();
            try
            {
                var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;var renderer=root.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name.EndsWith("LOD0"));var mesh=renderer.sharedMesh;
                int gum=System.Array.FindIndex(renderer.sharedMaterials,m=>m.name=="Halibut_Gum"),tooth=System.Array.FindIndex(renderer.sharedMaterials,m=>m.name=="Halibut_Teeth");Assert.That(gum,Is.GreaterThanOrEqualTo(0));Assert.That(tooth,Is.GreaterThanOrEqualTo(0));
                var triangles=mesh.GetTriangles(tooth);var adjacency=triangles.Distinct().ToDictionary(i=>i,i=>new HashSet<int>());
                for(int i=0;i<triangles.Length;i+=3)foreach(int a in triangles.Skip(i).Take(3))foreach(int b in triangles.Skip(i).Take(3))if(a!=b)adjacency[a].Add(b);
                var unseen=new HashSet<int>(adjacency.Keys);var groups=new List<int[]>();renderer.BakeMesh(baked,true);var rest=baked.vertices.Select(v=>renderer.transform.TransformPoint(v)).ToArray();
                while(unseen.Count>0)
                {
                    int seed=unseen.First();unseen.Remove(seed);var pending=new Stack<int>();pending.Push(seed);var group=new List<int>();
                    while(pending.Count>0){int current=pending.Pop();group.Add(current);foreach(int next in adjacency[current])if(unseen.Remove(next))pending.Push(next);}
                    Assert.That(group.Count,Is.EqualTo(6));int tip=group.OrderByDescending(i=>rest[i].x).First();groups.Add(group.Where(i=>i!=tip).ToArray());
                }
                Assert.That(groups.Count,Is.EqualTo(12));var gums=mesh.GetTriangles(gum);Assert.That(gums.Length,Is.GreaterThan(300));
                foreach(var clip in animator.runtimeAnimatorController.animationClips)foreach(float phase in new[]{0,.25f,.5f,.75f,1f})
                {
                    clip.SampleAnimation(animator.gameObject,clip.length*phase);renderer.BakeMesh(baked,true);var positions=baked.vertices.Select(v=>renderer.transform.TransformPoint(v)).ToArray();
                    foreach(var group in groups)
                    {
                        Vector3 center=group.Aggregate(Vector3.zero,(sum,i)=>sum+positions[i])/group.Length;float minimum=float.PositiveInfinity;
                        for(int i=0;i<gums.Length;i+=3)minimum=Mathf.Min(minimum,TriangleDistanceSquared(center,positions[gums[i]],positions[gums[i+1]],positions[gums[i+2]]));
                        Assert.That(minimum,Is.LessThan(.001f*.001f),clip.name+" phase="+phase+" tooth root left gum");
                    }
                }
            }
            finally{Object.DestroyImmediate(baked);Object.DestroyImmediate(root);}
        }
        static float TriangleDistanceSquared(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
        {
            var normal=Vector3.Cross(b-a,c-a);float size=normal.sqrMagnitude;
            if(size>1e-16f)
            {
                var projected=p-normal*(Vector3.Dot(p-a,normal)/size);
                if(Vector3.Dot(Vector3.Cross(b-a,projected-a),normal)>=-1e-15f&&Vector3.Dot(Vector3.Cross(c-b,projected-b),normal)>=-1e-15f&&Vector3.Dot(Vector3.Cross(a-c,projected-c),normal)>=-1e-15f)return (p-projected).sqrMagnitude;
            }
            return Mathf.Min(SegmentDistanceSquared(p,a,b),Mathf.Min(SegmentDistanceSquared(p,b,c),SegmentDistanceSquared(p,c,a)));
        }
        static float SegmentDistanceSquared(Vector3 p,Vector3 a,Vector3 b)
        {var edge=b-a;float size=edge.sqrMagnitude;return (p-a-edge*(size>0?Mathf.Clamp01(Vector3.Dot(p-a,edge)/size):0)).sqrMagnitude;}
        [Test] public void ImportedCandidatePreservesHorizontalFlatBodyAndBothEyedSideEyes()
        {
            var root=Object.Instantiate(Resources.Load<GameObject>("Models/Halibut"));
            try
            {
                var renderers=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);Assert.That(renderers.Length,Is.EqualTo(3));
                int previous=int.MaxValue;
                foreach(var renderer in renderers.OrderBy(r=>r.name))
                {
                    // Compensate the FBX renderer's scale before TransformPoint;
                    // the default BakeMesh snapshot already contains that scale.
                    var mesh=new Mesh();renderer.BakeMesh(mesh,true);
                    try
                    {
                        int count=mesh.triangles.Length/3;Assert.That(count,Is.LessThan(previous));previous=count;if(renderer.name.EndsWith("0"))Assert.That(count,Is.InRange(8000,25000));
                        var points=mesh.vertices.Select(v=>root.transform.InverseTransformPoint(renderer.transform.TransformPoint(v))).ToArray();
                        float height=points.Max(v=>v.y)-points.Min(v=>v.y),width=points.Max(v=>v.z)-points.Min(v=>v.z);
                        Assert.That(width,Is.InRange(1.0f,1.3f));Assert.That(height/width,Is.LessThan(.23f));Assert.That(points.Max(v=>v.x)-points.Min(v=>v.x),Is.InRange(2.3f,2.6f));
                        int slot=System.Array.FindIndex(renderer.sharedMaterials,m=>m.name=="Halibut_Iris");Assert.That(slot,Is.GreaterThanOrEqualTo(0));var eyes=mesh.GetTriangles(slot).Distinct().Select(i=>points[i]).ToArray();
                        string location=renderer.name+" eyes="+eyes.Min(v=>v.x)+".."+eyes.Max(v=>v.x)+" height="+eyes.Min(v=>v.y)+" scale="+renderer.transform.lossyScale;
                        Assert.That(eyes.All(v=>v.y>.02f),Is.True,"Both eyes must be above, with no blind-side eye "+location);Assert.That(eyes.Any(v=>v.x<-.75f)&&eyes.Any(v=>v.x>-.72f),Is.True,location);
                    }
                    finally{Object.DestroyImmediate(mesh);}
                }
                Assert.That(root.GetComponent<LODGroup>().GetLODs().Length,Is.EqualTo(3));Assert.That(root.GetComponent<Collider>().isTrigger,Is.True);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void MouthHookMovesWithActualImportedJawAndSwimDeformsTail()
        {
            var root=Object.Instantiate(Resources.Load<GameObject>("Models/Halibut"));
            try
            {
                var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;var visual=root.GetComponent<AuthoredFishVisual>();var clips=animator.runtimeAnimatorController.animationClips;
                var bite=clips.Single(c=>c.name=="bite");bite.SampleAnimation(animator.gameObject,0);var closed=visual.HookPosition;
                Assert.That(Vector3.Distance(closed,new Vector3(-1.045f,0,0)),Is.LessThan(.02f));bite.SampleAnimation(animator.gameObject,.5f);Assert.That(Vector3.Distance(closed,visual.HookPosition),Is.GreaterThan(.05f));
                var renderer=root.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name.EndsWith("LOD0"));var a=new Mesh();var b=new Mesh();
                try
                {
                    var swim=clips.Single(c=>c.name=="swim");swim.SampleAnimation(animator.gameObject,0);renderer.BakeMesh(a,true);
                    int skin=System.Array.FindIndex(renderer.sharedMaterials,m=>m.name=="Halibut_Skin");Assert.That(skin,Is.GreaterThanOrEqualTo(0));var initial=a.vertices;
                    var posterior=renderer.sharedMesh.GetTriangles(skin).Distinct().Where(i=>renderer.transform.TransformPoint(initial[i]).x>.2f).ToArray();Assert.That(posterior.Length,Is.GreaterThan(100));
                    float maximum=0,posteriorMaximum=0;
                    foreach(float time in new[]{.125f,.25f,.375f,.5f,.75f,1f})
                    {
                        swim.SampleAnimation(animator.gameObject,time);renderer.BakeMesh(b,true);var posed=b.vertices;float Distance(int i)=>Vector3.Distance(renderer.transform.TransformPoint(initial[i]),renderer.transform.TransformPoint(posed[i]));
                        maximum=Mathf.Max(maximum,Enumerable.Range(0,initial.Length).Max(Distance));posteriorMaximum=Mathf.Max(posteriorMaximum,posterior.Max(Distance));
                    }
                    Assert.That(posteriorMaximum,Is.GreaterThan(.005f),"Actual posterior skin must bend with the spine, not remain rigid on head while fins move");
                    Assert.That(maximum,Is.GreaterThan(.025f));Assert.That(b.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),Is.True);
                }
                finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void CandidateUsesSevenEditableClipsWithClosedCyclicBoundaries()
        {
            var clips=Resources.Load<GameObject>("Models/Halibut").GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips;
            CollectionAssert.AreEquivalent(new[]{"idle","swim","turn","burst","bite","hooked","release"},clips.Select(c=>c.name));
            foreach(var clip in clips)
            {
                Assert.That(AssetDatabase.GetAssetPath(clip),Does.StartWith("Assets/OceanAngler/Art/Halibut/Motions/"));Assert.That(clip.isLooping,Is.EqualTo(clip.name!="release"));if(!clip.isLooping)continue;
                foreach(var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    var keys=AnimationUtility.GetEditorCurve(clip,binding).keys;if(keys.Length<3||keys.Max(k=>k.value)-keys.Min(k=>k.value)<1e-6f)continue;
                    string label=clip.name+binding.path+binding.propertyName;
                    Assert.That(Mathf.Abs(keys[0].value-keys[keys.Length-1].value),Is.LessThan(.0001f),label+" value");Assert.That(Mathf.Abs(keys[0].outTangent-keys[keys.Length-1].inTangent),Is.LessThan(.0001f),label+" velocity");
                }
            }
        }
        [Test] public void TechnicalTexturesRemainLinearAndOrdinaryFishMapsStayWithinOneK()
        {
            const string folder="Assets/OceanAngler/Art/Halibut/";
            foreach(string file in new[]{"BaseColor","Normal","MetallicSmoothness","Fin","Fin_Normal","Fin_MetallicSmoothness","Iris"})
            {
                string path=folder+"Halibut_"+file+".png";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);Assert.That(texture,Is.Not.Null);Assert.That(texture.width,Is.LessThanOrEqualTo(1024));var importer=(TextureImporter)AssetImporter.GetAtPath(path);Assert.That(importer.sRGBTexture,Is.EqualTo(!file.Contains("Normal")&&!file.Contains("MetallicSmoothness")));
            }
        }
    }
}
