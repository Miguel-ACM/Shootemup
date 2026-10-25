using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class OrbitalMoonlet : SpaceSatellite
{
    protected override void Configure(){hull=44;hullScale=.28f;health=16;attackInterval=1.6f;nextAttack=.8f;}
    protected override void Attack(){Fire<SimpleRound>(transform.position,Aim,4.9f,.12f);}
}
