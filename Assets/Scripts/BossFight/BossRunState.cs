public static class BossRunState
{
    public static bool HasData;
    public static float Money;
    public static int Reputation;
    public static int AngeredCustomers;

    public static void Save(float money, int reputation, int angered)
    {
        Money = money;
        Reputation = reputation;
        AngeredCustomers = angered;
        HasData = true;
    }

    public static void Clear()
    {
        HasData = false;
    }
}