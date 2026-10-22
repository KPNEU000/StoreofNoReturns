using BossFight.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace BossFight
{
    // Top-right cracked-glass frame drawn procedurally; the money/objective HUD is moved inside it.
    public class ScreenCrackOverlay : MonoBehaviour
    {
        private const int TextureSize = 256;

        [SerializeField]
        private RawImage crackImage;
        [SerializeField]
        private int seed = 7;

        private Texture2D texture;

        public RectTransform Frame => (RectTransform)transform;
        public bool Shown { get; private set; }

        private void Awake()
        {
            if (crackImage != null) crackImage.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (texture != null) Destroy(texture);
        }

        public void Show()
        {
            if (crackImage == null) return;
            if (texture == null)
            {
                // The impact point sits in the top-right corner of the frame, where the shard hit the screen.
                var segments = CrackPattern.Generate(seed, new Vector2(0.88f, 0.86f), 9, 7, 1.1f);
                texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
                texture.SetPixels32(CrackPattern.Rasterize(TextureSize, segments, new Color(0.92f, 0.95f, 1f, 0.9f)));
                texture.Apply();
            }
            crackImage.texture = texture;
            crackImage.raycastTarget = false;
            crackImage.gameObject.SetActive(true);
            Shown = true;
        }
    }
}
