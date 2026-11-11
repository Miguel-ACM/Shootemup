[UnityEngine.Scripting.Preserve]
public sealed class LiveWire : APactItem
{
    public const int Price = 1;

    void Awake()
    {
        SetPact("Live Wire", "Paid actives use no energy.", "Each activation costs one multiplier step.");
    }

    protected override void Seal() => s.GetComponent<CombatUpgrades>().Add(UpgradeKind.LiveWire);
}
