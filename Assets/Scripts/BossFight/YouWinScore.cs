using UnityEngine;
using UnityEngine.SceneManagement;

namespace BossFight
{
    // Shows the same win overlay (stars, money left) in the You Win scene, so the scene file itself is never edited.
    public class YouWinScore : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != BossDirector.WinSceneName) return;
            var go = new GameObject("YouWinScore", typeof(YouWinScore));
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        private void Awake()
        {
            WinOverlay.Show(RunScore.Value, RunScore.Reputation);
        }
    }
}
