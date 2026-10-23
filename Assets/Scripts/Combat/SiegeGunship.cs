using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class SiegeGunship : DistrictEnemy
{
    protected override void Configure(){hull=7;health=65;hullScale=.9f;attackInterval=2.7f;Mover<TopTracker>();}
    protected override void Attack(){for(int i=-1;i<=1;i++)CombatRound.Launch<SuspendedRound>(transform.position+Vector3.right*i*1.15f,Vector2.down,5,.23f);}
}
