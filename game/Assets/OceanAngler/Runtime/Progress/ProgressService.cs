using System;
using System.Linq;
namespace OceanAngler
{
    public sealed class ProgressService
    {
        public ProgressData Data { get; private set; }
        public bool RequiresRecovery { get; private set; }
        public string LoadMessage { get; }
        readonly SaveStore store;
        readonly ContentCatalog catalog;
        public ProgressService(ContentCatalog content, SaveStore persistence)
        {
            catalog = content; store = persistence;
            var loaded = store.Load(); Data = loaded.data; RequiresRecovery = loaded.requiresRecovery; LoadMessage = loaded.message;
        }
        public int Settle(CatchRecord record)
        {
            EnsureReady(); ProgressData.ValidateCatch(record, catalog);
            if (Data.catches.Any(c => c.catchId == record.catchId)) return 0;
            if (catalog.Habitat(record.habitatId).unlockPoints > Data.totalPoints) throw new InvalidOperationException("Habitat locked");
            var fish = catalog.Fish(record.fishId);
            int bonus = Data.catches.Any(c => c.fishId == record.fishId) ? 0 : 100;
            double ratio = record.weight / ((fish.minWeight + fish.maxWeight) * 0.5f);
            int points = (int)Math.Round(fish.basePoints * Math.Max(0.5, Math.Min(1.8, ratio)), MidpointRounding.AwayFromZero) + bonus + record.skillPoints + 20;
            var next = Data.Clone();
            var settled = new CatchRecord { catchId = record.catchId, fishId = record.fishId, habitatId = record.habitatId, weight = record.weight, length = record.length, mode = record.mode, skillPoints = record.skillPoints, awardedPoints = points, released = true };
            next.catches.Add(settled); next.totalPoints = checked(next.totalPoints + points); next.tutorialComplete = true;
            Commit(next); return points;
        }
        public bool Observe(string id)
        {
            EnsureReady();var species=catalog.Observation(id);
            if(species.habitatId!=Data.habitatId||catalog.Habitat(species.habitatId).unlockPoints>Data.totalPoints)throw new InvalidOperationException("Observation habitat unavailable");
            if(Data.observations.Contains(id))return false;
            var next=Data.Clone();next.schemaVersion=2;next.observations.Add(id);Commit(next);return true;
        }
        public void SelectHabitat(string id)
        {
            EnsureReady(); if (catalog.Habitat(id).unlockPoints > Data.totalPoints) throw new InvalidOperationException("Habitat locked");
            var next = Data.Clone(); next.habitatId = id; Commit(next);
        }
        public void SelectGear(string id)
        {
            EnsureReady(); var g = catalog.Gear(id);
            if (g.unlockPoints > Data.totalPoints) throw new InvalidOperationException("Gear locked");
            var next = Data.Clone();
            switch (g.category) { case "rod": next.rodId=id; break; case "reel": next.reelId=id; break; case "line": next.lineId=id; break; case "lure": next.lureId=id; break; }
            Commit(next);
        }
        public void Configure(Action<GameSettings> configure) { EnsureReady(); var next=Data.Clone(); configure(next.settings); Commit(next); }
        public void Reset() { store.ResetPreservingOriginals(); Data = new ProgressData(); RequiresRecovery = false; }
        void EnsureReady() { if (RequiresRecovery) throw new InvalidOperationException("Explicit recovery required"); }
        void Commit(ProgressData next) { store.Write(next); Data = next; }
    }
}
