using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace BossFight.Editor
{
    // Final BuildAll step: throws if any required object or reference is missing.
    public static class BossRigValidator
    {
        public static void Validate()
        {
            var errors = new List<string>();

            var config = ConfigBuilder.Load();
            if (config == null)
            {
                errors.Add($"BossFightConfig asset missing at {BossFightPaths.ConfigAsset}");
            }

            var prefab = RigPrefabBuilder.Load();
            if (prefab == null)
            {
                errors.Add($"BossFightRig prefab missing at {BossFightPaths.RigPrefab}");
            }
            else
            {
                var rig = prefab.GetComponent<BossFightRig>();
                if (rig == null)
                {
                    errors.Add("BossFightRig prefab root has no BossFightRig component");
                }
                else
                {
                    if (rig.Config == null || rig.Config != config)
                    {
                        errors.Add("BossFightRig prefab: config reference is not the BossFightConfig asset");
                    }
                    if (rig.NavMeshSurface == null || rig.NavMeshSurface != prefab.GetComponent<NavMeshSurface>())
                    {
                        errors.Add("BossFightRig prefab: navMeshSurface reference is missing");
                    }
                }
            }

            if (prefab != null)
            {
                using (var boss = new BossSceneScope())
                {
                    var player = PlayerHitReceiverBuilder.FindPlayer(boss.Scene);
                    var receiver = player != null ? player.GetComponent<PlayerHitReceiver>() : null;
                    if (receiver == null)
                    {
                        errors.Add("Boss Scene player has no PlayerHitReceiver");
                    }
                    else
                    {
                        var so = new SerializedObject(receiver);
                        foreach (var field in new[] { "config", "moneyManager", "hitFlash" })
                        {
                            if (so.FindProperty(field).objectReferenceValue == null)
                            {
                                errors.Add($"Boss Scene PlayerHitReceiver: {field} reference is missing");
                            }
                        }
                    }

                    PlayerToolsBuilder.Validate(boss.Scene, errors);
                    ClothingEnemyBuilder.Validate(boss.Scene, errors);

                    var rigs = BossSceneRigBuilder.FindRigs(boss.Scene);
                    if (rigs.Count != 1)
                    {
                        errors.Add($"Boss Scene must contain exactly one BossFightRig, found {rigs.Count}");
                    }
                    else
                    {
                        if (!BossSceneRigBuilder.IsInstanceOf(rigs[0], prefab))
                        {
                            errors.Add("Boss Scene BossFightRig is not an instance of BossFightRig.prefab");
                        }
                        if (rigs[0].Config != config)
                        {
                            errors.Add("Boss Scene BossFightRig: config reference is not the BossFightConfig asset");
                        }
                    }
                }
            }

            foreach (var path in BossFightPaths.BuildScenes)
            {
                if (!File.Exists(path))
                {
                    errors.Add($"Build scene file missing: {path}");
                }
            }
            if (!BuildSettingsBuilder.Matches())
            {
                errors.Add("EditorBuildSettings scenes are not the five game scenes in order");
            }

            if (errors.Count > 0)
            {
                throw new System.InvalidOperationException(
                    "BossFight validation failed:\n- " + string.Join("\n- ", errors));
            }
        }
    }
}
