using System;
using System.Linq;
namespace OceanAngler
{
    // One selection path for the actual controller and balance simulation.
    // Current policy is uniform within a habitat; rarity/bait preferences remain planned.
    public static class EncounterSelection
    {
        public static FishDefinition Choose(ContentCatalog catalog,ProgressData progress,Random random)
        {
            if(catalog==null)throw new ArgumentNullException(nameof(catalog));
            if(progress==null)throw new ArgumentNullException(nameof(progress));
            if(random==null)throw new ArgumentNullException(nameof(random));
            var pool=catalog.fish.Where(f=>f.habitatId==progress.habitatId).ToArray();
            if(pool.Length==0)throw new InvalidOperationException("Habitat has no catchable fish");
            return pool[progress.tutorialComplete?random.Next(pool.Length):0];
        }
    }
}
