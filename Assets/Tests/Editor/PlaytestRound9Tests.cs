using BossFight;
using BossFight.Rules;
using NUnit.Framework;

// Rules added after the ninth playtest.
public class PlaytestRound9Tests
{
    [TestCase(100, 4)]
    [TestCase(50, 6)]
    [TestCase(0, 6)]
    public void NoRackEverReleasesMoreThanSix(int reputation, int expected)
    {
        var s = new BossFightSettings();
        var scheduler = new WaveScheduler(s, reputation);
        scheduler.Tick(s.waveFirstDelaySeconds + 0.1f, 0, 0, s.maxClothingAlive, 5);
        var release = scheduler.Tick(s.rackTelegraphSeconds + 0.1f, 0, 0, s.maxClothingAlive, 5);
        Assert.AreEqual(expected, release.Value.Count);
    }

    [Test]
    public void ALaneBringsItsNextRackOnceFourOfSixAreDead()
    {
        // Round 10 (was 4 of 6 with one lane and a cap of 8).
        var s = new BossFightSettings();
        Assert.IsFalse(WaveScheduler.Cleared(6, 3, s.waveClearFraction), "3 dead is not enough");
        Assert.IsTrue(WaveScheduler.Cleared(6, 2, s.waveClearFraction), "4 dead starts the lane's next rack (round 11)");
        Assert.AreEqual(14, s.maxClothingAlive, "two lanes of 6 plus one leftover each");
    }

    [Test]
    public void RoundNineTuning()
    {
        var s = new BossFightSettings();
        Assert.AreEqual(25f, s.clothingHitCost); // round 12
        Assert.AreEqual(65f, new ReputationScaling(s).MonsterHitCost(100f), 1e-4f);
        Assert.AreEqual(75f, new ReputationScaling(s).MonsterHitCost(50f), 1e-4f);
        Assert.AreEqual(1.05f * s.clothingSpeedFactor, s.SlashMoveSpeedFactor, 1e-5f);
        Assert.AreEqual(4.5f, s.rackTelegraphSeconds, 1e-5f); // round 10
    }
}
