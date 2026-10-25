using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class PursuitFighter : DistrictEnemy
{
    PulseLaser beam;
    protected override void Configure()
    {
        hull = 12;
        health = 20;
        hullScale = .45f;
        attackInterval = 2.5f;
        Mover<TopTracker>();
    }

    protected override void Attack()
    {
        if (beam != null && beam.IsAttachedTo(Hull))
            return;
        beam = CombatRound.Launch<PulseLaser>(transform.position, Aim, 0);
        if (beam != null)
        {
            beam.Bind(Hull, new Vector3(0, -1.38f, -.15f));
            beam.warning = .55f;
            beam.activeTime = .3f;
            beam.width = .15f;
        }
    }
}
