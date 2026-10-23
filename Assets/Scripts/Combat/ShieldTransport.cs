using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ShieldTransport : DistrictEnemy
{
    protected override void Configure(){hull=8;health=70;hullScale=.95f;attackInterval=2.5f;Mover<DriftDownPattern>();}
    protected override void Attack(){Fan<PlasmaOrb>(2,32,2,.55f);}
}
