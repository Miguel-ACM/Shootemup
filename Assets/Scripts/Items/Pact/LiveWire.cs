[UnityEngine.Scripting.Preserve]
public sealed class LiveWire : APactItem
{
    public const int Price = 3;

    void Awake()
    {
        SetPact("Live Wire", "Actives use no energy.", "Every active costs 3 multiplier steps.");
    }

    protected override void Seal() => s.GetComponent<CombatUpgrades>().Add(UpgradeKind.LiveWire);
}
