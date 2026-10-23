using System.Linq;
using BossFight.Rules;
using NUnit.Framework;
using UnityEngine;

public class CrackPatternTests
{
    [Test]
    public void SameSeedGivesSameCracks()
    {
        var a = CrackPattern.Generate(5, new Vector2(0.5f, 0.5f), 6, 6, 0.9f);
        var b = CrackPattern.Generate(5, new Vector2(0.5f, 0.5f), 6, 6, 0.9f);
        Assert.AreEqual(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++) Assert.AreEqual(a[i].B, b[i].B);
    }

    [Test]
    public void DifferentSeedsDiffer()
    {
        var a = CrackPattern.Generate(1, new Vector2(0.5f, 0.5f), 6, 6, 0.9f);
        var b = CrackPattern.Generate(2, new Vector2(0.5f, 0.5f), 6, 6, 0.9f);
        Assert.IsFalse(a.Select(s => s.B).SequenceEqual(b.Select(s => s.B)));
    }

    [Test]
    public void SegmentsStayInsideUnitSquare()
    {
        var segments = CrackPattern.Generate(9, new Vector2(0.88f, 0.86f), 9, 7, 1.1f);
        Assert.IsNotEmpty(segments);
        foreach (var s in segments)
        {
            Assert.That(s.B.x, Is.InRange(0f, 1f));
            Assert.That(s.B.y, Is.InRange(0f, 1f));
            Assert.Greater(s.Width, 0f);
        }
    }

    [Test]
    public void RasterizeDrawsSomethingAndLeavesRestTransparent()
    {
        var segments = CrackPattern.Generate(3, new Vector2(0.5f, 0.5f), 6, 6, 0.9f);
        var pixels = CrackPattern.Rasterize(64, segments, Color.white);
        Assert.AreEqual(64 * 64, pixels.Length);
        int drawn = pixels.Count(p => p.a > 0);
        Assert.Greater(drawn, 20);
        Assert.Less(drawn, pixels.Length / 2);
    }
}
