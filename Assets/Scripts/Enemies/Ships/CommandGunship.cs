using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class CommandGunship : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 14;
        health = 90;
        hullScale = .8f;
        attackInterval = Timing(2.1f, 1.5f);
        Mover<TopTracker>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        if (Difficulty.Easy && (volley + 1) % 2 == 0)
        {
            Cue(Muzzle(0), seconds);
            Cue(Muzzle(1), seconds);
        }
        else
            firingCue.Warn(seconds);
    }

    protected override void Attack()
    {
        if (volley % 2 == 0)
            Fan<ReturningDisc>(Shots(2, 4), 30, 2.9f, .29f);
        else
            Fan<NeedleRound>(Shots(4, 7), 16, 4.1f, .18f);
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
