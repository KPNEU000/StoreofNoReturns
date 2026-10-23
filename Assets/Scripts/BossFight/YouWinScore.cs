using UnityEngine;
using UnityEngine.SceneManagement;

namespace BossFight
{
    // Shows the result overlay in the You Win and Game Over scenes (stars, money left or the Witch's line), so the
    // scene files themselves are never edited and the player never lands in an empty void.
    public class YouWinScore : MonoBehaviour
    {
        public const string GameOverSceneName = "Game Over Screen";

        private bool lost;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            bool won = scene.name == BossDirector.WinSceneName;
            bool lostScene = scene.name == GameOverSceneName;
            if (!won && !lostScene) return;
            var go = new GameObject("RunResult");
            go.SetActive(false);
            go.AddComponent<YouWinScore>().lost = lostScene;
            SceneManager.MoveGameObjectToScene(go, scene);
            go.SetActive(true);
        }

        private void Awake()
        {
            if (lost) WinOverlay.ShowLost(RunScore.Reputation);
            else WinOverlay.Show(RunScore.Value, RunScore.Reputation);
        }
    }
}
