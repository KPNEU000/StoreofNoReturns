using BossFight.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace BossFight
{
    // The single entry point for every damage source: money is the player's health.
    public class PlayerHitReceiver : MonoBehaviour
    {
        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private MoneyManager moneyManager;
        [SerializeField]
        private ReputationManager reputationManager;
        [SerializeField]
        private Image hitFlash;
        [SerializeField]
        private AudioClip hitSound;

        private HitCost hitCost;
        private InvincibilityTimer invincibility;
        private float flashSeconds;
        private float flashStart = float.NegativeInfinity;

        private void Awake()
        {
            var settings = BossFightConfig.SettingsOf(config);
            hitCost = new HitCost(settings);
            invincibility = new InvincibilityTimer(settings.invincibilitySeconds);
            flashSeconds = settings.hitFlashSeconds;
        }

        // Returns false when the hit was ignored (invincibility frames or no money system).
        public bool TryHit(HitKind kind)
        {
            if (moneyManager == null)
            {
                moneyManager = FindFirstObjectByType<MoneyManager>();
            }
            if (reputationManager == null)
            {
                reputationManager = FindFirstObjectByType<ReputationManager>();
            }
            if (moneyManager == null || !invincibility.TryAccept(Time.unscaledTime))
            {
                return false;
            }

            float reputation = reputationManager != null ? reputationManager.CurrentReputation : ReputationScaling.MaxReputation;
            moneyManager.SubtractMoney(hitCost.For(kind, reputation));
            flashStart = Time.unscaledTime;
            if (hitSound != null)
            {
                Sfx.Play(hitSound, transform.position, 0.9f, 1f, Sfx.DefaultPitchJitter, 0f);
            }
            return true;
        }

        private void Update()
        {
            if (hitFlash == null)
            {
                return;
            }
            float t = (Time.unscaledTime - flashStart) / Mathf.Max(flashSeconds, 0.01f);
            var color = hitFlash.color;
            color.a = t >= 1f ? 0f : 0.45f * (1f - t);
            hitFlash.color = color;
        }
    }
}
