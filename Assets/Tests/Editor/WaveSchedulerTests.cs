using BossFight;
using BossFight.Rules;
using NUnit.Framework;

// Round 6 waves: telegraph, release, then the next telegraph only after most of the wave is dead plus a delay.
public class WaveSchedulerTests
{
    private BossFightSettings settings;

    [SetUp]
    public void SetUp()
    {
        settings = new BossFightSettings();
    }

    private static WaveScheduler.Wave? Tick(WaveScheduler s, float dt, int lastWaveAlive, int alive = 0, int cap = 22, int racks = 5)
    {
        return s.Tick(dt, lastWaveAlive, alive, cap, racks);
    }

    [Test]
    public void FirstRackShakesAfterTheFirstDelayThenReleases()
    {
        var s = new WaveScheduler(settings, 100);
        Assert.IsNull(Tick(s, settings.waveFirstDelaySeconds - 0.1f, 0));
        var telegraph = Tick(s, 0.2f, 0);
        Assert.AreEqual(WaveScheduler.WaveEvent.Telegraph, telegraph.Value.Event);
        Assert.IsTrue(telegraph.Value.FreshRack);
        Assert.IsNull(Tick(s, settings.rackTelegraphSeconds - 0.1f, 0), "shakes for the whole telegraph");
        var release = Tick(s, 0.2f, 0);
        Assert.AreEqual(WaveScheduler.WaveEvent.Release, release.Value.Event);
        Assert.AreEqual(settings.waveBaseCostumes, release.Value.Count);
    }

    [Test]
    public void NextRackWaitsForThreeQuartersOfTheWaveThenTheDelay()
    {
        var s = new WaveScheduler(settings, 100);
        Tick(s, settings.waveFirstDelaySeconds + 0.1f, 0);
        int size = Tick(s, settings.rackTelegraphSeconds + 0.1f, 0).Value.Count; // 6
        Assert.IsNull(Tick(s, 1f, size), "nobody died: no countdown (well under the stall time)");
        Assert.IsNull(Tick(s, 1f, 3), "one dead is not enough");
        Assert.AreEqual(WaveScheduler.Stage.WaitingForClear, s.Current);
        Assert.IsNull(Tick(s, 0f, 1), "three quarters dead starts the countdown");
        Assert.AreEqual(WaveScheduler.Stage.Countdown, s.Current);
        Assert.IsNull(Tick(s, settings.waveNextDelaySeconds - 0.1f, 1));
        Assert.AreEqual(WaveScheduler.WaveEvent.Telegraph, Tick(s, 0.2f, 1).Value.Event);
    }

    [TestCase(6, 6, false)]
    [TestCase(6, 1, true)]
    [TestCase(6, 2, false)]
    [TestCase(12, 3, true)]
    [TestCase(12, 4, false)]
    [TestCase(0, 0, true)]
    public void ClearedMeansThreeQuartersDead(int size, int alive, bool cleared)
    {
        Assert.AreEqual(cleared, WaveScheduler.Cleared(size, alive, 0.75f));
    }

    [TestCase(100, 4)]
    [TestCase(50, 6)]
    [TestCase(0, 8)]
    [TestCase(75, 5)]
    public void WorseReputationMeansABiggerSwarm(int reputation, int size)
    {
        Assert.AreEqual(size, WaveScheduler.SwarmSize(settings, reputation));
    }

    [TestCase(0, 2f)]
    [TestCase(15, 2f)]
    [TestCase(16, 1f)]
    [TestCase(100, 1f)]
    public void OnlyRockBottomReputationDoublesTheSwarmsHp(int reputation, float multiplier)
    {
        Assert.AreEqual(multiplier, WaveScheduler.ClothingHpMultiplier(settings, reputation));
    }

    [Test]
    public void ReleaseWaitsForRoomUnderTheCapAndNeverExceedsIt()
    {
        var s = new WaveScheduler(settings, 0); // 12 planned
        Tick(s, settings.waveFirstDelaySeconds + 0.1f, 0, 22, 22);
        Assert.IsNull(Tick(s, settings.rackTelegraphSeconds + 0.1f, 0, 22, 22), "full: waits");
        var release = Tick(s, 0.1f, 0, 17, 22);
        Assert.AreEqual(5, release.Value.Count);
    }

    [Test]
    public void StopsWhenTold()
    {
        var s = new WaveScheduler(settings, 100);
        s.Stop();
        Assert.IsNull(Tick(s, 100f, 0));
    }
}
