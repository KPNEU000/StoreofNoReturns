using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BossFight
{
    // Shop -> fight transition (2026-10-23 playtest): freezes the last shop frame (with MainScene's dither filter),
    // loads the next scene behind it, then fades that frame out so the filtered shop dissolves into the HD fight.
    public class SceneCrossfade : MonoBehaviour
    {
        public const float FadeSeconds = 2f;

        public static void Load(string sceneName)
        {
            var go = new GameObject("SceneCrossfade");
            DontDestroyOnLoad(go);
            go.AddComponent<SceneCrossfade>().StartCoroutine(go.GetComponent<SceneCrossfade>().Run(sceneName));
        }

        private IEnumerator Run(string sceneName)
        {
            yield return new WaitForEndOfFrame();
            var frame = ScreenCapture.CaptureScreenshotAsTexture();

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var image = new GameObject("Frame", typeof(RectTransform)).AddComponent<RawImage>();
            image.transform.SetParent(transform, false);
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            image.texture = frame;
            image.raycastTarget = false;

            yield return SceneManager.LoadSceneAsync(sceneName);

            for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                image.color = new Color(1f, 1f, 1f, 1f - t / FadeSeconds);
                yield return null;
            }
            Destroy(frame);
            Destroy(gameObject);
        }
    }
}
