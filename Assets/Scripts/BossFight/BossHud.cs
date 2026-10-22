using BossFight.Rules;
using TMPro;
using UnityEngine;

namespace BossFight
{
    // Top-right fight HUD: money and the current objective. The glass-crack frame comes later.
    public class BossHud : MonoBehaviour
    {
        [SerializeField]
        private BossDirector director;
        [SerializeField]
        private MoneyManager moneyManager;
        [SerializeField]
        private TMP_Text moneyText;
        [SerializeField]
        private TMP_Text objectiveText;

        private CanvasGroup group;
        private string message = "";
        private float messageUntil;

        // Shows a line (e.g. the Witch speaking) in place of the objective for a few seconds.
        public void ShowMessage(string line, float seconds)
        {
            message = line;
            messageUntil = Time.time + seconds;
        }

        // Hidden until the opening's screen crack appears.
        public void SetVisible(bool visible)
        {
            if (group == null) group = GetComponent<CanvasGroup>() != null ? GetComponent<CanvasGroup>() : gameObject.AddComponent<CanvasGroup>();
            group.alpha = visible ? 1f : 0f;
        }

        // Moves the money/objective labels into the crack frame, keeping their top-right anchoring.
        public void AttachTo(RectTransform frame)
        {
            if (frame == null) return;
            if (moneyText != null) moneyText.rectTransform.SetParent(frame, false);
            if (objectiveText != null) objectiveText.rectTransform.SetParent(frame, false);
        }

        private void Awake()
        {
            if (director == null) director = FindFirstObjectByType<BossDirector>();
            if (moneyManager == null) moneyManager = FindFirstObjectByType<MoneyManager>();
        }

        private void Update()
        {
            if (moneyText != null && moneyManager != null)
            {
                SetText(moneyText, BossHudText.Money(moneyManager.CurrentMoney));
            }
            if (objectiveText != null && Time.time < messageUntil)
            {
                SetText(objectiveText, message);
            }
            else if (objectiveText != null && director != null)
            {
                SetText(objectiveText, BossHudText.Objective(director.Phase, director.MonstersAlive));
            }
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label.text != value) label.text = value;
        }
    }
}
