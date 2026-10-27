using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class HeavyBomber : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 2;
        health = 80;
        hullScale = .9f;
        attackInterval = 1.8f;
        shellTravelDistance = 5.2f;
        Mover<Slalom>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        firingCue.Warn(seconds);
    }

    protected override void Attack()
    {
        if (volley % 2 == 0)
            Fan<BurstShell>(2, 32, 3.0f, .44f);
        else
            Fan<NeedleRound>(5, 18, 3.8f, .19f);
    }
}
