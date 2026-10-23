using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class FieldsHeavyWing : DistrictEnemy
{
    protected override void Configure(){hull=24;health=260;hullScale=1.1f;attackInterval=2.1f;Mover<TopTracker>();}
    protected override void Attack(){if(volley%2==0){Fan<BurstShell>(2,32,3.2f,.4f);}else{Fan<NeedleRound>(5,14,5);}}
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount){base.damage(amount*(movement!=null && !movement.CanFire ? .75f : 1.2f));}
}
