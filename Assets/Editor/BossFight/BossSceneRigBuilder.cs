using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BossFight.Editor
{
    public static class BossSceneRigBuilder
    {
        // Ensures Boss Scene holds exactly one BossFightRig, connected to the prefab, and saves only on change.
        public static void Build()
        {
            var prefab = RigPrefabBuilder.Load();
            if (prefab == null)
            {
                throw new System.InvalidOperationException($"Rig prefab missing at {BossFightPaths.RigPrefab}");
            }

            using (var boss = new BossSceneScope())
            {
                var rigs = FindRigs(boss.Scene);
                BossFightRig keep = null;
                foreach (var rig in rigs)
                {
                    if (keep == null && IsInstanceOf(rig, prefab))
                    {
                        keep = rig;
                        continue;
                    }
                    var instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(rig);
                    Object.DestroyImmediate(instanceRoot != null ? instanceRoot : rig.gameObject);
                    EditorSceneManager.MarkSceneDirty(boss.Scene);
                }

                if (keep == null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, boss.Scene);
                    instance.name = BossFightPaths.RigName;
                    EditorSceneManager.MarkSceneDirty(boss.Scene);
                }

                boss.SaveIfDirty();
            }
        }

        public static List<BossFightRig> FindRigs(Scene scene)
        {
            var rigs = new List<BossFightRig>();
            foreach (var root in scene.GetRootGameObjects())
            {
                rigs.AddRange(root.GetComponentsInChildren<BossFightRig>(true));
            }
            return rigs;
        }

        public static bool IsInstanceOf(BossFightRig rig, GameObject prefab)
        {
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(rig);
            return root == rig.gameObject && PrefabUtility.GetCorrespondingObjectFromSource(root) == prefab;
        }
    }
}
