[UnityEngine.Scripting.Preserve]
public sealed class Flyswatter : AItem
{
    void Awake() => SetData("Flyswatter", "Shoo", "Deal 25% more damage to small enemies.", ItemType.passive);

    public override void OnBegin() { }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Flyswatter);
        base.OnPickup();
    }
}
