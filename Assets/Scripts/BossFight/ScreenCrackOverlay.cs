using UnityEngine;

namespace BossFight
{
    // Top-right HUD frame over the glass shard: holds the money, objective and prompt labels. The shard itself is a
    // camera-parented GlassShard mesh (screen-space UI cannot sample the scene), shown when the monitor shard hits.
    public class ScreenCrackOverlay : MonoBehaviour
    {
        [SerializeField]
        private GlassShard glass;

        public RectTransform Frame => (RectTransform)transform;
        public bool Shown { get; private set; }

        private void Awake()
        {
            if (glass != null) glass.gameObject.SetActive(false);
        }

        public void Show()
        {
            if (glass != null) glass.gameObject.SetActive(true);
            Shown = true;
        }
    }
}
