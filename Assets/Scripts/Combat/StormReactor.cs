using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class StormReactor : DistrictMiniboss
{
    protected override void Configure(){hull=27;health=480;hullScale=1.1f;attackInterval=.72f;Mover<TopTracker>();}
    protected override void Attack()
    {
        if(volley%4!=1){Fan<NeedleRound>(9,13,5.5f);return;}
        var laser=Fire<PulseLaser>(transform.position,Aim,0);
        if(laser!=null)laser.Bind(Hull,new Vector3(0,-.35f,-.4f));
    }
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount){base.damage(amount*(movement!=null && !movement.CanFire ? .75f : 1.2f));}
}
