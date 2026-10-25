using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class MantaRay : OceanEnemy
{
    protected override void Configure()
    {
        hull = 51;
        hullScale = .57f;
        health = 58;
        attackInterval = 2.1f;
        Mover<Slalom>();
    }

    protected override void Attack()
    {
        for (int i = -1; i <= 1; i++)
            Fire<SimpleRound>(Hull.TransformPoint(new Vector3(i * .65f, -.20f, -.18f)), CombatVisuals.Rotate(Aim, i * 20), 3.4f, .21f);
    }
}
