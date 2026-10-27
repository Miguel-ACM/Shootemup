using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class AvalancheBomber : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 17;
        health = 80;
        hullScale = .85f;
        attackInterval = 2.0f;
        shellTravelDistance = 8.4f;
        Mover<DriftDownPattern>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        firingCue.Warn(seconds);
    }

    protected override void Attack()
    {
        if (volley % 2 == 1)
            Fan<BurstShell>(2, 37, 3.2f, .46f);
        else
            Fan<SuspendedRound>(4, 25, 2.8f, .23f);
    }
}
