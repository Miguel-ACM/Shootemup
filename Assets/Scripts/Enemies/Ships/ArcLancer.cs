using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ArcLancer : DistrictEnemy
{
    PulseLaser beam;
    protected override void Configure()
    {
        hull = 18;
        health = 34;
        hullScale = .62f;
        attackInterval = (3.4f);
        Mover<StrafeAndBrake>();
    }

    protected override Vector2 CueAxis => Vector2.down;

    protected override void Attack()
    {
        if (beam != null && beam.IsAttachedTo(Hull))
            return;
        beam = Fire<PulseLaser>(Muzzle(), Vector2.down, 0);
        if (beam != null)
        {
            beam.Bind(Hull, new Vector3(.52f, -1.45f, -.2f));
            beam.warning = (1.15f);
            beam.activeTime = (1.05f);
            beam.width = .24f;
        }
    }
}
