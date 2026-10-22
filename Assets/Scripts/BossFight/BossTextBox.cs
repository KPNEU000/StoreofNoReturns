using TMPro;
using UnityEngine;

namespace BossFight
{
    // Bottom-center panel for the Witch's lines and tutorial prompts. BossTextBox.Show(text, seconds) from anywhere;
    // seconds <= 0 keeps the line up until Hide() or the next Show.
    public class BossTextBox : MonoBehaviour
    {
        [SerializeField]
        private GameObject panel;
        [SerializeField]
        private TMP_Text label;

        private static BossTextBox instance;
        private float hideAt;
        private bool timed;

        public static bool IsShowing => instance != null && instance.panel != null && instance.panel.activeSelf;

        public static void Show(string text, float seconds)
        {
            if (instance != null) instance.Display(text, seconds);
        }

        public static void Hide()
        {
            if (instance != null) instance.Clear();
        }

        private void Awake()
        {
            instance = this;
            Clear();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Update()
        {
            if (timed && Time.time >= hideAt) Clear();
        }

        private void Display(string text, float seconds)
        {
            if (label != null) label.text = text;
            if (panel != null) panel.SetActive(true);
            timed = seconds > 0f;
            hideAt = Time.time + seconds;
        }

        private void Clear()
        {
            timed = false;
            if (panel != null) panel.SetActive(false);
        }
    }
}
