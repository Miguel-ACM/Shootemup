[UnityEngine.Scripting.Preserve]
public sealed class BrassKnuckles : AItem
{
    void Awake()
    {
        SetData("Brass Knuckles", "Up close and personal", "Point-blank hits land much harder.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.BrassKnuckles);
        base.OnPickup();
    }
}
