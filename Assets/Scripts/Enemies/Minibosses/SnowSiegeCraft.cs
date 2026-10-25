using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class SnowSiegeCraft : DistrictMiniboss
{
    static readonly Vector3[] ports={new Vector3(0,-1.46f,-.3f)};
    protected override void Configure(){hull=26;health=480;hullScale=1.1f;attackInterval=.72f;shellTravelDistance=7.2f;Mover<TopTracker>();}
    protected override void Attack(){if(volley%2==0){FanFrom<BurstShell>(5,22,3,.44f,ports);}else{FanFrom<SuspendedRound>(11,12,5,.24f,ports);}}
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount){base.damage(amount*(movement!=null && !movement.CanFire ? .75f : 1.2f));}
}
