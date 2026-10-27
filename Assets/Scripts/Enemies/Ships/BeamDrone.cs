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
        attackInterval = (2.9f);
        Mover<FreeReposition>();
    }

    public override bool NoseAimed => false;
    protected override Vector2 CueAxis => Vector2.down;

    protected override void Attack()
    {
        if (beam != null && beam.IsAttachedTo(Hull))
            return;
        beam = Fire<PulseLaser>(Muzzle(), Vector2.down, 0);
        if (beam != null)
        {
            beam.Bind(Hull, new Vector3(0, -.28f, -.24f));
            beam.warning = (1.05f);
            beam.activeTime = (.85f);
            beam.width = .21f;
        }
    }
}
