using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class PatrolGunship : DistrictEnemy
{
    static readonly Vector3[] ports = { new Vector3(-.64f, -1.00f, -.12f), new Vector3(.64f, -1.00f, -.12f) };
    protected override void Configure()
    {
        hull = 1;
        health = 35;
        attackInterval = Timing(2.0f, 1.5f);
        Mover<TopTracker>();
    }

    protected override void Attack()
    {
        FanFrom<NeedleRound>(Shots(2, 3), 12, 4.6f, .18f, ports);
    }
}
