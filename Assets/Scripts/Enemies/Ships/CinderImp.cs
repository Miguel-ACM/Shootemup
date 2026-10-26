using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class CinderImp : InfernalEnemy
{
    protected override void Configure()
    {
        hull = 48;
        hullScale = .40f;
        health = 27;
        attackInterval = Timing(1.7f, 1.2f);
        Mover<Slalom>();
    }

    protected override void Attack()
    {
        int n = Shots(1, 3);
        for (int i = 0; i < n; i++)
            Fire<HelixRound>(Muzzle(), CombatVisuals.Rotate(Aim, (i - (n - 1) * .5f) * 29), 3.1f, .2f);
    }
}
