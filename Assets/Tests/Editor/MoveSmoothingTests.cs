using BossFight.Rules;
using NUnit.Framework;
using UnityEngine;

public class MoveSmoothingTests
{
    const float Speed = 10f;

    [TestCase(0.1f, 60)]
    [TestCase(0.1f, 30)]
    [TestCase(0.2f, 60)]
    public void ReleasingKeyReachesZeroWithinAccelTime(float accel, int fps)
    {
        var s = new MoveSmoothing(accel);
        float dt = 1f / fps;
        var v = new Vector2(Speed, 0f);
        float t = 0f;
        while (t < accel - 1e-4f)
        {
            v = s.Approach(v, Vector2.zero, Speed, dt);
            t += dt;
        }
        Assert.AreEqual(0f, v.magnitude, 1e-3f);
    }

    [Test]
    public void PressingKeyReachesFullSpeedWithinAccelTime()
    {
        var s = new MoveSmoothing(0.1f);
        var v = Vector2.zero;
        for (int i = 0; i < 6; i++) v = s.Approach(v, new Vector2(0, Speed), Speed, 1f / 60f);
        Assert.AreEqual(Speed, v.y, 1e-3f);
    }

    [Test]
    public void ZeroAccelTimeSnapsToTarget()
    {
        var s = new MoveSmoothing(0f);
        Assert.AreEqual(new Vector2(3, 4), s.Approach(Vector2.zero, new Vector2(3, 4), Speed, 0.016f));
    }

    [TestCase(0.016f)]
    [TestCase(0.1f)]
    public void LookDeltaIgnoresFrameTime(float _)
    {
        Assert.AreEqual(new Vector2(5f, -2f), MoveSmoothing.LookDelta(new Vector2(50f, -20f), 0.1f));
    }

    [TestCase(100f, 0.195f)]
    [TestCase(0f, 0.195f)]
    [TestCase(0.3f, 0.3f)]
    public void LegacySensitivityIsNormalized(float input, float expected)
    {
        Assert.AreEqual(expected, MoveSmoothing.NormalizeSensitivity(input), 1e-6f);
    }
}
