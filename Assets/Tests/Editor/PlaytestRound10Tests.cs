using BossFight;
using BossFight.Rules;
using NUnit.Framework;

// Rules added after the tenth playtest.
public class PlaytestRound10Tests
{
    [Test]
    public void TheSecondLaneStartsItsFirstRackLater()
    {
        var s = new BossFightSettings();
        Assert.AreEqual(2, s.waveLanes);
        var first = new WaveScheduler(s, 50, s.waveFirstDelaySeconds);
        var second = new WaveScheduler(s, 50, s.waveFirstDelaySeconds + s.waveLaneStaggerSeconds);
        float t = s.waveFirstDelaySeconds + 0.1f;
        Assert.AreEqual(WaveScheduler.WaveEvent.Telegraph, first.Tick(t, 0, 0, s.maxClothingAlive, 14).Value.Event);
        Assert.IsNull(second.Tick(t, 0, 0, s.maxClothingAlive, 14), "the second rack waits for the stagger");
        Assert.AreEqual(WaveScheduler.WaveEvent.Telegraph, second.Tick(s.waveLaneStaggerSeconds, 0, 0, s.maxClothingAlive, 14).Value.Event);
    }

    [Test]
    public void BothLanesFullyOutFitUnderTheCap()
    {
        var s = new BossFightSettings();
        Assert.GreaterOrEqual(s.maxClothingAlive, s.waveLanes * s.waveMaxPerRack);
    }
}
