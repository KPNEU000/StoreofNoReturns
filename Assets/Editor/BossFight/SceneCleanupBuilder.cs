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
            foreach (var on in ShopShutdown.StillOn(scene))
            {
                errors.Add(on is CustomerBehaviour
                    ? $"Boss Scene shop customer '{on.name}' must be saved inactive"
                    : $"Boss Scene {on.GetType().Name} on '{on.name}' must be disabled");
            }
        }
    }
}
