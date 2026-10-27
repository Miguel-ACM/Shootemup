using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class OrbitalMoonlet : SpaceSatellite
{
    protected override void Configure()
    {
        hull = 44;
        hullScale = .28f;
        health = 24;
        attackInterval = 1.4f;
        nextAttack = .8f;
    }

    protected override void Attack()
    {
        Fan<SimpleRound>(2, 17, 4.1f, .18f);
    }
}
