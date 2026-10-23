using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class BeamDrone : DistrictEnemy
{
    protected override void Configure(){hull=9;health=25;hullScale=.55f;attackInterval=3;Mover<FreeReposition>();}
    protected override void Attack(){CombatRound.Launch<PulseLaser>(transform.position,Aim,0);}
}
