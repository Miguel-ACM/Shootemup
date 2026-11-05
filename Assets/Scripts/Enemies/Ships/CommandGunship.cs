using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class CommandGunship : DistrictEnemy
{
    protected override void Configure()
    {
        health = 90;
        hullScale = .8f;
        attackInterval = (1.5f);
        Mover<TopTracker>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        firingCue.Warn(seconds);
    }

    // Broad chevron headlights, then a street lattice of two crossing families.
    protected override void Attack()
    {
        if (volley % 2 == 0)
            Fan<ChevronRound>(4, 30, 2.9f, .24f);
        else
        {
            int lanes = Dense(4);
            for (int family = -1; family <= 1; family += 2)
                for (int i = 0; i < lanes; i++)
                    Fire<NeedleRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.down, family * 30 + (i - (lanes - 1) * .5f) * 9), 3.6f, .18f);
        }
    }

    protected override void Start()
    {
        base.Start();
        if (movement is FormationFlight)
            return;
        foreach (int side in new[] { -1, 1 })
        {
            var e = Spawn(typeof(ScoutInterceptor), transform.position + Vector3.right * side * 1.4f);
            e.Escort(transform);
        }
    }
}
