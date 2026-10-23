using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class HeavyBomber : DistrictEnemy
{
    protected override void Configure(){hull=2;health=55;hullScale=.9f;attackInterval=2.8f;Mover<Slalom>();}
    protected override void Attack(){Fan<BurstShell>(2,35,3,.42f);}
}
