using UnityEditor;
using UnityEngine;

namespace BossFight.Editor
{
    public static class ConfigBuilder
    {
        // Creates the config asset with context.md defaults. An existing asset is left alone so tuning survives.
        public static void Build()
        {
            EditorFolders.Ensure(BossFightPaths.SettingsFolder);
            if (AssetDatabase.LoadAssetAtPath<BossFightConfig>(BossFightPaths.ConfigAsset) != null)
            {
                return;
            }
            var config = ScriptableObject.CreateInstance<BossFightConfig>();
            AssetDatabase.CreateAsset(config, BossFightPaths.ConfigAsset);
            AssetDatabase.SaveAssetIfDirty(config);
        }

        public static BossFightConfig Load()
        {
            return AssetDatabase.LoadAssetAtPath<BossFightConfig>(BossFightPaths.ConfigAsset);
        }
    }
}
