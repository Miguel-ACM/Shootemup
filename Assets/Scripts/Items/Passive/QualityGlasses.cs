[UnityEngine.Scripting.Preserve]
public sealed class QualityGlasses : AItem
{
    void Awake()
    {
        SetData("Quality glasses", "Clear view", "Enemy bullets move 10% slower.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.QualityGlasses);
        base.OnPickup();
    }
}
