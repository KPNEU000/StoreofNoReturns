using BossFight;
using BossFight.Rules;
using NUnit.Framework;
using UnityEngine;

public class EnemyDeathTests
{
    [Test]
    public void ClothingVanishesAndMonsterRagdolls()
    {
        Assert.AreEqual(DeathOutcome.Vanish, EnemyDeathRules.OutcomeFor(EnemyKind.PossessedClothing));
        Assert.AreEqual(DeathOutcome.Ragdoll, EnemyDeathRules.OutcomeFor(EnemyKind.Monster));
        Assert.IsFalse(EnemyDeathRules.LeavesCorpse(EnemyKind.PossessedClothing));
        Assert.IsTrue(EnemyDeathRules.LeavesCorpse(EnemyKind.Monster));
    }

    [TestCase(0f, 0f, false)]
    [TestCase(1f, 0f, false)]
    [TestCase(2f, 3f, false)]
    [TestCase(2f, 0.01f, true)]
    [TestCase(7f, 5f, true)]
    public void SettlesWhenStillOrTimedOut(float elapsed, float speed, bool expected)
    {
        Assert.AreEqual(expected, EnemyDeathRules.HasSettled(elapsed, speed));
    }

    [TestCase("mixamorig:Hips", "Hips", true)]
    [TestCase("mixamorigHips", "Hips", true)]
    [TestCase("mixamorig:Hips1", "Hips", false)]
    [TestCase("mixamorig:LeftLeg", "LeftUpLeg", false)]
    public void BoneNamesMatchMixamo(string name, string bone, bool expected)
    {
        Assert.AreEqual(expected, EnemyDeathRules.IsBone(name, bone));
    }

    [Test]
    public void RagdollBoneTreeIsConnected()
    {
        var names = new System.Collections.Generic.HashSet<string>();
        foreach (var bone in EnemyDeathRules.Bones) names.Add(bone.Name);
        Assert.IsTrue(names.Contains("Hips") && names.Contains("Spine") && names.Contains("Head"));
        foreach (var bone in EnemyDeathRules.Bones)
        {
            if (bone.Name != "Hips") Assert.IsTrue(names.Contains(bone.Parent), bone.Name);
            Assert.Greater(bone.Mass, 0f);
        }
    }

    [Test]
    public void RagdollBuildsBodiesAndJointsFromMixamoBones()
    {
        var root = new GameObject("Rig");
        try
        {
            var parent = root.transform;
            Transform hips = null;
            var made = new System.Collections.Generic.Dictionary<string, Transform>();
            foreach (var spec in EnemyDeathRules.Bones)
            {
                var go = new GameObject(EnemyDeathRules.BonePrefix + spec.Name);
                var p = spec.Parent == "" ? parent : made[spec.Parent];
                go.transform.SetParent(p, false);
                go.transform.localPosition = new Vector3(0f, 0.3f, 0f);
                made[spec.Name] = go.transform;
                if (spec.Name == "Hips") hips = go.transform;
            }
            var ragdoll = root.AddComponent<MonsterRagdoll>();
            var other = new GameObject("Other").AddComponent<BoxCollider>();
            ragdoll.Activate(null, new Collider[0], new Collider[] { other });
            Assert.IsTrue(ragdoll.Active);
            Assert.AreEqual(EnemyDeathRules.Bones.Count, ragdoll.BodyCount);
            Assert.IsNotNull(hips.GetComponent<Rigidbody>());
            Assert.IsFalse(hips.GetComponent<Rigidbody>().isKinematic);
            Assert.IsNotNull(made["Head"].GetComponent<CharacterJoint>());
            Object.DestroyImmediate(other.gameObject);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
