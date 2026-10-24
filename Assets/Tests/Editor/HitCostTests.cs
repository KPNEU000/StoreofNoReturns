using BossFight;
using BossFight.Rules;
using NUnit.Framework;

public class HitCostTests
{
    private HitCost cost;

    [SetUp]
    public void SetUp()
    {
        cost = new HitCost(new BossFightSettings());
    }

    // 2026-10-23 playtest: clothing and monster hits doubled.
    [TestCase(HitKind.Clothing, 100f, 50f)]
    [TestCase(HitKind.Clothing, 0f, 50f)]
    [TestCase(HitKind.Book, 100f, 50f)]
    [TestCase(HitKind.Book, 0f, 50f)]
    [TestCase(HitKind.Monster, 0f, 200f)]
    [TestCase(HitKind.Monster, 50f, 150f)]
    [TestCase(HitKind.Monster, 100f, 130f)]
    public void CostByKindAndReputation(HitKind kind, float reputation, float expected)
    {
        Assert.AreEqual(expected, cost.For(kind, reputation), 1e-4f);
    }

    [Test]
    public void FirstHitIsAccepted()
    {
        Assert.IsTrue(new InvincibilityTimer(0.5f).TryAccept(0f));
    }

    [Test]
    public void SecondHitInsideWindowIsIgnored()
    {
        var timer = new InvincibilityTimer(0.5f);
        Assert.IsTrue(timer.TryAccept(10f));
        Assert.IsFalse(timer.TryAccept(10.3f));
    }

    [Test]
    public void HitAfterWindowIsAcceptedAndRestartsWindow()
    {
        var timer = new InvincibilityTimer(0.5f);
        timer.TryAccept(10f);
        Assert.IsTrue(timer.TryAccept(10.5f));
        Assert.IsFalse(timer.TryAccept(10.9f));
    }

    [Test]
    public void IgnoredHitDoesNotExtendWindow()
    {
        var timer = new InvincibilityTimer(0.5f);
        timer.TryAccept(10f);
        timer.TryAccept(10.4f);
        Assert.IsTrue(timer.TryAccept(10.5f));
    }

    [Test]
    public void DefaultInvincibilityIsThreeQuartersOfASecond()
    {
        Assert.AreEqual(0.75f, new BossFightSettings().invincibilitySeconds, 1e-4f);
    }

    [Test]
    public void RunScoreStoresMoneyLeft()
    {
        RunScore.Set(425f);
        Assert.AreEqual(425f, RunScore.Value);
        RunScore.Reset();
        Assert.AreEqual(0f, RunScore.Value);
    }
}
