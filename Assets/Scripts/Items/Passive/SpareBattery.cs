[UnityEngine.Scripting.Preserve]
public sealed class SpareBattery : AItem
{
    void Awake()
    {
        SetData("Hit-Charged Rocket", "Ship equipment", "Hits charge a powerful homing shot.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Battery);
        base.OnPickup();
    }
}
