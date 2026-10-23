using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class PatrolGunship : DistrictEnemy
{
    protected override void Configure(){hull=1;health=35;attackInterval=1.4f;Mover<TopTracker>();}
    protected override void Attack(){Fan<NeedleRound>(3,7,5.5f);}
}
