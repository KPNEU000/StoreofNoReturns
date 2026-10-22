using BossFight;
using BossFight.Editor;
using BossFight.Rules;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class WitchEntranceTests
{
    private static readonly Vector3 DoorCenter = new Vector3(4f, 0f, -6f);

    [Test]
    public void BuildersTargetTheVisibleFrontDoor()
    {
        Assert.AreEqual("FrontDoor", BossDirectorBuilder.DoorName);
    }

    [TestCase(1f, 0f, 3f)]
    [TestCase(0f, 1f, -2f)]
    [TestCase(-0.7f, 0.7f, 5f)]
    public void WitchStandsClearOfTheDoorwayLane(float inwardX, float inwardZ, float playerSide)
    {
        var inward = new Vector3(inwardX, 0f, inwardZ).normalized;
        var lateral = Vector3.Cross(Vector3.up, inward);
        var player = DoorCenter + inward * 8f + lateral * playerSide;
        var position = DoorwayRules.WitchPosition(DoorCenter, inward, player, 1f, 1.1f, 1.5f, 0.75f);
        Assert.IsTrue(DoorwayRules.IsClearOfLane(position, 1.1f, DoorCenter, inward, 1f));
        Assert.AreEqual(DoorCenter.y, position.y);
    }

    [Test]
    public void WitchSitsOnThePlayersSideOfTheDoor()
    {
        var inward = Vector3.forward;
        var lateral = Vector3.Cross(Vector3.up, inward);
        var left = DoorwayRules.WitchPosition(DoorCenter, inward, DoorCenter + inward * 5f - lateral * 2f, 1f, 1f, 1f, 0.5f);
        var right = DoorwayRules.WitchPosition(DoorCenter, inward, DoorCenter + inward * 5f + lateral * 2f, 1f, 1f, 1f, 0.5f);
        Assert.Less(Vector3.Dot(left - DoorCenter, lateral), 0f);
        Assert.Greater(Vector3.Dot(right - DoorCenter, lateral), 0f);
    }

    [Test]
    public void WitchInTheMiddleOfTheLaneBlocksIt()
    {
        Assert.IsFalse(DoorwayRules.IsClearOfLane(DoorCenter + Vector3.forward * 2f, 0.5f, DoorCenter, Vector3.forward, 1f));
    }

    [Test]
    public void PathIsOpenOnlyWhenCompleteAndReachingTheTarget()
    {
        var target = new Vector3(0f, 0f, 5f);
        Assert.IsTrue(DoorwayRules.IsPathComplete(NavMeshPathStatus.PathComplete, target + Vector3.right * 0.1f, target, 0.5f));
        Assert.IsFalse(DoorwayRules.IsPathComplete(NavMeshPathStatus.PathPartial, target, target, 0.5f));
        Assert.IsFalse(DoorwayRules.IsPathComplete(NavMeshPathStatus.PathInvalid, target, target, 0.5f));
        Assert.IsFalse(DoorwayRules.IsPathComplete(NavMeshPathStatus.PathComplete, target + Vector3.right * 3f, target, 0.5f));
    }

    [Test]
    public void WitchStandsOnTheFloorByDefault()
    {
        Assert.AreEqual(0f, new BossFightSettings().witchHoverHeight);
        var config = AssetDatabase.LoadAssetAtPath<BossFightConfig>(BossFightPaths.ConfigAsset);
        if (config != null) Assert.AreEqual(0f, config.ToSettings().witchHoverHeight);
    }
}
