using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class RelayDrone : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 10;
        health = 32;
        hullScale = .55f;
        attackInterval = 1.9f;
        Mover<StrafeAndBrake>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        firingCue.Warn(seconds);
    }

    protected override void Attack()
    {
        int n = 3;
        for (int i = 0; i < n; i++)
            Fire<NeedleRound>(Muzzle(n == 2 ? i * 2 : i), CombatVisuals.Rotate(Aim, (i - (n - 1) * .5f) * 15), 4.8f, .18f);
    }
}
