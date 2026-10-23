using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Enzo's 2026-10-23 shop balance, computed from MainScene's real customers and costume assets:
// $1350 start; approving every return ends at $550, perfect play at $1050; reputation 50, perfect 80, rejecting
// everything 0. Reputation can only clamp at 0, never end the run.
public class ShopBalanceTests
{
    private const string MainScenePath = "Assets/Scenes/Scenes in Game/MainScene.unity";

    private struct Shop
    {
        public float startMoney;
        public int startRep, reward, denialPenalty, minRep, maxRep;
        public List<(string name, float refund, bool valid)> customers;
    }

    private static Shop ReadShop()
    {
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);
        try
        {
            var shop = new Shop { customers = new List<(string, float, bool)>() };
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var money in root.GetComponentsInChildren<MoneyManager>(true))
                {
                    shop.startMoney = new SerializedObject(money).FindProperty("startingMoney").floatValue;
                }
                foreach (var rep in root.GetComponentsInChildren<ReputationManager>(true))
                {
                    var so = new SerializedObject(rep);
                    shop.startRep = so.FindProperty("startingReputation").intValue;
                    shop.reward = so.FindProperty("correctRefundReward").intValue;
                    shop.denialPenalty = so.FindProperty("unfairDenialPenalty").intValue;
                    shop.minRep = so.FindProperty("minimumReputation").intValue;
                    shop.maxRep = so.FindProperty("maximumReputation").intValue;
                }
                foreach (var spawner in root.GetComponentsInChildren<CustomerSpawner>(true))
                {
                    var list = new SerializedObject(spawner).FindProperty("customerPrefabs");
                    for (int i = 0; i < list.arraySize; i++)
                    {
                        var go = (GameObject)list.GetArrayElementAtIndex(i).objectReferenceValue;
                        var customer = go.GetComponent<CustomerScript>();
                        var costume = customer.getCostume();
                        // Valid = no reason to refuse. (Only Eric is "genuine", and he has no reasons to clear.)
                        shop.customers.Add((go.name, costume.getReturnValue(), costume.getReasons().Count == 0));
                    }
                }
            }
            return shop;
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void ApprovingEverythingEndsAt550AndPerfectPlayAt1050()
    {
        var shop = ReadShop();
        Assert.AreEqual(1350f, shop.startMoney);
        Assert.AreEqual(6, shop.customers.Count);
        float all = 0f, valid = 0f;
        foreach (var c in shop.customers)
        {
            Assert.GreaterOrEqual(c.refund, 0f, $"{c.name}'s refund must cost money, never pay out");
            all += c.refund;
            if (c.valid) valid += c.refund;
        }
        Assert.AreEqual(550f, shop.startMoney - all, "approve every return");
        Assert.AreEqual(1050f, shop.startMoney - valid, "approve only the valid returns");
    }

    [Test]
    public void ReputationStarts50PerfectReaches80RejectingEverythingHits0()
    {
        var shop = ReadShop();
        Assert.AreEqual(50, shop.startRep);
        int validCount = shop.customers.FindAll(c => c.valid).Count;
        Assert.AreEqual(80, Mathf.Clamp(shop.startRep + validCount * shop.reward, shop.minRep, shop.maxRep), "perfect play");
        Assert.AreEqual(0, Mathf.Clamp(shop.startRep - validCount * shop.denialPenalty, shop.minRep, shop.maxRep), "reject everything");
        Assert.AreEqual(0, shop.minRep, "reputation clamps at 0 and nothing ends the run on it");
    }
}
