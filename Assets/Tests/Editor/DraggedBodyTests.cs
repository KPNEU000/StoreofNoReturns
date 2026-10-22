using BossFight;
using BossFight.Rules;
using NUnit.Framework;
using UnityEngine;

public class DraggedBodyTests
{
    private class FakeMonster : IDamageable
    {
        public float Taken;
        public int Hits;
        public bool Alive = true;
        public bool IsAlive => Alive;

        public void TakeDamage(float amount, Vector3 from)
        {
            Taken += amount;
            Hits++;
        }
    }

    [TestCase(10f)]
    [TestCase(0.5f)]
    [TestCase(33f)]
    public void BodyDamageEqualsMonsterDamage(float amount)
    {
        var monster = new FakeMonster();
        Assert.IsTrue(DraggedBodyRules.Forward(monster, amount, Vector3.zero));
        Assert.AreEqual(amount, monster.Taken);
        Assert.AreEqual(1, monster.Hits);
    }

    [Test]
    public void DeadOrMissingMonsterTakesNothing()
    {
        var dead = new FakeMonster { Alive = false };
        Assert.IsFalse(DraggedBodyRules.Forward(dead, 10f, Vector3.zero));
        Assert.AreEqual(0, dead.Hits);
        Assert.IsFalse(DraggedBodyRules.Forward(null, 10f, Vector3.zero));
    }

    [Test]
    public void HitboxForwardsToItsMonster()
    {
        var monster = new FakeMonster();
        var go = new GameObject("hitbox");
        try
        {
            var hitbox = go.AddComponent<BodyHitbox>();
            hitbox.Bind(monster, 2f);
            hitbox.TakeDamage(10f, Vector3.back);
            Assert.AreEqual(10f, monster.Taken);
            Assert.IsTrue(hitbox.IsAlive);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [TestCase(31)]
    [TestCase(9)]
    [TestCase(30)]
    public void IgnoresEveryLayerExceptItsOwn(int bodyLayer)
    {
        var ignored = DraggedBodyRules.LayersToIgnore(bodyLayer);
        Assert.IsFalse(ignored.Contains(bodyLayer));
        Assert.AreEqual(31, ignored.Count);
        Assert.IsTrue(ignored.Contains(0), "Default holds walls, shelves, monsters and the player");
    }

    [Test]
    public void CollisionMatrixSetupIgnoresEverythingButItself()
    {
        const int bodyLayer = 31;
        DraggedBodyRules.ConfigureCollisionMatrix(bodyLayer);
        try
        {
            Assert.IsFalse(Physics.GetIgnoreLayerCollision(bodyLayer, bodyLayer));
            for (int layer = 0; layer < 31; layer++)
            {
                Assert.IsTrue(Physics.GetIgnoreLayerCollision(bodyLayer, layer), $"layer {layer}");
            }
        }
        finally
        {
            for (int layer = 0; layer < 32; layer++) Physics.IgnoreLayerCollision(bodyLayer, layer, false);
        }
    }

    [Test]
    public void FloorHeightUsesGroundHitElseFallback()
    {
        Assert.AreEqual(0.2f, DraggedBodyRules.FloorHeight(true, 0.2f, 1f));
        Assert.AreEqual(1f, DraggedBodyRules.FloorHeight(false, 0.2f, 1f));
    }

    [Test]
    public void SwayStaysWithinAmplitude()
    {
        for (float t = 0f; t < 5f; t += 0.1f)
        {
            Assert.LessOrEqual(Mathf.Abs(DraggedBodyRules.SwayAngle(t, 12f, 0.8f)), 12f + 1e-4f);
        }
        Assert.AreEqual(0f, DraggedBodyRules.SwayAngle(0f, 12f, 0.8f), 1e-4f);
    }

    [TestCase("ZombieRags", "Zombie", true)]
    [TestCase("WizardRobe", "Cat", false)]
    [TestCase("CatCostume", "cat(Clone)", true)]
    [TestCase("CatCostume", "", false)]
    public void CostumeMatchesCustomerName(string costume, string customer, bool expected)
    {
        Assert.AreEqual(expected, MonsterDrag.NamesMatch(costume, customer));
    }
}
