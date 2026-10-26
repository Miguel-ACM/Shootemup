using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class MaintenanceDrone : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 4;
        health = 25;
        hullScale = .5f;
        attackInterval = Timing(2.6f, 1.9f);
        Mover<FreeReposition>();
    }

    protected override void Attack()
    {
        for (int i = 0; i < Shots(1, 2); i++)
        {
            var shot = Fire<HelixRound>(Muzzle(i), CombatVisuals.Rotate(Aim, i == 0 ? -14 : 14), 2.5f, .19f);
            if (shot != null)
                shot.phase = i * Mathf.PI;
        }
    }
}
