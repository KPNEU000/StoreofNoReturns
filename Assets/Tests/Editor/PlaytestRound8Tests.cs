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
    public void OnlyKillsMoveTheSwarmOnNoStallTimer()
    {
        // Round 9 removed the stall timer: it stacked waves into a flood.
        var s = new BossFightSettings();
        var scheduler = new WaveScheduler(s, 50);
        scheduler.Tick(s.waveFirstDelaySeconds + 0.1f, 0, 0, 8, 5);
        int size = scheduler.Tick(s.rackTelegraphSeconds + 0.1f, 0, 0, 8, 5).Value.Count;
        Assert.IsNull(scheduler.Tick(600f, size, size, 8, 5));
        Assert.AreEqual(WaveScheduler.Stage.WaitingForClear, scheduler.Current, "ten minutes without a kill still waits");
    }
}
