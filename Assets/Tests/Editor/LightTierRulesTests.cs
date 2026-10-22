using BossFight;
using BossFight.Rules;
using NUnit.Framework;

public class LightTierRulesTests
{
    private BossFightSettings settings;
    private LightTierRules rules;

    [SetUp]
    public void SetUp()
    {
        settings = new BossFightSettings();
        rules = new LightTierRules(settings);
    }

    [TestCase(1000f, LightTier.Normal)]
    [TestCase(701f, LightTier.Normal)]
    [TestCase(700f, LightTier.MildFlicker)]
    [TestCase(601f, LightTier.MildFlicker)]
    [TestCase(600f, LightTier.StrongFlicker)]
    [TestCase(501f, LightTier.StrongFlicker)]
    [TestCase(500f, LightTier.Off)]
    [TestCase(0f, LightTier.Off)]
    public void TierFollowsMoney(float money, LightTier expected)
    {
        Assert.AreEqual(expected, rules.TierFor(money));
    }

    [Test]
    public void ThresholdsComeFromSettings()
    {
        settings.blackoutMoney = 300f;
        Assert.AreEqual(LightTier.StrongFlicker, rules.TierFor(400f));
        Assert.AreEqual(LightTier.Off, rules.TierFor(300f));
    }

    [TestCase(LightTier.MildFlicker)]
    [TestCase(LightTier.StrongFlicker)]
    [TestCase(LightTier.Off)]
    public void FlickerNeverDipsBelowFloor(LightTier tier)
    {
        settings.strongFlickerMinIntensity = 0f; // config tries to go darker than the floor
        settings.mildFlickerMinIntensity = 0f;
        var flicker = rules.CreateFlicker(5);
        float min = 1f;
        for (float t = 0f; t < 600f; t += 0.02f)
        {
            float v = flicker.Sample(tier, t, 0.5f, 1f);
            Assert.LessOrEqual(v, 1f);
            if (v < min) min = v;
        }
        Assert.GreaterOrEqual(min, rules.FloorIntensity);
        Assert.Less(min, 1f, "flicker should actually dip");
    }

    [Test]
    public void NormalNeverDips()
    {
        var flicker = rules.CreateFlicker(1);
        for (float t = 0f; t < 100f; t += 0.05f)
        {
            Assert.AreEqual(1f, flicker.Sample(LightTier.Normal, t, 0.5f, 1f));
        }
    }

    [Test]
    public void LightsAreNotSynchronized()
    {
        var a = rules.CreateFlicker(1);
        var b = rules.CreateFlicker(2);
        int differing = 0;
        for (float t = 0f; t < 120f; t += 0.02f)
        {
            if (a.Sample(LightTier.StrongFlicker, t, 0.5f, 1f) != b.Sample(LightTier.StrongFlicker, t, 0.5f, 1f)) differing++;
        }
        Assert.Greater(differing, 0);
    }

    [Test]
    public void SameSeedRepeats()
    {
        var a = rules.CreateFlicker(9);
        var b = rules.CreateFlicker(9);
        for (float t = 0f; t < 60f; t += 0.02f)
        {
            Assert.AreEqual(a.Sample(LightTier.MildFlicker, t, 0.5f, 1f), b.Sample(LightTier.MildFlicker, t, 0.5f, 1f));
        }
    }

    [Test]
    public void StrongFlickerDipsDeeperThanMild()
    {
        Assert.Less(rules.DipFloor(LightTier.StrongFlicker), rules.DipFloor(LightTier.MildFlicker));
    }
}
