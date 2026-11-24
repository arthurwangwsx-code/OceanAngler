using NUnit.Framework;
using UnityEngine;
namespace OceanAngler.Tests
{
    public sealed class WaterlinePresentationTests
    {
        [Test] public void ActualRigMechanismsStayAboveEachHabitatWaterlineForBothHands()
        {
            var rig=Object.Instantiate(Resources.Load<GameObject>("Models/SpinningRig")).GetComponent<AuthoredSpinningRig>();
            try
            {
                rig.InitializeMechanisms();
                foreach(string resource in new[]{"CoralBay","NearshoreChannel","ContinentalSlope"})
                {
                    var habitat=Resources.Load<GameObject>("Models/"+resource).GetComponent<MarineHabitat>();
                    foreach(bool left in new[]{false,true})
                    {
                        OceanWorld.PoseGearAtWaterline(rig,habitat.SeaLevel,left);rig.Drive(.9f,true,false,false,false,new Vector3(0,.2f,2));rig.Present(.05f);
                        foreach(var mechanism in new[]{rig.transform,rig.rotor,rig.spool,rig.crank,rig.roller})Assert.That(mechanism.position.y,Is.GreaterThan(habitat.SeaLevel),resource+" "+mechanism.name);
                        foreach(var guide in rig.guides)Assert.That(guide.position.y,Is.GreaterThan(habitat.SeaLevel),resource+" "+guide.name);
                        Assert.That(rig.MaximumGuideLineError(),Is.LessThan(.00001f));
                    }
                }
            }
            finally{rig.ReleasePresentation();Object.DestroyImmediate(rig.gameObject);}
        }
        [Test] public void CastLandsOnActualSurfaceAndSinksContinuouslyBeforeTheBite()
        {
            foreach(float sea in new[]{4.5f,200f})foreach(float power in new[]{.3f,1f})foreach(float lane in new[]{-1.3f,0,1.3f})
            {
                var launch=new Vector3(1,sea+2,-4);var hook=new Vector3(-.8f,0,0);
                var impact=FishingEncounter.Evaluate(FishingPhase.Casting,1,power,hook,launch,lane,0,false,sea);
                Assert.That(impact.lurePosition.y,Is.EqualTo(sea).Within(.00001f));
                var waiting=FishingEncounter.Evaluate(FishingPhase.Waiting,0,power,hook,launch,lane,0,false,sea);Assert.That(Vector3.Distance(impact.lurePosition,waiting.lurePosition),Is.LessThan(.00001f));
                float previous=sea;
                for(int i=0;i<=20;i++)
                {
                    var cast=FishingEncounter.Evaluate(FishingPhase.Casting,i/20f,power,hook,launch,lane,0,false,sea);Assert.That(cast.lurePosition.y,Is.GreaterThanOrEqualTo(sea-.00001f));
                    var sink=FishingEncounter.Evaluate(FishingPhase.Waiting,i/20f,power,hook,launch,lane,0,false,sea);Assert.That(sink.lurePosition.y,Is.LessThanOrEqualTo(previous+.00001f));previous=sink.lurePosition.y;
                }
                var end=FishingEncounter.Evaluate(FishingPhase.Waiting,1,power,hook,launch,lane,0,false,sea);var bite=FishingEncounter.Evaluate(FishingPhase.Bite,0,power,hook,launch,lane,0,false,sea);
                Assert.That(Vector3.Distance(end.lurePosition,bite.lurePosition),Is.LessThan(.00001f));Assert.That(Vector3.Distance(bite.fishPosition+bite.fishRotation*hook,bite.lurePosition),Is.LessThan(.00001f));
            }
        }
        [Test] public void NewCameraSettingIsAdditiveAndPreservesOldSaveFields()
        {
            var old=JsonUtility.FromJson<GameSettings>("{\"assisted\":true,\"leftHanded\":true,\"reducedMotion\":false,\"sound\":true,\"haptics\":true,\"music\":false}");
            Assert.That(old.underwaterOnly,Is.False);Assert.That(old.leftHanded,Is.True);old.underwaterOnly=true;
            var reloaded=JsonUtility.FromJson<GameSettings>(JsonUtility.ToJson(old));Assert.That(reloaded.underwaterOnly&&reloaded.leftHanded,Is.True);
        }
    }
}
