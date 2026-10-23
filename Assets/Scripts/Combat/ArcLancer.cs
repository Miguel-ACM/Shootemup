using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ArcLancer : DistrictEnemy
{
    protected override void Configure(){hull=18;health=34;hullScale=.62f;attackInterval=3.5f;Mover<StrafeAndBrake>();}
    protected override void Attack(){CombatRound.Launch<SegmentedBeam>(transform.position,Aim,0);}
}
