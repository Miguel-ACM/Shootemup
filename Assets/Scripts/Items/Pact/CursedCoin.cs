// Fortune for the rest of the run; the enemies are paid in kind.
[UnityEngine.Scripting.Preserve]
public sealed class CursedCoin : APactItem
{
    void Awake()
    {
        SetPact("Cursed Coin", "Future items are one rarity higher.", "Enemies have +25% health.");
    }

    protected override void Seal()
    {
        PactPool.BindCursedCoin();
    }
}
