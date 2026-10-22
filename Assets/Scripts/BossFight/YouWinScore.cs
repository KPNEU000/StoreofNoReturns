using BossFight.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BossFight
{
    // Adds "Money left: $X" to the You Win scene at runtime, so the scene file itself is never edited.
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
            // The fight leaves the cursor locked for mouse look.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var textGo = new GameObject("MoneyLeft", typeof(RectTransform));
            textGo.transform.SetParent(transform, false);
            var rect = (RectTransform)textGo.transform;
            rect.anchorMin = new Vector2(0.5f, 0.25f);
            rect.anchorMax = new Vector2(0.5f, 0.25f);
            rect.sizeDelta = new Vector2(800f, 100f);
            var label = textGo.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 56f;
            label.color = Color.white;
            label.text = BossHudText.Score(RunScore.Value);
        }
    }
}
