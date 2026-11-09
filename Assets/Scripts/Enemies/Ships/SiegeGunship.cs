using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class SiegeGunship : DistrictEnemy
{
    protected override void Configure()
    {
        health = 95;
        hullScale = .9f;
        attackInterval = (1.7f);
        Mover<TopTracker>();
    }

    protected override Vector2 CueAxis => Vector2.down;

    // A fan that stops, then every round turns the same way: the whole sheet slides sideways.
    // All three cannons share every volley, left rounds from the left barrel and so on;
    // rotating one barrel per volley left the third silent on short passes.
    protected override void Attack()
    {
        int n = Dense(5);
        int turn = volley % 2 == 0 ? 1 : -1;
        for (int i = 0; i < n; i++)
        {
            int gun = n == 1 ? 1 : Mathf.RoundToInt(i * 2f / (n - 1));
            Fire<SimpleRound>(Muzzle(gun), CombatVisuals.Rotate(Vector2.down, (i - (n - 1) * .5f) * 21), 3.1f, .23f)
                .Pause(.6f, .4f, CombatRound.Turn.Rotate, turn * 50, .8f);
        }
    }
}
