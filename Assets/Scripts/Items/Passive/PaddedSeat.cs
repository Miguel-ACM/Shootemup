[UnityEngine.Scripting.Preserve]
public sealed class PaddedSeat : AItem
{
    void Awake() => SetData("Padded Seat", "Soft landing", "Stay invulnerable longer after losing a health pip.", ItemType.passive);

    public override void OnBegin() { }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.PaddedSeat);
        base.OnPickup();
    }
}
