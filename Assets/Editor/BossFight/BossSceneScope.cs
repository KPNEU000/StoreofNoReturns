using System;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace BossFight.Editor
{
    // Gives builders the Boss Scene. Opens it additively if it is not already loaded and closes it
    // again on Dispose, so the caller's scene setup is left as it was.
    public sealed class BossSceneScope : IDisposable
    {
        public Scene Scene { get; }
        private readonly bool openedHere;

        public BossSceneScope()
        {
            Scene = SceneManager.GetSceneByPath(BossFightPaths.BossScene);
            if (!Scene.IsValid() || !Scene.isLoaded)
            {
                Scene = EditorSceneManager.OpenScene(BossFightPaths.BossScene, OpenSceneMode.Additive);
                openedHere = true;
            }
        }

        // Writes the scene only when a builder changed it, so re-running produces no file diff.
        public void SaveIfDirty()
        {
            if (Scene.isDirty)
            {
                EditorSceneManager.SaveScene(Scene);
            }
        }

        public void Dispose()
        {
            if (openedHere && Scene.IsValid())
            {
                EditorSceneManager.CloseScene(Scene, true);
            }
        }
    }
}
