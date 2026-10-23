using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class SnowSiegeCraft : DistrictEnemy
{
    protected override void Configure(){hull=26;health=260;hullScale=1.1f;attackInterval=2.1f;Mover<TopTracker>();}
    protected override void Attack(){if(volley%2==0){Fan<BurstShell>(4,24,3,.44f);}else{Fan<SuspendedRound>(5,18,5,.24f);}}
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount){base.damage(amount*(movement!=null && !movement.CanFire ? .75f : 1.2f));}
}
