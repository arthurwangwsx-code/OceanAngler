using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace OceanAngler
{
    [Serializable] public sealed class FishDefinition
    {
        public string id, name, habitatId, shape, note, scientificName;
        public float minWeight, maxWeight, minLength, maxLength, strength;
        public int basePoints;
    }
    [Serializable] public sealed class HabitatDefinition
    {
        public string id, name, description;
        public int unlockPoints;
    }
    [Serializable] public sealed class GearDefinition
    {
        public string id, name, category, description;
        public int unlockPoints;
        public float power;
    }
    [Serializable] public sealed class TaskDefinition
    {
        public string id, name, kind, targetId;
        public int target;
    }
    [Serializable] public sealed class ObservationDefinition
    {
        public string id, name, scientificName, habitatId, note;
    }
    [Serializable] public sealed class ContentCatalog
    {
        public FishDefinition[] fish;
        public HabitatDefinition[] habitats;
        public GearDefinition[] gear;
        public TaskDefinition[] tasks;
        public ObservationDefinition[] observations;
        public static ContentCatalog Load()
        {
            var asset = Resources.Load<TextAsset>("ContentCatalog");
            if (asset == null) throw new InvalidOperationException("Missing ContentCatalog");
            var result = JsonUtility.FromJson<ContentCatalog>(asset.text);
            result.Validate(); return result;
        }
        public FishDefinition Fish(string id) => fish.FirstOrDefault(x => x.id == id) ?? throw new ArgumentException("Unknown fish " + id);
        public HabitatDefinition Habitat(string id) => habitats.FirstOrDefault(x => x.id == id) ?? throw new ArgumentException("Unknown habitat " + id);
        public GearDefinition Gear(string id) => gear.FirstOrDefault(x => x.id == id) ?? throw new ArgumentException("Unknown gear " + id);
        public void Validate()
        {
            if (fish == null || habitats == null || gear == null || tasks == null || observations == null) throw new InvalidOperationException("Incomplete catalog");
            var ids = new HashSet<string>();
            foreach (var id in fish.Select(x => x.id).Concat(habitats.Select(x => x.id)).Concat(gear.Select(x => x.id)).Concat(tasks.Select(x => x.id)).Concat(observations.Select(x=>x.id)))
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw new InvalidOperationException("Duplicate/missing content ID");
            foreach (var f in fish)
                if (string.IsNullOrWhiteSpace(f.scientificName) || !habitats.Any(h => h.id == f.habitatId) || f.minWeight <= 0 || f.maxWeight < f.minWeight || f.minLength <= 0 || f.maxLength < f.minLength || f.basePoints <= 0 || f.strength <= 0)
                    throw new InvalidOperationException("Invalid fish " + f.id);
            var species=new HashSet<string>(fish.Select(f=>f.scientificName),StringComparer.OrdinalIgnoreCase);
            if(species.Count!=fish.Length)throw new InvalidOperationException("Duplicate catchable species");
            foreach(var o in observations)
                if(string.IsNullOrWhiteSpace(o.name)||string.IsNullOrWhiteSpace(o.scientificName)||!habitats.Any(h=>h.id==o.habitatId)||!species.Add(o.scientificName))
                    throw new InvalidOperationException("Invalid observation-only species "+o.id);
            foreach (var category in new[] { "rod", "reel", "line", "lure" })
                if (!gear.Any(g => g.category == category && g.unlockPoints == 0)) throw new InvalidOperationException("Missing starter gear");
            foreach (var g in gear)
                if (g.unlockPoints < 0 || g.power <= 0 || !new[] { "rod", "reel", "line", "lure" }.Contains(g.category)) throw new InvalidOperationException("Invalid gear");
            foreach (var h in habitats) if (h.unlockPoints < 0) throw new InvalidOperationException("Invalid habitat");
            foreach (var t in tasks)
                if (t.target < 1 || !new[] { "catches", "species", "habitat", "safe", "standard", "points" }.Contains(t.kind) || (t.kind == "habitat" && !habitats.Any(h => h.id == t.targetId)))
                    throw new InvalidOperationException("Invalid task " + t.id);
        }
    }
}
