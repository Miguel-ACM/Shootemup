using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ShieldTransport : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 8;
        health = 100;
        hullScale = .95f;
        attackInterval = (1.9f);
        Mover<DriftDownPattern>();
    }

    // Destructible plasma, then a gust thrown sideways that halts and turns downward.
    protected override void Attack()
    {
        if (volley % 2 == 0)
            Fan<PlasmaOrb>(3, 37, 2.2f, .48f);
        else
        {
            int count = Dense(4);
            for (int i = 0; i < count; i++)
            {
                int side = i % 2 == 0 ? -1 : 1;
                Fire<SimpleRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.right * side, -side * (10 + i * 6)), 3.0f, .2f)
                    .Pause(.5f, .3f, CombatRound.Turn.Rotate, -side * 110, .8f);
            }
        }
    }
}
