using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ArmoredCarrier : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 5;
        health = 130;
        hullScale = .9f;
        attackInterval = (1.9f);
        Mover<DriftDownPattern>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        firingCue.Warn(seconds);
    }

    // Each needle has a slow heavy twin on the same line.
    protected override void Attack()
    {
        if (volley % 2 == 0)
            Fan<ProximityMine>(2, 62, 2.4f, .3f);
        else
        {
            int count = Dense(5);
            var aim = Aim;
            for (int i = 0; i < count; i++)
            {
                var axis = CombatVisuals.Rotate(aim, (i - (count - 1) * .5f) * 17);
                Fire<NeedleRound>(Muzzle(i), axis, 4.2f, .18f);
                Fire<SimpleRound>(Muzzle(i), axis, 1.6f, .27f);
            }
        }
    }

    public override void damage(float amount)
    {
        base.damage(amount * (age % 3 < 1 ? 1 : .65f));
    }

    protected override void Start()
    {
        base.Start();
        if (!(movement is FormationFlight))
            SpawnEscorts();
    }

    void SpawnEscorts()
    {
        foreach (int side in new[] { -1, 1 })
        {
            var e = Spawn(typeof(ScoutInterceptor), transform.position + Vector3.right * side * 1.4f);
            e.Escort(transform);
        }
    }
}
