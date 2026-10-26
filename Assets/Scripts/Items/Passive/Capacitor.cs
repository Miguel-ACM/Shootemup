[UnityEngine.Scripting.Preserve]
public sealed class Capacitor : AItem
{
    void Awake()
    {
        SetData("Idle Charge", "Ship equipment", "Stop firing to charge triple damage.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Capacitor);
        base.OnPickup();
    }
}
