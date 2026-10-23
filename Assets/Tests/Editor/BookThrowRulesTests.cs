using System.Collections.Generic;
using BossFight;
using BossFight.Rules;
using NUnit.Framework;
using UnityEngine;

public class BookThrowRulesTests
{
    private BossFightSettings settings;

    [SetUp]
    public void SetUp()
    {
        settings = new BossFightSettings();
    }

    private BookThrowRules Make(int roll = 0) => new BookThrowRules(settings, n => roll);

    [Test]
    public void NeverThrowsOutsidePhase2()
    {
        var rules = Make();
        Assert.IsFalse(rules.Tick(settings.bookThrowIntervalSeconds * 3f, false, false));
    }

    [Test]
    public void FirstThrowOneIntervalAfterPhase2Starts()
    {
        var rules = Make();
        Assert.IsFalse(rules.Tick(settings.bookThrowIntervalSeconds - 0.1f, true, false));
        Assert.IsTrue(rules.Tick(0.1f, true, false));
    }

    [Test]
    public void ThrowsEveryInterval()
    {
        var rules = Make();
        Assert.IsTrue(rules.Tick(settings.bookThrowIntervalSeconds, true, false));
        Assert.IsFalse(rules.Tick(settings.bookThrowIntervalSeconds - 0.5f, true, false));
        Assert.IsTrue(rules.Tick(0.5f, true, false));
    }

    [Test]
    public void LeavingPhase2RestartsInterval()
    {
        var rules = Make();
        rules.Tick(settings.bookThrowIntervalSeconds - 0.1f, true, false);
        rules.Tick(0.1f, false, false);
        Assert.IsFalse(rules.Tick(settings.bookThrowIntervalSeconds - 0.1f, true, false));
    }

    [Test]
    public void CountdownOnlyStartsOnceThePreviousBookHasLanded()
    {
        var rules = Make();
        // Busy (throwing, or the book still in the air) holds the full interval however long it lasts.
        Assert.IsFalse(rules.Tick(settings.bookThrowIntervalSeconds * 3f, true, true));
        Assert.IsFalse(rules.Tick(settings.bookThrowIntervalSeconds - 0.1f, true, false), "the countdown starts at the landing");
        Assert.IsTrue(rules.Tick(0.2f, true, false));
    }

    [Test]
    public void TelegraphComesFromSettings()
    {
        settings.bookTelegraphSeconds = 0.6f;
        Assert.AreEqual(0.6f, Make().TelegraphSeconds);
    }

    [Test]
    public void PickBookNoneAvailable()
    {
        Assert.AreEqual(-1, Make().PickBook(new List<Vector3>(), Vector3.zero));
    }

    [Test]
    public void PickBookChoosesAmongNearestThree()
    {
        var books = new List<Vector3>
        {
            new Vector3(30, 0, 0), new Vector3(1, 0, 0), new Vector3(20, 0, 0), new Vector3(2, 0, 0), new Vector3(3, 0, 0)
        };
        for (int roll = 0; roll < 3; roll++)
        {
            int picked = Make(roll).PickBook(books, Vector3.zero);
            CollectionAssert.Contains(new[] { 1, 3, 4 }, picked);
        }
        Assert.AreEqual(1, Make(0).PickBook(books, Vector3.zero));
    }

    [Test]
    public void PickBookWithFewerThanThreeStaysInRange()
    {
        var books = new List<Vector3> { new Vector3(5, 0, 0), new Vector3(1, 0, 0) };
        Assert.AreEqual(0, Make(99).PickBook(books, Vector3.zero));
    }

    [Test]
    public void FlightIsStraightAndDoesNotHome()
    {
        var rules = Make();
        var start = Vector3.zero;
        var target = new Vector3(10, 0, 0);
        var dir = BookThrowRules.FlightDirection(start, target);
        var pos = rules.StepFlight(start, dir, 0.5f);
        Assert.AreEqual(settings.bookFlightSpeed * 0.5f, pos.x, 1e-4f);
        Assert.AreEqual(0f, pos.z, 1e-4f);
    }

    [Test]
    public void ReachedTargetOncePassed()
    {
        var dir = Vector3.right;
        var target = new Vector3(10, 0, 0);
        Assert.IsFalse(BookThrowRules.ReachedTarget(new Vector3(9.9f, 0, 0), dir, target));
        Assert.IsTrue(BookThrowRules.ReachedTarget(new Vector3(10.1f, 0, 0), dir, target));
    }

    [Test]
    public void HitsOnlyWithinRadius()
    {
        var rules = Make();
        Assert.IsTrue(rules.Hits(Vector3.zero, new Vector3(settings.bookHitRadius - 0.05f, 0, 0)));
        Assert.IsFalse(rules.Hits(Vector3.zero, new Vector3(settings.bookHitRadius + 0.05f, 0, 0)));
    }

    [Test]
    public void BookHitCostsFifty()
    {
        Assert.AreEqual(50f, new HitCost(settings).For(HitKind.Book, 50f));
    }

    [Test]
    public void GlowFadesToZeroOverConfiguredSeconds()
    {
        settings.bookGlowFadeSeconds = 1f;
        var rules = Make();
        Assert.AreEqual(1f, rules.GlowAt(0f), 1e-4f);
        Assert.AreEqual(0.5f, rules.GlowAt(0.5f), 1e-4f);
        Assert.AreEqual(0f, rules.GlowAt(2f), 1e-4f);
    }
}
