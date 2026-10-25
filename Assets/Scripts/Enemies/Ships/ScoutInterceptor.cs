using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ScoutInterceptor : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 0;
        health = 13;
        hullScale = .48f;
        attackInterval = .65f;
        Mover<Zigzag>();
    }

    protected override void Attack()
    {
        Fan<NeedleRound>(2, 10, 6);
    }
}
