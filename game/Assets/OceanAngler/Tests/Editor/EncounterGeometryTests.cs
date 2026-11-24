using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace OceanAngler.Tests
{
    public sealed class EncounterGeometryTests
    {
        [Test] public void BluefinApproachAndRetrieveClearTheAuthoredBayGeometry()
        {
            var bay=Object.Instantiate(Resources.Load<GameObject>("Models/CoralBay"));
            var fish=Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin"));
            try
            {
                var triangles=new System.Collections.Generic.List<(string,Bounds)>();
                foreach(var filter in bay.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(!filter.name.Contains("_LOD")||filter.name.StartsWith("Water_"))continue;
                    var vertices=filter.sharedMesh.vertices.Select(filter.transform.TransformPoint).ToArray();var indices=filter.sharedMesh.triangles;
                    for(int i=0;i<indices.Length;i+=3){var b=new Bounds(vertices[indices[i]],Vector3.zero);b.Encapsulate(vertices[indices[i+1]]);b.Encapsulate(vertices[indices[i+2]]);triangles.Add((filter.name,b));}
                }
                var animator=fish.GetComponentInChildren<Animator>();animator.enabled=false;
                var hook=fish.transform.InverseTransformPoint(fish.GetComponent<FishVisual>().HookPosition);
                var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/OceanAngler/Art/Bluefin/Bluefin.fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__")&&(c.name=="swim"||c.name.EndsWith("|swim")));clip.SampleAnimation(animator.gameObject,.2f);
                var local=new Bounds(Vector3.zero,Vector3.zero);
                foreach(var skin in fish.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mesh=new Mesh();try{skin.BakeMesh(mesh,true);foreach(var v in mesh.vertices)local.Encapsulate(fish.transform.InverseTransformPoint(skin.transform.TransformPoint(v)));}finally{Object.DestroyImmediate(mesh);}
                }
                int poses=0;
                foreach(float power in new[]{.3f,1f})foreach(float lane in new[]{-1.3f,0,1.3f})foreach(var phase in new[]{FishingPhase.Waiting,FishingPhase.Fighting})for(int step=0;step<=10;step++)
                {
                    var pose=FishingEncounter.Evaluate(phase,step/10f,power,hook,Vector3.up*2,lane,3,false);var world=new Bounds(pose.fishPosition,Vector3.zero);
                    for(int corner=0;corner<8;corner++)world.Encapsulate(pose.fishPosition+pose.fishRotation*new Vector3((corner&1)==0?local.min.x:local.max.x,(corner&2)==0?local.min.y:local.max.y,(corner&4)==0?local.min.z:local.max.z));
                    Assert.That(world.max.z,Is.LessThan(bay.GetComponent<BayHabitat>().school.centres.min.z-BayFishSchool.SafetyRadius),"Target must remain ahead of every ambient school body");
                    foreach(var triangle in triangles)if(world.Intersects(triangle.Item2))Assert.Fail(phase+" p="+power+" lane="+lane+" step="+step+" potential body intersection "+triangle.Item1+" fish="+world+" triangle="+triangle.Item2);
                    poses++;
                }
                Debug.Log("ENCOUNTER_BAY_CLEAR poses="+poses+" environmentTriangles="+triangles.Count);
            }
            finally{fish.GetComponent<AuthoredFishVisual>().ReleasePresentation();Object.DestroyImmediate(fish);Object.DestroyImmediate(bay);}
        }
        [Test] public void ConnectedLineClearsTheActualFishBodyOutsideTheMouth()
        {
            var fish=Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin")).GetComponent<AuthoredFishVisual>();
            var rig=Object.Instantiate(Resources.Load<GameObject>("Models/SpinningRig")).GetComponent<AuthoredSpinningRig>();
            try
            {
                rig.InitializeMechanisms();
                var animator=fish.GetComponentInChildren<Animator>();animator.enabled=false;
                var hook=fish.transform.InverseTransformPoint(fish.HookPosition);
                var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/OceanAngler/Art/Bluefin/Bluefin.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
                foreach(float seaLevel in new[]{4.5f,200f})foreach(bool left in new[]{false,true})foreach(var frame in new[]{(FishingPhase.Bite,0f,"bite"),(FishingPhase.Fighting,.5f,"hooked"),(FishingPhase.Landed,1f,"idle")})
                {
                    OceanWorld.PoseGearAtWaterline(rig,seaLevel,left);
                    var pose=FishingEncounter.Evaluate(frame.Item1,frame.Item2,.6f,hook,rig.CastOrigin,0,3,false);fish.transform.SetPositionAndRotation(pose.fishPosition,pose.fishRotation);
                    clips.Single(c=>c.name==frame.Item3||c.name.EndsWith("|"+frame.Item3)).SampleAnimation(animator.gameObject,.2f);
                    rig.Drive(.45f,false,true,false,false,Vector3.zero,false,true,fish);rig.Present(0);
                    foreach(var skin in fish.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.name.EndsWith("_LOD0")))
                    {
                        var mesh=new Mesh();
                        try
                        {
                            skin.BakeMesh(mesh,true);var vertices=mesh.vertices.Select(skin.transform.TransformPoint).ToArray();var indices=mesh.triangles;
                            for(int segment=rig.guides.Length;segment<rig.fishingLine.positionCount-1;segment++)
                            {
                                var a=rig.fishingLine.GetPosition(segment);var delta=rig.fishingLine.GetPosition(segment+1)-a;
                                for(int t=0;t<indices.Length;t+=3)
                                {
                                    if(!Hit(a,delta,vertices[indices[t]],vertices[indices[t+1]],vertices[indices[t+2]],out var point)||Vector3.Distance(point,fish.HookPosition)<.12f)continue;
                                    Assert.Fail(frame.Item1+" line enters "+skin.name+" at "+point+" distance from mouth "+Vector3.Distance(point,fish.HookPosition));
                                }
                            }
                        }
                        finally{Object.DestroyImmediate(mesh);}
                    }
                }
            }
            finally{fish.ReleasePresentation();Object.DestroyImmediate(fish.gameObject);Object.DestroyImmediate(rig.gameObject);}
        }
        static bool Hit(Vector3 origin,Vector3 delta,Vector3 a,Vector3 b,Vector3 c,out Vector3 point)
        {
            point=Vector3.zero;var e1=b-a;var e2=c-a;var cross=Vector3.Cross(delta,e2);float det=Vector3.Dot(e1,cross);if(Mathf.Abs(det)<1e-9f)return false;
            var s=origin-a;float u=Vector3.Dot(s,cross)/det;if(u<0||u>1)return false;
            var q=Vector3.Cross(s,e1);float v=Vector3.Dot(delta,q)/det;if(v<0||u+v>1)return false;
            float t=Vector3.Dot(e2,q)/det;if(t<0||t>1)return false;point=origin+delta*t;return true;
        }
    }
}
