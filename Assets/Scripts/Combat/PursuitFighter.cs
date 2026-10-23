using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class PursuitFighter : DistrictEnemy
{
    protected override void Configure(){hull=12;health=20;hullScale=.45f;attackInterval=2.5f;Mover<TopTracker>();}
    protected override void Attack(){CombatRound.Launch<PulseLaser>(transform.position,Aim,0);}
}
