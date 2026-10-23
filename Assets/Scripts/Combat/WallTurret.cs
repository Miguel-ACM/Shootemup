using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class WallTurret : DistrictEnemy
{
    protected override void Configure(){hull=3;health=35;hullScale=.55f;attackInterval=2.3f;Mover<DriftDownPattern>();}
    protected override void Attack(){CombatRound.Launch<RicochetBolt>(transform.position,new Vector2(transform.position.x>0 ? -1 : 1,-.6f),5,.2f);}
}
