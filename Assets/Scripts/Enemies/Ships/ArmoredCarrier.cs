using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ArmoredCarrier : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 5;
        health = 130;
        hullScale = .9f;
        attackInterval = 1.25f;
        Mover<DriftDownPattern>();
    }

    protected override void Attack()
    {
        Fan<ProximityMine>(3, 50, 2.8f, .25f);
        Fan<NeedleRound>(9, 12, 4.5f);
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
