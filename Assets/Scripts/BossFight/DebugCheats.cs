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
            // A real run (arrived from the shop) ignores every test key, so money and reputation stay what the shop left.
            if (!Debug.isDebugBuild || kb == null || fromShop) return;
            if (kb.pKey.wasPressedThisFrame) JumpToBossStart();
            if (kb.leftBracketKey.wasPressedThisFrame && moneyManager != null) moneyManager.SubtractMoney(settings.debugLoseMoneyLarge);
            if (kb.rightBracketKey.wasPressedThisFrame && moneyManager != null) moneyManager.SubtractMoney(settings.debugLoseMoneySmall);
        }

        private void Start()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Boss Scene")
                return;

            StartCoroutine(StartBossAutomatically());
        }

        private System.Collections.IEnumerator StartBossAutomatically()
        {
            yield return null;

            var cameraBehaviour = FindFirstObjectByType<CameraBehaviour>();
            if (cameraBehaviour != null)
                cameraBehaviour.DisableForBoss();

            if (director == null || director.Phase != BossPhase.Intro)
                yield break;

            if (BossRunState.HasData)
            {
                fromShop = true;
                if (moneyManager != null)
                    moneyManager.SetMoney(BossRunState.Money);

                if (reputationManager != null)
                {
                    reputationManager.ChangeReputation(
                        BossRunState.Reputation - reputationManager.CurrentReputation
                    );
                }

                int angeredCount = BossRunState.AngeredCustomers;
                BossRunState.Clear();

                director.StartFight(angeredCount);
            }
            else
            {
                JumpToBossStart();
            }
        }

        private bool fromShop;

        private void JumpToBossStart()
        {
            // Only before the fight starts: P during the opening used to reset money and reputation to the test values.
            if (director == null || director.Phase != BossPhase.Intro || director.OpeningRunning) return;
            if (moneyManager != null) moneyManager.SetMoney(settings.debugStartMoney);
            if (reputationManager != null)
            {
                reputationManager.ChangeReputation(settings.debugReputation - reputationManager.CurrentReputation);
            }
            director.StartFight(settings.debugAngeredCustomers);
        }
    }
}
