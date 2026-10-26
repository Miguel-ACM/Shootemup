using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class ReefDart : OceanEnemy
{
    protected override void Configure()
    {
        hull = 50;
        hullScale = .49f;
        health = 33;
        attackInterval = Timing(2.5f, 1.9f);
        Mover<Slalom>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        if (Difficulty.Easy)
            Cue(Muzzle(), seconds);
        else
            firingCue.Warn(seconds);
    }

    protected override void Attack()
    {
        Fan<NeedleRound>(Shots(1, 2), 17, 3.7f, .2f);
    }
}
