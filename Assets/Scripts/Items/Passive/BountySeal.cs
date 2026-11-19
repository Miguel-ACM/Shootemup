[UnityEngine.Scripting.Preserve]
public sealed class BountySeal : AItem
{
    void Awake() => SetData("Bounty Seal", "Signed in duplicate", "Bosses drop two reward items. Keep both.", ItemType.passive);

    public override void OnBegin() { }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.BountySeal);
        base.OnPickup();
    }
}
