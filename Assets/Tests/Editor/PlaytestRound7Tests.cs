using BossFight;
using NUnit.Framework;
using UnityEngine.AI;

// Rules added after the seventh playtest.
public class PlaytestRound7Tests
{
    [Test]
    public void IndoorMaskExcludesOnlyTheOutsideArea()
    {
        Assert.AreEqual(0, NavAgents.IndoorMask & NavAgents.OutsideMask);
        Assert.AreEqual(NavMesh.AllAreas, NavAgents.IndoorMask | NavAgents.OutsideMask);
        Assert.AreNotEqual(0, NavAgents.IndoorMask & 1, "ordinary walkable floor stays allowed");
    }

    [Test]
    public void RoundSevenTuning()
    {
        var s = new BossFightSettings();
        Assert.AreEqual(0.75f, s.invincibilitySeconds, 1e-5f); // round 8
        Assert.AreEqual(5f, s.waveNextDelaySeconds, 1e-5f); // round 11
        Assert.AreEqual(4.5f, s.rackTelegraphSeconds, 1e-5f); // round 10
        Assert.AreEqual(8.4f, s.bookFlightSpeed, 1e-4f);
        Assert.AreEqual(45f * 1.05f, s.flashlightSpotAngle, 1e-4f);
        Assert.AreEqual(6, BossFight.Rules.WaveScheduler.SwarmSize(s, 50), "six costumes at medium reputation");
    }
}
