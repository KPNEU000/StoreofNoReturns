using BossFight.Rules;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossFight
{
    // Boss Scene only, editor/dev builds: P = jump to boss start with test values, [ = -$100, ] = -$25.
    public class DebugCheats : MonoBehaviour
    {
        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private BossDirector director;
        [SerializeField]
        private MoneyManager moneyManager;
        [SerializeField]
        private ReputationManager reputationManager;

        private BossFightSettings settings;

        private void Awake()
        {
            settings = BossFightConfig.SettingsOf(config);
            if (director == null) director = FindFirstObjectByType<BossDirector>();
            if (moneyManager == null) moneyManager = FindFirstObjectByType<MoneyManager>();
            if (reputationManager == null) reputationManager = FindFirstObjectByType<ReputationManager>();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (!Debug.isDebugBuild || kb == null) return;
            if (kb.pKey.wasPressedThisFrame) JumpToBossStart();
            if (kb.leftBracketKey.wasPressedThisFrame && moneyManager != null) moneyManager.SubtractMoney(settings.debugLoseMoneyLarge);
            if (kb.rightBracketKey.wasPressedThisFrame && moneyManager != null) moneyManager.SubtractMoney(settings.debugLoseMoneySmall);
        }

        private void JumpToBossStart()
        {
            if (director == null || director.Phase != BossPhase.Intro) return;
            if (moneyManager != null) moneyManager.SetMoney(settings.debugStartMoney);
            if (reputationManager != null)
            {
                reputationManager.ChangeReputation(settings.debugReputation - reputationManager.CurrentReputation);
            }
            director.StartFight(settings.debugAngeredCustomers);
        }
    }
}
