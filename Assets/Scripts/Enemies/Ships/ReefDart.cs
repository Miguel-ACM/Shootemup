using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class ReefDart : OceanEnemy
{
    protected override void Configure()
    {
        hull = 50;
        hullScale = .49f;
        health = 33;
        attackInterval = (1.9f);
        Mover<Slalom>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        firingCue.Warn(seconds);
    }

    protected override void Attack()
    {
        Fan<NeedleRound>((2), 17, 3.7f, .2f);
    }
}
