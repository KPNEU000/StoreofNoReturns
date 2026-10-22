using BossFight;
using BossFight.Rules;
using NUnit.Framework;
using UnityEngine;

public class SwordPoseTests
{
    private const float Hit = 0.15f / 0.65f;

    private static Vector3 Blade(float t) => SwordPose.Evaluate(t, Hit).rotation * Vector3.up;

    [TestCase(0f)]
    [TestCase(1f)]
    public void UprightAtRest(float t)
    {
        Assert.That(Vector3.Angle(Blade(t), Vector3.up), Is.LessThan(0.5f));
        Assert.That(SwordPose.Evaluate(t, Hit).positionOffset.sqrMagnitude, Is.LessThan(1e-6f));
    }

    [Test]
    public void PointsForwardBeforeSweepStarts()
    {
        Vector3 blade = Blade(Hit * 0.7f);
        Assert.That(blade.z, Is.GreaterThan(0.45f), "tip points away from the camera");
        Assert.That(blade.y, Is.EqualTo(0f).Within(0.05f), "blade is level");
    }

    [Test]
    public void SweepGoesRightToLeftInFrontOfPlayer()
    {
        float start = Hit * 0.7f;
        float end = Hit + (1f - Hit) * 0.2f;
        float previousX = float.MaxValue;
        for (int i = 0; i <= 10; i++)
        {
            Vector3 blade = Blade(Mathf.Lerp(start, end, i / 10f));
            Assert.That(blade.z, Is.GreaterThan(0.4f), "blade stays in front");
            Assert.That(blade.y, Is.EqualTo(0f).Within(0.05f), "sweep is horizontal");
            Assert.That(blade.x, Is.LessThanOrEqualTo(previousX + 1e-4f));
            previousX = blade.x;
        }
        Assert.That(Blade(start).x, Is.GreaterThan(0.5f));
        Assert.That(Blade(end).x, Is.LessThan(-0.5f));
    }

    [Test]
    public void YawIsRightOfCentreAtStartAndLeftAtEnd()
    {
        float yawStart = SwordPose.Evaluate(Hit * 0.7f, Hit).rotation.eulerAngles.y;
        float yawEnd = SwordPose.Evaluate(Hit + (1f - Hit) * 0.2f, Hit).rotation.eulerAngles.y;
        Assert.That(Mathf.DeltaAngle(0f, yawStart), Is.GreaterThan(45f));
        Assert.That(Mathf.DeltaAngle(0f, yawEnd), Is.LessThan(-45f));
    }

    // Builds the real sword with PlayerToolsBuilder and returns the TrailRenderer's emitter position in camera space.
    private static Vector3 Tip(GameObject camera, Transform pivot, Transform tip, Vector3 rest, float t)
    {
        PlayerSword.ApplyPose(pivot, rest, Vector3.zero, t, Hit);
        return camera.transform.InverseTransformPoint(tip.position);
    }

    [Test]
    public void TipArcsInFrontOfPlayerFromRightToLeft()
    {
        var camera = new GameObject("TestCamera");
        try
        {
            bool changed = false;
            BossFight.Editor.PlayerToolsBuilder.BuildSword(camera.transform, null, null, ref changed);
            var pivot = camera.transform.Find(BossFight.Editor.PlayerToolsBuilder.SwordName + "/" + BossFight.Editor.PlayerToolsBuilder.SwordPivotName);
            var trail = pivot.GetComponentInChildren<TrailRenderer>();
            Assert.That(trail, Is.Not.Null, "builder puts the trail on the blade tip");
            AssertArc(camera, pivot, trail.transform);
        }
        finally { Object.DestroyImmediate(camera); }
    }

    private static void AssertArc(GameObject camera, Transform pivot, Transform tipTransform)
    {
        Vector3 rest = pivot.localPosition;
        float start = Hit * 0.7f;
        float end = Hit + (1f - Hit) * 0.2f;
        System.Func<float, Vector3> Tip = t => SwordPoseTests.Tip(camera, pivot, tipTransform, rest, t);
        float previousX = float.MaxValue;
        for (int i = 0; i <= 10; i++)
        {
            Vector3 tip = Tip(Mathf.Lerp(start, end, i / 10f));
            Assert.That(tip.z, Is.GreaterThan(0.9f), "trail stays well in front of the camera");
            Assert.That(tip.x, Is.LessThanOrEqualTo(previousX + 1e-4f));
            previousX = tip.x;
        }
        Assert.That(Tip(start).x, Is.GreaterThan(0.3f), "starts to the right");
        Assert.That(Tip(end).x, Is.LessThan(-0.3f), "ends to the left");
        Assert.That(Tip(Hit).z, Is.GreaterThan(Tip(start).z), "arc bulges forward through the hit");
    }
}
