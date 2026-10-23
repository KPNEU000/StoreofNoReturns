using BossFight;
using BossFight.Rules;
using NUnit.Framework;
using UnityEngine;

// Rules added after the fifth playtest.
public class PlaytestRound5Tests
{
    [Test]
    public void TheTutorialHitTakesTenOffTheWitchExactlyOnceWithoutStartingTheFight()
    {
        var s = new BossFightSettings();
        var machine = new BossPhaseMachine(s);
        Assert.IsFalse(machine.WitchDamageable, "shielded until the tutorial opens the hit");
        machine.OpenIntroHit();
        Assert.IsTrue(machine.WitchDamageable);
        Assert.IsFalse(machine.DamageWitch(s.tutorialWitchHit), "no phase change");
        Assert.AreEqual(BossPhase.Intro, machine.Phase);
        Assert.AreEqual(s.witchHp - s.tutorialWitchHit, machine.WitchHp, 1e-4f);
        Assert.IsFalse(machine.WitchDamageable, "only one hit");
        machine.OpenIntroHit();
        Assert.IsFalse(machine.WitchDamageable, "cannot be reopened");
        machine.Start(2);
        Assert.AreEqual(s.witchHp - s.tutorialWitchHit, machine.WitchHp, 1e-4f, "the fight keeps the tutorial damage");
    }

    [Test]
    public void TheTutorialHitCanNeverKillTheWitch()
    {
        var s = new BossFightSettings();
        var machine = new BossPhaseMachine(s);
        machine.OpenIntroHit();
        machine.DamageWitch(10000f);
        Assert.AreEqual(BossPhase.Intro, machine.Phase);
        Assert.Greater(machine.WitchHp, 0f);
    }

    [TestCase(1000f, false)]
    [TestCase(501f, false)]
    [TestCase(500f, true)]
    [TestCase(120f, true)]
    public void FlashlightTurnsOnByItselfAtTheBlackout(float money, bool on)
    {
        Assert.AreEqual(on, new FlashlightRules(new BossFightSettings()).ShouldBeOn(money));
    }

    [Test]
    public void DraggedBodiesStayInTheBoxBehindTheMonster()
    {
        var inFront = DraggedBodyRules.ClampBehind(new Vector3(0f, 0.2f, 1.5f));
        Assert.LessOrEqual(inFront.z, -DraggedBodyRules.BoxNearBack, "never in front");
        var wide = DraggedBodyRules.ClampBehind(new Vector3(4f, 0.2f, -1f));
        Assert.AreEqual(DraggedBodyRules.BoxHalfWidth, wide.x, 1e-5f);
        var far = DraggedBodyRules.ClampBehind(new Vector3(0f, 0.2f, -9f));
        Assert.AreEqual(-DraggedBodyRules.BoxFarBack, far.z, 1e-5f);
        var inside = new Vector3(0.3f, 0.2f, -1.5f);
        Assert.AreEqual(inside, DraggedBodyRules.ClampBehind(inside));
    }

    [Test]
    public void AChunkLandingOnThePlayerCostsDebrisOnce()
    {
        var s = new BossFightSettings();
        Assert.IsTrue(ChunkPlacement.LandsOn(Vector3.zero, new Vector3(0.5f, 1.6f, 0.3f), s.caveInChunkRadius));
        Assert.IsFalse(ChunkPlacement.LandsOn(Vector3.zero, new Vector3(3f, 1.6f, 0f), s.caveInChunkRadius));
        Assert.AreEqual(s.debrisHitCost, new HitCost(s).For(HitKind.Debris, 50f), 1e-4f);
    }

    [Test]
    public void SightAnchorsSpreadAcrossTheStoreAtOneHeight()
    {
        var footprint = Rect.MinMaxRect(-12f, -15.38f, 12f, 11.5f);
        var anchors = StoreBounds.SightAnchors(footprint, 3.8f);
        Assert.AreEqual(5, anchors.Length);
        foreach (var a in anchors)
        {
            Assert.AreEqual(3.8f, a.y, 1e-5f);
            Assert.IsTrue(StoreBounds.Inside(footprint, a, 2f), "anchors sit well inside the walls");
        }
    }

    [Test]
    public void RoundFiveTuning()
    {
        var s = new BossFightSettings();
        Assert.AreEqual(120f, s.slashArcDegrees);
        Assert.AreEqual(2.75f, s.slashReach, 1e-5f);
        Assert.AreEqual(0.95f * s.clothingSpeedFactor, s.SlashMoveSpeedFactor, 1e-5f);
        Assert.AreEqual(0.75f, s.invincibilitySeconds, 1e-5f);
        Assert.AreEqual(7f, s.bookThrowIntervalSeconds, 1e-5f);
        Assert.AreEqual(2.25f, s.bookScale, 1e-5f);
        Assert.AreEqual(0.195f, MoveSmoothing.DefaultSensitivity, 1e-5f);
    }
}
