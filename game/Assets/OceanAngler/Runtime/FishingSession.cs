using System;
namespace OceanAngler
{
    public enum FishingPhase { Ready, Casting, Waiting, Bite, Fighting, Landed, Escaped }
    // 纯模拟对象；表现、持久化和积分不参与物理推进。
    public sealed class FishingSession
    {
        public FishingPhase Phase { get; private set; }
        public float Tension { get; private set; }
        public float Progress { get; private set; }
        public float Elapsed { get; private set; }
        public bool Paused { get; set; }
        public CatchRecord Catch { get; private set; }
        public bool Assisted { get; private set; }
        public float CastPower { get; private set; }
        // Presentation reads the same paused/clamped clock as the simulation.
        public float PhaseProgress => Math.Max(0,Math.Min(1,Phase==FishingPhase.Casting?phaseTime/.8f:Phase==FishingPhase.Waiting?phaseTime/biteDelay:Phase==FishingPhase.Fighting?Progress:Phase==FishingPhase.Landed?1:0));
        public int Rescues { get; private set; }
        float phaseTime, fightTime, overload, unsafeTime, retrieveSpeed = 1, resistance = 1, biteDelay = 3;
        FishDefinition fish;
        public void Cast(FishDefinition target, ProgressData data, ContentCatalog catalog, Random random, float power = 0.6f)
        {
            if (Paused || Phase != FishingPhase.Ready) return;
            target=catalog.Fish(target?.id);
            if (target.habitatId != data.habitatId) throw new ArgumentException("Fish outside selected habitat");
            fish = target; Assisted = data.settings.assisted || !data.tutorialComplete;
            CastPower = Math.Max(0.3f, Math.Min(1, power));
            retrieveSpeed = catalog.Gear(data.reelId).power;
            resistance = catalog.Gear(data.rodId).power * catalog.Gear(data.lineId).power;
            biteDelay = (Assisted ? 3f : 5f) / catalog.Gear(data.lureId).power;
            float sample = (float)random.NextDouble();
            Catch = new CatchRecord { catchId = Guid.NewGuid().ToString(), fishId = target.id, habitatId = data.habitatId, mode = Assisted ? "assisted" : "standard", weight = target.minWeight + (target.maxWeight-target.minWeight)*sample, length = target.minLength + (target.maxLength-target.minLength)*sample };
            phaseTime = fightTime = overload = unsafeTime = Tension = Progress = Elapsed = 0; Rescues = 0;
            Phase = FishingPhase.Casting;
        }
        public void Strike()
        {
            if (!Paused && Phase == FishingPhase.Bite) { Phase = FishingPhase.Fighting; phaseTime = 0; }
        }
        public void Tick(float dt, bool reeling)
        {
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt) || Paused || Phase == FishingPhase.Ready || Phase == FishingPhase.Landed || Phase == FishingPhase.Escaped) return;
            dt = Math.Min(dt, 0.1f); phaseTime += dt; Elapsed += dt;
            switch (Phase)
            {
                case FishingPhase.Casting:
                    if (phaseTime >= 0.8f) Enter(FishingPhase.Waiting);
                    break;
                case FishingPhase.Waiting:
                    if (phaseTime >= biteDelay) Enter(FishingPhase.Bite);
                    break;
                case FishingPhase.Bite:
                    if (phaseTime >= (Assisted ? 7 : 3))
                    {
                        if (Assisted) { Rescues++; Strike(); }
                        else Enter(FishingPhase.Escaped);
                    }
                    break;
                case FishingPhase.Fighting:
                    fightTime += dt;
                    float force = (0.36f + 0.13f * (float)Math.Sin(fightTime * 2)) * fish.strength / resistance;
                    if (Assisted && Tension > 0.8f && reeling) { Tension = Math.Max(0, Tension - dt * 0.5f); Rescues++; }
                    else Tension = Math.Max(0, Math.Min(1, Tension + dt * (reeling ? force : -0.65f)));
                    if (reeling && Tension < 0.85f) Progress = Math.Min(1, Progress + dt * 0.09f * retrieveSpeed / (float)Math.Sqrt(fish.strength));
                    if (Tension > 0.85f) unsafeTime += dt;
                    overload = Tension > 0.92f ? overload + dt : Math.Max(0, overload - dt);
                    if (!Assisted && overload > 1.2f) Enter(FishingPhase.Escaped);
                    else if (Progress >= 1) { Catch.skillPoints = Math.Max(0, 30 - (int)(unsafeTime * 5)); Enter(FishingPhase.Landed); }
                    break;
            }
        }
        void Enter(FishingPhase next) { Phase = next; phaseTime = 0; }
        public void FinishRelease() { if (!Paused && Phase == FishingPhase.Landed) Cancel(); }
        public void Retry() { if (!Paused && Phase == FishingPhase.Escaped) { Catch = null; Enter(FishingPhase.Ready); } }
        public void Cancel() { Catch = null; Tension = Progress = 0; Enter(FishingPhase.Ready); }
    }
}
