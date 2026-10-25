using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class Bathysphere : OceanEnemy
{
    protected override void Configure()
    {
        hull = 52;
        hullScale = .62f;
        health = 85;
        attackInterval = 2.3f;
        Mover<TopTracker>();
    }

    protected override void Attack()
    {
        if (volley % 2 == 1)
            for (int s = -1; s <= 1; s += 2)
                Fire<SuspendedRound>(Hull.TransformPoint(new Vector3(s * .7f, -.4f, -.2f)), CombatVisuals.Rotate(Vector2.down, s * 30), 2.8f, .21f);
        else
            Fan<NeedleRound>(3, 16, 4.2f, .15f);
    }
}
