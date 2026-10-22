using System;
using UnityEngine;

namespace BossFight.Editor
{
    // Entry point for every boss-fight scene/prefab/asset change.
    // Headless: unity run . -- -executeMethod BossFight.Editor.BuildAll.Run  (the CLI adds -quit itself and rejects it after --)
    public static class BuildAll
    {
        // Ordered builder steps. Later issues add one line here. Every step must be idempotent.
        private static readonly (string name, Action run)[] Steps =
        {
            ("Config asset", ConfigBuilder.Build),
            ("Rig prefab", RigPrefabBuilder.Build),
            ("Boss Scene rig instance", BossSceneRigBuilder.Build),
            ("Player hit receiver", PlayerHitReceiverBuilder.Build),
            ("Build settings scenes", BuildSettingsBuilder.Build),
        };

        public static void Run()
        {
            foreach (var step in Steps)
            {
                try
                {
                    step.run();
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"BossFight BuildAll step '{step.name}' failed: {e.Message}", e);
                }
            }
            BossRigValidator.Validate();
            Debug.Log("BossFight BuildAll finished; validator passed.");
        }
    }
}
