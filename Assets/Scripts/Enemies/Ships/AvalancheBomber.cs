using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class AvalancheBomber : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 17;
        health = 80;
        hullScale = .85f;
        attackInterval = 1.25f;
        shellTravelDistance = 8.4f;
        Mover<DriftDownPattern>();
    }

    protected override void Attack()
    {
        Fan<BurstShell>(2, 34, 4.3f, .44f);
        Fan<SuspendedRound>(4, 20, 3.8f, .18f);
    }
}
