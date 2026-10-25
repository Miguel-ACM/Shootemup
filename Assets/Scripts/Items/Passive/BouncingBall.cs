[UnityEngine.Scripting.Preserve]
public sealed class BouncingBall : AItem
{
    void Awake()
    {
        SetData("Bouncing ball", "One more", "Shots bounce once toward another enemy.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.BouncingBall);
        base.OnPickup();
    }
}
