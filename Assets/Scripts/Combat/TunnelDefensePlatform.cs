using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class TunnelDefensePlatform : DistrictEnemy
{
    static readonly Vector3[] ports={new Vector3(-.20f,-1.09f,-.48f),new Vector3(0,-1.09f,-.48f),new Vector3(.20f,-1.09f,-.48f)};
    protected override void Configure(){hull=25;health=260;hullScale=1.1f;attackInterval=2.1f;Mover<TopTracker>();}
    protected override void Attack(){if(volley%2==0){FanFrom<RicochetBolt>(4,30,4,.22f,ports);}else{FanFrom<ProximityMine>(2,50,2.5f,.25f,ports);}}
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount){base.damage(amount*(movement!=null && !movement.CanFire ? .75f : 1.2f));}
}
