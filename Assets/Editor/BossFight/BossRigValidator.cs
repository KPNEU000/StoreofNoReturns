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
            CostumeReturnValueBuilder.Validate(errors);

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
                    var lighting = prefab.GetComponent<StoreLightingController>();
                    if (lighting == null)
                    {
                        errors.Add("BossFightRig prefab: StoreLightingController missing");
                    }
                    else
                    {
                        var lso = new UnityEditor.SerializedObject(lighting);
                        if (lso.FindProperty("config").objectReferenceValue != config || lso.FindProperty("outsideLight").objectReferenceValue == null)
                        {
                            errors.Add("BossFightRig prefab: StoreLightingController config/outsideLight reference is missing");
                        }
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
                    var player = BuilderUtil.FindPlayer(boss.Scene);
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

                    SceneCleanupBuilder.Validate(boss.Scene, errors);
                    PlayerToolsBuilder.Validate(boss.Scene, errors);
                    ClothingEnemyBuilder.Validate(boss.Scene, errors);
                    RackGapBuilder.Validate(boss.Scene, errors);
                    SoundBuilder.Validate(boss.Scene, errors);
                    BossDirectorBuilder.Validate(boss.Scene, errors);
                    MonsterEnemyBuilder.Validate(boss.Scene, errors);
                    DraggedBodyBuilder.Validate(errors);
                    ClothingWaveBuilder.Validate(boss.Scene, errors);
                    GarmentClothBuilder.Validate(boss.Scene, errors);
                    BookThrowBuilder.Validate(boss.Scene, errors);
                    CaveInBuilder.Validate(boss.Scene, errors);
                    OpeningBuilder.Validate(boss.Scene, errors);

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
