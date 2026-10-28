using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class SiegeGunship : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 7;
        health = 95;
        hullScale = .9f;
        attackInterval = (1.7f);
        Mover<TopTracker>();
    }

    protected override Vector2 CueAxis => Vector2.down;
    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        Cue(Muzzle(volley % 3), seconds);
    }

    // A fan that stops, then every round turns the same way: the whole sheet slides sideways.
    protected override void Attack()
    {
        int gun = (volley - 1) % 3;
        int n = Dense(5);
        int turn = volley % 2 == 0 ? 1 : -1;
        for (int i = 0; i < n; i++)
            Fire<SimpleRound>(Muzzle(gun), CombatVisuals.Rotate(Vector2.down, (i - (n - 1) * .5f) * 21 + (gun - 1) * 9), 3.1f, .23f)
                .Pause(.6f, .4f, CombatRound.Turn.Rotate, turn * 50, .8f);
    }
}
