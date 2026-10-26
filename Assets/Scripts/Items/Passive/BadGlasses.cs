[UnityEngine.Scripting.Preserve]
public sealed class BadGlasses : AItem
{
    void Awake()
    {
        SetData("Close-Range Slow", "Near sighted", "Enemy bullets move 10% slower near you.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.BadGlasses);
        base.OnPickup();
    }
}
