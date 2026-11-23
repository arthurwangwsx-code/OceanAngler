using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace OceanAngler.Tests
{
    // Uses the imported shipping prefab and actual frame updates. No camera/GPU assertion.
    public sealed class SpinningRigTests
    {
        AuthoredSpinningRig rig;
        float previousMaximumDelta;
        readonly Vector3 target=new Vector3(2,.3f,4);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousMaximumDelta=Time.maximumDeltaTime;Time.maximumDeltaTime=.04f;
            var prefab=Resources.Load<GameObject>("Models/SpinningRig");Assert.That(prefab,Is.Not.Null);
            rig=Object.Instantiate(prefab).GetComponent<AuthoredSpinningRig>();
            rig.transform.SetPositionAndRotation(new Vector3(3,1,-2),Quaternion.Euler(17,31,-12));
            rig.transform.localScale=Vector3.one*1.7f;
            rig.Drive(0,false,false,false,false,target,true);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {if(rig!=null)Object.Destroy(rig.gameObject);Time.maximumDeltaTime=previousMaximumDelta;yield return null;}

        [Test]
        public void ImportedAssembliesHaveOneLodOwnerAndRealMechanicalParents()
        {
            Assert.That(rig.rodBones.Length,Is.EqualTo(10));Assert.That(rig.guides.Length,Is.EqualTo(9));
            var groups=rig.GetComponentsInChildren<LODGroup>(true);Assert.That(groups.Length,Is.EqualTo(1));
            var lods=groups[0].GetLODs();Assert.That(lods.Length,Is.EqualTo(3));
            var all=lods.SelectMany(l=>l.renderers).ToArray();Assert.That(all.Length,Is.EqualTo(21));Assert.That(all.Distinct().Count(),Is.EqualTo(21));
            foreach(var lod in lods)Assert.That(lod.renderers.Length,Is.EqualTo(7));
            foreach(var part in new[]{("Rotor",rig.rotor),("Spool",rig.spool),("Crank",rig.crank),("BailWire",rig.bail)})
                foreach(var renderer in all.Where(r=>r.name.StartsWith(part.Item1+"_LOD")))Assert.That(renderer.transform.IsChildOf(part.Item2),Is.True,renderer.name);
            var tip=rig.transform.InverseTransformPoint(rig.guides[8].position);
            Assert.That(tip.y,Is.EqualTo(2.35f).Within(.001f));
        }

        [UnityTest]
        public IEnumerator WindingMovesActualCrankRotorAndOscillatingSpool()
        {
            rig.Drive(.45f,true,false,false,false,target,true);
            float end=Time.time+.7f,crankTravel=0,rotorTravel=0,min=float.PositiveInfinity,max=float.NegativeInfinity;
            var crank=rig.crank.localRotation;var rotor=rig.rotor.localRotation;
            while(Time.time<end)
            {
                yield return new WaitForSeconds(.03f);
                crankTravel+=Quaternion.Angle(crank,rig.crank.localRotation);rotorTravel+=Quaternion.Angle(rotor,rig.rotor.localRotation);
                crank=rig.crank.localRotation;rotor=rig.rotor.localRotation;
                float y=rig.transform.InverseTransformPoint(rig.spool.position).y;min=Mathf.Min(min,y);max=Mathf.Max(max,y);
            }
            Assert.That(crankTravel,Is.GreaterThan(100));Assert.That(rotorTravel/crankTravel,Is.EqualTo(5.2f).Within(.04f));
            Assert.That(max-min,Is.InRange(.002f,.0121f));
        }

        [UnityTest]
        public IEnumerator PauseStopAndReducedMotionFreezeActualMechanisms()
        {
            rig.Drive(.4f,true,false,false,false,target,true);yield return new WaitForSeconds(.2f);
            foreach(int mode in new[]{0,1,2})
            {
                rig.Drive(.4f,mode!=1,mode==0,mode==2,false,target,true);yield return null;
                var crank=rig.crank.localRotation;var rotor=rig.rotor.localRotation;var spool=rig.spool.position;
                yield return new WaitForSeconds(.15f);
                Assert.That(Quaternion.Angle(crank,rig.crank.localRotation),Is.LessThan(.02f),"crank mode "+mode);
                Assert.That(Quaternion.Angle(rotor,rig.rotor.localRotation),Is.LessThan(.02f),"rotor mode "+mode);
                Assert.That(Vector3.Distance(spool,rig.spool.position),Is.LessThan(.00001f),"spool mode "+mode);
            }
        }

        [UnityTest]
        public IEnumerator CastingOpensActualBailAndClosingRestoresIt()
        {
            var closed=rig.bail.localRotation;var rotor=rig.rotor.localRotation;
            rig.Drive(0,false,false,false,true,target,true);yield return new WaitForSeconds(.3f);
            Assert.That(Quaternion.Angle(closed,rig.bail.localRotation),Is.EqualTo(82).Within(.1f));
            Assert.That(Quaternion.Angle(rotor,rig.rotor.localRotation),Is.LessThan(.02f));
            rig.Drive(0,false,false,false,false,target,true);yield return new WaitForSeconds(.3f);
            Assert.That(Quaternion.Angle(closed,rig.bail.localRotation),Is.LessThan(.02f));
        }

        [UnityTest]
        public IEnumerator LureNoseAndHookedLineFollowActualAnimatedMouth()
        {
            rig.Drive(0,false,false,false,false,target);yield return null;yield return null;
            Assert.That(Vector3.Distance(rig.lureNose.position,target),Is.LessThan(.00001f));
            var fish=Object.Instantiate(Resources.Load<GameObject>("Models/Bluefin")).GetComponent<AuthoredFishVisual>();
            try
            {
                fish.transform.SetPositionAndRotation(new Vector3(-1,.6f,3),Quaternion.Euler(0,-25,0));fish.semantic="bite";
                rig.Drive(.2f,false,false,false,false,Vector3.zero,false,false,fish);yield return null;yield return null;
                for(int i=0;i<8;i++)
                {
                    fish.transform.position+=new Vector3(.07f,.02f,-.03f);yield return null;
                    Assert.That(Vector3.Distance(rig.lureNose.position,fish.HookPosition),Is.LessThan(.00001f));
                    Assert.That(Vector3.Distance(rig.fishingLine.GetPosition(rig.fishingLine.positionCount-1),fish.HookPosition),Is.LessThan(.00001f));
                }
                fish.semantic="idle";rig.Drive(.2f,false,false,false,false,Vector3.zero,false,true,fish);yield return null;yield return null;
                Assert.That(rig.lure.gameObject.activeSelf,Is.False);
                Assert.That(Vector3.Distance(rig.fishingLine.GetPosition(rig.fishingLine.positionCount-1),fish.HookPosition),Is.LessThan(.00001f));
            }
            finally{Object.Destroy(fish.gameObject);}
        }
        [UnityTest]
        public IEnumerator LoadedSkinMovesWithGuideAnchorsAndLineReachesHookTarget()
        {
            var skin=rig.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="Rod_LOD0");
            // Explicitly compensate the FBX renderer's scale before TransformPoint.
            var baked=new Mesh();skin.BakeMesh(baked,true);var before=baked.vertices;
            var tipBefore=rig.guides[8].position;
            // Select geometric ring vertices in the unbent imported mesh, independently of line markers.
            float[] radii={.028f,.022f,.016f,.011f,.0085f};
            var rings=Enumerable.Range(0,5).Select(i=>Enumerable.Range(0,before.Length).Where(v=>
            {
                var delta=rig.transform.InverseTransformPoint(skin.transform.TransformPoint(before[v]))-rig.transform.InverseTransformPoint(rig.guides[i].position);
                return Mathf.Abs(delta.y)<.0013f&&Mathf.Abs(new Vector2(delta.x,delta.z).magnitude-(radii[i]+.0013f))<.0015f;
            }).ToArray()).ToArray();
            foreach(var ring in rings)Assert.That(ring.Length,Is.GreaterThan(12));
            rig.Drive(.9f,false,false,false,false,target,false,true);yield return null;yield return null;
            Assert.That(Vector3.Distance(tipBefore,rig.guides[8].position),Is.GreaterThan(.04f));
            Assert.That(rig.MaximumGuideLineError(),Is.LessThan(.00001f));
            Assert.That(Vector3.Distance(rig.fishingLine.GetPosition(rig.fishingLine.positionCount-1),target),Is.LessThan(.00001f));
            Assert.That(rig.lure.gameObject.activeSelf,Is.False);
            skin.BakeMesh(baked,true);var after=baked.vertices;
            for(int i=0;i<rings.Length;i++)
            {
                var center=Vector3.zero;foreach(int v in rings[i])center+=skin.transform.TransformPoint(after[v]);center/=rings[i].Length;
                Assert.That(Vector3.Distance(center,rig.guides[i].position)/rig.transform.lossyScale.x,Is.LessThan(.003f),"baked guide "+i);
            }
            Object.Destroy(baked);
        }
    }
}
