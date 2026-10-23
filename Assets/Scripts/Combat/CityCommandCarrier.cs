using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class CityCommandCarrier : DistrictEnemy
{
    protected override void Configure(){hull=28;health=260;hullScale=1.1f;attackInterval=2.1f;Mover<TopTracker>();}
    protected override void Attack(){if(volley%2==0){Fan<ReturningDisc>(3,24,4,.22f);}else{Fan<NeedleRound>(7,9,6);}}
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount){base.damage(amount*(movement!=null && !movement.CanFire ? .75f : 1.2f));}
}
