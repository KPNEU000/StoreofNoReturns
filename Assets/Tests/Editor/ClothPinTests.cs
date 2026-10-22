using System.Collections.Generic;
using System.Linq;
using BossFight;
using BossFight.Editor;
using BossFight.Rules;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClothPinTests
{
    private SceneSetup[] previousSetup;

    [SetUp]
    public void SaveSceneSetup()
    {
        previousSetup = EditorSceneManager.GetSceneManagerSetup();
    }

    [TearDown]
    public void RestoreSceneSetup()
    {
        if (previousSetup.Length > 0 && previousSetup.All(s => !string.IsNullOrEmpty(s.path)))
        {
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }
        else if (SceneManager.sceneCount > 1)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    [TestCase(1f, 0.15f, true)]
    [TestCase(0.85f, 0.15f, true)]
    [TestCase(0.84f, 0.15f, false)]
    [TestCase(0f, 0.15f, false)]
    [TestCase(0.5f, 0.5f, true)]
    [TestCase(0f, 1f, true)]
    public void TopBandIsPinned(float height, float band, bool pinned)
    {
        Assert.AreEqual(pinned, ClothPinRules.IsPinned(height, 0f, 1f, band));
    }

    [Test]
    public void PinnedVerticesGetZeroDistanceAndTheRestSwing()
    {
        var vertices = new List<Vector3>();
        for (int i = 0; i <= 20; i++) vertices.Add(new Vector3(i % 3, -0.8f + i * 0.08f, 0f)); // -0.8 .. 0.8
        var distances = ClothPinRules.MaxDistances(vertices, 0.15f, 0.35f);
        for (int i = 0; i < vertices.Count; i++)
        {
            bool top = vertices[i].y >= 0.8f - 0.15f * 1.6f - 1e-4f;
            Assert.AreEqual(top ? 0f : 0.35f, distances[i], 1e-6f, $"vertex {i} at y {vertices[i].y}");
        }
        Assert.AreEqual(4, ClothPinRules.PinnedCount(distances));
    }

    [Test]
    public void FlatGarmentIsFullyPinnedAndEmptyIsEmpty()
    {
        var flat = new[] { Vector3.zero, Vector3.right, Vector3.forward };
        Assert.AreEqual(3, ClothPinRules.PinnedCount(ClothPinRules.MaxDistances(flat, 0.15f, 0.3f)));
        Assert.AreEqual(0, ClothPinRules.MaxDistances(new Vector3[0], 0.15f, 0.3f).Length);
    }

    [Test]
    public void ClusteringFitsTheParticleBudgetWithoutDegenerateTriangles()
    {
        const int n = 40;
        var positions = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            positions.Add(new Vector3(x * 0.01f, y * 0.01f, 0f));
            uvs.Add(new Vector2(x, y) / (n - 1));
            if (x < n - 1 && y < n - 1)
            {
                int i = y * n + x;
                tris.AddRange(new[] { i, i + n, i + 1, i + 1, i + n, i + n + 1 });
            }
        }
        var result = GarmentMeshRules.ClusterToBudget(positions, uvs, new[] { tris.ToArray() }, 100);
        int particles = GarmentMeshRules.ParticleCount(result.Positions);
        Assert.LessOrEqual(particles, 100);
        Assert.Greater(particles, 20);
        Assert.Greater(result.Triangles[0].Length, 0);
        var t = result.Triangles[0];
        for (int i = 0; i < t.Length; i += 3)
        {
            Assert.AreNotEqual(result.Positions[t[i]], result.Positions[t[i + 1]]);
            Assert.AreNotEqual(result.Positions[t[i + 1]], result.Positions[t[i + 2]]);
            Assert.AreNotEqual(result.Positions[t[i]], result.Positions[t[i + 2]]);
        }
        Assert.AreEqual(result.Positions.Length, result.Members.Length);
        // The coarse sheet still spans the plane and its UVs still span the texture.
        var size = new Bounds(result.Positions[0], Vector3.zero);
        foreach (var p in result.Positions) size.Encapsulate(p);
        Assert.Greater(size.size.x, 0.3f);
        Assert.Greater(size.size.y, 0.3f);
        Assert.Greater(result.Uvs.Max(uv => uv.x) - result.Uvs.Min(uv => uv.x), 0.8f);
    }

    [Test]
    public void UvSeamsSplitVerticesButShareTheParticle()
    {
        // Two triangles meeting at the same corner positions but on far-apart UV charts.
        var positions = new[] { new Vector3(0.005f, 0.005f, 0f), new Vector3(0.105f, 0.005f, 0f), new Vector3(0.005f, 0.105f, 0f), new Vector3(0.006f, 0.006f, 0f), new Vector3(0.106f, 0.006f, 0f), new Vector3(0.006f, 0.106f, 0f) };
        var uvs = new[] { Vector2.zero, new Vector2(0.1f, 0f), new Vector2(0f, 0.1f), new Vector2(0.9f, 0.9f), new Vector2(1f, 0.9f), new Vector2(0.9f, 1f) };
        var result = GarmentMeshRules.Cluster(positions, uvs, new[] { new[] { 0, 1, 2, 3, 4, 5 } }, 0.05f);
        Assert.AreEqual(6, result.Positions.Length, "seam keeps separate UV vertices");
        Assert.AreEqual(3, GarmentMeshRules.ParticleCount(result.Positions), "welded into three particles");
        Assert.AreEqual(6, result.Triangles[0].Length);
    }

    [Test]
    public void RepeatedCoarseTrianglesAreDroppedOnce()
    {
        var positions = new[] { new Vector3(0.01f, 0.01f, 0f), new Vector3(0.11f, 0.01f, 0f), new Vector3(0.01f, 0.11f, 0f), new Vector3(0.02f, 0.02f, 0f), new Vector3(0.12f, 0.02f, 0f), new Vector3(0.02f, 0.12f, 0f) };
        var uvs = new Vector2[6];
        var result = GarmentMeshRules.Cluster(positions, uvs, new[] { new[] { 0, 1, 2, 4, 5, 3 } }, 0.05f);
        Assert.AreEqual(3, result.Triangles[0].Length);
    }

    [Test]
    public void EachRackTypeReleasesItsOwnBakedGarment()
    {
        var settings = BossFightConfig.SettingsOf(ConfigBuilder.Load());
        using (var boss = new BossSceneScope())
        {
            var costumes = new HashSet<string>();
            var garments = new HashSet<GameObject>();
            foreach (var rack in GarmentClothBuilder.Racks(boss.Scene))
            {
                var hung = GarmentClothBuilder.HungCostumes(rack.transform);
                Assert.AreEqual(hung.Count, rack.Garments.Length, rack.name);
                for (int i = 0; i < hung.Count; i++)
                {
                    var costume = hung[i].costume;
                    Assert.AreEqual(costume, rack.CostumePrefabs[i], rack.name);
                    Assert.IsNotNull(rack.Garments[i], rack.name);
                    Assert.AreEqual(GarmentClothBuilder.GarmentPrefabPath(costume), AssetDatabase.GetAssetPath(rack.Garments[i]), rack.name);
                    costumes.Add(costume.name);
                    garments.Add(rack.Garments[i]);
                }
            }
            foreach (var expected in new[] { "SuperheroSuit", "CatCostume_untorn", "ZombieRags", "WizardRobe", "BirdCostume" })
            {
                Assert.IsTrue(costumes.Any(c => c.StartsWith(expected)), $"no rack releases {expected}; found {string.Join(", ", costumes)}");
            }
            Assert.AreEqual(costumes.Count, garments.Count, "each costume type has its own garment");

            foreach (var garment in garments)
            {
                var skin = garment.GetComponent<SkinnedMeshRenderer>();
                Assert.IsNotNull(skin, garment.name);
                Assert.AreEqual(0, skin.bones.Length, garment.name);
                var mesh = skin.sharedMesh;
                Assert.IsTrue(mesh.isReadable, garment.name);
                Assert.LessOrEqual(GarmentMeshRules.ParticleCount(mesh.vertices), settings.clothMaxParticles, garment.name);
                Assert.AreEqual(mesh.subMeshCount, skin.sharedMaterials.Length, garment.name);
                // Hung garments are centred on the hanger and taller than they are deep.
                Assert.Less(mesh.bounds.center.magnitude, 0.01f, garment.name);
                Assert.Greater(mesh.bounds.size.y, 0.4f, garment.name);
                Assert.Greater(mesh.bounds.size.y, mesh.bounds.size.z * 0.8f, garment.name);
            }
        }
    }

    [Test]
    public void FallbackGarmentIsTheClothingPrefabsOwnCostume()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ClothingEnemyBuilder.ClothingPrefab);
        var clothing = prefab.GetComponent<PossessedClothing>();
        Assert.IsNotNull(clothing.Garment);
        var costume = new SerializedObject(clothing).FindProperty("costumePrefab").objectReferenceValue as GameObject;
        Assert.AreEqual(GarmentClothBuilder.GarmentPrefabPath(costume), AssetDatabase.GetAssetPath(clothing.Garment));
    }

    [Test]
    public void DressOverridesTheFallbackGarment()
    {
        var go = new GameObject("Costume");
        var other = new GameObject("RackGarment");
        try
        {
            go.AddComponent<UnityEngine.AI.NavMeshAgent>();
            var clothing = go.AddComponent<PossessedClothing>();
            Assert.IsNull(clothing.Garment);
            clothing.Dress(null);
            Assert.IsNull(clothing.Garment);
            clothing.Dress(other);
            Assert.AreEqual(other, clothing.Garment);
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(other);
        }
    }

    [Test]
    public void AttachedClothHangsFromTheTopBandAndDrapesOnThePlayerOnly()
    {
        var settings = new BossFightSettings();
        var garment = AssetDatabase.LoadAssetAtPath<GameObject>(GarmentClothBuilder.PrefabFolder + "/WizardRobeGarment.prefab");
        Assert.IsNotNull(garment);
        var instance = Object.Instantiate(garment);
        var player = new GameObject("Player");
        try
        {
            var controller = player.AddComponent<CharacterController>();
            var capsule = GarmentCloth.PlayerCapsule(player.transform);
            Assert.IsTrue(capsule.isTrigger);
            Assert.AreEqual(controller.height, capsule.height);
            Assert.AreSame(capsule, GarmentCloth.PlayerCapsule(player.transform), "re-uses the existing capsule");

            var cloth = GarmentCloth.Attach(instance.GetComponent<SkinnedMeshRenderer>(), settings, capsule);
            Assert.AreEqual(settings.clothSolverFrequency, cloth.clothSolverFrequency);
            Assert.LessOrEqual(cloth.clothSolverFrequency, 60f);
            Assert.AreEqual(0f, cloth.selfCollisionDistance);
            Assert.AreEqual(0f, cloth.useVirtualParticles);
            Assert.AreEqual(1, cloth.capsuleColliders.Length);
            Assert.AreSame(capsule, cloth.capsuleColliders[0]);
            Assert.AreEqual(0, cloth.sphereColliders.Length);
            var coefficients = cloth.coefficients;
            Assert.Greater(coefficients.Length, 0);
            Assert.LessOrEqual(coefficients.Length, settings.clothMaxParticles * 4);
            int pinned = coefficients.Count(c => c.maxDistance == 0f);
            int free = coefficients.Count(c => Mathf.Approximately(c.maxDistance, settings.clothFreeDistance));
            Assert.Greater(pinned, 0, "top band pinned to the hanger");
            Assert.Greater(free, pinned, "most of the garment simulates");
            Assert.AreEqual(coefficients.Length, pinned + free);
        }
        finally
        {
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(player);
        }
    }
}
