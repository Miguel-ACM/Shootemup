[UnityEngine.Scripting.Preserve]
public sealed class LoadedDice : AItem
{
    void Awake()
    {
        SetData("Loaded Dice", "Weighted in your favour", "Chance effects can trigger twice.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.LoadedDice);
        base.OnPickup();
    }
}
