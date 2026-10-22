using System.Collections.Generic;
using BossFight;
using BossFight.Rules;
using NUnit.Framework;
using UnityEngine;

public class CaveInFixTests
{
    private BossFightSettings settings;

    [SetUp]
    public void SetUp()
    {
        settings = new BossFightSettings();
    }

    [Test]
    public void RoofPointsAreNeverChosen()
    {
        var placement = new ChunkPlacement(settings, () => 0.5f);
        var candidates = new List<Vector3>();
        for (int i = 0; i < 12; i++) candidates.Add(new Vector3(10f + i * 5f, i % 2 == 0 ? 4.2f : 0f, 0f));
        var spots = placement.Place(candidates, Vector3.zero, 0f);
        Assert.IsNotEmpty(spots);
        foreach (var spot in spots) Assert.LessOrEqual(spot.Position.y, ChunkPlacement.FloorTolerance);
    }

    [Test]
    public void OnlyRoofCandidatesYieldNoSpots()
    {
        var placement = new ChunkPlacement(settings, () => 0.5f);
        var candidates = new List<Vector3> { new Vector3(10f, 4f, 0f), new Vector3(20f, 4f, 5f) };
        Assert.IsEmpty(placement.Place(candidates, Vector3.zero, 0.1f));
    }

    [Test]
    public void ChunksAreBigButStillChestHeight()
    {
        Assert.AreEqual(1.6f, settings.caveInChunkRadius, 1e-4f);
        var mesh = ChunkMesh.Build(11, settings.caveInChunkRadius * 2f, settings.caveInChunkRestHeight, 0.3f);
        Assert.Less(mesh.bounds.max.y, 1.7f);
        Assert.Greater(mesh.bounds.size.x, 3f);
    }

    [Test]
    public void ChunkMeshIsFinelySubdividedAndIrregular()
    {
        Assert.GreaterOrEqual(ChunkMesh.Subdivisions, 8);
        var mesh = ChunkMesh.Build(4, 3.2f, 1.3f, 0.3f);
        // A plain cube has all vertices on 6 planes; jagged side vertices spread over many distinct X values.
        var xs = new HashSet<float>();
        foreach (var v in mesh.vertices) xs.Add(Mathf.Round(v.x * 100f));
        Assert.Greater(xs.Count, 40);
    }
}
