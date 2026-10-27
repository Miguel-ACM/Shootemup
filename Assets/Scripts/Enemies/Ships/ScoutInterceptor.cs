using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ScoutInterceptor : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 0;
        health = 13;
        hullScale = .48f;
        attackInterval = (0.8f);
        Mover<Zigzag>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        firingCue.Warn(seconds);
    }

    protected override void Attack()
    {
        Fan<NeedleRound>((2), 13, 5.0f, .17f);
    }
}
