using BossFight;
using BossFight.Rules;
using NUnit.Framework;

// Round 11 tuning.
public class PlaytestRound11Tests
{
    [Test]
    public void RoundElevenTuning()
    {
        var s = new BossFightSettings();
        Assert.AreEqual(20f, s.clothingHitCost);
        Assert.AreEqual(5f, s.waveNextDelaySeconds, 1e-5f);
        Assert.AreEqual(2.85f, s.slashReach, 1e-5f);
        Assert.AreEqual(0.25f, s.monsterAttackMoveFactor, 1e-5f);
        Assert.AreEqual(4f / 6f, s.waveClearFraction, 1e-5f);
    }
}
