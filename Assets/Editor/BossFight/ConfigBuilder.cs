using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BossFight.Editor
{
    public static class ConfigBuilder
    {
        // Settings fields whose value is retuned in code. They are written into an existing asset by name,
        // from the BossFightSettings defaults, because Unity keeps values already serialized in the asset.
        private static readonly string[] TunedFields =
        {
            "clothingSpeedFactor", "monsterSpeedFactor", "boostedMonsterSpeedFactor",
            "flashlightFullIntensity", "flashlightRange", "flashlightSpotAngle", "monsterBaseHp", "monsterWindupSeconds",
            "monsterAttackReach", "monsterAttackCooldownSeconds",
            "outsideLightIntensity", "blackoutEnvironmentFactor",
            "bookScale", "bookTelegraphSeconds", "witchHoverHeight", "caveInChunkRadius", "bookFlightSpeed",
            "glassHudWidth", "glassHudHeight", "caveInChunkRestHeight",
            "invincibilitySeconds", "slashReach", "bookThrowIntervalSeconds", "slashArcDegrees",
            "flashlightInnerSpotAngle",
        };

        // Creates the config asset with defaults, then writes the tuned values into it (new or existing).
        public static void Build()
        {
            EditorFolders.Ensure(BossFightPaths.SettingsFolder);
            var config = AssetDatabase.LoadAssetAtPath<BossFightConfig>(BossFightPaths.ConfigAsset);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BossFightConfig>();
                AssetDatabase.CreateAsset(config, BossFightPaths.ConfigAsset);
            }
            ApplyTuning(config);
            AssetDatabase.SaveAssetIfDirty(config);
        }

        private static void ApplyTuning(BossFightConfig config)
        {
            var defaults = new BossFightSettings();
            var so = new SerializedObject(config);
            foreach (var name in TunedFields)
            {
                var property = so.FindProperty("settings." + name);
                var field = typeof(BossFightSettings).GetField(name, BindingFlags.Public | BindingFlags.Instance);
                if (property == null || field == null) throw new System.InvalidOperationException($"Tuned setting '{name}' not found");
                property.floatValue = (float)field.GetValue(defaults);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static BossFightConfig Load()
        {
            return AssetDatabase.LoadAssetAtPath<BossFightConfig>(BossFightPaths.ConfigAsset);
        }
    }
}
