using System;
using System.Linq;
namespace OceanAngler
{
    public sealed class ProgressService
    {
        public ProgressData Data { get; private set; }
        public bool RequiresRecovery { get; private set; }
        public bool DeveloperMode => store.DeveloperSandbox;
        public bool DeveloperSelected { get; private set; }
        public bool RecoveredBackup { get; }
        public bool IsUnlocked(int points) => !RequiresRecovery&&(DeveloperMode||Data.totalPoints>=points);
        public string LoadMessage { get; }
        readonly SaveStore store;
        readonly ContentCatalog catalog;
        public ProgressService(ContentCatalog content, SaveStore persistence)
        {
            catalog = content; store = persistence;
            var loaded = store.Load(); Data = loaded.data; RequiresRecovery = loaded.requiresRecovery; LoadMessage = loaded.message;
            DeveloperSelected=loaded.developerSelected;RecoveredBackup=loaded.recoveredBackup;
        }
        public int Settle(CatchRecord record)
        {
            EnsureReady(); ProgressData.ValidateCatch(record, catalog);
            if (Data.catches.Any(c => c.catchId == record.catchId)) return 0;
            if (!IsUnlocked(catalog.Habitat(record.habitatId).unlockPoints)) throw new InvalidOperationException("Habitat locked");
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
            if(species.habitatId!=Data.habitatId||!IsUnlocked(catalog.Habitat(species.habitatId).unlockPoints))throw new InvalidOperationException("Observation habitat unavailable");
            if(Data.observations.Contains(id))return false;
            var next=Data.Clone();next.schemaVersion=2;next.observations.Add(id);Commit(next);return true;
        }
        public void SelectHabitat(string id)
        {
            EnsureReady(); if (!IsUnlocked(catalog.Habitat(id).unlockPoints)) throw new InvalidOperationException("Habitat locked");
            var next = Data.Clone(); next.habitatId = id; Commit(next);
        }
        public void SelectGear(string id)
        {
            EnsureReady(); var g = catalog.Gear(id);
            if (!IsUnlocked(g.unlockPoints)) throw new InvalidOperationException("Gear locked");
            var next = Data.Clone();
            switch (g.category) { case "rod": next.rodId=id; break; case "reel": next.reelId=id; break; case "line": next.lineId=id; break; case "lure": next.lureId=id; break; }
            Commit(next);
        }
        public void Configure(Action<GameSettings> configure) { EnsureReady(); var next=Data.Clone(); configure(next.settings); Commit(next); }
        public void SelectDeveloperProfile(bool selected,GameSettings initialSettings=null)
        {
            EnsureReady();if(!DeveloperMode)throw new InvalidOperationException("Only the test profile can select developer mode");
            var next=Data.Clone();
            if(initialSettings!=null)next.settings=new GameSettings{assisted=initialSettings.assisted,leftHanded=initialSettings.leftHanded,reducedMotion=initialSettings.reducedMotion,sound=initialSettings.sound,haptics=initialSettings.haptics,music=initialSettings.music,underwaterOnly=initialSettings.underwaterOnly};
            store.Write(next,selected);Data=next;DeveloperSelected=selected;
        }
        public void Reset(bool selectDeveloper = false)
        {
            if(selectDeveloper&&!DeveloperMode)throw new InvalidOperationException("Only the test profile can select developer mode");
            bool selected=DeveloperSelected||selectDeveloper;store.ResetPreservingOriginals(selected);Data=store.CreateData();RequiresRecovery=false;DeveloperSelected=selected;
        }
        void EnsureReady() { if (RequiresRecovery) throw new InvalidOperationException("Explicit recovery required"); }
        void Commit(ProgressData next) { store.Write(next,DeveloperSelected); Data = next; }
    }
}
