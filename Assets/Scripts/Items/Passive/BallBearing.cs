[UnityEngine.Scripting.Preserve]
public sealed class BallBearing : AItem
{
    void Awake()
    {
        SetData("Shrapnel Bearing", "Ship equipment", "Hits sometimes spray three fragments.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Bearing);
        base.OnPickup();
    }
}
