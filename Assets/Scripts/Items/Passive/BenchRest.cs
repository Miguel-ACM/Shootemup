[UnityEngine.Scripting.Preserve]
public sealed class BenchRest : AItem
{
    void Awake() => SetData("Bench Rest", "Hold steady", "Deal 20% more damage while standing still.", ItemType.passive);

    public override void OnBegin() { }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.BenchRest);
        base.OnPickup();
    }
}
