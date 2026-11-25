#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using Unity.Collections;
namespace OceanAngler.Tests
{
    // Actual imported meshes and driven transforms; no pixel or physical-device claim.
    public sealed class RollerMechanismTests
    {
        AuthoredSpinningRig rig;
        readonly Vector3 target=new Vector3(2,.3f,4);
        [UnitySetUp] public IEnumerator Setup()
        {
            rig=Object.Instantiate(Resources.Load<GameObject>("Models/SpinningRig")).GetComponent<AuthoredSpinningRig>();rig.enabled=false;
            rig.transform.SetPositionAndRotation(new Vector3(3,1,-2),Quaternion.Euler(17,31,-12));rig.transform.localScale=Vector3.one*1.7f;
            rig.Drive(0,false,false,false,false,target,true);rig.Present(0);yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup(){if(rig!=null)Object.Destroy(rig.gameObject);yield return null;}
        Vector3[] ImportedPoints(MeshFilter filter)
        {
            // Read a diagnostic Editor snapshot; do not enable production
            // Read/Write or keep an extra mesh copy in the shipping Player.
            using(var snapshot=MeshUtility.AcquireReadOnlyMeshData(filter.sharedMesh))
            using(var vertices=new NativeArray<Vector3>(snapshot[0].vertexCount,Allocator.Temp))
            {snapshot[0].GetVertices(vertices);return vertices.ToArray();}
        }
        int[] ImportedTriangles(Mesh mesh)
        {
            var result=new System.Collections.Generic.List<int>();
            using(var snapshot=MeshUtility.AcquireReadOnlyMeshData(mesh))
            {
                for(int sub=0;sub<snapshot[0].subMeshCount;sub++)
                using(var indices=new NativeArray<int>(snapshot[0].GetSubMesh(sub).indexCount,Allocator.Temp))
                {snapshot[0].GetIndices(indices,sub);result.AddRange(indices.ToArray());}
            }
            return result.ToArray();
        }
        static bool SegmentHits(Vector3 start,Vector3 end,Vector3 a,Vector3 b,Vector3 c)
        {
            var direction=end-start;var edge1=b-a;var edge2=c-a;var p=Vector3.Cross(direction,edge2);float determinant=Vector3.Dot(edge1,p);if(Mathf.Abs(determinant)<1e-12f)return false;
            var offset=start-a;float u=Vector3.Dot(offset,p)/determinant;if(u<0||u>1)return false;var q=Vector3.Cross(offset,edge1);float v=Vector3.Dot(direction,q)/determinant;if(v<0||u+v>1)return false;
            float t=Vector3.Dot(edge2,q)/determinant;return t>.0001f&&t<.9999f;
        }
        float ActualGrooveRadius(int lod)
        {
            var filter=rig.GetComponentsInChildren<MeshFilter>(true).Single(m=>m.name=="LineRoller_LOD"+lod);
            var axis=(rig.rollerAxis.position-rig.roller.position).normalized;float marker=Vector3.Distance(rig.rollerSurface.position,rig.roller.position);
            var points=ImportedPoints(filter).Select(filter.transform.TransformPoint).Select(p=>p-rig.roller.position).ToArray();
            Assert.That(points.Min(p=>Vector3.ProjectOnPlane(p,axis).magnitude),Is.EqualTo(.0019f*1.7f).Within(.00003f));
            var middle=points.Where(p=>Mathf.Abs(Vector3.Dot(p,axis))<marker*.25f).ToArray();Assert.That(middle.Length,Is.GreaterThan(10));
            float radius=middle.Max(p=>Vector3.ProjectOnPlane(p,axis).magnitude);Assert.That(radius,Is.EqualTo(marker).Within(.00003f));return radius;
        }
        [Test] public void ImportedBoreAndGrooveKeepLineOutsideTheSolidAcrossWindingAndBending()
        {
            for(int lod=0;lod<3;lod++)Assert.That(ActualGrooveRadius(lod),Is.GreaterThan(.003f));
            for(int step=0;step<330;step++)
            {
                rig.Drive((Mathf.Sin(step*.12f)+1)*.5f,true,false,false,false,target,true);rig.Present(.001f);
                var normal=(rig.lineExit.position-rig.roller.position).normalized;float radius=ActualGrooveRadius(0);
                Assert.That(Vector3.Distance(rig.lineExit.position,rig.roller.position)-radius,Is.EqualTo(rig.fishingLine.startWidth*.5f).Within(.00003f));
                // Bound the actual tangent-plane miss in metres. An angular
                // epsilon amplifies float subtraction at this3m/1.7x fixture;
                // 0.03mm is4.3% of the0.7mm inspection line width.
                Assert.That(Mathf.Abs(Vector3.Dot(rig.guides[0].position-rig.lineExit.position,normal)),Is.LessThan(.00003f));
                Assert.That(Vector3.Distance(rig.fishingLine.GetPosition(0),rig.lineExit.position),Is.LessThan(.00001f));Assert.That(rig.MaximumGuideLineError(),Is.LessThan(.00001f));
            }
        }
        [Test] public void ActualRollerSurfaceTravelMatchesPickupAndFreezesOnPauseStopOrReducedMotion()
        {
            float rollerTravel=0,rotorTravel=0;float radius=ActualGrooveRadius(0),pickup=Vector3.Distance(rig.spool.position,rig.spoolLineRadius.position);
            rig.Drive(.4f,true,false,false,false,target,true);
            for(int step=0;step<330;step++)
            {
                var roller=rig.roller.localRotation;var rotor=rig.rotor.localRotation;rig.Present(.001f);
                rollerTravel+=Quaternion.Angle(roller,rig.roller.localRotation);rotorTravel+=Quaternion.Angle(rotor,rig.rotor.localRotation);
            }
            Assert.That(rollerTravel,Is.GreaterThan(2000));Assert.That(rollerTravel*radius/(rotorTravel*pickup),Is.EqualTo(1).Within(.003f));
            foreach(int mode in new[]{0,1,2})
            {
                rig.Drive(.4f,mode!=1,mode==0,mode==2,false,target,true);var rotation=rig.roller.localRotation;
                for(int step=0;step<20;step++)rig.Present(.01f);
                Assert.That(Quaternion.Angle(rotation,rig.roller.localRotation),Is.LessThan(.01f));
            }
        }
        [Test] public void OpenBailPaysOverActualFrontLipAndClosingRestoresRollerContact()
        {
            var before=rig.roller.position;
            rig.Drive(0,false,false,false,true,target,true);for(int i=0;i<3;i++)rig.Present(.1f);
            Assert.That(rig.BailOpenDegrees,Is.EqualTo(82));Assert.That(rig.LinePassesRoller,Is.False);
            Assert.That(Vector3.Distance(before,rig.roller.position),Is.GreaterThan(.005f),"Pickup carrier must open with the bail");
            var axis=(rig.rotorAxis.position-rig.rotor.position).normalized;var center=rig.spoolLipCenter.position;var start=rig.fishingLine.GetPosition(0);
            var filter=rig.GetComponentsInChildren<MeshFilter>(true).Single(m=>m.name=="Spool_LOD0");
            var lip=ImportedPoints(filter).Select(filter.transform.TransformPoint).Select(p=>p-center).Where(p=>Mathf.Abs(Vector3.Dot(p,axis))<.00003f).ToArray();Assert.That(lip.Length,Is.GreaterThan(20));
            float radius=lip.Max(p=>Vector3.ProjectOnPlane(p,axis).magnitude);
            Assert.That(Vector3.Distance(start,center),Is.EqualTo(radius+rig.fishingLine.startWidth*.5f).Within(.00003f));Assert.That(Mathf.Abs(Vector3.Dot(start-center,axis)),Is.LessThan(.00003f));
            Assert.That(Vector3.Distance(start,rig.lineExit.position),Is.GreaterThan(.01f));Assert.That(rig.MaximumGuideLineError(),Is.LessThan(.00001f));
            rig.Drive(0,false,false,false,false,target,true);for(int i=0;i<3;i++)rig.Present(.1f);
            Assert.That(rig.LinePassesRoller,Is.True);Assert.That(Vector3.Distance(rig.fishingLine.GetPosition(0),rig.lineExit.position),Is.LessThan(.00001f));
        }
        [Test] public void GuideSegmentClearsTheActualOscillatingSpoolInBothBailModes()
        {
            var filter=rig.GetComponentsInChildren<MeshFilter>(true).Single(m=>m.name=="Spool_LOD0");var vertices=ImportedPoints(filter);var triangles=ImportedTriangles(filter.sharedMesh);
            // More than ten rotor turns and one full spool oscillation.
            foreach(bool open in new[]{false,true})
            {
                rig.Drive(.4f,true,false,false,open,target,true);
                for(int step=0;step<330;step++)
                {
                    rig.Present(.01f);var start=filter.transform.InverseTransformPoint(rig.fishingLine.GetPosition(0));var end=filter.transform.InverseTransformPoint(rig.fishingLine.GetPosition(1));
                    for(int i=0;i<triangles.Length;i+=3)
                        if(SegmentHits(start,end,vertices[triangles[i]],vertices[triangles[i+1]],vertices[triangles[i+2]]))Assert.Fail("Line intersects imported spool triangle "+i/3+" at rotor "+rig.RotorDegrees+" degrees, open="+open);
                }
            }
        }
    }
}
#endif
