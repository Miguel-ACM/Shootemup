using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class HeavyBomber : DistrictEnemy
{
    protected override void Configure(){hull=2;health=80;hullScale=.9f;attackInterval=1.35f;Mover<Slalom>();}
    protected override void Attack(){Fan<BurstShell>(3,28,4.2f,.42f);Fan<NeedleRound>(7,12,4.5f);}
}
