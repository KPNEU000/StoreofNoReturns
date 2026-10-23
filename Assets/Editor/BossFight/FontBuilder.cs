using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BossFight.Editor
{
    // Puts the boss fight's own labels (HUD money/objective/prompt, the Witch's text box) in BossFont. The team's
    // shop UI in Boss Scene keeps its fonts.
    public static class FontBuilder
    {
        public static void Build()
        {
            var font = Load();
            using (var boss = new BossSceneScope())
            {
                bool changed = false;
                foreach (var label in Labels(boss.Scene))
                {
                    if (label.font == font) continue;
                    label.font = font;
                    EditorUtility.SetDirty(label);
                    changed = true;
                }
                if (changed) EditorSceneManager.MarkSceneDirty(boss.Scene);
                boss.SaveIfDirty();
            }
        }

        internal static TMP_FontAsset Load()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BossFont.AssetPath);
            if (font == null) throw new System.InvalidOperationException($"Boss font missing: {BossFont.AssetPath}");
            return font;
        }

        private static List<TMP_Text> Labels(UnityEngine.SceneManagement.Scene scene)
        {
            var labels = new List<TMP_Text>();
            void Add(UnityEngine.Component owner, params string[] fields)
            {
                if (owner == null) return;
                var so = new SerializedObject(owner);
                foreach (var field in fields)
                {
                    if (so.FindProperty(field)?.objectReferenceValue is TMP_Text text && text != null) labels.Add(text);
                }
            }
            Add(BuilderUtil.FindInScene<BossHud>(scene), "moneyText", "objectiveText", "promptText");
            Add(BuilderUtil.FindInScene<BossTextBox>(scene), "label");
            return labels;
        }

        // Called by BossRigValidator with the Boss Scene open.
        public static void Validate(UnityEngine.SceneManagement.Scene scene, List<string> errors)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BossFont.AssetPath);
            if (font == null) { errors.Add($"Boss font missing: {BossFont.AssetPath}"); return; }
            var labels = Labels(scene);
            if (labels.Count < 4) errors.Add($"Font: expected the 4 boss HUD/text box labels, found {labels.Count}");
            foreach (var label in labels)
            {
                if (label.font != font) errors.Add($"Font: {label.name} is not {font.name}");
            }
        }
    }
}
