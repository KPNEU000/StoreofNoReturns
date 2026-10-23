using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace BossFight.Editor
{
    // Builds the PossessedClothing prefab and places the ClothingSpawner (with a spawn point) in the Boss Scene.
    public static class ClothingEnemyBuilder
    {
        public const string SpawnerName = "ClothingSpawner";
        public const string SpawnPointName = "ClothingSpawnPoint";
        public const string ClothingPrefab = BossFightPaths.PrefabsFolder + "/PossessedClothing.prefab";
        private const string CostumePrefab = "Assets/Prefabs/Costumes/Evil Blood Wizard.prefab"; // renamed from WizardRobe on main

        public static void Build()
        {
            var config = ConfigBuilder.Load();
            EditorFolders.Ensure(BossFightPaths.PrefabsFolder);
            var prefab = EnsurePrefab(config);

            using (var boss = new BossSceneScope())
            {
                var player = BuilderUtil.FindPlayer(boss.Scene);
                if (player == null)
                {
                    throw new System.InvalidOperationException("Boss Scene has no PlayerMovement object to place the clothing spawn point near");
                }
                bool changed = false;
                var spawner = FindSpawner(boss.Scene);
                if (spawner == null)
                {
                    var go = new GameObject(SpawnerName);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, boss.Scene);
                    spawner = go.AddComponent<ClothingSpawner>();
                    changed = true;
                }
                var point = spawner.transform.Find(SpawnPointName);
                if (point == null)
                {
                    point = new GameObject(SpawnPointName).transform;
                    point.SetParent(spawner.transform, false);
                    point.position = player.transform.position + player.transform.forward * 6f;
                    changed = true;
                }

                var so = new SerializedObject(spawner);
                changed |= BuilderUtil.SetReference(so, "config", config);
                changed |= BuilderUtil.SetReference(so, "prefab", prefab.GetComponent<PossessedClothing>());
                changed |= BuilderUtil.SetReference(so, "spawnPoint", point);
                changed |= so.ApplyModifiedPropertiesWithoutUndo();

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(boss.Scene);
                }
                boss.SaveIfDirty();
            }
        }

        public static ClothingSpawner FindSpawner(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var spawner = root.GetComponentInChildren<ClothingSpawner>(true);
                if (spawner != null)
                {
                    return spawner;
                }
            }
            return null;
        }

        // Called by BossRigValidator with the Boss Scene open.
        public static void Validate(UnityEngine.SceneManagement.Scene scene, List<string> errors)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ClothingPrefab);
            if (prefab == null)
            {
                errors.Add($"PossessedClothing prefab missing at {ClothingPrefab}");
            }
            else
            {
                var clothing = prefab.GetComponent<PossessedClothing>();
                if (clothing == null)
                {
                    errors.Add("PossessedClothing prefab has no PossessedClothing component");
                }
                else
                {
                    BuilderUtil.RequireFields(clothing, "PossessedClothing prefab", new[] { "config", "costumePrefab" }, errors);
                }
                if (prefab.GetComponent<NavMeshAgent>() == null)
                {
                    errors.Add("PossessedClothing prefab has no NavMeshAgent");
                }
            }

            var spawner = FindSpawner(scene);
            if (spawner == null)
            {
                errors.Add($"Boss Scene has no {SpawnerName} with a ClothingSpawner component");
            }
            else
            {
                BuilderUtil.RequireFields(spawner, "Boss Scene ClothingSpawner", new[] { "config", "prefab", "spawnPoint" }, errors);
            }
        }

        private static GameObject EnsurePrefab(BossFightConfig config)
        {
            var costume = AssetDatabase.LoadAssetAtPath<GameObject>(CostumePrefab);
            if (costume == null)
            {
                throw new System.InvalidOperationException($"Costume prefab missing at {CostumePrefab}");
            }
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(ClothingPrefab) != null;
            var root = exists ? PrefabUtility.LoadPrefabContents(ClothingPrefab) : new GameObject("PossessedClothing");
            try
            {
                bool changed = !exists;
                var agent = root.GetComponent<NavMeshAgent>();
                if (agent == null)
                {
                    agent = root.AddComponent<NavMeshAgent>();
                    agent.radius = 0.4f;
                    agent.height = 1.8f;
                    agent.acceleration = 20f;
                    agent.angularSpeed = 360f;
                    agent.stoppingDistance = 0.5f;
                    changed = true;
                }
                var clothing = root.GetComponent<PossessedClothing>();
                if (clothing == null)
                {
                    clothing = root.AddComponent<PossessedClothing>();
                    changed = true;
                }
                var so = new SerializedObject(clothing);
                changed |= BuilderUtil.SetReference(so, "config", config);
                changed |= BuilderUtil.SetReference(so, "costumePrefab", costume);
                changed |= so.ApplyModifiedPropertiesWithoutUndo();
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, ClothingPrefab);
                }
            }
            finally
            {
                if (exists)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                else
                {
                    Object.DestroyImmediate(root);
                }
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(ClothingPrefab);
        }
    }
}
