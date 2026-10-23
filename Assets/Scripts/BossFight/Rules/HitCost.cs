namespace BossFight.Rules
{
    // Money cost of one hit on the player.
    public class HitCost
    {
        private readonly BossFightSettings settings;
        private readonly ReputationScaling reputationScaling;

        public HitCost(BossFightSettings settings)
        {
            this.settings = settings;
            reputationScaling = new ReputationScaling(settings);
        }

        public float For(HitKind kind, float reputation)
        {
            switch (kind)
            {
                case HitKind.Clothing:
                    return settings.clothingHitCost;
                case HitKind.Book:
                    return settings.bookHitCost;
                case HitKind.Debris:
                    return settings.debrisHitCost;
                default:
                    return reputationScaling.MonsterHitCost(reputation);
            }
        }
    }
}
