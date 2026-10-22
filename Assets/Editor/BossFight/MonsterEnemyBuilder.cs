using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace BossFight.Editor
{
    // Builds MonsterCustomer.prefab from the Monster prefab and places the MonsterSpawner (spawn point outside the door)
    // and the Witch's tether beam in the Boss Scene.
    public static class MonsterEnemyBuilder
    {
        public const string SpawnerName = "MonsterSpawner";
        public const string SpawnPointName = "MonsterSpawnPoint";
        public const string TetherName = "WitchTether";
        public const string MonsterSource = "Assets/Prefabs/CC Attribution Characters/Monster.prefab";
        public const string MonsterPrefab = BossFightPaths.PrefabsFolder + "/MonsterCustomer.prefab";
        public const string TetherMaterial = BossFightPaths.SettingsFolder + "/WitchTether.mat";
        private const float SpawnDistanceOutsideDoor = 2f;

        public static void Build()
        {
            var config = ConfigBuilder.Load();
            EditorFolders.Ensure(BossFightPaths.PrefabsFolder);
            EditorFolders.Ensure(BossFightPaths.SettingsFolder);
            var prefab = EnsurePrefab(config);
            var tetherMaterial = BuilderUtil.EnsureTransparentMaterial(TetherMaterial, "Universal Render Pipeline/Unlit", new Color(0.7f, 0.25f, 1f, 0.8f));

            using (var boss = new BossSceneScope())
            {
                var player = BuilderUtil.FindPlayer(boss.Scene);
                var door = BuilderUtil.FindByName(boss.Scene, BossDirectorBuilder.DoorName);
                var director = BuilderUtil.FindInScene<BossDirector>(boss.Scene);
                var witch = BuilderUtil.FindInScene<WitchBoss>(boss.Scene);
                if (player == null || door == null || director == null || witch == null)
                {
                    throw new System.InvalidOperationException($"Monster builder needs the player, '{BossDirectorBuilder.DoorName}', BossDirector and Witch in Boss Scene");
                }

                bool changed = false;
                var spawner = BuilderUtil.FindInScene<MonsterSpawner>(boss.Scene);
                if (spawner == null)
                {
                    var go = new GameObject(SpawnerName);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, boss.Scene);
                    spawner = go.AddComponent<MonsterSpawner>();
                    changed = true;
                }
                var point = spawner.transform.Find(SpawnPointName);
                if (point == null)
                {
                    point = new GameObject(SpawnPointName).transform;
                    point.SetParent(spawner.transform, false);
                    changed = true;
                }
                // Always outside the real front door, so a spawn point made for an earlier door target is moved.
                var doorCenter = OpeningBuilder.DoorBounds(door).center;
                var inward = player.transform.position - doorCenter;
                inward.y = 0f;
                inward = inward.sqrMagnitude > 1e-4f ? inward.normalized : Vector3.forward;
                var position = doorCenter - inward * SpawnDistanceOutsideDoor;
                var controller = player.GetComponent<CharacterController>();
                position.y = controller != null
                    ? player.transform.position.y + controller.center.y - controller.height * 0.5f
                    : player.transform.position.y - 1f;
                var rotation = Quaternion.LookRotation(inward);
                if ((point.position - position).sqrMagnitude > 1e-6f || Quaternion.Angle(point.rotation, rotation) > 0.01f)
                {
                    point.SetPositionAndRotation(position, rotation);
                    changed = true;
                }

                var so = new SerializedObject(spawner);
                changed |= BuilderUtil.SetReference(so, "config", config);
                changed |= BuilderUtil.SetReference(so, "director", director);
                changed |= BuilderUtil.SetReference(so, "prefab", prefab.GetComponent<MonsterCustomer>());
                changed |= BuilderUtil.SetReference(so, "spawnPoint", point);
                changed |= so.ApplyModifiedPropertiesWithoutUndo();

                var tether = BuilderUtil.FindInScene<WitchTether>(boss.Scene);
                if (tether == null)
                {
                    var go = new GameObject(TetherName);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, boss.Scene);
                    var line = go.AddComponent<LineRenderer>();
                    line.widthMultiplier = 0.08f;
                    line.positionCount = 2;
                    line.useWorldSpace = true;
                    line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    line.enabled = false;
                    tether = go.AddComponent<WitchTether>();
                    changed = true;
                }
                var lineRenderer = tether.GetComponent<LineRenderer>();
                if (lineRenderer.sharedMaterial != tetherMaterial)
                {
                    lineRenderer.sharedMaterial = tetherMaterial;
                    changed = true;
                }
                so = new SerializedObject(tether);
                changed |= BuilderUtil.SetReference(so, "config", config);
                changed |= BuilderUtil.SetReference(so, "director", director);
                changed |= BuilderUtil.SetReference(so, "witch", witch.transform);
                changed |= so.ApplyModifiedPropertiesWithoutUndo();

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(boss.Scene);
                }
                boss.SaveIfDirty();
            }
        }

        // Called by BossRigValidator with the Boss Scene open.
        public static void Validate(UnityEngine.SceneManagement.Scene scene, List<string> errors)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefab);
            if (prefab == null)
            {
                errors.Add($"MonsterCustomer prefab missing at {MonsterPrefab}");
            }
            else
            {
                var monster = prefab.GetComponent<MonsterCustomer>();
                if (monster == null)
                {
                    errors.Add("MonsterCustomer prefab has no MonsterCustomer component");
                }
                else
                {
                    BuilderUtil.RequireFields(monster, "MonsterCustomer prefab", new[] { "config", "animator" }, errors);
                }
                if (prefab.GetComponent<NavMeshAgent>() == null)
                {
                    errors.Add("MonsterCustomer prefab has no NavMeshAgent");
                }
                if (prefab.GetComponentInChildren<Enemy>(true) != null)
                {
                    errors.Add("MonsterCustomer prefab still has the legacy Enemy script");
                }
                if (prefab.GetComponentInChildren<Collider>(true) == null)
                {
                    errors.Add("MonsterCustomer prefab has no collider for the sword to hit");
                }
            }
            if (AssetDatabase.LoadAssetAtPath<Material>(TetherMaterial) == null)
            {
                errors.Add($"Material missing at {TetherMaterial}");
            }

            var spawner = BuilderUtil.FindInScene<MonsterSpawner>(scene);
            if (spawner == null)
            {
                errors.Add($"Boss Scene has no {SpawnerName} with a MonsterSpawner component");
            }
            else
            {
                BuilderUtil.RequireFields(spawner, "Boss Scene MonsterSpawner", new[] { "config", "director", "prefab", "spawnPoint" }, errors);
            }
            var tether = BuilderUtil.FindInScene<WitchTether>(scene);
            if (tether == null)
            {
                errors.Add($"Boss Scene has no {TetherName} with a WitchTether component");
            }
            else
            {
                BuilderUtil.RequireFields(tether, "Boss Scene WitchTether", new[] { "config", "director", "witch" }, errors);
            }
        }

        // A plain root (NavMeshAgent + MonsterCustomer) holding an unpacked copy of the Monster prefab without its legacy Enemy script.
        private static GameObject EnsurePrefab(BossFightConfig config)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefab) == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterSource);
                if (source == null)
                {
                    throw new System.InvalidOperationException($"Monster prefab missing at {MonsterSource}");
                }
                var root = new GameObject("MonsterCustomer");
                try
                {
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(source);
                    PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    visual.name = "Visual";
                    visual.transform.SetParent(root.transform, false);
                    foreach (var legacy in visual.GetComponentsInChildren<Enemy>(true))
                    {
                        Object.DestroyImmediate(legacy);
                    }
                    var agent = root.AddComponent<NavMeshAgent>();
                    agent.radius = 0.4f;
                    agent.height = 1.9f;
                    agent.acceleration = 20f;
                    agent.angularSpeed = 360f;
                    agent.stoppingDistance = 1.2f;
                    root.AddComponent<MonsterCustomer>();
                    PrefabUtility.SaveAsPrefabAsset(root, MonsterPrefab);
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }

            var contents = PrefabUtility.LoadPrefabContents(MonsterPrefab);
            try
            {
                var monster = contents.GetComponent<MonsterCustomer>();
                var so = new SerializedObject(monster);
                bool changed = BuilderUtil.SetReference(so, "config", config);
                changed |= BuilderUtil.SetReference(so, "animator", contents.GetComponentInChildren<Animator>(true));
                changed |= so.ApplyModifiedPropertiesWithoutUndo();
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, MonsterPrefab);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefab);
        }
    }
}
