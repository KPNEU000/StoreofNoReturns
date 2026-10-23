using TMPro;
using UnityEngine;

namespace BossFight
{
    // The fight's text font (Roboto Bold, 2026-10-23 team request). It lives in a TMP Resources folder, so runtime
    // UI (the win overlay) can load it too.
    public static class BossFont
    {
        public const string ResourcePath = "Fonts & Materials/Roboto-Bold SDF";
        public const string AssetPath = "Assets/TextMesh Pro/Examples & Extras/Resources/" + ResourcePath + ".asset";

        private static TMP_FontAsset cached;

        public static TMP_FontAsset Get()
        {
            if (cached == null) cached = Resources.Load<TMP_FontAsset>(ResourcePath);
            return cached;
        }
    }
}
