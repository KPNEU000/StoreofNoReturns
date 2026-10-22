using BossFight;
using BossFight.Rules;
using NUnit.Framework;
using UnityEngine;

public class TutorialTests
{
    private static readonly Bounds Desk = new Bounds(new Vector3(0f, 0.5f, 2f), new Vector3(4f, 1f, 1.5f));

    [TestCase(0f, 1f)]
    [TestCase(0.3f, 1f)]
    [TestCase(-0.3f, 1f)]
    public void VaultLandsPastTheFarEdgeOfTheDesk(float x, float z)
    {
        var forward = new Vector3(x, 0f, z).normalized;
        var player = new Vector3(0f, 1f, 0f);
        var target = TutorialRules.VaultTarget(player, forward, Desk, 1.2f);
        Assert.IsFalse(new Bounds(Desk.center, Desk.size + new Vector3(0f, 10f, 0f)).Contains(target));
        Assert.Greater(Vector3.Dot(target - player, forward), 2.75f);
        Assert.AreEqual(player.y, target.y);
    }

    [Test]
    public void VaultTargetBuilderValueIsBeyondTheDeskOnTheZAxis()
    {
        var target = TutorialRules.VaultTarget(new Vector3(0f, 1f, 0f), Vector3.forward, Desk, 1f);
        Assert.AreEqual(Desk.max.z + 1f, target.z, 1e-4f);
    }

    [Test]
    public void PassCrossesInFrontOfThePlayerFromOneSideToTheOther()
    {
        var player = new Vector3(2f, 1f, 3f);
        TutorialRules.PassEndpoints(player, Vector3.forward, 3.5f, 3f, out var from, out var to);
        Assert.AreEqual(player.z + 3.5f, from.z, 1e-4f);
        Assert.AreEqual(player.z + 3.5f, to.z, 1e-4f);
        Assert.Less(from.x, player.x);
        Assert.Greater(to.x, player.x);
    }

    [Test]
    public void HarmlessCostumeCannotHurtThePlayer()
    {
        Assert.IsFalse(TutorialRules.CanHurtPlayer(true));
        Assert.IsTrue(TutorialRules.CanHurtPlayer(false));
    }

    [Test]
    public void MakeHarmlessSetsTheFlagOnTheCostume()
    {
        var go = new GameObject("costume");
        try
        {
            go.AddComponent<UnityEngine.AI.NavMeshAgent>();
            var costume = go.AddComponent<PossessedClothing>();
            Assert.IsFalse(costume.Harmless);
            costume.MakeHarmless(Vector3.zero, Vector3.right);
            Assert.IsTrue(costume.Harmless);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
