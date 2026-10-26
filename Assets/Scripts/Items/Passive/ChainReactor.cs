[UnityEngine.Scripting.Preserve]
public sealed class ChainReactor : AItem
{
    void Awake()
    {
        SetData("Burn Detonator", "Ship equipment", "Burning enemies explode on death.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.ChainReactor);
        base.OnPickup();
    }
}
