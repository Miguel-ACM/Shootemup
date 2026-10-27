using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class Bathysphere : OceanEnemy
{
    protected override void Configure()
    {
        hull = 52;
        hullScale = .62f;
        health = 85;
        attackInterval = 2.4f;
        Mover<TopTracker>();
    }

    protected override void Attack()
    {
        if (volley % 2 == 1)
            for (int s = -1; s <= 1; s += 2)
                Fire<SuspendedRound>(Muzzle(s < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, s * 35), 2.2f, .27f);
        else
            Fan<NeedleRound>(3, 23, 3.8f, .19f);
    }
}
