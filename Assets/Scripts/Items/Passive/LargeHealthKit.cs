[UnityEngine.Scripting.Preserve]
public sealed class LargeHealthKit : AItem
{
    void Awake()
    {
        SetData("Large Health Kit", "Emergency repair", "Repair three hits.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        HealthRecovery.Begin(s, 3);
        base.OnPickup();
    }
}
