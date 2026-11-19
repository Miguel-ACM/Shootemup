[UnityEngine.Scripting.Preserve]
public sealed class FocusingLens : AItem
{
    void Awake()
    {
        SetData("Combo Lens", "Ship equipment", "Consecutive hits deal up to +15% damage.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Focus);
        base.OnPickup();
    }
}
