using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BossFight.Editor
{
    // Tags the Boss Scene clothing racks as PossessableRack and wires the wave driver onto the ClothingSpawner object.
    public static class ClothingWaveBuilder
    {
        private const string RackPrefix = "ClothingRack";
        private static readonly string[] WallNames = { "Clothing Racks Left Wall", "Clothing Racks Right Wall" };

        public static void Build()
        {
            var config = ConfigBuilder.Load();
            using (var boss = new BossSceneScope())
            {
                var spawner = ClothingEnemyBuilder.FindSpawner(boss.Scene);
                if (spawner == null)
                {
                    throw new System.InvalidOperationException("Boss Scene has no ClothingSpawner to drive waves");
                }
                var director = BuilderUtil.FindInScene<BossDirector>(boss.Scene);
                if (director == null)
                {
                    throw new System.InvalidOperationException("Boss Scene has no BossDirector to drive waves");
                }
                var racks = FindRackObjects(boss.Scene);
                if (racks.Count == 0)
                {
                    throw new System.InvalidOperationException("Boss Scene has no clothing rack objects (ClothingRack* / Clothing Racks Left/Right Wall)");
                }

                bool changed = false;
                foreach (var rack in racks)
                {
                    var possessable = rack.GetComponent<PossessableRack>();
                    if (possessable == null)
                    {
                        possessable = rack.gameObject.AddComponent<PossessableRack>();
                        changed = true;
                    }
                    var rackSo = new SerializedObject(possessable);
                    changed |= BuilderUtil.SetReference(rackSo, "config", config);
                    changed |= rackSo.ApplyModifiedPropertiesWithoutUndo();
                }

                var driver = spawner.GetComponent<ClothingWaveDriver>();
                if (driver == null)
                {
                    driver = spawner.gameObject.AddComponent<ClothingWaveDriver>();
                    changed = true;
                }
                var so = new SerializedObject(driver);
                changed |= BuilderUtil.SetReference(so, "config", config);
                changed |= BuilderUtil.SetReference(so, "director", director);
                changed |= BuilderUtil.SetReference(so, "spawner", spawner);
                var rig = BossSceneRigBuilder.FindRigs(boss.Scene);
                if (rig.Count > 0) changed |= BuilderUtil.SetReference(so, "rig", rig[0]);
                changed |= so.ApplyModifiedPropertiesWithoutUndo();

                // The director drives spawning now, so the testing auto-spawn goes off.
                var spawnerSo = new SerializedObject(spawner);
                var auto = spawnerSo.FindProperty("autoSpawnForTesting");
                if (auto.boolValue)
                {
                    auto.boolValue = false;
                    changed |= spawnerSo.ApplyModifiedPropertiesWithoutUndo();
                }

                if (changed) EditorSceneManager.MarkSceneDirty(boss.Scene);
                boss.SaveIfDirty();
            }
        }

        // Rack roots: objects named ClothingRack* (outermost only), plus children of the wall groups that hold none.
        internal static List<Transform> FindRackObjects(UnityEngine.SceneManagement.Scene scene)
        {
            var result = new List<Transform>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith(RackPrefix) && !HasRackAncestor(t)) result.Add(t);
                }
            }
            foreach (var wallName in WallNames)
            {
                var wall = BuilderUtil.FindByName(scene, wallName);
                if (wall == null) continue;
                foreach (Transform child in wall)
                {
                    if (child.name.StartsWith(RackPrefix) || HasNamedRackInside(child) || HasRackAncestor(child)) continue;
                    if (child.GetComponentInChildren<Renderer>(true) != null) result.Add(child);
                }
            }
            return result;
        }

        private static bool HasRackAncestor(Transform t)
        {
            for (var p = t.parent; p != null; p = p.parent)
            {
                if (p.name.StartsWith(RackPrefix)) return true;
            }
            return false;
        }

        private static bool HasNamedRackInside(Transform t)
        {
            foreach (var c in t.GetComponentsInChildren<Transform>(true))
            {
                if (c != t && c.name.StartsWith(RackPrefix)) return true;
            }
            return false;
        }

        public static void Validate(UnityEngine.SceneManagement.Scene scene, List<string> errors)
        {
            var spawner = ClothingEnemyBuilder.FindSpawner(scene);
            if (spawner == null) return; // already reported by ClothingEnemyBuilder
            var auto = new SerializedObject(spawner).FindProperty("autoSpawnForTesting");
            if (auto != null && auto.boolValue)
            {
                errors.Add("Boss Scene ClothingSpawner: autoSpawnForTesting must be off (the wave driver spawns)");
            }
            var driver = spawner.GetComponent<ClothingWaveDriver>();
            if (driver == null)
            {
                errors.Add("Boss Scene ClothingSpawner has no ClothingWaveDriver");
            }
            else
            {
                var so = new SerializedObject(driver);
                foreach (var field in new[] { "config", "director", "spawner" })
                {
                    if (so.FindProperty(field).objectReferenceValue == null)
                    {
                        errors.Add($"Boss Scene ClothingWaveDriver: {field} reference is missing");
                    }
                }
            }
            var racks = FindRackObjects(scene);
            if (racks.Count == 0) errors.Add("Boss Scene has no clothing rack objects to possess");
            foreach (var rack in racks)
            {
                if (rack.GetComponent<PossessableRack>() == null)
                {
                    errors.Add($"Boss Scene rack '{rack.name}' has no PossessableRack");
                }
            }
        }
    }
}
