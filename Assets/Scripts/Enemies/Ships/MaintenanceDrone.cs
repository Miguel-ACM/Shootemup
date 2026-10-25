using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class MaintenanceDrone : DistrictEnemy
{
    protected override void Configure(){hull=4;health=25;hullScale=.5f;attackInterval=1.7f;Mover<FreeReposition>();}
    protected override void Attack(){HelixRound.Pair(transform.position,Aim);}
}
