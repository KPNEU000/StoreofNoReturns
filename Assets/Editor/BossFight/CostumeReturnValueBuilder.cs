using UnityEditor;

namespace BossFight.Editor
{
    // Sets returnValue = 100 on every CostumeScriptData asset (field value only).
    public static class CostumeReturnValueBuilder
    {
        public const float ReturnValue = 100f;

        public static void Build()
        {
            bool changed = false;
            foreach (var costume in FindAll())
            {
                var so = new SerializedObject(costume);
                var prop = so.FindProperty("returnValue");
                if (prop.floatValue != ReturnValue)
                {
                    prop.floatValue = ReturnValue;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(costume);
                    changed = true;
                }
            }
            if (changed) AssetDatabase.SaveAssets();
        }

        public static void Validate(System.Collections.Generic.List<string> errors)
        {
            foreach (var costume in FindAll())
            {
                if (costume.getReturnValue() != ReturnValue)
                {
                    errors.Add($"CostumeScriptData '{costume.name}' returnValue is {costume.getReturnValue()}, expected {ReturnValue}");
                }
            }
        }

        private static System.Collections.Generic.List<CostumeScriptData> FindAll()
        {
            var list = new System.Collections.Generic.List<CostumeScriptData>();
            foreach (var guid in AssetDatabase.FindAssets("t:CostumeScriptData"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<CostumeScriptData>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) list.Add(asset);
            }
            return list;
        }
    }
}
