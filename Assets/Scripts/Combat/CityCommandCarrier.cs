using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class CityCommandCarrier : DistrictEnemy
{
    static readonly Vector3[] ports={new Vector3(-1.04f,-1.33f,-.18f),new Vector3(1.04f,-1.33f,-.18f)};
    protected override void Configure(){hull=28;health=260;hullScale=1.1f;attackInterval=2.1f;Mover<TopTracker>();}
    protected override void Attack(){if(volley%2==0){FanFrom<ReturningDisc>(3,24,4,.22f,ports);}else{FanFrom<NeedleRound>(7,9,6,.14f,ports);}}
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount){base.damage(amount*(movement!=null && !movement.CanFire ? .75f : 1.2f));}
}
