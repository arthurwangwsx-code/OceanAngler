using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
namespace OceanAngler.Editor
{
    public static class JournalEnvelopeBuilder
    {
        public static string[] SourcePaths()=>new[]{"Assets/OceanAngler/Runtime/FishModel.cs","Assets/OceanAngler/Runtime/FishVisual.cs","Assets/OceanAngler/Runtime/AuthoredFishVisual.cs","Assets/OceanAngler/Resources/ContentCatalog.json"}.Concat(new[]{"Bluefin","Halibut"}.SelectMany(name=>new[]{"Assets/OceanAngler/Resources/Models/"+name+".prefab","Assets/OceanAngler/Art/"+name+"/"+name+".fbx","Assets/OceanAngler/Art/"+name+"/"+name+".fbx.meta","Assets/OceanAngler/Art/"+name+"/"+name+".controller","Assets/OceanAngler/Art/"+name+"/Motions/idle.anim","Assets/OceanAngler/Art/"+name+"/Motions/swim.anim"})).ToArray();
        public static string Digest(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        public static void Bake()
        {
            var data=new JournalEnvelopeData{unityVersion=Application.unityVersion,scope="Own actual meshes: 65 swim phases; authored idle phases too; up to32 nonempty longitudinal sections plus12mm margin. Sampled presentation envelope, not continuous biomechanics or final art acceptance"};
            var shapes=new List<JournalEnvelopeShape>();var scratch=new Mesh();
            try
            {
                foreach(var fish in ContentCatalog.Load().fish)
                {
                    string resource=fish.id=="trevally"?"Models/Bluefin":fish.id=="halibut"?"Models/Halibut":null;
                    var root=resource!=null?UnityEngine.Object.Instantiate(Resources.Load<GameObject>(resource)):new GameObject("Envelope-"+fish.id);
                    try
                    {
                        var prototype=resource==null?root.AddComponent<FishModel>():null;if(prototype!=null){prototype.Initialize(fish);prototype.paused=true;}
                        var animator=root.GetComponentInChildren<Animator>();if(animator!=null)animator.enabled=false;
                        var points=new List<Vector3>();int poses=0,vertices=0;
                        void Collect()
                        {
                            int count=0;
                            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
                            {
                                var matrix=root.transform.worldToLocalMatrix*renderer.localToWorldMatrix;
                                if(renderer is SkinnedMeshRenderer skin){skin.BakeMesh(scratch,true);foreach(var v in scratch.vertices){points.Add(matrix.MultiplyPoint3x4(v));count++;}}
                                else if(renderer is MeshRenderer mesh){var filter=mesh.GetComponent<MeshFilter>();if(filter!=null)foreach(var v in filter.sharedMesh.vertices){points.Add(matrix.MultiplyPoint3x4(v));count++;}}
                                else if(renderer is LineRenderer line)for(int i=0;i<line.positionCount;i++){points.Add(line.useWorldSpace?root.transform.InverseTransformPoint(line.GetPosition(i)):matrix.MultiplyPoint3x4(line.GetPosition(i)));count++;}
                            }
                            if(vertices!=0&&vertices!=count)throw new InvalidOperationException("Geometry count changed through animation");vertices=count;poses++;
                        }
                        foreach(string semantic in animator!=null?new[]{"swim","idle"}:new[]{"swim"})
                        {
                            var clip=animator!=null?animator.runtimeAnimatorController.animationClips.Single(c=>c.name==semantic):null;
                            for(int phase=0;phase<=64;phase++)
                            {
                                if(clip!=null)clip.SampleAnimation(animator.gameObject,clip.length*phase/64);
                                else{typeof(FishModel).GetField("animationTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(prototype,Mathf.PI*2/3.5f*phase/64);prototype.SendMessage("Update");}
                                Collect();
                            }
                        }
                        var whole=new Bounds(points[0],Vector3.zero);foreach(var point in points)whole.Encapsulate(point);
                        var sections=new Bounds[data.sections];var used=new bool[data.sections];
                        foreach(var point in points){int index=Mathf.Clamp((int)((point.x-whole.min.x)/whole.size.x*data.sections),0,data.sections-1);if(!used[index]){sections[index]=new Bounds(point,Vector3.zero);used[index]=true;}else sections[index].Encapsulate(point);}
                        // Sparse tail triangles can bridge bins without any
                        // vertex in them. Frustum containment is convex, so its
                        // endpoint vertices also bound those triangle interiors.
                        sections=sections.Where((value,index)=>used[index]).ToArray();
                        for(int i=0;i<sections.Length;i++)sections[i].Expand(data.padding*2);whole.Expand(data.padding*2);
                        shapes.Add(new JournalEnvelopeShape{fishId=fish.id,poses=poses,sourceVertices=vertices,bounds=whole,sections=sections});
                    }
                    finally{UnityEngine.Object.DestroyImmediate(root);}
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(scratch);}
            data.shapes=shapes.ToArray();data.inputs=SourcePaths().Select(path=>new JournalEnvelopeInput{path=path,sha256=Digest(path)}).ToArray();
            const string target="Assets/OceanAngler/Resources/JournalEnvelopes.json";File.WriteAllText(target,JsonUtility.ToJson(data,true)+"\n");AssetDatabase.ImportAsset(target);Debug.Log("JOURNAL_ENVELOPES_BAKED "+shapes.Count);
        }
    }
}
