using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace OceanAngler
{
    [Serializable] public sealed class CatchRecord
    {
        public string catchId, fishId, habitatId, mode;
        public float weight, length;
        public int skillPoints, awardedPoints;
        public bool released;
    }
    [Serializable] public sealed class GameSettings
    {
        public bool assisted = true, leftHanded, reducedMotion, sound = true, haptics = true, music;
        public bool underwaterOnly;
    }
    [Serializable] public sealed class ProgressData
    {
        public int schemaVersion = 1, totalPoints;
        public bool tutorialComplete;
        public string habitatId = "bay", rodId = "rod-1", reelId = "reel-1", lineId = "line-1", lureId = "lure-1";
        public List<CatchRecord> catches = new List<CatchRecord>();
        public GameSettings settings = new GameSettings();
        public List<string> observations = new List<string>();
        public ProgressData Clone() => JsonUtility.FromJson<ProgressData>(JsonUtility.ToJson(this));
        public void Validate(ContentCatalog catalog)
        {
            if (schemaVersion != 1 && schemaVersion != 2) throw new InvalidOperationException("Unsupported save version");
            if (settings == null || catches == null || observations == null || totalPoints < 0) throw new InvalidOperationException("Invalid save");
            if(schemaVersion==1&&observations.Count!=0)throw new InvalidOperationException("Observation records require save version 2");
            if(observations.Distinct().Count()!=observations.Count)throw new InvalidOperationException("Duplicate observation");
            foreach(var id in observations)
                if(catalog.Habitat(catalog.Observation(id).habitatId).unlockPoints>totalPoints)throw new InvalidOperationException("Locked observation");
            long expected = 0; var ids = new HashSet<string>();
            foreach (var c in catches)
            {
                ValidateCatch(c, catalog);
                if (!ids.Add(c.catchId) || !c.released || c.awardedPoints < 1) throw new InvalidOperationException("Duplicate/unsettled catch");
                expected += c.awardedPoints;
            }
            if (expected != totalPoints) throw new InvalidOperationException("Point ledger mismatch");
            if (catalog.Habitat(habitatId).unlockPoints > totalPoints) throw new InvalidOperationException("Locked habitat in save");
            foreach (var pair in new[] { (rodId,"rod"), (reelId,"reel"), (lineId,"line"), (lureId,"lure") })
            {
                var g = catalog.Gear(pair.Item1);
                if (g.category != pair.Item2 || g.unlockPoints > totalPoints) throw new InvalidOperationException("Invalid loadout");
            }
        }
        public static void ValidateCatch(CatchRecord c, ContentCatalog catalog)
        {
            if (c == null || !Guid.TryParse(c.catchId, out _) || (c.mode != "assisted" && c.mode != "standard")) throw new InvalidOperationException("Invalid catch identity");
            var f = catalog.Fish(c.fishId);
            if (f.habitatId != c.habitatId || !Finite(c.weight) || !Finite(c.length) || c.weight < f.minWeight || c.weight > f.maxWeight || c.length < f.minLength || c.length > f.maxLength || c.skillPoints < 0 || c.skillPoints > 30)
                throw new InvalidOperationException("Invalid catch values");
        }
        static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
    }
    public static class TaskProgress
    {
        public static bool Gentle(CatchRecord record)=>record.skillPoints>=25;
        public static string GoalText(TaskDefinition task,ContentCatalog catalog)
        {
            switch(task.kind)
            {
                case "catches":return "在任意海域记录并放流 "+task.target+" 条鱼。";
                case "species":return "发现并放流 "+task.target+" 种鱼，同种鱼不重复计数。";
                case "habitat":return "在"+catalog.Habitat(task.targetId).name+"记录并放流 "+task.target+" 条鱼。";
                case "safe":return "警戒时松手降张力，记录 "+task.target+" 次轻柔放流。";
                case "standard":return "关闭初学辅助，以标准模式放流 "+task.target+" 条鱼。";
                case "points":return "累计获得 "+task.target+" 研究积分，不会扣除。";
                default:throw new ArgumentException("Unknown task kind");
            }
        }
        public static int Value(TaskDefinition t, ProgressData p)
        {
            switch (t.kind)
            {
                case "catches": return p.catches.Count;
                case "species": return p.catches.Select(c => c.fishId).Distinct().Count();
                case "habitat": return p.catches.Count(c => c.habitatId == t.targetId);
                case "safe": return p.catches.Count(Gentle);
                case "standard": return p.catches.Count(c => c.mode == "standard");
                case "points": return p.totalPoints;
                default: throw new ArgumentException("Unknown task kind");
            }
        }
        public static bool Complete(TaskDefinition t, ProgressData p) => Value(t, p) >= t.target;
    }
}
