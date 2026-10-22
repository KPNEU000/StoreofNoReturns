using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BossFight.Editor
{
    // The CaveInController object on the Boss Scene plus its concrete material (procedural noise texture), purple hand
    // and dust materials. Scene references are assigned by exact name; a missing Ceiling fails loudly.
    public static class CaveInBuilder
    {
        public const string ObjectName = "CaveInController";
        public const string CeilingName = "Ceiling";
        public const string ConcreteTexture = BossFightPaths.SettingsFolder + "/ChunkConcrete.png";
        public const string ConcreteMaterial = BossFightPaths.SettingsFolder + "/ChunkConcrete.mat";
        public const string HandMaterial = BossFightPaths.SettingsFolder + "/CaveInHand.mat";
        public const string DustMaterial = BossFightPaths.SettingsFolder + "/CaveInDust.mat";
        private const int TextureSize = 128;

        public static void Build()
        {
            var config = ConfigBuilder.Load();
            var concrete = EnsureConcreteMaterial();
            var hand = BuilderUtil.EnsureTransparentMaterial(HandMaterial, "Universal Render Pipeline/Unlit", new Color(0.55f, 0.15f, 0.8f, 0.95f));
            var dust = BuilderUtil.EnsureTransparentMaterial(DustMaterial, "Universal Render Pipeline/Particles/Unlit", new Color(0.6f, 0.6f, 0.6f, 0.6f));

            using (var boss = new BossSceneScope())
            {
                var ceiling = BuilderUtil.FindByName(boss.Scene, CeilingName);
                if (ceiling == null) throw new System.InvalidOperationException($"Boss Scene has no '{CeilingName}' object for the cave-in");
                var director = BuilderUtil.FindInScene<BossDirector>(boss.Scene);
                var money = BuilderUtil.FindInScene<MoneyManager>(boss.Scene);
                var witch = BuilderUtil.FindInScene<WitchBoss>(boss.Scene);
                var player = BuilderUtil.FindPlayer(boss.Scene);
                if (director == null || money == null || witch == null || player == null)
                {
                    throw new System.InvalidOperationException("Cave-in builder needs the BossDirector, MoneyManager, Witch and player in Boss Scene");
                }

                bool changed = false;
                var controller = BuilderUtil.FindInScene<CaveInController>(boss.Scene);
                if (controller == null)
                {
                    var go = new GameObject(ObjectName);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, boss.Scene);
                    controller = go.AddComponent<CaveInController>();
                    changed = true;
                }
                var so = new SerializedObject(controller);
                changed |= BuilderUtil.SetReference(so, "config", config);
                changed |= BuilderUtil.SetReference(so, "director", director);
                changed |= BuilderUtil.SetReference(so, "moneyManager", money);
                changed |= BuilderUtil.SetReference(so, "player", player.GetComponent<PlayerMovement>());
                changed |= BuilderUtil.SetReference(so, "witch", witch.transform);
                changed |= BuilderUtil.SetReference(so, "ceiling", ceiling);
                changed |= BuilderUtil.SetReference(so, "chunkMaterial", concrete);
                changed |= BuilderUtil.SetReference(so, "handMaterial", hand);
                changed |= BuilderUtil.SetReference(so, "dustMaterial", dust);
                changed |= so.ApplyModifiedPropertiesWithoutUndo();

                if (changed) EditorSceneManager.MarkSceneDirty(boss.Scene);
                boss.SaveIfDirty();
            }
        }

        private static Material EnsureConcreteMaterial()
        {
            return BuilderUtil.EnsureLitMaterial(ConcreteMaterial, new Color(0.62f, 0.62f, 0.64f), material =>
            {
                material.SetTexture("_BaseMap", EnsureNoiseTexture());
                material.SetFloat("_Smoothness", 0.1f);
            });
        }

        // Seeded value noise in greys, written once and imported as a normal texture.
        private static Texture2D EnsureNoiseTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(ConcreteTexture);
            if (existing != null) return existing;
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            var rng = new System.Random(1337);
            var grid = new float[17, 17];
            for (int x = 0; x < 17; x++) for (int y = 0; y < 17; y++) grid[x, y] = (float)rng.NextDouble();
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float coarse = Sample(grid, x / 8f, y / 8f);
                    float fine = (float)rng.NextDouble();
                    float v = Mathf.Clamp01(0.55f + (coarse - 0.5f) * 0.5f + (fine - 0.5f) * 0.25f);
                    texture.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            }
            texture.Apply();
            File.WriteAllBytes(ConcreteTexture, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(ConcreteTexture);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ConcreteTexture);
        }

        private static float Sample(float[,] grid, float x, float y)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            float a = Mathf.Lerp(grid[x0, y0], grid[x0 + 1, y0], fx);
            float b = Mathf.Lerp(grid[x0, y0 + 1], grid[x0 + 1, y0 + 1], fx);
            return Mathf.Lerp(a, b, fy);
        }

        public static void Validate(UnityEngine.SceneManagement.Scene scene, List<string> errors)
        {
            foreach (var path in new[] { ConcreteMaterial, HandMaterial, DustMaterial })
            {
                if (AssetDatabase.LoadAssetAtPath<Material>(path) == null) errors.Add($"Material missing at {path}");
            }
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(ConcreteTexture) == null) errors.Add($"Texture missing at {ConcreteTexture}");
            var controller = BuilderUtil.FindInScene<CaveInController>(scene);
            if (controller == null)
            {
                errors.Add($"Boss Scene has no {ObjectName} with a CaveInController component");
                return;
            }
            var so = new SerializedObject(controller);
            foreach (var field in new[] { "config", "director", "moneyManager", "player", "witch", "ceiling", "chunkMaterial", "handMaterial", "dustMaterial" })
            {
                if (so.FindProperty(field).objectReferenceValue == null) errors.Add($"Boss Scene CaveInController: {field} reference is missing");
            }
        }
    }
}
