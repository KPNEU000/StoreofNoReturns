using System.Linq;
using BossFight;
using BossFight.Rules;
using NUnit.Framework;

public class OpeningStepsTests
{
    private BossFightSettings settings;
    private OpeningSteps steps;

    [SetUp]
    public void SetUp()
    {
        settings = new BossFightSettings();
        steps = new OpeningSteps(settings);
    }

    private static readonly OpeningSignals None = default;

    // Feeds the signal that completes the current step and returns the step reached.
    private OpeningStep Complete(OpeningStepInfo info)
    {
        var s = new OpeningSignals
        {
            LeftClick = info.Condition == OpeningCondition.LeftClick,
            VaultDone = info.Condition == OpeningCondition.VaultDone,
            CostumeDead = info.Condition == OpeningCondition.CostumeDead,
            WitchHit = info.Condition == OpeningCondition.WitchHit,
        };
        Assert.IsTrue(steps.Advance(s, info.Condition == OpeningCondition.Seconds ? info.Seconds : 0f));
        return steps.Finished ? OpeningStep.DoorBlast : steps.Current.Step;
    }

    [Test]
    public void StepsRunInContextOrder()
    {
        var expected = new[]
        {
            OpeningStep.LockPlayer, OpeningStep.MagicToMonitor, OpeningStep.ShatterMonitor, OpeningStep.CrackScreen,
            OpeningStep.WitchLine, OpeningStep.GrabSword, OpeningStep.Vault, OpeningStep.TutorialSlash,
            OpeningStep.HitWitch, OpeningStep.WitchRetreat, OpeningStep.DoorBlast,
        };
        CollectionAssert.AreEqual(expected, steps.All.Select(i => i.Step).ToArray());
    }

    [Test]
    public void WalkingEveryStepWithItsOwnConditionFinishesInOrder()
    {
        var visited = new System.Collections.Generic.List<OpeningStep> { steps.Current.Step };
        while (!steps.Finished)
        {
            var info = steps.Current;
            Complete(info);
            if (!steps.Finished) visited.Add(steps.Current.Step);
        }
        CollectionAssert.AreEqual(steps.All.Select(i => i.Step).ToArray(), visited.ToArray());
    }

    [Test]
    public void NoSignalsMeansOnlyImmediateAndTimedStepsMove()
    {
        Assert.IsTrue(steps.Advance(None, 0f)); // LockPlayer is immediate
        Assert.AreEqual(OpeningStep.MagicToMonitor, steps.Current.Step);
        Assert.IsFalse(steps.Advance(None, settings.openingMagicSeconds - 0.01f));
        Assert.IsTrue(steps.Advance(None, 0.02f));
        Assert.AreEqual(OpeningStep.ShatterMonitor, steps.Current.Step);
    }

    [Test]
    public void CinematicBeforeTheTutorialIsRoughlyTwelveSeconds()
    {
        float total = steps.All.TakeWhile(i => i.Step != OpeningStep.GrabSword).Sum(i => i.Seconds);
        Assert.That(total, Is.InRange(10f, 14f));
    }

    [Test]
    public void TimedStepsReadTheirDurationFromSettings()
    {
        settings.openingWitchLineSeconds = 9f;
        var custom = new OpeningSteps(settings);
        Assert.AreEqual(9f, custom.All.First(i => i.Step == OpeningStep.WitchLine).Seconds, 1e-4f);
    }

    private void GoTo(OpeningStep target)
    {
        while (steps.Current.Step != target) Complete(steps.Current);
    }

    [Test]
    public void SwordWaitsForLeftClick()
    {
        GoTo(OpeningStep.GrabSword);
        Assert.IsFalse(steps.Advance(new OpeningSignals { VaultDone = true, WitchHit = true }, 100f));
        Assert.IsTrue(steps.Advance(new OpeningSignals { LeftClick = true }, 0f));
        Assert.AreEqual(OpeningStep.Vault, steps.Current.Step);
    }

    [Test]
    public void TheTutorialNoLongerTeachesTheFlashlight()
    {
        // Playtest round 5: the flashlight is automatic, so no step waits on it.
        var names = steps.All.Select(i => i.Step.ToString()).ToArray();
        CollectionAssert.DoesNotContain(names, "GrabFlashlight");
        CollectionAssert.DoesNotContain(names, "TutorialLight");
    }

    [Test]
    public void OnlyOneStepAdvancesPerCall()
    {
        GoTo(OpeningStep.GrabSword);
        var all = new OpeningSignals { LeftClick = true, VaultDone = true, CostumeDead = true, WitchHit = true };
        Assert.IsTrue(steps.Advance(all, 0f));
        Assert.AreEqual(OpeningStep.Vault, steps.Current.Step);
    }

    [Test]
    public void VaultNeedsTheVaultToFinish()
    {
        GoTo(OpeningStep.Vault);
        Assert.IsFalse(steps.Advance(new OpeningSignals { LeftClick = true }, 100f));
        Assert.IsTrue(steps.Advance(new OpeningSignals { VaultDone = true }, 0f));
        Assert.AreEqual(OpeningStep.TutorialSlash, steps.Current.Step);
    }

    [Test]
    public void CostumeThenOneHitOnTheWitchThenSheRetreatsBeforeTheDoorBlast()
    {
        GoTo(OpeningStep.TutorialSlash);
        Assert.IsFalse(steps.Advance(new OpeningSignals { LeftClick = true, WitchHit = true }, 100f));
        Assert.IsTrue(steps.Advance(new OpeningSignals { CostumeDead = true }, 0f));
        Assert.AreEqual(OpeningStep.HitWitch, steps.Current.Step);
        Assert.IsFalse(steps.Advance(new OpeningSignals { LeftClick = true, CostumeDead = true }, 100f), "waits for a real hit");
        Assert.IsTrue(steps.Advance(new OpeningSignals { WitchHit = true }, 0f));
        Assert.AreEqual(OpeningStep.WitchRetreat, steps.Current.Step);
        Assert.IsFalse(steps.Advance(None, settings.openingWitchReturnSeconds - 0.01f));
        Assert.IsTrue(steps.Advance(None, 0.02f));
        Assert.AreEqual(OpeningStep.DoorBlast, steps.Current.Step);
        Assert.IsFalse(steps.Finished);
    }

    [Test]
    public void DoorBlastIsLastAndFinishesTheOpening()
    {
        GoTo(OpeningStep.DoorBlast);
        Assert.IsTrue(steps.Advance(None, 0f));
        Assert.IsTrue(steps.Finished);
        Assert.IsFalse(steps.Advance(new OpeningSignals { LeftClick = true }, 1f));
    }

    [Test]
    public void ElapsedResetsOnEachStep()
    {
        steps.Advance(None, 0f);
        steps.Advance(None, 1.5f);
        Assert.AreEqual(1.5f, steps.Elapsed, 1e-4f);
        steps.Advance(None, settings.openingMagicSeconds);
        Assert.AreEqual(0f, steps.Elapsed, 1e-4f);
    }
}
