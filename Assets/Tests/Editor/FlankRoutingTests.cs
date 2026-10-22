using System.Collections.Generic;
using BossFight;
using BossFight.Rules;
using NUnit.Framework;
using UnityEngine;

public class FlankRoutingTests
{
    private FlankRouting routing;

    [SetUp]
    public void SetUp()
    {
        routing = new FlankRouting(new BossFightSettings());
    }

    [TestCase(Flank.Left)]
    [TestCase(Flank.Right)]
    [TestCase(Flank.Wide)]
    [TestCase(Flank.Direct)]
    public void CollapsesToPlayerInsideDirectRadius(Flank flank)
    {
        var player = new Vector3(2f, 0f, 3f);
        var result = routing.Destination(player, player + new Vector3(3f, 0f, 0f), flank);
        Assert.AreEqual(player, result);
    }

    [Test]
    public void DirectAlwaysTargetsPlayer()
    {
        var result = routing.Destination(Vector3.zero, new Vector3(0f, 0f, 20f), Flank.Direct);
        Assert.AreEqual(Vector3.zero, result);
    }

    [TestCase(Flank.Left)]
    [TestCase(Flank.Right)]
    [TestCase(Flank.Wide)]
    public void FlankDestinationIsOffsetFromPlayer(Flank flank)
    {
        var result = routing.Destination(Vector3.zero, new Vector3(0f, 0f, 20f), flank);
        Assert.Greater(result.magnitude, 1f);
        Assert.AreEqual(0f, result.y, 1e-4f);
    }

    [Test]
    public void LeftAndRightMirrorEachOther()
    {
        var left = routing.Destination(Vector3.zero, new Vector3(0f, 0f, 20f), Flank.Left);
        var right = routing.Destination(Vector3.zero, new Vector3(0f, 0f, 20f), Flank.Right);
        Assert.AreEqual(-left.x, right.x, 1e-3f);
        Assert.AreEqual(left.z, right.z, 1e-3f);
        Assert.AreNotEqual(left.x, right.x);
    }

    [Test]
    public void WideEndsBehindThePlayer()
    {
        var wide = routing.Destination(Vector3.zero, new Vector3(0f, 0f, 20f), Flank.Wide);
        Assert.Less(wide.z, 0f);
    }

    [Test]
    public void AssignmentsSpreadEvenlyAcrossAGroup()
    {
        var counts = new Dictionary<Flank, int>();
        for (int i = 0; i < 12; i++)
        {
            var flank = FlankRouting.Assign(i);
            counts[flank] = counts.TryGetValue(flank, out var n) ? n + 1 : 1;
        }
        Assert.AreEqual(4, counts.Count);
        foreach (var count in counts.Values) Assert.AreEqual(3, count);
    }
}
