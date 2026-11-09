[UnityEngine.Scripting.Preserve]
public sealed class InkPot : AItem
{
    void Awake()
    {
        SetData("Ink Pot", "Mind the carpet", "Kills spill ink that slows bullets.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Ink);
        base.OnPickup();
    }
}
