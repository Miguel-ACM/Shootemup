using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class SiegeGunship : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 7;
        health = 95;
        hullScale = .9f;
        attackInterval = 1.7f;
        Mover<TopTracker>();
    }

    protected override Vector2 CueAxis => Vector2.down;
    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        Cue(Muzzle(volley % 3), seconds);
    }

    protected override void Attack()
    {
        int gun = (volley - 1) % 3;
        int n = 5;
        for (int i = 0; i < n; i++)
            Fire<SuspendedRound>(Muzzle(gun), CombatVisuals.Rotate(Vector2.down, (i - (n - 1) * .5f) * 21 + (gun - 1) * 9), 3.1f, .25f);
    }
}
