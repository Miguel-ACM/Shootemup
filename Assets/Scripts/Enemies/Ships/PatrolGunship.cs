using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class PatrolGunship : DistrictEnemy
{
    static readonly Vector3[] ports = { new Vector3(-.64f, -1.00f, -.12f), new Vector3(.64f, -1.00f, -.12f) };
    protected override void Configure()
    {
        health = 35;
        attackInterval = (1.5f);
        Mover<TopTracker>();
    }

    protected override void Attack()
    {
        // Four arrows on Normal and Hard, three on Easy, over a similar width.
        int count = Difficulty.Easy ? 3 : 4;
        FanFrom<NeedleRound>(count, count == 4 ? 9 : 12, 4.6f, .18f, ports);
    }
}
