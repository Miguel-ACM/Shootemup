using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class AvalancheBomber : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 17;
        health = 80;
        hullScale = .85f;
        attackInterval = (2.0f);
        shellTravelDistance = 8.4f;
        Mover<DriftDownPattern>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        firingCue.Warn(seconds);
    }

    // Snow shells, then pillars that start almost still and accelerate.
    protected override void Attack()
    {
        if (volley % 2 == 1)
            Fan<BurstShell>(2, 37, 3.2f, .46f);
        else
        {
            int count = Dense(4);
            for (int i = 0; i < count; i++)
                Fire<SimpleRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.down, (i - (count - 1) * .5f) * 22), .3f, .22f)
                    .Ramp(11, 1.4f, .3f + i * .2f);
        }
    }
}
