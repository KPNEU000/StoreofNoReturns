using UnityEngine;

namespace BossFight.Rules
{
    public enum LightTier
    {
        Normal,
        MildFlicker,
        StrongFlicker,
        Off,
    }

    // Money -> store light tier, plus the per-light flicker sampler. Thresholds come from the settings snapshot.
    public class LightTierRules
    {

        private readonly BossFightSettings settings;

        // A flicker dip never goes below this fraction of full intensity, whatever the tier minimums say.
        public float FloorIntensity => settings.flickerFloorIntensity;
        public float DipSecondsMin => settings.flickerDipSecondsMin;
        public float DipSecondsMax => Mathf.Max(settings.flickerDipSecondsMin, settings.flickerDipSecondsMax);

        public LightTierRules(BossFightSettings settings)
        {
            this.settings = settings;
        }

        public LightTier TierFor(float money)
        {
            if (money <= settings.blackoutMoney) return LightTier.Off;
            if (money <= settings.strongFlickerMoney) return LightTier.StrongFlicker;
            if (money <= settings.mildFlickerMoney) return LightTier.MildFlicker;
            return LightTier.Normal;
        }

        // Lowest intensity fraction a dip may reach in this tier. Off fades out separately, so it has no dip floor.
        public float DipFloor(LightTier tier)
        {
            switch (tier)
            {
                case LightTier.MildFlicker: return Mathf.Max(FloorIntensity, settings.mildFlickerMinIntensity);
                case LightTier.StrongFlicker:
                case LightTier.Off: return Mathf.Max(FloorIntensity, settings.strongFlickerMinIntensity);
                default: return 1f;
            }
        }

        // Stronger tiers dip more often.
        public float IntervalScale(LightTier tier) => tier == LightTier.MildFlicker ? 1f : settings.strongFlickerIntervalScale;

        public LightFlicker CreateFlicker(int seed) => new LightFlicker(this, seed);
    }

    // One light's flicker: a seeded stream of dips, so no two lights are synchronized.
    public class LightFlicker
    {
        private readonly LightTierRules rules;
        private readonly System.Random random;
        private float nextDipStart = -1f;
        private float dipEnd;
        private float dipDepth; // 0..1 along the way from full intensity to the tier floor

        internal LightFlicker(LightTierRules rules, int seed)
        {
            this.rules = rules;
            random = new System.Random(seed);
        }

        private float Next() => (float)random.NextDouble();

        // Intensity fraction (floor..1) at `now`. Call with non-decreasing times.
        public float Sample(LightTier tier, float now, float intervalMin, float intervalMax)
        {
            if (tier == LightTier.Normal) return 1f;
            if (nextDipStart < 0f)
            {
                nextDipStart = now + Mathf.Lerp(intervalMin, intervalMax, Next()) * rules.IntervalScale(tier);
            }
            if (now >= nextDipStart && now >= dipEnd)
            {
                dipEnd = now + Mathf.Lerp(rules.DipSecondsMin, rules.DipSecondsMax, Next());
                dipDepth = Next();
                nextDipStart = dipEnd + Mathf.Lerp(intervalMin, intervalMax, Next()) * rules.IntervalScale(tier);
            }
            if (now < dipEnd)
            {
                return Mathf.Max(rules.FloorIntensity, Mathf.Lerp(1f, rules.DipFloor(tier), dipDepth));
            }
            return 1f;
        }
    }
}
