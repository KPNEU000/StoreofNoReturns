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
}
