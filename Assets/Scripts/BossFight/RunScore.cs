namespace BossFight
{
    // Money left when the fight is won; read by the You Win screen.
    public static class RunScore
    {
        public static float Value { get; private set; }

        public static void Set(float moneyLeft)
        {
            Value = moneyLeft;
        }

        public static void Reset()
        {
            Value = 0f;
        }
    }
}
