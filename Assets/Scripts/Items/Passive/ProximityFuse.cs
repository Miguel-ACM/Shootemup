[UnityEngine.Scripting.Preserve]
public sealed class ProximityFuse : AItem
{
    void Awake()
    {
        SetData("Proximity Fuse", "Ship equipment", "Shots burst near enemies.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Proximity);
        base.OnPickup();
    }
}
