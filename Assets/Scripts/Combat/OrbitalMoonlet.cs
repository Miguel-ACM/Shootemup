using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class OrbitalMoonlet : SpaceSatellite
{
    protected override void Configure(){hull=44;hullScale=.28f;health=16;attackInterval=1.3f;nextAttack=.8f;}
    protected override void Attack(){for(int s=-1;s<=1;s+=2)Fire<SimpleRound>(transform.position,CombatVisuals.Rotate(Aim,s*7),5.7f,.12f);}
}
