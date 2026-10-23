using BossFight;
using BossFight.Rules;
using NUnit.Framework;

public class BossPhaseMachineTests
{
    private BossPhaseMachine machine;

    [SetUp]
    public void SetUp()
    {
        machine = new BossPhaseMachine(new BossFightSettings());
    }

    [Test]
    public void StartsInIntroWithFullWitchHp()
    {
        Assert.AreEqual(BossPhase.Intro, machine.Phase);
        Assert.AreEqual(100f, machine.WitchHp, 1e-4f);
        Assert.IsFalse(machine.WitchDamageable);
    }

    [TestCase(3, BossPhase.Phase1)]
    [TestCase(1, BossPhase.Phase1)]
    [TestCase(0, BossPhase.Phase2)]
    [TestCase(-2, BossPhase.Phase2)]
    public void StartPicksPhaseFromMonsterCount(int monsters, BossPhase expected)
    {
        Assert.IsTrue(machine.Start(monsters));
        Assert.AreEqual(expected, machine.Phase);
    }

    [Test]
    public void StartOnlyLeavesIntroOnce()
    {
        machine.Start(0);
        Assert.IsFalse(machine.Start(3));
        Assert.AreEqual(BossPhase.Phase2, machine.Phase);
    }

    [Test]
    public void Phase2WhenLastMonsterDies()
    {
        machine.Start(3);
        Assert.IsFalse(machine.SetMonstersAlive(2));
        Assert.IsFalse(machine.SetMonstersAlive(1));
        Assert.AreEqual(BossPhase.Phase1, machine.Phase);
        Assert.IsTrue(machine.SetMonstersAlive(0));
        Assert.AreEqual(BossPhase.Phase2, machine.Phase);
        Assert.AreEqual(0, machine.MonstersAlive);
    }

    [Test]
    public void MonsterCountBeforeStartDoesNotChangePhase()
    {
        Assert.IsFalse(machine.SetMonstersAlive(0));
        Assert.AreEqual(BossPhase.Intro, machine.Phase);
    }

    [Test]
    public void WitchIgnoresDamageInIntroAndPhase1()
    {
        Assert.IsFalse(machine.DamageWitch(50f));
        machine.Start(2);
        Assert.IsFalse(machine.WitchDamageable);
        machine.DamageWitch(500f);
        Assert.AreEqual(100f, machine.WitchHp, 1e-4f);
        Assert.AreEqual(BossPhase.Phase1, machine.Phase);
    }

    [Test]
    public void WitchDamageableInPhase2()
    {
        machine.Start(0);
        Assert.IsTrue(machine.WitchDamageable);
        machine.DamageWitch(10f);
        Assert.AreEqual(90f, machine.WitchHp, 1e-4f);
    }

    [Test]
    public void WonAtWitchHpZeroAfterTenSlashes()
    {
        machine.Start(0);
        var slash = new BossFightSettings().slashDamage;
        for (int i = 0; i < 9; i++)
        {
            Assert.IsFalse(machine.DamageWitch(slash));
        }
        Assert.IsTrue(machine.DamageWitch(slash));
        Assert.AreEqual(BossPhase.Won, machine.Phase);
        Assert.IsFalse(machine.WitchDamageable);
        Assert.IsFalse(machine.DamageWitch(slash));
    }

    [Test]
    public void OverkillClampsHpAndWins()
    {
        machine.Start(0);
        Assert.IsTrue(machine.DamageWitch(250f));
        Assert.AreEqual(0f, machine.WitchHp, 1e-4f);
        Assert.AreEqual(BossPhase.Won, machine.Phase);
    }

    [TestCase(0f)]
    [TestCase(-25f)]
    public void LostWhenMoneyRunsOut(float money)
    {
        machine.Start(3);
        Assert.IsTrue(machine.OnMoneyChanged(money));
        Assert.AreEqual(BossPhase.Lost, machine.Phase);
        Assert.IsFalse(machine.WitchDamageable);
    }

    [Test]
    public void PositiveMoneyKeepsFighting()
    {
        machine.Start(0);
        Assert.IsFalse(machine.OnMoneyChanged(25f));
        Assert.AreEqual(BossPhase.Phase2, machine.Phase);
    }

    [Test]
    public void LosingMoneyAfterWinDoesNotLose()
    {
        machine.Start(0);
        machine.DamageWitch(100f);
        Assert.IsFalse(machine.OnMoneyChanged(-10f));
        Assert.AreEqual(BossPhase.Won, machine.Phase);
    }

    [Test]
    public void LostIsFinal()
    {
        machine.Start(1);
        machine.OnMoneyChanged(0f);
        Assert.IsFalse(machine.SetMonstersAlive(0));
        Assert.AreEqual(BossPhase.Lost, machine.Phase);
    }

    [TestCase(BossPhase.Phase1, 3, "Kill the monster customers (3 left)")]
    [TestCase(BossPhase.Phase1, 1, "Kill the monster customers (1 left)")]
    [TestCase(BossPhase.Phase2, 0, "Kill the Witch")]
    [TestCase(BossPhase.Intro, 3, "")]
    [TestCase(BossPhase.Won, 0, "")]
    public void ObjectiveText(BossPhase phase, int monsters, string expected)
    {
        Assert.AreEqual(expected, BossHudText.Objective(phase, monsters));
    }

    [TestCase(575f, "Money left: $575")]
    [TestCase(600.4f, "Money left: $600")]
    public void ScoreText(float money, string expected)
    {
        Assert.AreEqual(expected, BossHudText.Score(money));
    }
}
