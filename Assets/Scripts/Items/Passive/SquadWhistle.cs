[UnityEngine.Scripting.Preserve]
public sealed class SquadWhistle : AItem
{
    void Awake()
    {
        SetData("Squad Whistle", "Rally the wing", "+50% companion damage.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Squad);
        base.OnPickup();
    }
}
