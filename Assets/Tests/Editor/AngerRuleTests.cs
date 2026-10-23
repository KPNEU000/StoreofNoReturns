using System.Linq;
using BossFight;
using BossFight.Rules;
using NUnit.Framework;

public class AngerRuleTests
{
    [SetUp]
    public void SetUp()
    {
        AngeredCustomerRegistry.Reset();
    }

    [TestCase(100, 90, true)]
    [TestCase(100, 99, true)]
    [TestCase(100, 100, false)]
    [TestCase(90, 91, false)]
    public void IsAngered_OnlyWhenReputationNetDrops(int start, int end, bool expected)
    {
        Assert.AreEqual(expected, AngerRule.IsAngered(start, end));
    }

    [Test]
    public void Registry_CountsOnlyAngeredCustomers()
    {
        AngeredCustomerRegistry.Record("Alice", 100, 90);
        AngeredCustomerRegistry.Record("Bob", 100, 101);
        AngeredCustomerRegistry.Record("Carol", 50, 40);

        Assert.AreEqual(2, AngeredCustomerRegistry.Count);
        CollectionAssert.AreEquivalent(new[] { "Alice", "Carol" }, AngeredCustomerRegistry.Names.ToArray());
    }

    [Test]
    public void Registry_SameCustomerIsCountedOnce()
    {
        AngeredCustomerRegistry.Record("Alice", 100, 90);
        AngeredCustomerRegistry.Record("Alice", 90, 80);

        Assert.AreEqual(1, AngeredCustomerRegistry.Count);
    }

    [Test]
    public void Registry_LatestInteractionWins()
    {
        AngeredCustomerRegistry.Record("Alice", 100, 90);
        AngeredCustomerRegistry.Record("Alice", 90, 90);

        Assert.AreEqual(0, AngeredCustomerRegistry.Count);
        Assert.IsFalse(AngeredCustomerRegistry.IsAngered("Alice"));
    }

    [Test]
    public void Reset_ClearsEverything()
    {
        AngeredCustomerRegistry.Record("Alice", 100, 90);
        AngeredCustomerRegistry.Reset();

        Assert.AreEqual(0, AngeredCustomerRegistry.Count);
        Assert.IsEmpty(AngeredCustomerRegistry.Names.ToArray());
    }
}
