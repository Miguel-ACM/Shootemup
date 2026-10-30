// The early bird: every energy drop is worth twice as much.
[UnityEngine.Scripting.Preserve]
public sealed class EarlyRise : AItem
{
    void Awake()
    {
        SetData("Early Rise", "Seize the day", "Energy drops give double.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        GameRules.manaDropMultiplier *= 2;
        base.OnPickup();
    }
}
