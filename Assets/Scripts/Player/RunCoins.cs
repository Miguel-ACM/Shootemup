// A run's wallet is independent of score and multiplier, and survives district travel.
public static class RunCoins
{
    public static int Count
    {
        get; private set;
    }

    public static void Reset()
    {
        Count = 0;
    }

    public static void Add(int amount)
    {
        if (amount > 0)
            Count = amount > int.MaxValue - Count ? int.MaxValue : Count + amount;
    }

    public static bool TrySpend(int amount)
    {
        if (amount < 0 || Count < amount)
            return false;
        Count -= amount;
        return true;
    }
}
