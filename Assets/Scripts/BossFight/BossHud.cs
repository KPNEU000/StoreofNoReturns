using BossFight.Rules;
using TMPro;
using UnityEngine;

namespace BossFight
{
    // Top-right fight HUD on the glass shard: money, the current objective and tutorial prompts.
    // The Witch's spoken lines go to the bottom BossTextBox instead.
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
        [SerializeField]
        private TMP_Text promptText;

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

        // Tutorial prompt under the objective; it stays until HidePrompt or the next ShowPrompt.
        public void ShowPrompt(string text)
        {
            if (promptText == null) return;
            promptText.text = text;
            promptText.gameObject.SetActive(true);
        }

        public void HidePrompt()
        {
            if (promptText != null) promptText.gameObject.SetActive(false);
        }

        private void Awake()
        {
            if (director == null) director = FindFirstObjectByType<BossDirector>();
            if (moneyManager == null) moneyManager = FindFirstObjectByType<MoneyManager>();
            HidePrompt();
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
