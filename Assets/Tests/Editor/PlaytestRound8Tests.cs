using BossFight;
using BossFight.Rules;
using NUnit.Framework;

// Rules added after the eighth playtest.
public class PlaytestRound8Tests
{
    [Test]
    public void ARisingRackClearsTheTallestNeighbour()
    {
        // Floor 0.6, racks 2 m tall (tops at 2.6), ceiling underside 6.75.
        float lift = RackLift.Meters(0.6f, 2f, 2.6f, 6.75f);
        Assert.GreaterOrEqual(0.6f + lift, 2.6f, "its bottom clears the neighbours' tops");
        Assert.LessOrEqual(0.6f + lift + 2f, 6.75f - RackLift.Clearance + 1e-4f, "its top stays under the ceiling");
    }

    [Test]
    public void ARisingRackNeverPokesThroughALowCeiling()
    {
        float lift = RackLift.Meters(0.6f, 2f, 2.6f, 4f);
        Assert.LessOrEqual(0.6f + lift + 2f, 4f - RackLift.Clearance + 1e-4f);
        Assert.GreaterOrEqual(lift, 0f);
    }

    [Test]
    public void IfNothingDiesTheNextRackStartsAnywayAndTheOldWaveStays()
    {
        var s = new BossFightSettings();
        var scheduler = new WaveScheduler(s, 50);
        scheduler.Tick(s.waveFirstDelaySeconds + 0.1f, 0, 0, 22, 5);
        int size = scheduler.Tick(s.rackTelegraphSeconds + 0.1f, 0, 0, 22, 5).Value.Count;
        Assert.IsNull(scheduler.Tick(s.waveStallSeconds - 0.5f, size, size, 22, 5));
        Assert.AreEqual(WaveScheduler.Stage.WaitingForClear, scheduler.Current);
        Assert.IsNull(scheduler.Tick(1f, size, size, 22, 5), "stalled: countdown starts at zero");
        Assert.AreEqual(WaveScheduler.Stage.Countdown, scheduler.Current);
        var next = scheduler.Tick(0.01f, size, size, 22, 5);
        Assert.AreEqual(WaveScheduler.WaveEvent.Telegraph, next.Value.Event);
    }

    [Test]
    public void AKillResetsTheStallTimer()
    {
        var s = new BossFightSettings();
        var scheduler = new WaveScheduler(s, 50);
        scheduler.Tick(s.waveFirstDelaySeconds + 0.1f, 0, 0, 22, 5);
        int size = scheduler.Tick(s.rackTelegraphSeconds + 0.1f, 0, 0, 22, 5).Value.Count; // 6
        scheduler.Tick(s.waveStallSeconds - 1f, size, size, 22, 5);
        scheduler.Tick(0.1f, size - 1, size - 1, 22, 5); // one kill
        scheduler.Tick(s.waveStallSeconds - 1f, size - 1, size - 1, 22, 5);
        Assert.AreEqual(WaveScheduler.Stage.WaitingForClear, scheduler.Current, "the kill reset the stall timer");
    }
}
