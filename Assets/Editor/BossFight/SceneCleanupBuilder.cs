using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace BossFight.Editor
{
    // Saves the Boss Scene's shop objects switched off rather than deleted: every shop customer (including the
    // duplicate shop Witch) inactive, CustomerSpawner and TimeManager components disabled.
    // BossFightRig re-applies the same ShopShutdown at runtime.
    public static class SceneCleanupBuilder
    {
        public static void Build()
        {
            using (var boss = new BossSceneScope())
            {
                if (ShopShutdown.Apply(boss.Scene) > 0)
                {
                    EditorSceneManager.MarkSceneDirty(boss.Scene);
                }
                boss.SaveIfDirty();
            }
        }

        // Called by BossRigValidator with the Boss Scene open.
        public static void Validate(Scene scene, List<string> errors)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var customer in root.GetComponentsInChildren<CustomerBehaviour>(true))
                {
                    if (customer.gameObject.activeSelf && customer.GetComponentInParent<WitchBoss>(true) == null)
                    {
                        errors.Add($"Boss Scene shop customer '{customer.name}' must be saved inactive");
                    }
                }
                foreach (var spawner in root.GetComponentsInChildren<CustomerSpawner>(true))
                {
                    if (spawner.enabled) errors.Add($"Boss Scene CustomerSpawner on '{spawner.name}' must be disabled");
                }
                foreach (var clock in root.GetComponentsInChildren<TimeManager>(true))
                {
                    if (clock.enabled) errors.Add($"Boss Scene TimeManager on '{clock.name}' must be disabled");
                }
            }
        }
    }
}
