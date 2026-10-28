using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class MaintenanceDrone : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 4;
        health = 25;
        hullScale = .5f;
        attackInterval = (1.9f);
        Mover<FreeReposition>();
    }

    // Helix pairs: a quick one and a slow one wound round the same aim.
    protected override void Attack()
    {
        for (int i = 0; i < 2; i++)
        {
            var shot = Fire<HelixRound>(Muzzle(i), CombatVisuals.Rotate(Aim, i == 0 ? -14 : 14), i == 0 ? 3.4f : 1.8f, .19f);
            if (shot != null)
                shot.phase = i * Mathf.PI;
        }
    }
}
