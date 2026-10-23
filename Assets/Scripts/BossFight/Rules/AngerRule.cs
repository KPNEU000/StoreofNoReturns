namespace BossFight.Rules
{
    // A customer leaves angry when their interaction net-lowered the shop's reputation.
    public static class AngerRule
    {
        public static bool IsAngered(int reputationAtStart, int reputationAtEnd)
        {
            return reputationAtEnd < reputationAtStart;
        }
    }
}
