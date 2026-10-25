using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class GhostGalleon : InfernalEnemy
{
    protected override void Configure()
    {
        hull = 47;
        hullScale = .64f;
        health = 95;
        attackInterval = 1.35f;
        Mover<TopTracker>();
    }

    protected override void Attack()
    {
        // Alternating broadsides make two nested, mirrored curtains with a central answer.
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 3; i++)
            {
                var port = Hull.TransformPoint(new Vector3(side * .61f, .43f - i * .42f, -.22f));
                Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, side * (40 + i * 12 + (volley % 2) * 6)), 4.0f + i * .2f, .18f);
            }
        Fan<NeedleRound>(3, 11, 5.8f, .14f);
    }
}
