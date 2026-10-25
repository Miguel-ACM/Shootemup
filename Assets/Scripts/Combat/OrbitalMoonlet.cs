using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class OrbitalMoonlet : SpaceSatellite
{
    protected override void Configure(){hull=44;hullScale=.28f;health=24;attackInterval=1.1f;nextAttack=.8f;}
    protected override void Attack(){for(int s=-1;s<=1;s++)Fire<SimpleRound>(transform.position,CombatVisuals.Rotate(Aim,s*11),5.7f,.12f);}
}
