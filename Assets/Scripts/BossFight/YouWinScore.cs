using UnityEngine;
using UnityEngine.SceneManagement;

namespace BossFight
{
    // Shows the result overlay in the You Win scene (stars, money left), so the scene file itself is never edited
    // and the player never lands in an empty void. The Game Over scene has the team's own GAME OVER screen
    // (2026-10-23: a second "You Lost" on top of it was confusing), so there it only frees the mouse for its buttons.
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
            if (lost)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else WinOverlay.Show(RunScore.Value, RunScore.Reputation);
        }
    }
}
