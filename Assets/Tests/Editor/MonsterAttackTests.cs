using BossFight;
using BossFight.Rules;
using NUnit.Framework;
using UnityEngine;

public class MonsterAttackTests
{
    private BossFightSettings settings;
    private MonsterAttack attack;
    private static readonly Vector3 Origin = Vector3.zero;
    private static readonly Vector3 Forward = Vector3.forward;

    [SetUp]
    public void SetUp()
    {
        settings = new BossFightSettings();
        attack = new MonsterAttack(settings);
    }

    [Test]
    public void DoesNotBeginOutOfReach()
    {
        Assert.IsFalse(attack.TryBegin(Origin, new Vector3(0f, 0f, 3f)));
        Assert.AreEqual(MonsterAttackState.Idle, attack.State);
    }

    [Test]
    public void NoHitBeforeWindupEnds()
    {
        var player = new Vector3(0f, 0f, 1f);
        Assert.IsTrue(attack.TryBegin(Origin, player));
        Assert.IsFalse(attack.Tick(settings.monsterWindupSeconds - 0.05f, Origin, Forward, player));
        Assert.AreEqual(MonsterAttackState.Windup, attack.State);
    }

    [Test]
    public void HitsAtEndOfWindupWhenPlayerStaysInReachAndInFront()
    {
        var player = new Vector3(0f, 0f, 1f);
        attack.TryBegin(Origin, player);
        Assert.IsTrue(attack.Tick(settings.monsterWindupSeconds + 0.01f, Origin, Forward, player));
        Assert.AreEqual(MonsterAttackState.Recovery, attack.State, "the clip keeps playing after the strike");
    }

    [Test]
    public void WhiffsWhenPlayerBacksOutOfReach()
    {
        attack.TryBegin(Origin, new Vector3(0f, 0f, 1f));
        Assert.IsFalse(attack.Tick(settings.monsterWindupSeconds + 0.01f, Origin, Forward, new Vector3(0f, 0f, settings.monsterAttackReach + 0.5f)));
        Assert.AreEqual(MonsterAttackState.Recovery, attack.State, "a whiff still finishes its swing");
    }

    [Test]
    public void WhiffsWhenPlayerIsBehind()
    {
        attack.TryBegin(Origin, new Vector3(0f, 0f, 1f));
        Assert.IsFalse(attack.Tick(settings.monsterWindupSeconds + 0.01f, Origin, Forward, new Vector3(0f, 0f, -1f)));
    }

    [TestCase(0f, true)]
    [TestCase(40f, true)]
    [TestCase(50f, false)]
    [TestCase(90f, false)]
    public void ConeIsNinetyDegreesWide(float angle, bool expected)
    {
        var dir = Quaternion.Euler(0f, angle, 0f) * Forward;
        Assert.AreEqual(expected, attack.InFront(Origin, Forward, dir));
    }

    [Test]
    public void CooldownBlocksNewWindupUntilItElapses()
    {
        var player = new Vector3(0f, 0f, 1f);
        attack.TryBegin(Origin, player);
        attack.Tick(settings.monsterWindupSeconds + 0.01f, Origin, Forward, player);
        Assert.IsFalse(attack.TryBegin(Origin, player));
        attack.Tick(attack.RecoverySeconds + 0.01f, Origin, Forward, player);
        Assert.AreEqual(MonsterAttackState.Cooldown, attack.State);
        Assert.IsFalse(attack.TryBegin(Origin, player));
        attack.Tick(settings.monsterAttackCooldownSeconds + 0.01f, Origin, Forward, player);
        Assert.AreEqual(MonsterAttackState.Idle, attack.State);
        Assert.IsTrue(attack.TryBegin(Origin, player));
    }

    [Test]
    public void HeightDifferenceIsIgnored()
    {
        Assert.IsTrue(attack.InReach(Origin, new Vector3(0f, 5f, 1f)));
    }

    [Test]
    public void TetherStartsAfterIntervalAndEndsAfterDuration()
    {
        var tether = new TetherSchedule(settings);
        Assert.AreEqual(0, tether.Tick(settings.tetherIntervalSeconds - 1f));
        Assert.AreEqual(1, tether.Tick(1.01f));
        Assert.IsTrue(tether.Active);
        Assert.AreEqual(0, tether.Tick(settings.tetherDurationSeconds - 0.5f));
        Assert.AreEqual(-1, tether.Tick(0.6f));
        Assert.IsFalse(tether.Active);
    }

    [Test]
    public void BoostedSpeedLadderMatchesSpec()
    {
        Assert.AreEqual(0.95f, settings.monsterSpeedFactor);
        Assert.AreEqual(1.4f, settings.boostedMonsterSpeedFactor);
        Assert.AreEqual(1.1f, settings.monsterWindupSeconds);
    }

    [Test]
    public void TheSwingPlaysTheWholeAttackClipAndIsCommittedThroughout()
    {
        var player = new Vector3(0f, 0f, 1f);
        Assert.IsTrue(attack.TryBegin(Origin, player));
        Assert.IsTrue(attack.Committed);
        Assert.AreEqual(settings.monsterAttackClipSeconds, attack.WindupSeconds + attack.RecoverySeconds, 1e-4f);
        // Walking away mid-swing does not end it early.
        var away = new Vector3(0f, 0f, 20f);
        attack.Tick(settings.monsterWindupSeconds + 0.01f, Origin, Forward, away);
        Assert.IsTrue(attack.Committed);
        attack.Tick(attack.RecoverySeconds - 0.05f, Origin, Forward, away);
        Assert.IsTrue(attack.Committed);
        attack.Tick(0.1f, Origin, Forward, away);
        Assert.IsFalse(attack.Committed);
    }
}
