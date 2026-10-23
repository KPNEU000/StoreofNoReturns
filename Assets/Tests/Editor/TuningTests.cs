using BossFight;
using BossFight.Rules;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class TuningTests
{
    [Test]
    public void SpeedLadderDefaults()
    {
        var s = new BossFightSettings();
        Assert.AreEqual(0.51f, s.clothingSpeedFactor);
        Assert.AreEqual(0.95f, s.monsterSpeedFactor);
        Assert.AreEqual(1.4f, s.boostedMonsterSpeedFactor);
        Assert.AreEqual(1.05f * 0.51f, s.SlashMoveSpeedFactor, 1e-4f);
    }

    [Test]
    public void FlashlightIsBrightWithANarrowCone()
    {
        var s = new BossFightSettings();
        Assert.GreaterOrEqual(s.flashlightRange, 30f);
        Assert.Greater(s.flashlightFullIntensity, 8f, "round-4 look with a slightly brighter centre");
        Assert.LessOrEqual(s.flashlightSpotAngle, 50f, "playtest round 6: the wide cone lit everything");
        Assert.Less(s.flashlightInnerSpotAngle, s.flashlightSpotAngle);
        Assert.AreEqual(0.1f, s.flashlightFaintGlow);
    }

    [Test]
    public void BookTuning()
    {
        var s = new BossFightSettings();
        Assert.AreEqual(1.6f, s.bookScale);
        Assert.AreEqual(0.8f, new BookThrowRules(s).TelegraphSeconds);
        BookThrowRules.ScaleAboutBase(new Vector3(1f, 2f, 3f), new Vector3(0.1f, 0.4f, 0.2f), 1.5f, out var pos, out var scale);
        Assert.AreEqual(new Vector3(0.15f, 0.6f, 0.3f), scale);
        Assert.AreEqual(2f + 0.1f, pos.y, 1e-4f); // bottom edge (y - height/2) unchanged
    }

    [Test]
    public void BlackoutDrivesEnvironmentNearBlackAndRestores()
    {
        var s = new BossFightSettings();
        var rules = new BlackoutRules(s);
        var original = new EnvironmentLight
        {
            ambientIntensity = 1f, ambientLight = Color.white, ambientSky = Color.white, ambientEquator = Color.gray,
            ambientGround = Color.gray, reflectionIntensity = 1f, fogColor = new Color(0.5f, 0.6f, 0.7f, 1f),
        };
        var lit = rules.Target(original, 1f);
        Assert.AreEqual(original.ambientIntensity, lit.ambientIntensity);
        Assert.AreEqual(original.fogColor, lit.fogColor);

        var off = rules.Target(original, 0f);
        // Dark enough that the flashlight matters, but (round-2 playtest) not so dark the store can't be navigated.
        Assert.Less(off.ambientIntensity, 0.12f);
        Assert.Greater(off.ambientIntensity, 0.03f); // round 7: a tad darker than round 5, still above pitch black
        Assert.Less(off.reflectionIntensity, 0.12f);
        Assert.Less(off.fogColor.g, 0.12f);
        Assert.Less(off.ambientLight.r, 0.12f);

        var mid = rules.Target(original, 0.5f);
        Assert.Greater(mid.ambientIntensity, off.ambientIntensity);
        Assert.Less(mid.ambientIntensity, lit.ambientIntensity);
    }

    [Test]
    public void ConfigAssetHoldsTunedValues()
    {
        BossFight.Editor.ConfigBuilder.Build();
        var config = BossFight.Editor.ConfigBuilder.Load();
        Assert.IsNotNull(config);
        var s = config.ToSettings();
        Assert.AreEqual(0.51f, s.clothingSpeedFactor);
        Assert.AreEqual(0.95f, s.monsterSpeedFactor);
        Assert.AreEqual(1.4f, s.boostedMonsterSpeedFactor);
        Assert.GreaterOrEqual(s.flashlightRange, 30f);
        Assert.AreEqual(1.6f, s.bookScale);
        Assert.AreEqual(0.8f, s.bookTelegraphSeconds);
    }
}
