using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace OceanAngler.Tests
{
    public sealed class HalibutAssetTests
    {
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
                    float maximum=0;
                    foreach(float time in new[]{.125f,.25f,.375f,.5f,.75f,1f}){swim.SampleAnimation(animator.gameObject,time);renderer.BakeMesh(b,true);maximum=Mathf.Max(maximum,a.vertices.Zip(b.vertices,(x,y)=>Vector3.Distance(renderer.transform.TransformPoint(x),renderer.transform.TransformPoint(y))).Max());}
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
