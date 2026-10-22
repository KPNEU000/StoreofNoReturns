namespace BossFight
{
    // Money and reputation left when the fight is won; read by the win overlay.
    public static class RunScore
    {
        public static float Value { get; private set; }
        public static int Reputation { get; private set; }

        public static void Set(float moneyLeft, int reputation = 0)
        {
            Value = moneyLeft;
            Reputation = reputation;
        }

        public static void Reset()
        {
            Value = 0f;
            Reputation = 0;
        }
    }
}
