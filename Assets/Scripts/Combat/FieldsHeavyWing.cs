using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class FieldsHeavyWing : DistrictEnemy
{
    static readonly Vector3[] ports={new Vector3(-.68f,-1.20f,-.30f),new Vector3(.68f,-1.20f,-.30f)};
    protected override void Configure(){hull=24;health=480;hullScale=1.1f;attackInterval=.65f;Mover<TopTracker>();}
    protected override void Attack(){if(volley%2==0){FanFrom<BurstShell>(4,22,3.2f,.4f,ports);}else{FanFrom<NeedleRound>(11,11,5,.14f,ports);}}
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount){base.damage(amount*(movement!=null && !movement.CanFire ? .75f : 1.2f));}
}
