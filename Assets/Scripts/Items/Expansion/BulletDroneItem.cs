[UnityEngine.Scripting.Preserve]
public sealed class BulletDroneItem : AItem
{
    void Awake(){SetData("Bullet drone","Rear guard","A trailing companion with twin guns.",ItemType.partner);}
    public override void OnBegin(){}
    public override void OnPickup(){TrailDrone.Attach(s,false);base.OnPickup();}
}
