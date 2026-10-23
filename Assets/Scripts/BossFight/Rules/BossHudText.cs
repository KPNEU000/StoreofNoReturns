using UnityEngine;

namespace BossFight.Rules
{
    // Strings shown by the fight HUD and the You Win screen.
    public static class BossHudText
    {
        public static string Money(float money)
        {
            return "$" + Mathf.RoundToInt(money);
        }

        public static string Objective(BossPhase phase, int monstersAlive)
        {
            switch (phase)
            {
                case BossPhase.Phase1:
                    return $"Kill the monster customers ({monstersAlive} left)";
                case BossPhase.Phase2:
                    return "Kill the Witch";
                default:
                    return "";
            }
        }

        public static string Score(float moneyLeft)
        {
            return "Money left: " + Money(moneyLeft);
        }
    }
}
