[UnityEngine.Scripting.Preserve]
public sealed class SplitterCore : AItem
{
    void Awake()
    {
        SetData("Splitter Core", "Ship equipment", "Hits sometimes split into two shots.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Splitter);
        base.OnPickup();
    }
}
