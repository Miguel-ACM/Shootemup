[UnityEngine.Scripting.Preserve]
public sealed class Compost : AItem
{
    void Awake()
    {
        SetData("Compost", "Nothing goes to waste", "Kills leave fumes that poison enemies passing through.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Compost);
        base.OnPickup();
    }
}
