using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ShieldTransport : DistrictEnemy
{
    protected override void Configure()
    {
        health = 100;
        hullScale = .95f;
        attackInterval = (1.9f);
        Mover<DriftDownPattern>();
    }

    // Destructible plasma, then a gust thrown sideways that halts and turns downward.
    protected override void Attack()
    {
        // Even counts on every difficulty, so no round sits on the aim line: Fan adds
        // one plasma orb on Hard (three becomes four); Normal fires four, Easy two.
        if (volley % 2 == 0)
            Fan<PlasmaOrb>(Firing >= FiringTier.Hard ? 3 : Firing == FiringTier.Easy ? 2 : 4, Firing == FiringTier.Normal ? 74f / 3 : 37, 2.2f, .48f);
        else
        {
            // The gust halts and turns into falling sheets: dark against the bright plasma.
            UseTone(HostileTone.Dark);
            int count = Mathf.Max(2, (Dense(4) + 1) / 2 * 2);
            for (int i = 0; i < count; i++)
            {
                int side = i % 2 == 0 ? -1 : 1;
                Fire<SimpleRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.right * side, -side * (10 + i * 6)), 3.0f, .2f)
                    .Pause(.5f, .3f, CombatRound.Turn.Rotate, -side * 110, .8f);
            }
        }
    }
}
