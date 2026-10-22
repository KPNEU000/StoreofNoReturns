using BossFight;
using BossFight.Rules;
using NUnit.Framework;

public class ReputationScalingTests
{
    private ReputationScaling scaling;

    [SetUp]
    public void SetUp()
    {
        scaling = new ReputationScaling(new BossFightSettings());
    }

    [TestCase(0f, 2f)]
    [TestCase(25f, 1.5f)]
    [TestCase(50f, 1f)]
    [TestCase(75f, 0.75f)]
    [TestCase(100f, 0.5f)]
    public void MonsterHpMultiplierFollowsCurve(float reputation, float expected)
    {
        Assert.AreEqual(expected, scaling.MonsterHpMultiplier(reputation), 1e-4f);
    }

    [TestCase(0f, 100f)]
    [TestCase(25f, 87.5f)]
    [TestCase(50f, 75f)]
    [TestCase(75f, 62.5f)]
    [TestCase(100f, 50f)]
    public void MonsterHitCostFollowsCurve(float reputation, float expected)
    {
        Assert.AreEqual(expected, scaling.MonsterHitCost(reputation), 1e-4f);
    }

    [TestCase(-10f, 2f, 100f)]
    [TestCase(-1000f, 2f, 100f)]
    [TestCase(110f, 0.5f, 50f)]
    [TestCase(1000f, 0.5f, 50f)]
    public void OutOfRangeReputationIsClamped(float reputation, float expectedHp, float expectedCost)
    {
        Assert.AreEqual(expectedHp, scaling.MonsterHpMultiplier(reputation), 1e-4f);
        Assert.AreEqual(expectedCost, scaling.MonsterHitCost(reputation), 1e-4f);
    }

    [Test]
    public void UsesSettingsSnapshotValues()
    {
        var settings = new BossFightSettings { monsterHitCostAtRep0 = 200f, monsterHitCostAtRep50 = 100f };
        Assert.AreEqual(150f, new ReputationScaling(settings).MonsterHitCost(25f), 1e-4f);
    }

    [Test]
    public void SlashSpeedIsDerivedFromClothingSpeed()
    {
        var settings = new BossFightSettings { clothingSpeedFactor = 0.6f };
        Assert.AreEqual(0.9f * 0.6f, settings.SlashMoveSpeedFactor, 1e-4f);
    }

    [Test]
    public void ConfigToSettingsReturnsIndependentCopy()
    {
        var config = UnityEngine.ScriptableObject.CreateInstance<BossFightConfig>();
        try
        {
            var a = config.ToSettings();
            a.witchHp = 1f;
            Assert.AreEqual(100f, config.ToSettings().witchHp);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
        }
    }
}
