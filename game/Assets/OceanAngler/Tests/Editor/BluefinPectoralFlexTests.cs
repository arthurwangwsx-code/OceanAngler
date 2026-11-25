using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace OceanAngler.Tests
{
    public sealed class BluefinPectoralFlexTests
    {
        static readonly string[] Controls={"pectoralLMid","pectoralLTip","pectoralRMid","pectoralRTip"};
        [Test] public void ImportedMeshesActuallyFlexDistallyWhileRootsAndOtherPartsStayAttached()
        {
            var root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin"));var mesh=new Mesh();
            try
            {
                var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;var renderers=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var reference=renderers.Single(r=>r.name.EndsWith("_LOD0"));var controls=Controls.Select(n=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==n)).ToArray();
                var rest=controls.Select(b=>BindLocal(reference,b)).ToArray();
                foreach(var clip in animator.runtimeAnimatorController.animationClips)
                {
                    foreach(var renderer in renderers)
                    {
                        var weights=renderer.sharedMesh.boneWeights;var names=renderer.bones.Select(b=>b.name).ToArray();
                        bool Fin(BoneWeight w)=>Influences(w).Any(p=>p.Item2>1e-6f&&(names[p.Item1].StartsWith("pectoralL")||names[p.Item1].StartsWith("pectoralR")));
                        bool AttachedRoot(BoneWeight w)=>Influences(w).Any(p=>p.Item2>.99999f&&(names[p.Item1]=="pectoralL"||names[p.Item1]=="pectoralR"));
                        int[] fin=Enumerable.Range(0,weights.Length).Where(i=>Fin(weights[i])).ToArray();int[] roots=fin.Where(i=>AttachedRoot(weights[i])).ToArray();
                        Assert.That(fin.Length,Is.GreaterThan(30));Assert.That(roots.Length,Is.GreaterThan(5));float distal=0,rootError=0,bodyError=0;
                        foreach(float time in new[]{0,.25f,.5f,.75f})
                        {
                            clip.SampleAnimation(animator.gameObject,clip.length*time);renderer.BakeMesh(mesh,true);var active=mesh.vertices;
                            for(int i=0;i<controls.Length;i++){controls[i].localPosition=rest[i].GetColumn(3);controls[i].localRotation=rest[i].rotation;}
                            renderer.BakeMesh(mesh,true);var held=mesh.vertices;
                            foreach(int i in fin)distal=Mathf.Max(distal,renderer.transform.TransformVector(active[i]-held[i]).magnitude);
                            foreach(int i in roots)rootError=Mathf.Max(rootError,renderer.transform.TransformVector(active[i]-held[i]).magnitude);
                            foreach(int i in Enumerable.Range(0,weights.Length).Where(i=>!Fin(weights[i])))bodyError=Mathf.Max(bodyError,renderer.transform.TransformVector(active[i]-held[i]).magnitude);
                        }
                        string label=clip.name+"/"+renderer.name;
                        Debug.Log("PECTORAL_FLEX_WORLD_METRES "+label+" distal="+distal+" root="+rootError+" other="+bodyError+" scale="+renderer.transform.lossyScale);
                        Assert.That(distal,Is.GreaterThan(.0005f),label+" must move the visible surface beyond rigid-root motion in world metres");
                        Assert.That(rootError,Is.LessThan(.00001f),label+" root remains attached");Assert.That(bodyError,Is.LessThan(.00001f),label+" body/eyes/gill/mouth/other fins remain unaffected");
                    }
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(root);}
        }
        static (int,float)[] Influences(BoneWeight w)=>new[]{(w.boneIndex0,w.weight0),(w.boneIndex1,w.weight1),(w.boneIndex2,w.weight2),(w.boneIndex3,w.weight3)};
        static Matrix4x4 BindLocal(SkinnedMeshRenderer renderer,Transform bone)
        {
            int index=Array.IndexOf(renderer.bones,bone),parent=Array.IndexOf(renderer.bones,bone.parent);
            Assert.That(index,Is.GreaterThanOrEqualTo(0));Assert.That(parent,Is.GreaterThanOrEqualTo(0));
            return renderer.sharedMesh.bindposes[parent]*renderer.sharedMesh.bindposes[index].inverse;
        }
    }
}
