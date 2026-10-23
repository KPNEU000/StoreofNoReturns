using BossFight.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BossFight
{
    // The end-of-run result over a dimmed screen: "You Won" with reputation stars and the money left, or "You Lost"
    // with the stars and a line about the Witch. Space returns to the title. Built at runtime; no scene edits.
    public class WinOverlay : MonoBehaviour
    {
        public const string TitleSceneName = "Title Screen";
        public const string LostLine = "The Witch got her refund. Every last cent of it.";
        private static readonly Color StarOn = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color StarOff = new Color(1f, 1f, 1f, 0.18f);

        private static Sprite starSprite;

        public static WinOverlay Show(float moneyLeft, int reputation)
        {
            return Build("YOU WON", reputation, BossHudText.Score(moneyLeft), 64f);
        }

        public static WinOverlay ShowLost(int reputation)
        {
            return Build("YOU LOST", reputation, LostLine, 44f);
        }

        private static WinOverlay Build(string title, int reputation, string detail, float detailSize)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            var go = new GameObject("WinOverlay", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var overlay = go.AddComponent<WinOverlay>();

            var dim = AddRect(go.transform, "Dim", Vector2.zero, Vector2.one, Vector2.zero);
            dim.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.01f, 0.04f, 0.78f);

            AddLabel(go.transform, "Title", title, 120f, new Vector2(0.5f, 0.68f), Color.white);

            int stars = StarRating.Stars(reputation);
            var row = AddRect(go.transform, "Stars", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f));
            row.sizeDelta = new Vector2(StarRating.MaxStars * 110f, 100f);
            for (int i = 0; i < StarRating.MaxStars; i++)
            {
                var star = AddRect(row, "Star" + (i + 1), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(55f + i * 110f, 0f));
                star.sizeDelta = new Vector2(96f, 96f);
                var image = star.gameObject.AddComponent<Image>();
                image.sprite = StarSprite();
                image.color = i < stars ? StarOn : StarOff;
            }
            AddLabel(go.transform, "Reputation", $"Reputation {Mathf.Clamp(reputation, 0, 100)}/100", 34f, new Vector2(0.5f, 0.43f), new Color(1f, 1f, 1f, 0.75f));
            AddLabel(go.transform, "Detail", detail, detailSize, new Vector2(0.5f, 0.33f), Color.white);
            AddLabel(go.transform, "Continue", "Press Space to return to the title", 30f, new Vector2(0.5f, 0.15f), new Color(1f, 1f, 1f, 0.6f));
            return overlay;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !(keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) return;
            enabled = false;
            if (SceneLoadingManager.Instance != null) SceneLoadingManager.Instance.LoadSceneByName(TitleSceneName);
            else SceneManager.LoadScene(TitleSceneName);
        }

        private static RectTransform AddRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            if (anchorMin != anchorMax) rect.sizeDelta = Vector2.zero;
            return rect;
        }

        private static void AddLabel(Transform parent, string name, string text, float size, Vector2 anchor, Color color)
        {
            var rect = AddRect(parent, name, anchor, anchor, Vector2.zero);
            rect.sizeDelta = new Vector2(1600f, size * 1.6f);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
        }

        private static Sprite StarSprite()
        {
            if (starSprite != null) return starSprite;
            const int size = 128;
            var outline = StarRating.Outline();
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                    pixels[y * size + x] = StarRating.Contains(outline, p) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            starSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
            return starSprite;
        }
    }
}
