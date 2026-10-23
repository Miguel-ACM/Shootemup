using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class AvalancheBomber : DistrictEnemy
{
    protected override void Configure(){hull=17;health=50;hullScale=.85f;attackInterval=2.4f;Mover<DriftDownPattern>();}
    protected override void Attack(){Fan<BurstShell>(3,24,3.2f,.46f);}
}
