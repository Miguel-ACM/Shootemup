using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class StormSkimmer : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 11;
        health = 22;
        hullScale = .55f;
        attackInterval = Timing(3.0f,2.2f);
        Mover<Slalom>();
    }

    protected override void Attack()
    {
        Fan<GuidedMissile>(Shots(1,2),30,3.8f,.23f);
    }
}
