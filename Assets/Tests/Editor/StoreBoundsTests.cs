using BossFight.Rules;
using NUnit.Framework;
using UnityEngine;

// The guard that puts the player back if anything ever moves them through a wall or off the floor.
public class StoreBoundsTests
{
    // Boss Scene's ceiling in world space: x -12..12, z -15.38..11.5.
    private static readonly Bounds Ceiling = new Bounds(new Vector3(0f, 7.75f, -1.94f), new Vector3(24f, 1f, 26.88f));

    [Test]
    public void PlayerStartBehindTheCounterAndTheWitchByTheDoorAreInside()
    {
        var footprint = StoreBounds.Footprint(Ceiling);
        Assert.IsTrue(StoreBounds.Inside(footprint, new Vector3(0.09f, 1.59f, 11.08f), 0.25f), "start position records as safe");
        Assert.IsTrue(StoreBounds.Inside(footprint, new Vector3(-3.9f, 0.59f, -13.07f), 0f));
    }

    [Test]
    public void PushedThroughAWallMustBeRestored()
    {
        var footprint = StoreBounds.Footprint(Ceiling);
        Assert.IsTrue(StoreBounds.MustRestore(footprint, true, new Vector3(12.6f, 1.6f, 0f), 1.6f), "outside the side wall");
        Assert.IsTrue(StoreBounds.MustRestore(footprint, true, new Vector3(0f, 1.6f, 12.2f), 1.6f), "out the back wall");
        Assert.IsFalse(StoreBounds.MustRestore(footprint, true, new Vector3(11.9f, 1.6f, 0f), 1.6f), "hugging the wall is fine");
    }

    [Test]
    public void FallingBelowTheFloorMustBeRestored()
    {
        var footprint = StoreBounds.Footprint(Ceiling);
        Assert.IsTrue(StoreBounds.MustRestore(footprint, true, new Vector3(0f, -1.5f, 0f), 1.6f));
        Assert.IsFalse(StoreBounds.MustRestore(footprint, true, new Vector3(0f, 1.2f, 0f), 1.6f), "small drops are fine");
    }

    [Test]
    public void WithoutAFootprintOnlyFallingRestores()
    {
        Assert.IsFalse(StoreBounds.MustRestore(default, false, new Vector3(500f, 1.6f, 500f), 1.6f));
        Assert.IsTrue(StoreBounds.MustRestore(default, false, new Vector3(0f, -5f, 0f), 1.6f));
    }
}
