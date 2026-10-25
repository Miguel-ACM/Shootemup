using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class CinderImp : InfernalEnemy
{
    protected override void Configure()
    {
        hull = 48;
        hullScale = .40f;
        health = 27;
        attackInterval = 1.0f;
        Mover<Slalom>();
    }

    protected override void Attack()
    {
        for (int i = -1; i <= 1; i++)
            Fire<HelixRound>(transform.position, CombatVisuals.Rotate(Aim, i * 24 + (volley % 2 == 0 ? 4 : -4)), 4.1f, .14f);
    }
}
