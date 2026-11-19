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
        int count = Firing == FiringTier.Easy ? 3 : Firing == FiringTier.Extreme ? 5 : 4;
        float width = count == 3 ? 24 : 27;
        FanFrom<NeedleRound>(count, width / (count - 1), 4.6f, .18f, ports);
    }
}
