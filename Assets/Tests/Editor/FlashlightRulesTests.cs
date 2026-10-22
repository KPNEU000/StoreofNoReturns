using BossFight;
using BossFight.Rules;
using NUnit.Framework;

public class FlashlightRulesTests
{
    private BossFightSettings settings;
    private FlashlightRules rules;

    [SetUp]
    public void SetUp()
    {
        settings = new BossFightSettings();
        rules = new FlashlightRules(settings);
    }

    private float Run(float start, bool held, bool slashing, float seconds)
    {
        float f = start;
        const float dt = 0.01f;
        for (float t = 0f; t < seconds - 1e-4f; t += dt) f = rules.Step(f, held, slashing, dt);
        return f;
    }

    [Test]
    public void IdleStaysAtFaintGlow()
    {
        Assert.AreEqual(0.1f, Run(rules.Faint, false, false, 2f), 1e-4f);
        Assert.Greater(rules.Intensity(rules.Faint), 0f);
    }

    [Test]
    public void HoldingReachesFullInHalfASecond()
    {
        Assert.Less(Run(rules.Faint, true, false, 0.25f), 0.9f);
        Assert.AreEqual(1f, Run(rules.Faint, true, false, 0.5f), 1e-3f);
        Assert.AreEqual(1f, Run(rules.Faint, true, false, 2f), 1e-4f);
    }

    [Test]
    public void ReleaseReturnsToFaintInQuarterSecond()
    {
        Assert.AreEqual(rules.Faint, Run(1f, false, false, 0.25f), 1e-3f);
        Assert.Greater(Run(1f, false, false, 0.1f), rules.Faint);
    }

    [Test]
    public void SlashingForcesFaintEvenWhileHeld()
    {
        Assert.AreEqual(rules.Faint, Run(1f, true, true, 0.01f), 1e-4f);
    }

    [Test]
    public void IntensityScalesWithFractionAndTutorialMultiplier()
    {
        Assert.AreEqual(settings.flashlightFullIntensity, rules.Intensity(1f), 1e-4f);
        rules.BrightMultiplier = 2f;
        Assert.AreEqual(settings.flashlightFullIntensity * 2f, rules.Intensity(1f), 1e-4f);
    }
}
