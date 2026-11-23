using NUnit.Framework;
namespace OceanAngler.Tests
{
    public sealed class FishingSessionTests
    {
        static void Advance(FishingSession s, int count, bool reel = false) { for (int i = 0; i < count; i++) s.Tick(0.05f, reel); }
        static FishingSession Fighting() { var s = new FishingSession(); s.Cast(); Advance(s, 61); s.Strike(); return s; }
        [Test] public void MissedBiteCanRetry() { var s = new FishingSession(); s.Cast(); Advance(s, 150); Assert.AreEqual(FishingPhase.Escaped, s.Phase); s.Retry(); Assert.AreEqual(FishingPhase.Ready, s.Phase); }
        [Test] public void PauseFreezesAndBlocksActions() { var s = Fighting(); s.Paused = true; Advance(s, 100, true); Assert.AreEqual(0, s.Progress); Assert.AreEqual(0, s.Release()); }
        [Test] public void OverloadEscapes() { var s = Fighting(); Advance(s, 150, true); Assert.AreEqual(FishingPhase.Escaped, s.Phase); Assert.AreEqual(0, s.Points); }
        [Test] public void ControlledReelingSettlesOnce() { var s = Fighting(); for (int i = 0; i < 3000 && s.Phase == FishingPhase.Fighting; i++) s.Tick(0.05f, s.Tension < 0.65f); Assert.AreEqual(FishingPhase.Landed, s.Phase); Assert.AreEqual(120, s.Release()); Assert.AreEqual(0, s.Release()); Assert.AreEqual(120, s.Points); }
        [Test] public void InvalidTimeDoesNotCorrupt() { var s = Fighting(); s.Tick(float.NaN, true); s.Tick(float.PositiveInfinity, true); Assert.AreEqual(0, s.Tension); }
    }
}
