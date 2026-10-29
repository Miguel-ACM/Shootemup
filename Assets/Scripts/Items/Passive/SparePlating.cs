[UnityEngine.Scripting.Preserve]
public sealed class SparePlating : AItem
{
    void Awake()
    {
        SetData("Spare Plating", "Bolt-on armor", "+1 maximum hit.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.AddMaxHits(1);
        base.OnPickup();
    }
}
