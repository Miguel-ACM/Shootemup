using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class BeamDrone : DistrictEnemy
{
    PulseLaser beam;
    protected override void Configure()
    {
        hull = 9;
        health = 25;
        hullScale = .55f;
        attackInterval = 3;
        Mover<FreeReposition>();
    }

    protected override void Attack()
    {
        if (beam != null && beam.IsAttachedTo(Hull))
            return;
        beam = Fire<PulseLaser>(transform.position, Aim, 0);
        if (beam != null)
        {
            beam.Bind(Hull, new Vector3(0, -.28f, -.24f));
            beam.warning = .7f;
            beam.activeTime = .48f;
            beam.width = .20f;
        }
    }
}
