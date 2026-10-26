[UnityEngine.Scripting.Preserve]
public sealed class DoubleTrouble : AItem
{
    void Awake()
    {
        SetData("Linked Triggers", "Two barrels", "Either trigger fires both equipped guns.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.DoubleTrouble);
        base.OnPickup();
    }
}
