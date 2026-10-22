using BossFight;
using BossFight.Rules;
using NUnit.Framework;

public class ClothingBrainTests
{
    private BossFightSettings settings;
    private ClothingBrain brain;

    [SetUp]
    public void SetUp()
    {
        settings = new BossFightSettings();
        brain = new ClothingBrain(settings);
    }

    [Test]
    public void StartsChasingWithFullHealthAndCannotHit()
    {
        Assert.AreEqual(ClothingState.Chase, brain.State);
        Assert.AreEqual(10f, brain.Health);
        Assert.IsFalse(brain.CanDealHit);
    }

    [Test]
    public void ContactStartsLungeAndOnlyLungeCanHit()
    {
        Assert.IsTrue(brain.OnContact());
        Assert.AreEqual(ClothingState.Lunge, brain.State);
        Assert.IsTrue(brain.CanDealHit);
    }

    [Test]
    public void FullCycleReturnsToChase()
    {
        brain.OnContact();
        brain.Tick(brain.LungeSeconds + 0.01f);
        Assert.AreEqual(ClothingState.Flopped, brain.State);
        Assert.IsFalse(brain.CanDealHit);
        brain.Tick(settings.clothingFlopSeconds - 0.1f);
        Assert.AreEqual(ClothingState.Flopped, brain.State);
        brain.Tick(0.2f);
        Assert.AreEqual(ClothingState.Rising, brain.State);
        Assert.IsFalse(brain.CanDealHit);
        brain.Tick(5f);
        Assert.AreEqual(ClothingState.Chase, brain.State);
    }

    [Test]
    public void ContactIsIgnoredWhileFloppedOrRising()
    {
        brain.OnContact();
        brain.Tick(brain.LungeSeconds + 0.01f);
        Assert.IsFalse(brain.OnContact());
        Assert.AreEqual(ClothingState.Flopped, brain.State);
    }

    [Test]
    public void DiesAtZeroOfTenHealth()
    {
        brain.TakeDamage(9f);
        Assert.IsTrue(brain.IsAlive);
        brain.TakeDamage(1f);
        Assert.AreEqual(ClothingState.Dead, brain.State);
        Assert.IsFalse(brain.IsAlive);
        Assert.IsFalse(brain.CanDealHit);
    }

    [Test]
    public void SlashDamageKillsInOneHit()
    {
        brain.TakeDamage(settings.slashDamage);
        Assert.AreEqual(ClothingState.Dead, brain.State);
    }

    [Test]
    public void DeadStaysDeadAndIgnoresContactAndTicks()
    {
        brain.TakeDamage(100f);
        Assert.IsFalse(brain.OnContact());
        brain.Tick(10f);
        Assert.AreEqual(ClothingState.Dead, brain.State);
    }
}
