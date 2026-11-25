using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace OceanAngler.Tests
{
    public sealed class TaskJourneyTests
    {
        [Test] public void StarterEquipmentCanCompleteAllEighteenTasksAndUnlockAllGearAfterReloads()
        {
            var catalog=ContentCatalog.Load();string directory=Path.Combine(Path.GetTempPath(),"OceanTaskJourney-"+Guid.NewGuid());
            try
            {
                var store=new SaveStore(Path.Combine(directory,"progress.json"),catalog);
                for(int cycle=0;cycle<20;cycle++)
                {
                    var service=new ProgressService(catalog,store);
                    var unlocked=catalog.fish.Where(f=>catalog.Habitat(f.habitatId).unlockPoints<=service.Data.totalPoints).ToArray();
                    var fish=unlocked.FirstOrDefault(f=>!service.Data.catches.Any(c=>c.fishId==f.id))??unlocked[0];
                    service.SelectHabitat(fish.habitatId);service.Configure(s=>s.assisted=false);
                    var session=new FishingSession();session.Cast(fish,service.Data,catalog,new Random(cycle));
                    for(int frame=0;frame<5400&&session.Phase!=FishingPhase.Landed;frame++)
                    {
                        if(session.Phase==FishingPhase.Bite)session.Strike();
                        session.Tick(1f/30,session.Tension<.65f);Assert.That(session.Phase,Is.Not.EqualTo(FishingPhase.Escaped));
                    }
                    Assert.That(session.Phase,Is.EqualTo(FishingPhase.Landed));Assert.That(session.Elapsed,Is.LessThan(180));
                    var record=session.Catch;int points=service.Settle(record);Assert.That(points,Is.GreaterThan(0));session.FinishRelease();
                    var restarted=new ProgressService(catalog,store);Assert.That(restarted.Data.catches.Count,Is.EqualTo(cycle+1));
                    int before=restarted.Data.totalPoints;Assert.That(restarted.Settle(record),Is.Zero);Assert.That(restarted.Data.totalPoints,Is.EqualTo(before));
                }
                var completed=new ProgressService(catalog,store);Assert.That(completed.Data.catches.Select(c=>c.fishId).Distinct().Count(),Is.EqualTo(12));
                Assert.That(catalog.tasks.Where(t=>!TaskProgress.Complete(t,completed.Data)).Select(t=>t.id),Is.Empty);
                int total=completed.Data.totalPoints;
                foreach(var gear in catalog.gear)
                {
                    completed.SelectGear(gear.id);completed=new ProgressService(catalog,store);Assert.That(completed.Data.totalPoints,Is.EqualTo(total));
                    Assert.That(new[]{completed.Data.rodId,completed.Data.reelId,completed.Data.lineId,completed.Data.lureId},Does.Contain(gear.id));
                }
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
    }
}
