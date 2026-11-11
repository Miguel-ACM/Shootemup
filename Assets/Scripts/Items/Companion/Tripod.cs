[UnityEngine.Scripting.Preserve]
public sealed class Tripod : AItem
{
    void Awake()
    {
        SetData("Tripod", "Set up here", "Tap Focus to plant a turret or recall it. It fires with you while planted.", ItemType.partner);
    }

    public override void OnBegin() { }

    public override void OnPickup()
    {
        TripodTurret.Attach(s);
        base.OnPickup();
    }
}
