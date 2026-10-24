using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class AvalancheBomber : DistrictEnemy
{
    protected override void Configure(){hull=17;health=80;hullScale=.85f;attackInterval=1.25f;Mover<DriftDownPattern>();}
    protected override void Attack(){Fan<BurstShell>(5,24,4.6f,.46f);Fan<SuspendedRound>(7,14,4,.2f);}
}
