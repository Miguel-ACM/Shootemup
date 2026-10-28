using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class MantaRay : OceanEnemy
{
    protected override void Configure()
    {
        hull = 51;
        hullScale = .57f;
        health = 58;
        attackInterval = (2.2f);
        Mover<Slalom>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        firingCue.Warn(seconds);
    }

    // Three rounds caught in a current that sweeps them toward the arena centre.
    protected override void Attack()
    {
        int n = Dense(3);
        float current = transform.position.x > 0 ? -1.3f : 1.3f;
        for (int i = 0; i < n; i++)
            Fire<SimpleRound>(Muzzle(n == 2 ? i * 2 : i), CombatVisuals.Rotate(Aim, (i - (n - 1) * .5f) * 29 - current * 12), 2.5f, .25f)
                .Fall(new Vector2(current, 0));
    }
}
