[UnityEngine.Scripting.Preserve]
public sealed class ReturnModule : AItem
{
    void Awake()
    {
        SetData("Returning Volley", "Ship equipment", "Every fourth volley returns.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Return);
        base.OnPickup();
    }
}
