[UnityEngine.Scripting.Preserve]
public sealed class LaserDroneItem : AItem
{
    void Awake()
    {
        SetData("Laser drone", "Follow the leader", "A trailing companion that fires lasers straight up.", ItemType.partner);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        TrailDrone.Attach(s, true);
        base.OnPickup();
    }
}
