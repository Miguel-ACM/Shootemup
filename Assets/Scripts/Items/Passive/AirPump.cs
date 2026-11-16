[UnityEngine.Scripting.Preserve]
public sealed class AirPump : AItem
{
    void Awake()
    {
        SetData("Air Pump", "Under pressure", "Rounds swell and slow as they travel.", ItemType.passive);
    }

    public override void OnBegin() { }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.AirPump);
        base.OnPickup();
    }
}
