using System.Collections.Generic;
using BossFight;
using BossFight.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

public class ChunkPlacementTests
{
    private BossFightSettings settings;

    [SetUp]
    public void SetUp()
    {
        settings = new BossFightSettings();
    }

    private ChunkPlacement Make() => new ChunkPlacement(settings, () => 0.5f);

    private static List<Vector3> Line(int count, float spacing, float startX = 10f)
    {
        var points = new List<Vector3>();
        for (int i = 0; i < count; i++) points.Add(new Vector3(startX + i * spacing, 0f, 0f));
        return points;
    }

    [TestCase(-4, 3)]
    [TestCase(0, 3)]
    [TestCase(3, 3)]
    [TestCase(5, 5)]
    [TestCase(10, 10)]
    [TestCase(40, 10)]
    public void CountIsClampedToThreeThroughTen(int requested, int expected)
    {
        settings.caveInChunkCount = requested;
        Assert.AreEqual(expected, Make().Count);
    }

    [Test]
    public void DefaultCountIsFive()
    {
        Assert.AreEqual(5, Make().Count);
    }

    [Test]
    public void RejectsPointsCloserToPlayerThanClearance()
    {
        var spots = Make().Place(new[] { new Vector3(1f, 0f, 1f), new Vector3(settings.caveInPlayerClearance - 0.1f, 0f, 0f) }, Vector3.zero);
        Assert.IsEmpty(spots);
    }

    [Test]
    public void AcceptsPointAtExactlyTheClearance()
    {
        var spots = Make().Place(new[] { new Vector3(settings.caveInPlayerClearance, 0f, 0f) }, Vector3.zero);
        Assert.AreEqual(1, spots.Count);
    }

    [Test]
    public void ClearanceIgnoresHeight()
    {
        var spots = Make().Place(new[] { new Vector3(1f, 50f, 0f) }, Vector3.zero);
        Assert.IsEmpty(spots);
    }

    [Test]
    public void RejectsOverlappingChunksButAllowsCloseOnes()
    {
        float twoRadii = settings.caveInChunkRadius * 2f;
        var candidates = new[]
        {
            new Vector3(10f, 0f, 0f),
            new Vector3(10f + twoRadii - 0.1f, 0f, 0f),
            new Vector3(10f + twoRadii + 0.05f, 0f, 0f)
        };
        var spots = Make().Place(candidates, Vector3.zero);
        Assert.AreEqual(2, spots.Count);
        Assert.AreEqual(candidates[0], spots[0].Position);
        Assert.AreEqual(candidates[2], spots[1].Position);
    }

    [Test]
    public void StopsAtCount()
    {
        var spots = Make().Place(Line(30, 3f), Vector3.zero);
        Assert.AreEqual(5, spots.Count);
    }

    [Test]
    public void ReturnsFewerWhenCandidatesRunOut()
    {
        Assert.AreEqual(2, Make().Place(Line(2, 4f), Vector3.zero).Count);
    }

    [Test]
    public void DelaysSpreadEvenlyOverTheDuration()
    {
        var spots = Make().Place(Line(10, 3f), Vector3.zero);
        Assert.AreEqual(0f, spots[0].Delay, 1e-4f);
        Assert.AreEqual(2f, spots[1].Delay, 1e-4f);
        Assert.AreEqual(8f, spots[4].Delay, 1e-4f);
        Assert.Less(spots[4].Delay, settings.caveInDurationSeconds);
    }

    [Test]
    public void YawComesFromRandomSource()
    {
        var spots = Make().Place(Line(3, 3f), Vector3.zero);
        Assert.AreEqual(180f, spots[0].Yaw, 1e-3f);
    }

    [Test]
    public void OnlyCompletePathsKeepTheChunk()
    {
        Assert.IsFalse(ChunkPlacement.Crumbles(NavMeshPathStatus.PathComplete));
        Assert.IsTrue(ChunkPlacement.Crumbles(NavMeshPathStatus.PathPartial));
        Assert.IsTrue(ChunkPlacement.Crumbles(NavMeshPathStatus.PathInvalid));
    }

    [Test]
    public void TriggerFiresOnceAtOrUnderThresholdDuringTheFight()
    {
        var trigger = new CaveInTrigger(settings);
        Assert.IsFalse(trigger.Check(251f, BossPhase.Phase2));
        Assert.IsFalse(trigger.Check(100f, BossPhase.Intro));
        Assert.IsFalse(trigger.Check(100f, BossPhase.Won));
        Assert.IsTrue(trigger.Check(250f, BossPhase.Phase2));
        Assert.IsFalse(trigger.Check(100f, BossPhase.Phase2));
        Assert.IsTrue(trigger.Triggered);
    }

    [Test]
    public void TriggerAlsoFiresInPhase1()
    {
        Assert.IsTrue(new CaveInTrigger(settings).Check(200f, BossPhase.Phase1));
    }

    [Test]
    public void ChunkMeshIsDeterministicPerSeed()
    {
        var a = ChunkMesh.Build(7, 1.8f, 1.3f, 0.1f);
        var b = ChunkMesh.Build(7, 1.8f, 1.3f, 0.1f);
        var c = ChunkMesh.Build(8, 1.8f, 1.3f, 0.1f);
        CollectionAssert.AreEqual(a.vertices, b.vertices);
        CollectionAssert.AreNotEqual(a.vertices, c.vertices);
    }

    [Test]
    public void ChunkMeshSitsOnTheFloorAndStaysBelowEyeHeight()
    {
        var mesh = ChunkMesh.Build(3, 1.8f, settings.caveInChunkRestHeight, 0.1f);
        Assert.AreEqual(0f, mesh.bounds.min.y, 1e-4f);
        Assert.Less(mesh.bounds.max.y, settings.caveInChunkRestHeight + 0.11f);
        Assert.Less(mesh.bounds.max.y, 1.5f);
    }

    [Test]
    public void ChunkMeshIsWatertightAcrossFaceSeams()
    {
        var mesh = ChunkMesh.Build(5, 1.8f, 1.3f, 0.1f);
        var unique = new HashSet<Vector3>(mesh.vertices);
        int n = ChunkMesh.Subdivisions;
        Assert.AreEqual(6 * (n + 1) * (n + 1), mesh.vertexCount);
        // Surface lattice of an n-cube grid: (n+1)^3 - (n-1)^3 distinct points once the seams coincide.
        Assert.AreEqual((n + 1) * (n + 1) * (n + 1) - (n - 1) * (n - 1) * (n - 1), unique.Count);
    }
}
