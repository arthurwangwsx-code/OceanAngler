using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace OceanAngler.Tests
{
    public sealed class FishingSessionTests
    {
        ContentCatalog catalog;
        string directory;
        [SetUp] public void Setup() { catalog=ContentCatalog.Load();directory=Path.Combine(Path.GetTempPath(),"OceanTests-"+Guid.NewGuid());Directory.CreateDirectory(directory); }
        [TearDown] public void Teardown() { Directory.Delete(directory,true); }
        SaveStore Store() => new SaveStore(Path.Combine(directory,"progress.json"),catalog);
        ProgressService Service() => new ProgressService(catalog,Store());
        static void Advance(FishingSession s,int frames,bool reel=false) { for(int i=0;i<frames;i++)s.Tick(.05f,reel); }
        FishingSession Cast(ProgressData p=null)
        {
            var s=new FishingSession();s.Cast(catalog.Fish("trevally"),p??new ProgressData(),catalog,new System.Random(42));return s;
        }
        FishingSession Fighting(bool standard=false)
        {
            var p=new ProgressData { tutorialComplete=true };p.settings.assisted=!standard;
            var s=Cast(p);while(s.Phase!=FishingPhase.Bite)s.Tick(.05f,false);s.Strike();return s;
        }
        CatchRecord Record(string fish="trevally",string id=null)
        {
            var f=catalog.Fish(fish);return new CatchRecord { catchId=id??Guid.NewGuid().ToString(),fishId=f.id,habitatId=f.habitatId,weight=(f.minWeight+f.maxWeight)/2,length=(f.minLength+f.maxLength)/2,skillPoints=30,mode="assisted" };
        }
        [Test] public void CompleteCatalogHasStarterLoadoutAndEighteenTasks()
        {
            Assert.AreEqual(12,catalog.fish.Length);Assert.AreEqual(3,catalog.habitats.Length);Assert.AreEqual(18,catalog.tasks.Length);
            Assert.AreEqual(3,catalog.observations.Length);
            foreach(var pair in new[]{("rod",6),("reel",4),("line",4),("lure",6)})Assert.AreEqual(pair.Item2,catalog.gear.Count(g=>g.category==pair.Item1));
            new ProgressData().Validate(catalog);
        }
        [Test] public void ObservationSpeciesCannotBeHookedOrAwardedPoints()
        {
            var service=Service();
            foreach(var observed in catalog.observations)
            {
                var session=new FishingSession();var data=new ProgressData{habitatId=observed.habitatId};
                Assert.Throws<ArgumentException>(()=>session.Cast(new FishDefinition{id=observed.id,habitatId=observed.habitatId},data,catalog,new System.Random(1)));
                Assert.That(session.Phase,Is.EqualTo(FishingPhase.Ready));Assert.That(session.Catch,Is.Null);
                var record=Record();record.fishId=observed.id;record.habitatId=observed.habitatId;
                Assert.Throws<ArgumentException>(()=>service.Settle(record));
            }
            Assert.That(Service().Data.totalPoints,Is.Zero);Assert.That(Service().Data.catches,Is.Empty);
        }
        [Test] public void MissedStandardBiteCanRetryWithoutLosingPoints()
        {
            var p=new ProgressData {tutorialComplete=true};p.settings.assisted=false;var s=Cast(p);Advance(s,200);
            Assert.AreEqual(FishingPhase.Escaped,s.Phase);s.Retry();Assert.AreEqual(FishingPhase.Ready,s.Phase);Assert.AreEqual(0,p.totalPoints);
        }
        [Test] public void FirstCatchAlwaysHasAssistance() { var p=new ProgressData();p.settings.assisted=false;var s=Cast(p);Assert.IsTrue(s.Assisted);Advance(s,250);Assert.AreEqual(FishingPhase.Fighting,s.Phase); }
        [Test] public void PauseFreezesAndBlocksInput()
        {
            var s=Fighting();s.Paused=true;Advance(s,200,true);s.Strike();Assert.AreEqual(0,s.Progress);Assert.AreEqual(0,s.Tension);
        }
        [Test] public void EncounterClockUsesPausedSimulationAndConfiguredBiteDelay()
        {
            var s=Cast();Advance(s,8);Assert.That(s.PhaseProgress,Is.EqualTo(.5f).Within(.001f));
            s.Paused=true;Advance(s,20);Assert.That(s.PhaseProgress,Is.EqualTo(.5f).Within(.001f));
            s.Paused=false;while(s.Phase==FishingPhase.Casting)s.Tick(.05f,false);
            Advance(s,15);Assert.That(s.Phase,Is.EqualTo(FishingPhase.Waiting));Assert.That(s.PhaseProgress,Is.EqualTo(.25f).Within(.001f));
            s.Paused=true;Advance(s,100);Assert.That(s.PhaseProgress,Is.EqualTo(.25f).Within(.001f));
        }
        [Test] public void EncounterStageBoundariesMeetAndMouthReachesLure()
        {
            foreach(float power in new[]{.3f,.6f,1f})foreach(float lane in new[]{-1.3f,0,1.3f})
            {
                var hook=new Vector3(-1.1f,.04f,0);var launch=new Vector3(1,1.6f,.3f);
                FishingEncounter.Pose Pose(FishingPhase phase,float progress)=>FishingEncounter.Evaluate(phase,progress,power,hook,launch,lane,3,false);
                var cast=Pose(FishingPhase.Casting,1);var wait=Pose(FishingPhase.Waiting,0);var approach=Pose(FishingPhase.Waiting,1);var bite=Pose(FishingPhase.Bite,0);var fight=Pose(FishingPhase.Fighting,0);var end=Pose(FishingPhase.Fighting,1);var landed=Pose(FishingPhase.Landed,1);
                Assert.That(Vector3.Distance(cast.fishPosition,wait.fishPosition),Is.LessThan(.00001f));Assert.That(Quaternion.Angle(cast.fishRotation,wait.fishRotation),Is.LessThan(.01f));
                Assert.That(Vector3.Distance(cast.lurePosition,wait.lurePosition),Is.LessThan(.00001f));
                Assert.That(Vector3.Distance(approach.fishPosition,bite.fishPosition),Is.LessThan(.00001f));Assert.That(Quaternion.Angle(approach.fishRotation,bite.fishRotation),Is.LessThan(.01f));
                Assert.That(Vector3.Distance(bite.fishPosition+bite.fishRotation*hook,bite.lurePosition),Is.LessThan(.00001f));
                Assert.That(Vector3.Distance(bite.fishPosition,fight.fishPosition),Is.LessThan(.00001f));
                Assert.That(Vector3.Distance(end.fishPosition,landed.fishPosition),Is.LessThan(.00001f));Assert.That(Quaternion.Angle(end.fishRotation,landed.fishRotation),Is.LessThan(.01f));
                Assert.That(Vector3.Distance(Pose(FishingPhase.Casting,0).lurePosition,Pose(FishingPhase.Ready,0).lurePosition),Is.LessThan(.00001f));
                Assert.That(Vector3.Distance(wait.fishPosition,approach.fishPosition),Is.GreaterThan(4));
            }
        }
        [Test] public void ReducedEncounterKeepsOneStaticFishPoseWithoutCastArc()
        {
            var home=new Vector3(-1.3f,.3f,1.2f);Quaternion? rotation=null;
            foreach(FishingPhase phase in Enum.GetValues(typeof(FishingPhase)))foreach(float progress in new[]{0f,.25f,.5f,1f})
            {
                var pose=FishingEncounter.Evaluate(phase,progress,.6f,Vector3.left,Vector3.up*2,-1.3f,progress*100,true);
                Assert.That(pose.fishPosition,Is.EqualTo(home));
                if(rotation.HasValue)Assert.That(Quaternion.Angle(rotation.Value,pose.fishRotation),Is.LessThan(.001f));rotation=pose.fishRotation;
                Assert.That(Vector3.Distance(pose.lurePosition,new Vector3(-1.4f,.2f,2.16f)),Is.LessThan(.00001f));
            }
        }
        [Test] public void StandardOverloadEscapes() { var s=Fighting(true);Advance(s,200,true);Assert.AreEqual(FishingPhase.Escaped,s.Phase); }
        [Test] public void AssistedModeRescuesHeldReelAndLands() { var s=Fighting();Advance(s,1000,true);Assert.AreEqual(FishingPhase.Landed,s.Phase);Assert.Greater(s.Rescues,0); }
        [Test] public void ReleaseInputReducesTension() { var s=Fighting(true);Advance(s,20,true);float prior=s.Tension;Advance(s,10);Assert.Less(s.Tension,prior); }
        [Test] public void InvalidTimeDoesNotCorruptSimulation() { var s=Fighting();s.Tick(float.NaN,true);s.Tick(float.PositiveInfinity,true);s.Tick(-1,true);Assert.AreEqual(0,s.Tension); }
        [Test] public void WrongHabitatFishRejected() { var s=new FishingSession();Assert.Throws<ArgumentException>(()=>s.Cast(catalog.Fish("bigeye"),new ProgressData(),catalog,new System.Random())); }
        [Test] public void FirstDiscoveryAndRepeatScoreDifferByOneHundred()
        {
            var service=Service();Assert.AreEqual(210,service.Settle(Record()));Assert.AreEqual(110,service.Settle(Record()));
            Assert.AreEqual(320,Service().Data.totalPoints);
        }
        [Test] public void SameCatchCannotAwardAgainAfterRestart()
        {
            var r=Record();Service().Settle(r);var restarted=Service();Assert.AreEqual(0,restarted.Settle(r));Assert.AreEqual(210,restarted.Data.totalPoints);Assert.AreEqual(1,restarted.Data.catches.Count);
        }
        [Test] public void TwentyCyclesAcrossUnlocksPersistWithoutDeadEnd()
        {
            float maxSeconds=0;
            for(int i=0;i<20;i++)
            {
                var service=Service();string habitat=service.Data.totalPoints>=1800?"slope":service.Data.totalPoints>=900?"channel":"bay";service.SelectHabitat(habitat);
                var f=catalog.fish.Where(x=>x.habitatId==habitat).ToArray()[i%4];
                var s=new FishingSession();s.Cast(f,service.Data,catalog,new System.Random(i));
                for(int frame=0;frame<3600&&s.Phase!=FishingPhase.Landed;frame++)
                {
                    if(s.Phase==FishingPhase.Bite)s.Strike();s.Tick(1f/30,s.Tension<.65f);
                    Assert.AreNotEqual(FishingPhase.Escaped,s.Phase);
                }
                Assert.AreEqual(FishingPhase.Landed,s.Phase);maxSeconds=Math.Max(maxSeconds,s.Elapsed);
                Assert.Greater(service.Settle(s.Catch),0);s.FinishRelease();Assert.AreEqual(FishingPhase.Ready,s.Phase);
                Assert.AreEqual(0,s.Tension);Assert.AreEqual(0,s.Progress);Assert.IsNull(s.Catch);
                Assert.AreEqual(i+1,Service().Data.catches.Count);
            }
            Assert.Less(maxSeconds,180);Assert.AreEqual(20,Service().Data.catches.Count);Assert.GreaterOrEqual(Service().Data.totalPoints,1800);
        }
        [Test] public void TamperedPrimaryRecoversPreviousCompleteBackup()
        {
            var service=Service();service.Settle(Record());service.Settle(Record());File.WriteAllText(Store().PathName,"broken");
            var loaded=Store().Load();Assert.IsTrue(loaded.recoveredBackup);Assert.AreEqual(210,loaded.data.totalPoints);Assert.AreEqual("broken",File.ReadAllText(Store().PathName));
        }
        [Test] public void RecoveryCommitDoesNotOverwriteGoodBackupWithCorruption()
        {
            var service=Service();service.Settle(Record());service.Settle(Record());File.WriteAllText(Store().PathName,"broken");
            Service().Settle(Record());Assert.AreEqual(320,Service().Data.totalPoints);Assert.IsTrue(Directory.GetFiles(directory,"*.preserved-*").Length>0);
            File.WriteAllText(Store().PathName,"broken again");Assert.AreEqual(210,Store().Load().data.totalPoints);
        }
        [Test] public void BothInvalidCopiesRequireExplicitRecoveryAndRemainIntact()
        {
            File.WriteAllText(Store().PathName,"broken");File.WriteAllText(Store().PathName+".bak","broken backup");
            var service=Service();Assert.IsTrue(service.RequiresRecovery);Assert.Throws<InvalidOperationException>(()=>service.Settle(Record()));Assert.AreEqual("broken",File.ReadAllText(Store().PathName));
            service.Reset();Assert.IsFalse(service.RequiresRecovery);Assert.AreEqual(0,Service().Data.totalPoints);Assert.GreaterOrEqual(Directory.GetFiles(directory,"*.preserved-*").Length,2);
        }
        [Test] public void NewerVersionIsNeverSilentlyDowngraded()
        {
            Service().Settle(Record());File.WriteAllText(Store().PathName,"{\"version\":2}");Assert.IsTrue(Service().RequiresRecovery);
            Assert.Throws<InvalidOperationException>(()=>Store().Write(new ProgressData()));Assert.AreEqual("{\"version\":2}",File.ReadAllText(Store().PathName));
        }
        [Test] public void FailedDiskCommitDoesNotChangeInMemoryPoints()
        {
            var service=Service();Directory.CreateDirectory(Store().PathName+".tmp");Assert.Throws<UnauthorizedAccessException>(()=>service.Settle(Record()));Assert.AreEqual(0,service.Data.totalPoints);Assert.AreEqual(0,service.Data.catches.Count);
        }
        [Test] public void NaNAndInvalidDimensionsCannotBecomeRewards()
        {
            var r=Record();r.weight=float.NaN;Assert.Throws<InvalidOperationException>(()=>Service().Settle(r));r=Record();r.length=-1;Assert.Throws<InvalidOperationException>(()=>Service().Settle(r));
        }
        [Test] public void LockedGearAndHabitatCannotBeSelected()
        {
            Assert.Throws<InvalidOperationException>(()=>Service().SelectGear("rod-6"));Assert.Throws<InvalidOperationException>(()=>Service().SelectHabitat("slope"));
        }
        [Test] public void SettingsLoadoutAndRecordsSurviveRestart()
        {
            var s=Service();s.Settle(Record());s.Settle(Record());s.SelectGear("rod-2");s.Configure(x=>{x.leftHanded=true;x.reducedMotion=true;x.assisted=false;x.music=true;});
            var p=Service().Data;Assert.IsTrue(p.settings.leftHanded);Assert.IsTrue(p.settings.reducedMotion);Assert.IsFalse(p.settings.assisted);Assert.IsTrue(p.settings.music);Assert.AreEqual("rod-2",p.rodId);Assert.AreEqual(2,p.catches.Count);
        }
        [Test] public void MusicDefaultsOffForNewAndLegacySettings()
        {
            Assert.That(new GameSettings().music,Is.False);
            var legacy=JsonUtility.FromJson<GameSettings>("{\"assisted\":true,\"sound\":true,\"haptics\":true,\"leftHanded\":true}");
            Assert.That(legacy.music,Is.False);Assert.That(legacy.leftHanded&&legacy.sound&&legacy.haptics,Is.True);
        }
        [Test] public void FailedTaskIsNotMarkedCompleteByUnrelatedCatch()
        {
            var service=Service();service.Settle(Record());Assert.IsTrue(TaskProgress.Complete(catalog.tasks.First(t=>t.id=="task-01"),service.Data));
            Assert.IsFalse(TaskProgress.Complete(catalog.tasks.First(t=>t.kind=="habitat"&&t.targetId=="slope"),service.Data));Assert.AreEqual(1,TaskProgress.Value(catalog.tasks.First(t=>t.kind=="species"),service.Data));
        }
    }
}
