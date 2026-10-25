[UnityEngine.Scripting.Preserve]
public sealed class HotSauce : AItem
{
    void Awake()
    {
        SetData("Hot sauce", "Extra spicy", "Burning enemies leave fire in their wake.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.HotSauce);
        base.OnPickup();
    }
}
