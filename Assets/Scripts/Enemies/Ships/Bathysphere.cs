using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class Bathysphere : OceanEnemy
{
    protected override void Configure()
    {
        hull = 52;
        hullScale = .62f;
        health = 85;
        attackInterval = (2.4f);
        Mover<TopTracker>();
    }

    // Torpedoes that leave almost still and wind up, then a short needle reply.
    protected override void Attack()
    {
        if (volley % 2 == 1)
            for (int s = -1; s <= 1; s += 2)
                Fire<SimpleRound>(Muzzle(s < 0 ? 0 : 1), CombatVisuals.Rotate(Aim, s * 14), .45f, .27f).Ramp(8, 1.0f, .4f);
        else
            Fan<NeedleRound>(3, 23, 3.8f, .19f);
    }
}
