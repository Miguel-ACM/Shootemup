[UnityEngine.Scripting.Preserve]
public sealed class Crowbar : AItem
{
    void Awake() => SetData("Crowbar", "A little leverage", "Deal 20% more damage to bosses.", ItemType.passive);

    public override void OnBegin() { }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Crowbar);
        base.OnPickup();
    }
}
