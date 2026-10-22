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
            if (objectiveText != null && director != null)
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
