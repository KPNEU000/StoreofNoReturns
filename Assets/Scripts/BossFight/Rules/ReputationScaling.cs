using UnityEngine;

namespace BossFight.Rules
{
    // Reputation (0-100) -> monster HP multiplier and monster hit cost.
    // Piecewise linear through rep 0 / 50 / 100, clamped outside that range.
    public class ReputationScaling
    {
        public const float MinReputation = 0f;
        public const float MidReputation = 50f;
        public const float MaxReputation = 100f;

        private readonly BossFightSettings settings;

        public ReputationScaling(BossFightSettings settings)
        {
            this.settings = settings;
        }

        public float MonsterHpMultiplier(float reputation)
        {
            return Piecewise(reputation,
                settings.monsterHpMultiplierAtRep0,
                settings.monsterHpMultiplierAtRep50,
                settings.monsterHpMultiplierAtRep100);
        }

        public float MonsterHitCost(float reputation)
        {
            return Piecewise(reputation,
                settings.monsterHitCostAtRep0,
                settings.monsterHitCostAtRep50,
                settings.monsterHitCostAtRep100);
        }

        private static float Piecewise(float reputation, float atMin, float atMid, float atMax)
        {
            float rep = Mathf.Clamp(reputation, MinReputation, MaxReputation);
            if (rep <= MidReputation)
            {
                return Mathf.Lerp(atMin, atMid, Mathf.InverseLerp(MinReputation, MidReputation, rep));
            }
            return Mathf.Lerp(atMid, atMax, Mathf.InverseLerp(MidReputation, MaxReputation, rep));
        }
    }
}
