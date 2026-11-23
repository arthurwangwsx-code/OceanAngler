using System;
namespace OceanAngler
{
    public enum FishingPhase { Ready, Waiting, Bite, Fighting, Landed, Escaped }
    // 纯规则对象：模拟不依赖渲染、真实时间或场景对象。
    public sealed class FishingSession
    {
        public FishingPhase Phase { get; private set; }
        public float Tension { get; private set; }
        public float Progress { get; private set; }
        public int Points { get; private set; }
        public bool Paused { get; set; }
        private float elapsed, overload;
        private bool settled;
        public void Cast()
        {
            if (Paused || Phase != FishingPhase.Ready) return;
            elapsed = overload = Tension = Progress = 0; settled = false;
            Phase = FishingPhase.Waiting;
        }
        public void Strike()
        {
            if (!Paused && Phase == FishingPhase.Bite) { Phase = FishingPhase.Fighting; elapsed = 0; }
        }
        public void Tick(float dt, bool reeling)
        {
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt) || Paused) return;
            dt = Math.Min(dt, 0.1f); elapsed += dt;
            if (Phase == FishingPhase.Waiting && elapsed >= 3) { Phase = FishingPhase.Bite; elapsed = 0; }
            else if (Phase == FishingPhase.Bite && elapsed >= 4) Phase = FishingPhase.Escaped;
            else if (Phase == FishingPhase.Fighting)
            {
                float force = 0.4f + 0.15f * (float)Math.Sin(elapsed * 2);
                Tension = Math.Max(0, Math.Min(1, Tension + dt * (reeling ? force : -0.65f)));
                if (reeling && Tension < 0.85f) Progress = Math.Min(1, Progress + dt * 0.12f);
                overload = Tension > 0.9f ? overload + dt : Math.Max(0, overload - dt);
                if (overload > 1.2f) Phase = FishingPhase.Escaped;
                else if (Progress >= 1) Phase = FishingPhase.Landed;
            }
        }
        public int Release()
        {
            if (Paused || Phase != FishingPhase.Landed || settled) return 0;
            settled = true; Points += 120; Phase = FishingPhase.Ready; return 120;
        }
        public void Retry() { if (!Paused && Phase == FishingPhase.Escaped) Phase = FishingPhase.Ready; }
    }
}
