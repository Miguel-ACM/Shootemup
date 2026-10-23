using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class SiegeGunship : DistrictEnemy
{
    protected override void Configure(){hull=7;health=65;hullScale=.9f;attackInterval=2.7f;Mover<TopTracker>();}
    protected override void Attack()
    {
        for(int i=-1;i<=1;i++)
        {
            var muzzle=Hull.TransformPoint(new Vector3(i*.78f,i==0 ? -1.06f : -.98f,-.34f));
            CombatRound.Launch<SuspendedRound>(muzzle,Vector2.down,5,.23f);
        }
    }
}
