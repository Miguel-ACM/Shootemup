using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class OrbitalScout : SpaceSatellite
{
    protected override void Configure()
    {
        hullScale = .46f;
        health = 35;
        attackInterval = 2.1f;
        Mover<Slalom>();
    }

    protected override void Attack()
    {
        Fan<NeedleRound>(3, 16, 4.7f, .18f);
    }
}
