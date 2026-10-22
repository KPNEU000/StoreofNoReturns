using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace BossFight.Editor
{
    public static class RigPrefabBuilder
    {
        // Creates BossFightRig.prefab, or edits the existing one in place (keeping its file IDs)
        // and saves only when something actually changed.
        public static void Build()
        {
            EditorFolders.Ensure(BossFightPaths.PrefabsFolder);
            var config = ConfigBuilder.Load();
            if (config == null)
            {
                throw new System.InvalidOperationException($"Config missing at {BossFightPaths.ConfigAsset}");
            }

            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(BossFightPaths.RigPrefab) != null;
            GameObject root = exists
                ? PrefabUtility.LoadPrefabContents(BossFightPaths.RigPrefab)
                : new GameObject(BossFightPaths.RigName);
            try
            {
                bool changed = Configure(root, config);
                if (!exists || changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, BossFightPaths.RigPrefab);
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
        }

        public static GameObject Load()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(BossFightPaths.RigPrefab);
        }

        private static bool Configure(GameObject root, BossFightConfig config)
        {
            bool changed = false;

            var surface = root.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = root.AddComponent<NavMeshSurface>();
                changed = true;
            }
            // Physics colliders, not render meshes: imported shop meshes are not Read/Write enabled,
            // which runtime builds from render meshes require.
            if (surface.collectObjects != CollectObjects.All)
            {
                surface.collectObjects = CollectObjects.All;
                changed = true;
            }
            if (surface.useGeometry != NavMeshCollectGeometry.PhysicsColliders)
            {
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                changed = true;
            }

            var rig = root.GetComponent<BossFightRig>();
            if (rig == null)
            {
                rig = root.AddComponent<BossFightRig>();
                changed = true;
            }
            var so = new SerializedObject(rig);
            changed |= SetReference(so, "config", config);
            changed |= SetReference(so, "navMeshSurface", surface);
            so.ApplyModifiedPropertiesWithoutUndo();

            return changed;
        }

        internal static bool SetReference(SerializedObject so, string field, Object value)
        {
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                throw new System.InvalidOperationException($"{so.targetObject.GetType().Name} has no serialized field '{field}'");
            }
            if (prop.objectReferenceValue == value)
            {
                return false;
            }
            prop.objectReferenceValue = value;
            return true;
        }
    }
}
