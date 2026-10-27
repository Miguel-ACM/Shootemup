using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ArmoredCarrier : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 5;
        health = 130;
        hullScale = .9f;
        attackInterval = 1.9f;
        Mover<DriftDownPattern>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        firingCue.Warn(seconds);
    }

    protected override void Attack()
    {
        if (volley % 2 == 0)
            Fan<ProximityMine>(2, 62, 2.4f, .3f);
        else
            Fan<NeedleRound>(7, 17, 3.9f, .19f);
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
