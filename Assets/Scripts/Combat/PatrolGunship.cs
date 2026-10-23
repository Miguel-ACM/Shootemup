using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class PatrolGunship : DistrictEnemy
{
    static readonly Vector3[] ports={new Vector3(-.64f,-1.00f,-.12f),new Vector3(.64f,-1.00f,-.12f)};
    protected override void Configure(){hull=1;health=35;attackInterval=1.4f;Mover<TopTracker>();}
    protected override void Attack(){FanFrom<NeedleRound>(3,7,5.5f,.14f,ports);}
}
