[UnityEngine.Scripting.Preserve]
public sealed class Pinheads : AItem
{
    void Awake() => SetData("Pinheads", "Sharp little things", "Rounds are smaller and faster.", ItemType.passive);

    public override void OnBegin() { }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Pinheads);
        base.OnPickup();
    }
}
