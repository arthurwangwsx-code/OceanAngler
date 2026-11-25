using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace OceanAngler.Tests
{
    public sealed class EncounterBalanceTests
    {
        [Serializable] sealed class Distribution
        {public float minimum,median,p95,maximum;public Distribution(float[] data){var sorted=data.OrderBy(x=>x).ToArray();minimum=sorted[0];median=sorted[(int)Math.Ceiling(sorted.Length*.5)-1];p95=sorted[(int)Math.Ceiling(sorted.Length*.95)-1];maximum=sorted[sorted.Length-1];}}
        [Serializable] sealed class Balance
        {
            public int journeys=1000,completed,unfinished,twentyCatchFullCollections,twentyCatchAllTasks;
            public Distribution firstCatchSeconds,channelUnlockCatch,slopeUnlockCatch,fullCollectionCatch,allTasksCatch,activeSeconds;
            public string policy="Stay in each habitat until all four species recorded, then move to next unlocked habitat; standard after tutorial, starter equipment, automatic strike and tension below 0.65; no real UI/travel/pause time";
        }
        [Test] public void SelectionPreservesTutorialAndUniformHabitatPoolWithoutMutatingProgress()
        {
            var catalog=ContentCatalog.Load();var data=new ProgressData();var random=new System.Random(71);var control=new System.Random(71);
            for(int i=0;i<20;i++)Assert.That(EncounterSelection.Choose(catalog,data,random).id,Is.EqualTo("trevally"));
            Assert.That(random.NextDouble(),Is.EqualTo(control.NextDouble()),"Tutorial must not consume selection RNG");
            data.tutorialComplete=true;
            foreach(var habitat in catalog.habitats)
            {
                data.habitatId=habitat.id;var counts=catalog.fish.Where(f=>f.habitatId==habitat.id).ToDictionary(f=>f.id,_=>0);
                for(int i=0;i<10000;i++)counts[EncounterSelection.Choose(catalog,data,random).id]++;
                Assert.That(counts.Count,Is.EqualTo(4));foreach(int count in counts.Values)Assert.That(count,Is.InRange(2200,2800));
            }
            Assert.That(data.catches,Is.Empty);Assert.That(data.totalPoints,Is.Zero);
            data.habitatId="missing";Assert.Throws<InvalidOperationException>(()=>EncounterSelection.Choose(catalog,data,random));
        }
        [Test] public void ThousandSeededJourneysMeasureActualSelectionAndSettlementAfterReloads()
        {
            var catalog=ContentCatalog.Load();var directory=Path.Combine(Path.GetTempPath(),"OceanBalance-"+Guid.NewGuid());Directory.CreateDirectory(directory);
            var first=new float[1000];var channel=new float[1000];var slope=new float[1000];var collection=new float[1000];var tasks=new float[1000];var seconds=new float[1000];var result=new Balance();
            try
            {
                for(int seed=0;seed<1000;seed++)
                {
                    var store=new SaveStore(Path.Combine(directory,seed+".json"),catalog);var random=new System.Random(seed);var service=new ProgressService(catalog,store);service.Configure(s=>s.assisted=false);
                    for(int count=1;count<=120;count++)
                    {
                        var desired=catalog.habitats.FirstOrDefault(h=>h.unlockPoints<=service.Data.totalPoints&&catalog.fish.Where(f=>f.habitatId==h.id).Any(f=>!service.Data.catches.Any(c=>c.fishId==f.id)))??catalog.habitats.Last(h=>h.unlockPoints<=service.Data.totalPoints);
                        if(service.Data.habitatId!=desired.id)service.SelectHabitat(desired.id);
                        var fish=EncounterSelection.Choose(catalog,service.Data,random);var session=new FishingSession();session.Cast(fish,service.Data,catalog,random,.6f);
                        for(int frame=0;frame<5400&&session.Phase!=FishingPhase.Landed;frame++)
                        {
                            if(session.Phase==FishingPhase.Bite)session.Strike();session.Tick(1f/30,session.Tension<.65f);
                            Assert.That(session.Phase,Is.Not.EqualTo(FishingPhase.Escaped));
                        }
                        Assert.That(session.Phase,Is.EqualTo(FishingPhase.Landed));Assert.That(session.Elapsed,Is.LessThan(180));
                        if(count==1){first[seed]=session.Elapsed;Assert.That(fish.id,Is.EqualTo("trevally"));Assert.That(session.Assisted,Is.True);}
                        service.Settle(session.Catch);session.FinishRelease();service=new ProgressService(catalog,store);
                        Assert.That(service.Data.catches.Count,Is.EqualTo(count));seconds[seed]+=session.Elapsed;
                        if(channel[seed]==0&&service.Data.totalPoints>=catalog.Habitat("channel").unlockPoints)channel[seed]=count;
                        if(slope[seed]==0&&service.Data.totalPoints>=catalog.Habitat("slope").unlockPoints)slope[seed]=count;
                        bool full=service.Data.catches.Select(c=>c.fishId).Distinct().Count()==12;
                        bool all=catalog.tasks.All(t=>TaskProgress.Complete(t,service.Data));
                        if(full&&collection[seed]==0)collection[seed]=count;
                        if(count==20){if(full)result.twentyCatchFullCollections++;if(all)result.twentyCatchAllTasks++;}
                        if(all){tasks[seed]=count;result.completed++;break;}
                    }
                    if(tasks[seed]==0)result.unfinished++;
                }
                result.firstCatchSeconds=new Distribution(first);result.channelUnlockCatch=new Distribution(channel);result.slopeUnlockCatch=new Distribution(slope);result.fullCollectionCatch=new Distribution(collection);result.allTasksCatch=new Distribution(tasks);result.activeSeconds=new Distribution(seconds);
                Debug.Log("ENCOUNTER_BALANCE "+JsonUtility.ToJson(result));
                Assert.That(result.unfinished,Is.Zero,"Seeded sample only; not a guarantee for unbounded random play");
            }
            finally{Directory.Delete(directory,true);}
        }
    }
}
