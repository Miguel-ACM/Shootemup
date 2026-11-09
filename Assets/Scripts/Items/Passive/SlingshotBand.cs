[UnityEngine.Scripting.Preserve]
public sealed class SlingshotBand : AItem
{
    void Awake()
    {
        SetData("Slingshot Band", "Pull back further", "Shots accelerate and hit harder the farther they fly.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Slingshot);
        base.OnPickup();
    }
}
