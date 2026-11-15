[UnityEngine.Scripting.Preserve]
public sealed class TapeMeasure : AItem
{
    void Awake()
    {
        SetData("Tape Measure", "Measure twice", "Hits land harder the farther they travel.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.TapeMeasure);
        base.OnPickup();
    }
}
