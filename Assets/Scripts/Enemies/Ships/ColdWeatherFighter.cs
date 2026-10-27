using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ColdWeatherFighter : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 6;
        health = 24;
        hullScale = .62f;
        attackInterval = 1.25f;
        Mover<Slalom>();
    }

    protected override void Attack()
    {
        Fan<NeedleRound>(3, 18, 4.0f, .19f);
    }
}
