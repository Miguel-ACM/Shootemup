[UnityEngine.Scripting.Preserve]
public sealed class TicketPunch : AItem
{
    void Awake() => SetData("Ticket Punch", "Fifth one’s on us", "Every fifth kill drops an extra coin.", ItemType.passive);

    public override void OnBegin() { }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.TicketPunch);
        base.OnPickup();
    }
}
