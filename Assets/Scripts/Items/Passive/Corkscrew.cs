[UnityEngine.Scripting.Preserve]
public sealed class Corkscrew : AItem
{
    void Awake()
    {
        SetData("Corkscrew", "Twist, then pull", "Shots spiral.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Corkscrew);
        base.OnPickup();
    }
}
