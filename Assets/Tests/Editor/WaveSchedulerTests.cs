using BossFight;
using BossFight.Rules;
using NUnit.Framework;

public class WaveSchedulerTests
{
    private BossFightSettings settings;

    [SetUp]
    public void SetUp()
    {
        settings = new BossFightSettings();
    }

    private WaveScheduler Make(int roll) => new WaveScheduler(settings, (min, max) => roll);

    [Test]
    public void NoWaveBeforeInterval()
    {
        var scheduler = Make(5);
        Assert.IsNull(scheduler.Tick(settings.waveIntervalSeconds - 1f, 0, 15, 5));
    }

    [Test]
    public void WaveAtIntervalAndEveryIntervalAfter()
    {
        var scheduler = Make(5);
        Assert.IsNotNull(scheduler.Tick(settings.waveIntervalSeconds, 0, 15, 5));
        Assert.IsNull(scheduler.Tick(settings.waveIntervalSeconds - 1f, 0, 15, 4));
        Assert.IsNotNull(scheduler.Tick(1f, 0, 15, 4));
    }

    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    public void ReleasesRolledCountWhenRoomAvailable(int roll)
    {
        var wave = Make(roll).Tick(settings.waveIntervalSeconds, 0, 15, 3);
        Assert.AreEqual(roll, wave.Value.Count);
    }

    [Test]
    public void DefaultRollStaysInRange()
    {
        for (int i = 0; i < 50; i++)
        {
            var wave = new WaveScheduler(settings).Tick(settings.waveIntervalSeconds, 0, 15, 3);
            Assert.That(wave.Value.Count, Is.InRange(settings.waveMinCostumes, settings.waveMaxCostumes));
        }
    }

    [TestCase(12, 3)]
    [TestCase(14, 1)]
    [TestCase(10, 5)]
    public void CountClampedToAliveCap(int alive, int expected)
    {
        var wave = Make(6).Tick(settings.waveIntervalSeconds, alive, 15, 3);
        Assert.AreEqual(expected, wave.Value.Count);
    }

    [Test]
    public void WaitsWhileAtCapThenFiresWhenRoomOpens()
    {
        var scheduler = Make(5);
        Assert.IsNull(scheduler.Tick(settings.waveIntervalSeconds, 15, 15, 3));
        Assert.IsNull(scheduler.Tick(1f, 16, 15, 3));
        var wave = scheduler.Tick(0.1f, 13, 15, 3);
        Assert.AreEqual(2, wave.Value.Count);
    }

    [Test]
    public void FreshRackOnlyWhileRacksRemain()
    {
        var scheduler = Make(5);
        Assert.IsTrue(scheduler.Tick(settings.waveIntervalSeconds, 0, 15, 1).Value.FreshRack);
        Assert.IsFalse(scheduler.Tick(settings.waveIntervalSeconds, 0, 15, 0).Value.FreshRack);
    }

    [Test]
    public void StopEndsWaves()
    {
        var scheduler = Make(5);
        scheduler.Stop();
        Assert.IsTrue(scheduler.Stopped);
        Assert.IsNull(scheduler.Tick(settings.waveIntervalSeconds * 3f, 0, 15, 3));
    }
}
