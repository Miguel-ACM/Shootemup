using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ShieldTransport : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 8;
        health = 100;
        hullScale = .95f;
        attackInterval = 1.3f;
        Mover<DriftDownPattern>();
    }

    protected override void Attack()
    {
        Fan<PlasmaOrb>(3, 30, 2.6f, .55f);
        Fan<NeedleRound>(7, 14, 4.8f);
    }
}
