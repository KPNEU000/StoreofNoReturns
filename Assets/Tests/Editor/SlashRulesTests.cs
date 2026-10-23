using System.Collections.Generic;
using BossFight;
using BossFight.Rules;
using NUnit.Framework;
using UnityEngine;

public class SlashRulesTests
{
    private SlashRules rules;

    [SetUp]
    public void SetUp()
    {
        rules = new SlashRules(new BossFightSettings());
    }

    private List<int> Select(params Vector3[] targets)
    {
        return rules.SelectTargets(Vector3.zero, Vector3.forward, targets);
    }

    [Test]
    public void TargetInFrontWithinReachIsHit()
    {
        CollectionAssert.AreEqual(new[] { 0 }, Select(new Vector3(0f, 0f, 2f)));
    }

    [Test]
    public void TargetBeyondReachIsMissed()
    {
        Assert.IsEmpty(Select(new Vector3(0f, 0f, new BossFightSettings().slashReach + 0.1f)));
    }

    [Test]
    public void TargetBehindIsMissed()
    {
        Assert.IsEmpty(Select(new Vector3(0f, 0f, -1f)));
    }

    [TestCase(59f, true)]
    [TestCase(61f, false)]
    [TestCase(-59f, true)]
    [TestCase(-61f, false)]
    public void ArcIsPlusMinus60Degrees(float degrees, bool hit)
    {
        Vector3 pos = Quaternion.Euler(0f, degrees, 0f) * Vector3.forward * 1.5f;
        Assert.AreEqual(hit, Select(pos).Count == 1);
    }

    [Test]
    public void HeightDifferenceIsIgnored()
    {
        Assert.AreEqual(1, Select(new Vector3(0f, 5f, 1f)).Count);
    }

    [Test]
    public void OnlyThreeClosestAreReturnedClosestFirst()
    {
        var result = Select(
            new Vector3(0f, 0f, 2f),
            new Vector3(0f, 0f, 0.5f),
            new Vector3(0f, 0f, 1.5f),
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 0f, 0.8f));
        CollectionAssert.AreEqual(new[] { 1, 4, 3 }, result);
    }

    [Test]
    public void TimingPhasesAreWindupThenRecoveryThenDone()
    {
        Assert.AreEqual(0.15f, rules.HitTime, 1e-4f);
        Assert.AreEqual(0.65f, rules.Duration, 1e-4f);
        Assert.AreEqual(SlashPhase.Windup, rules.PhaseAt(0.1f));
        Assert.AreEqual(SlashPhase.Recovery, rules.PhaseAt(0.15f));
        Assert.AreEqual(SlashPhase.Recovery, rules.PhaseAt(0.6f));
        Assert.AreEqual(SlashPhase.Done, rules.PhaseAt(0.65f));
    }

    [Test]
    public void SlashMoveSpeedIsFivePercentFasterThanTheClothes()
    {
        var settings = new BossFightSettings();
        float walk = 10f;
        float clothing = walk * settings.clothingSpeedFactor;
        Assert.AreEqual(1.05f * clothing, rules.MoveSpeed(walk), 1e-4f);
    }
}
