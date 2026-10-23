using BossFight.Rules;
using NUnit.Framework;
using UnityEngine;

// Rules added after the third playtest: path request throttling and the win screen's reputation stars.
public class PlaytestRound3Tests
{
    [Test]
    public void RepathGateAllowsTheFirstRequestThenThrottles()
    {
        var gate = new RepathGate(0.25f, 2f);
        Assert.IsTrue(gate.ShouldRepath(0f, Vector3.zero));
        Assert.IsFalse(gate.ShouldRepath(0.1f, new Vector3(0.5f, 0f, 0f)), "small drift inside the interval waits");
        Assert.IsTrue(gate.ShouldRepath(0.26f, new Vector3(0.6f, 0f, 0f)), "the interval elapsed");
    }

    [Test]
    public void RepathGateRepathsAtOnceWhenTheTargetJumps()
    {
        var gate = new RepathGate(0.25f, 2f);
        gate.ShouldRepath(0f, Vector3.zero);
        Assert.IsTrue(gate.ShouldRepath(0.05f, new Vector3(3f, 0f, 0f)));
    }

    [TestCase(0, 0)]
    [TestCase(9, 0)]
    [TestCase(10, 1)]
    [TestCase(50, 3)]
    [TestCase(89, 4)]
    [TestCase(90, 5)]
    [TestCase(100, 5)]
    [TestCase(150, 5)]
    [TestCase(-20, 0)]
    public void ReputationBecomesWholeStars(int reputation, int stars)
    {
        Assert.AreEqual(stars, StarRating.Stars(reputation));
    }

    [Test]
    public void StarOutlineContainsItsCentreButNotItsCorners()
    {
        var outline = StarRating.Outline();
        Assert.IsTrue(StarRating.Contains(outline, new Vector2(0.5f, 0.5f)));
        Assert.IsFalse(StarRating.Contains(outline, new Vector2(0.02f, 0.02f)));
        Assert.IsFalse(StarRating.Contains(outline, new Vector2(0.98f, 0.98f)));
    }
}
