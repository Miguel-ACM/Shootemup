[UnityEngine.Scripting.Preserve]
public sealed class RelaySwitch : AItem
{
    void Awake()
    {
        SetData("Relay Switch", "Other contact", "Fire one gun to charge the other. Swap triggers for a burst of faster fire.", ItemType.passive);
    }

    public override void OnBegin() { }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.RelaySwitch);
        base.OnPickup();
    }
}
