[UnityEngine.Scripting.Preserve]
public sealed class Poison : AItem
{
    void Awake()
    {
        SetData("Poison", "Do not drink", "Green bullets poison enemies.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Poison);
        base.OnPickup();
    }
}
