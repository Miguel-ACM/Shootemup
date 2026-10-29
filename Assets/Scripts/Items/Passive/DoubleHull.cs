[UnityEngine.Scripting.Preserve]
public sealed class DoubleHull : AItem
{
    void Awake()
    {
        SetData("Double Hull", "Belt and braces", "+2 maximum hits.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.AddMaxHits(2);
        base.OnPickup();
    }
}
