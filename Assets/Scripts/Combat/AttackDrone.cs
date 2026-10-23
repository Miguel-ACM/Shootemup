using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class AttackDrone : DistrictEnemy
{
    protected override void Configure(){hull=13;health=14;hullScale=.38f;attackInterval=1.7f;Mover<FreeReposition>();}
    protected override void Attack(){Fan<BurstShell>(1,0,4,.28f);}
}
