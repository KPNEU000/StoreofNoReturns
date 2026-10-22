using System.Collections.Generic;
using System.Linq;
using BossFight;
using BossFight.Editor;
using BossFight.Rules;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class GlassHudTests
{
    private readonly List<Object> created = new List<Object>();
    private SceneSetup[] previousSetup;

    [SetUp]
    public void SaveSceneSetup()
    {
        previousSetup = EditorSceneManager.GetSceneManagerSetup();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var o in created)
        {
            if (o != null) Object.DestroyImmediate(o);
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

    [Test]
    public void ShardSitsTopRightAndIsALittleUnderAQuarterOfTheScreen()
    {
        var s = new BossFightSettings();
        var rect = GlassShardRules.ScreenRect(s.glassHudWidth, s.glassHudHeight, s.glassHudMargin);
        float area = rect.width * rect.height;
        Assert.Less(area, 0.25f);
        Assert.Greater(area, 0.18f);
        Assert.AreEqual(1f - s.glassHudMargin, rect.xMax, 1e-5f);
        Assert.AreEqual(1f - s.glassHudMargin, rect.yMax, 1e-5f);
        Assert.Greater(rect.xMin, 0.5f, "stays in the right half");
        Assert.Greater(rect.yMin, 0.5f, "stays in the top half");
    }

    [Test]
    public void DistortionIsZeroAtTheCentreOneAtTheEdgeAndGrowsFasterTowardTheEdge()
    {
        float exponent = new BossFightSettings().glassEdgeFalloffExponent;
        Assert.AreEqual(0f, GlassShardRules.EdgeFalloff(0f, exponent), 1e-6f);
        Assert.AreEqual(1f, GlassShardRules.EdgeFalloff(1f, exponent), 1e-6f);
        Assert.Less(GlassShardRules.EdgeFalloff(0.5f, exponent), 0.5f);
        float previous = 0f, previousStep = 0f;
        for (int i = 1; i <= 10; i++)
        {
            float value = GlassShardRules.EdgeFalloff(i / 10f, exponent);
            Assert.Greater(value, previous);
            Assert.Greater(value - previous, previousStep, "each step outward adds more distortion than the last");
            previousStep = value - previous;
            previous = value;
        }
    }

    [TestCase(7)]
    [TestCase(42)]
    public void OutlineIsAJaggedStarShapeInsideTheUnitSquare(int seed)
    {
        var s = new BossFightSettings();
        var outline = GlassShardRules.Outline(seed, s.glassHudOutlinePoints, s.glassHudJaggedness);
        Assert.AreEqual(s.glassHudOutlinePoints, outline.Length);
        float previousAngle = -1f;
        var radii = new List<float>();
        foreach (var p in outline)
        {
            Assert.LessOrEqual(Mathf.Abs(p.x), 0.5f + 1e-5f);
            Assert.LessOrEqual(Mathf.Abs(p.y), 0.5f + 1e-5f);
            float angle = Mathf.Atan2(p.y, p.x);
            if (angle < 0f) angle += Mathf.PI * 2f;
            Assert.Greater(angle, previousAngle, "points go round the centre once, so a fan from the centre covers it");
            previousAngle = angle;
            // Fraction of the way out to the square's edge in this direction.
            radii.Add(Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) / 0.5f);
        }
        Assert.Greater(radii.Max() - radii.Min(), 0.08f, "the edge zig-zags");
        CollectionAssert.AreEqual(outline, GlassShardRules.Outline(seed, s.glassHudOutlinePoints, s.glassHudJaggedness));
        CollectionAssert.AreNotEqual(outline, GlassShardRules.Outline(seed + 1, s.glassHudOutlinePoints, s.glassHudJaggedness));
    }

    [Test]
    public void ZeroJaggednessWithFourPointsIsTheFullRectangle()
    {
        var outline = GlassShardRules.Outline(3, 4, 0f);
        foreach (var p in outline)
        {
            Assert.AreEqual(0.5f, Mathf.Abs(p.x), 1e-5f);
            Assert.AreEqual(0.5f, Mathf.Abs(p.y), 1e-5f);
        }
    }

    [Test]
    public void MeshEdgeWeightRisesRingByRingToOneOnTheOutline()
    {
        var s = new BossFightSettings();
        var outline = GlassShardRules.Outline(s.glassHudSeed, s.glassHudOutlinePoints, s.glassHudJaggedness);
        GlassShardRules.BuildMesh(outline, s.glassEdgeFalloffExponent, out var vertices, out var uv, out var edge, out var triangles);
        int n = outline.Length;
        int rings = GlassShardRules.RingFractions.Length - 1;
        Assert.AreEqual(1 + rings * n, vertices.Length);
        Assert.AreEqual(0f, edge[0].x);
        for (int r = 0; r < rings; r++)
        {
            float weight = edge[1 + r * n].x;
            for (int i = 0; i < n; i++) Assert.AreEqual(weight, edge[1 + r * n + i].x, 1e-6f);
            if (r > 0) Assert.Greater(weight, edge[1 + (r - 1) * n].x);
        }
        for (int i = 0; i < n; i++)
        {
            Assert.AreEqual(1f, edge[1 + (rings - 1) * n + i].x, 1e-6f);
            Assert.AreEqual((Vector2)vertices[1 + (rings - 1) * n + i], outline[i]);
        }
        Assert.IsTrue(uv.All(u => u.x >= 0f && u.x <= 1f && u.y >= 0f && u.y <= 1f));

        // Every triangle faces the camera (clockwise seen from behind the shard) and together they cover the polygon.
        Assert.AreEqual(0, triangles.Length % 3);
        float meshArea = 0f;
        for (int t = 0; t < triangles.Length; t += 3)
        {
            Vector2 a = vertices[triangles[t]], b = vertices[triangles[t + 1]], c = vertices[triangles[t + 2]];
            float cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            Assert.Less(cross, 0f);
            meshArea += -cross * 0.5f;
        }
        float polygonArea = 0f;
        for (int i = 0; i < n; i++)
        {
            var p = outline[i];
            var q = outline[(i + 1) % n];
            polygonArea += (p.x * q.y - q.x * p.y) * 0.5f;
        }
        Assert.AreEqual(polygonArea, meshArea, 1e-4f);
    }

    [TestCase(60f, 16f / 9f)]
    [TestCase(75f, 4f / 3f)]
    public void CameraPlacementCoversTheViewportRect(float fov, float aspect)
    {
        var go = new GameObject("GlassTestCamera");
        created.Add(go);
        var camera = go.AddComponent<Camera>();
        camera.fieldOfView = fov;
        camera.aspect = aspect;
        var rect = GlassShardRules.ScreenRect(0.46f, 0.46f, 0.01f);
        GlassShardRules.CameraPlacement(rect, fov, aspect, 0.32f, out var position, out var size);
        var min = camera.WorldToViewportPoint(go.transform.TransformPoint(position - (Vector3)(size * 0.5f)));
        var max = camera.WorldToViewportPoint(go.transform.TransformPoint(position + (Vector3)(size * 0.5f)));
        Assert.AreEqual(rect.xMin, min.x, 1e-3f);
        Assert.AreEqual(rect.yMin, min.y, 1e-3f);
        Assert.AreEqual(rect.xMax, max.x, 1e-3f);
        Assert.AreEqual(rect.yMax, max.y, 1e-3f);
        Assert.AreEqual(0.32f, min.z, 1e-4f);
    }

    [Test]
    public void GlassShardTurnsOnTheCameraOpaqueTexture()
    {
        var go = new GameObject("GlassTestCamera");
        created.Add(go);
        var camera = go.AddComponent<Camera>();
        GlassShard.EnableOpaqueTexture(camera);
        var data = go.GetComponent<UniversalAdditionalCameraData>();
        Assert.IsNotNull(data);
        Assert.IsTrue(data.requiresColorTexture);
    }

    [Test]
    public void GlassShaderCompilesWithRefractionEdgeAndCrackTextureSlots()
    {
        var shader = Shader.Find(OpeningBuilder.GlassShader);
        Assert.IsNotNull(shader);
        Assert.IsFalse(ShaderUtil.ShaderHasError(shader), string.Join("\n", ShaderUtil.GetShaderMessages(shader).Select(m => m.message)));
        foreach (var property in new[] { "_Refraction", "_BlurRadius", "_SheenStrength", "_CrackTex", "_CrackCoverage", "_ZTest" })
        {
            Assert.GreaterOrEqual(shader.FindPropertyIndex(property), 0, property);
        }
    }

    [Test]
    public void BuiltMeshMatchesTheRuleAndCarriesEdgeWeights()
    {
        var mesh = GlassShard.BuildMesh(new BossFightSettings(), true);
        created.Add(mesh);
        var s = new BossFightSettings();
        Assert.AreEqual(1 + (GlassShardRules.RingFractions.Length - 1) * s.glassHudOutlinePoints, mesh.vertexCount);
        var edge = new List<Vector2>();
        mesh.GetUVs(1, edge);
        Assert.AreEqual(1f, edge.Max(e => e.x), 1e-6f);
        Assert.AreEqual(0f, edge.Min(e => e.x), 1e-6f);
    }

    [Test]
    public void BossSceneHasTheGlassHudAndCrackedMonitorAfterBuild()
    {
        BuildAll.Run();
        using (var boss = new BossSceneScope())
        {
            var errors = new List<string>();
            OpeningBuilder.Validate(boss.Scene, errors);
            CollectionAssert.IsEmpty(errors);

            var settings = BossFightConfig.SettingsOf(ConfigBuilder.Load());
            var rect = GlassShardRules.ScreenRect(settings.glassHudWidth, settings.glassHudHeight, settings.glassHudMargin);
            var overlay = BuilderUtil.FindInScene<ScreenCrackOverlay>(boss.Scene);
            Assert.AreEqual(rect.min, overlay.Frame.anchorMin, "the text frame covers the same rect as the shard");
            Assert.AreEqual(rect.max, overlay.Frame.anchorMax);
            Assert.IsNull(overlay.transform.Find(OpeningBuilder.CrackImageName), "the old white crack is gone");

            var hud = new SerializedObject(BuilderUtil.FindInScene<BossHud>(boss.Scene));
            foreach (var field in new[] { "moneyText", "objectiveText", "promptText" })
            {
                var label = (TMP_Text)hud.FindProperty(field).objectReferenceValue;
                Assert.AreEqual(overlay.transform, label.transform.parent, field);
            }
            var money = (TMP_Text)hud.FindProperty("moneyText").objectReferenceValue;
            var objective = (TMP_Text)hud.FindProperty("objectiveText").objectReferenceValue;
            Assert.Greater(money.fontSize, objective.fontSize, "money is the large line");

            var player = BuilderUtil.FindPlayer(boss.Scene);
            var cameraTransform = (Transform)new SerializedObject(player).FindProperty("cameraTrans").objectReferenceValue;
            var hudGlass = (GlassShard)new SerializedObject(overlay).FindProperty("glass").objectReferenceValue;
            Assert.AreEqual(cameraTransform, hudGlass.transform.parent);
            Assert.AreEqual(OpeningBuilder.GlassShader, hudGlass.GetComponent<MeshRenderer>().sharedMaterial.shader.name);

            var sequence = new SerializedObject(BuilderUtil.FindInScene<OpeningSequence>(boss.Scene));
            var monitor = (Transform)sequence.FindProperty("monitor").objectReferenceValue;
            var monitorGlass = (GlassShard)sequence.FindProperty("monitorGlass").objectReferenceValue;
            Assert.IsTrue(monitorGlass.transform.IsChildOf(monitor), "the cracked glass sits on the monitor's own screen");
            Assert.IsNotNull(monitorGlass.GetComponentInParent<Canvas>(true));
            Assert.AreEqual(hudGlass.GetComponent<MeshRenderer>().sharedMaterial.shader, monitorGlass.GetComponent<MeshRenderer>().sharedMaterial.shader,
                "monitor and HUD use the same cracked-glass shader");
            Assert.IsTrue(monitor.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled && r.GetComponent<GlassShard>() == null), "the computer body stays");
        }
    }
}
