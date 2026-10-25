[UnityEngine.Scripting.Preserve]
public sealed class DroneLink : AItem
{
    void Awake()
    {
        SetData("Drone Link", "Ship equipment", "Support drone. +20% companion damage.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Drone);
        base.OnPickup();
    }
}
