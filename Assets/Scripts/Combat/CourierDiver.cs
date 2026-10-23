using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class CourierDiver : DistrictEnemy
{
    protected override void Configure(){hull=15;health=9;hullScale=.34f;attackInterval=.8f;Mover<CommittedDive>();}
    protected override void Attack(){Fan<NeedleRound>(1,0,6);}
}
