[UnityEngine.Scripting.Preserve]
public sealed class RubberBand : AItem
{
    void Awake()
    {
        SetData("Rubber band", "Second chance", "Missed rounds turn back once at the top edge.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.RubberBand);
        base.OnPickup();
    }
}
