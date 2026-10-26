using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class OrbitalScout : SpaceSatellite
{
    protected override void Configure()
    {
        hull = 41;
        hullScale = .46f;
        health = 35;
        attackInterval = Timing(2.2f, 1.6f);
        Mover<Slalom>();
    }

    protected override void Attack()
    {
        Fan<NeedleRound>(Shots(3, 5), 16, 4.7f, .18f);
    }
}
