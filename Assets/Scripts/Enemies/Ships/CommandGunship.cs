using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class CommandGunship : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 14;
        health = 90;
        hullScale = .8f;
        attackInterval = .9f;
        Mover<TopTracker>();
    }

    protected override void Attack()
    {
        Fan<ReturningDisc>(4, 24, 3.8f, .24f);
        Fan<NeedleRound>(9, 10, 5);
    }

    protected override void Start()
    {
        base.Start();
        if (movement is FormationFlight)
            return;
        foreach (int side in new[] { -1, 1 })
        {
            var e = Spawn(typeof(ScoutInterceptor), transform.position + Vector3.right * side * 1.4f);
            e.Escort(transform);
        }
    }
}
