[UnityEngine.Scripting.Preserve]
public sealed class LaserDroneItem : AItem
{
    void Awake(){SetData("Laser drone","Follow the leader","A trailing companion with a charged laser.",ItemType.partner);}
    public override void OnBegin(){}
    public override void OnPickup(){TrailDrone.Attach(s,true);base.OnPickup();}
}
