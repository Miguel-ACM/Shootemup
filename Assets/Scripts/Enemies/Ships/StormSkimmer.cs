using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class StormSkimmer : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 11;
        health = 22;
        hullScale = .55f;
        attackInterval = 2.3f;
        Mover<Slalom>();
    }

    protected override void Attack()
    {
        Fan<GuidedMissile>(2, 24, 5, .18f);
    }
}
