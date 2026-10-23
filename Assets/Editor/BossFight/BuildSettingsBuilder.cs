using System.Linq;
using UnityEditor;

namespace BossFight.Editor
{
    public static class BuildSettingsBuilder
    {
        // Sets the build scene list to the five game scenes, writing only when it differs.
        public static void Build()
        {
            if (Matches())
            {
                return;
            }
            EditorBuildSettings.scenes = BossFightPaths.BuildScenes
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();
        }

        public static bool Matches()
        {
            var current = EditorBuildSettings.scenes;
            return current.Length == BossFightPaths.BuildScenes.Length
                && current.Select(s => s.path).SequenceEqual(BossFightPaths.BuildScenes)
                && current.All(s => s.enabled);
        }
    }
}
