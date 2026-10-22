using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BossFight;
using BossFight.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneCleanupTests
{
    private readonly List<GameObject> created = new List<GameObject>();
    private SceneSetup[] previousSetup;

    [SetUp]
    public void SaveSceneSetup()
    {
        previousSetup = EditorSceneManager.GetSceneManagerSetup();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in created)
        {
            if (go != null) Object.DestroyImmediate(go);
        }
        created.Clear();
        // Same guard as BuildAllTests: an untitled scene has no path and cannot be restored by path.
        if (previousSetup.Length > 0 && previousSetup.All(s => !string.IsNullOrEmpty(s.path)))
        {
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }
        else if (SceneManager.sceneCount > 1)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    private GameObject Make(string name, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        else created.Add(go);
        return go;
    }

    private Light MakeLight(string name, LightType type, Transform parent = null)
    {
        var light = Make(name, parent).AddComponent<Light>();
        light.type = type;
        light.shadows = LightShadows.Soft;
        return light;
    }

    [Test]
    public void ShopShutdownSwitchesOffCustomersSpawnerAndClock()
    {
        var shopper = Make("Customer").AddComponent<CustomerBehaviour>();
        var shopWitch = Make("Witch").AddComponent<CustomerBehaviour>();
        var manager = Make("Game Manager");
        var clock = manager.AddComponent<TimeManager>();
        var spawner = Make("CustomerSpawn").AddComponent<CustomerSpawner>();

        int switchedOff = ShopShutdown.Apply(SceneManager.GetActiveScene());

        Assert.AreEqual(4, switchedOff);
        Assert.IsFalse(shopper.gameObject.activeSelf);
        Assert.IsFalse(shopWitch.gameObject.activeSelf, "the duplicate shop Witch customer must be off too");
        Assert.IsFalse(clock.enabled);
        Assert.IsTrue(manager.activeSelf, "Game Manager holds other shop components, so only TimeManager is disabled");
        Assert.IsFalse(spawner.enabled);
        Assert.AreEqual(0, ShopShutdown.Apply(SceneManager.GetActiveScene()), "a second pass changes nothing");
    }

    [Test]
    public void ShopShutdownLeavesTheBossWitchAlone()
    {
        var boss = Make("Witch").AddComponent<WitchBoss>();
        var nested = Make("Model", boss.transform).AddComponent<CustomerBehaviour>();

        ShopShutdown.Apply(SceneManager.GetActiveScene());

        Assert.IsTrue(nested.gameObject.activeSelf);
    }

    [Test]
    public void TimeManagerReachesClosingTimeWithoutSceneLoadingManager()
    {
        Assume.That(Object.FindFirstObjectByType<SceneLoadingManager>(), Is.Null);
        var clock = Make("Clock").AddComponent<TimeManager>();
        clock.endHour = 0;
        clock.endMinute = 0;
        typeof(TimeManager).GetProperty(nameof(TimeManager.CurrentHour)).SetValue(clock, 0);
        typeof(TimeManager).GetProperty(nameof(TimeManager.CurrentMinute)).SetValue(clock, 0);
        var check = typeof(TimeManager).GetMethod("CheckEndOfTime", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.DoesNotThrow(() => check.Invoke(clock, null));
        Assert.IsTrue((bool)typeof(TimeManager).GetField("shopClosed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(clock));
    }

    [Test]
    public void StoreLightsLoseShadowsAndFlashlightKeepsThem()
    {
        var rig = Make("BossFightRig").transform;
        var ceiling = MakeLight("CeilingLight", LightType.Point);
        var spot = MakeLight("CeilingSpot", LightType.Spot);
        var outside = MakeLight("OutsideLight", LightType.Point, rig);
        var sun = MakeLight("Sun", LightType.Directional);
        var flashlightRoot = Make("Flashlight");
        flashlightRoot.AddComponent<PlayerFlashlight>();
        var beam = MakeLight("FlashlightBeam", LightType.Spot, flashlightRoot.transform);
        var camera = Make("Camera").AddComponent<Camera>();
        var cameraLight = MakeLight("CameraLight", LightType.Point, camera.transform);

        var store = StoreLightingController.PrepareStoreLights(new[] { ceiling, spot, outside, sun, beam, cameraLight }, outside, rig);

        CollectionAssert.AreEquivalent(new[] { ceiling, spot }, store);
        Assert.AreEqual(LightShadows.None, ceiling.shadows);
        Assert.AreEqual(LightShadows.None, spot.shadows);
        Assert.AreEqual(LightShadows.Soft, beam.shadows, "the flashlight is the one shadow-casting light");
        Assert.AreEqual(LightShadows.Soft, cameraLight.shadows);
        Assert.AreEqual(LightShadows.Soft, sun.shadows);
    }

    [Test]
    public void FlashlightIsNeverAStoreLight()
    {
        var flashlightRoot = Make("Flashlight");
        flashlightRoot.AddComponent<PlayerFlashlight>();
        var beam = MakeLight("FlashlightBeam", LightType.Spot, flashlightRoot.transform);

        Assert.IsFalse(StoreLightingController.IsStoreCeilingLight(beam, null, null));
        Assert.IsTrue(StoreLightingController.IsStoreCeilingLight(MakeLight("Ceiling", LightType.Point), null, null));
    }

    [Test]
    public void BossSceneIsCleanAfterBuild()
    {
        BuildAll.Run();
        using (var boss = new BossSceneScope())
        {
            var errors = new List<string>();
            SceneCleanupBuilder.Validate(boss.Scene, errors);
            CollectionAssert.IsEmpty(errors);

            var roots = boss.Scene.GetRootGameObjects();
            var customers = roots.SelectMany(r => r.GetComponentsInChildren<CustomerBehaviour>(true)).ToList();
            Assert.IsNotEmpty(customers, "Boss Scene should still hold the shop customers, switched off");
            Assert.IsTrue(customers.All(c => !c.gameObject.activeSelf));
            Assert.IsTrue(roots.SelectMany(r => r.GetComponentsInChildren<TimeManager>(true)).All(t => !t.enabled));
            Assert.IsTrue(roots.SelectMany(r => r.GetComponentsInChildren<CustomerSpawner>(true)).All(s => !s.enabled));

            Assert.IsFalse(roots.Any(r => r.name.StartsWith(PlayerToolsBuilder.DummyName) || r.GetComponentInChildren<TrainingDummy>(true) != null),
                "the TrainingDummy blocked the first-person view and must be gone");

            var crack = BuilderUtil.FindInScene<ScreenCrackOverlay>(boss.Scene);
            Assert.IsNotNull(crack);
            var image = crack.GetComponentInChildren<RawImage>(true);
            Assert.IsNotNull(image);
            Assert.IsFalse(image.gameObject.activeSelf, "the untextured crack image draws as a white box until Show");
        }
    }
}
