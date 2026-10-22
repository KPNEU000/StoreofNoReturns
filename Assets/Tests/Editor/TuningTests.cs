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
        Assert.AreEqual(0.85f, s.clothingSpeedFactor);
        Assert.AreEqual(0.95f, s.monsterSpeedFactor);
        Assert.AreEqual(1.4f, s.boostedMonsterSpeedFactor);
        Assert.AreEqual(0.95f * 0.85f, s.SlashMoveSpeedFactor, 1e-4f);
    }

    [Test]
    public void FlashlightIsBrighterAndWider()
    {
        var s = new BossFightSettings();
        Assert.GreaterOrEqual(s.flashlightRange, 30f);
        Assert.Greater(s.flashlightFullIntensity, 3f);
        Assert.Greater(s.flashlightSpotAngle, 55f);
        Assert.AreEqual(0.1f, s.flashlightFaintGlow);
    }

    [Test]
    public void BookTuning()
    {
        var s = new BossFightSettings();
        Assert.AreEqual(1.5f, s.bookScale);
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
        Assert.Less(off.ambientIntensity, 0.05f);
        Assert.Less(off.reflectionIntensity, 0.05f);
        Assert.Less(off.fogColor.g, 0.05f);
        Assert.Less(off.ambientLight.r, 0.05f);

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
        Assert.AreEqual(0.85f, s.clothingSpeedFactor);
        Assert.AreEqual(0.95f, s.monsterSpeedFactor);
        Assert.AreEqual(1.4f, s.boostedMonsterSpeedFactor);
        Assert.GreaterOrEqual(s.flashlightRange, 30f);
        Assert.AreEqual(1.5f, s.bookScale);
        Assert.AreEqual(0.8f, s.bookTelegraphSeconds);
    }
}
