using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ReefLeviathan : DistrictMiniboss
{
    protected override void Configure()
    {
        hull = 58;
        hullScale = 1;
        health = 560;
        attackInterval = 1.0f;
        nextAttack = 1.2f;
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        int beat = volley % 8;
        if (beat < 3 || beat == 5 || beat == 6)
            firingCue.Warn(seconds);
    }

    // Underwater reef leviathan: plumes of arrows thrown up that rain back down,
    // torpedo racks that wind up at the player, and mirrored current-swept volleys.
    protected override void Attack()
    {
        int beat = volley % 3;
        if (beat == 0)
        {
            int lanes = Dense(13);
            for (int layer = 0; layer < 2; layer++)
                for (int i = 0; i < lanes; i++)
                    Fire<NeedleRound>(transform.position, CombatVisuals.Rotate(Vector2.up, (i - (lanes - 1) * .5f) * 12 + layer * 6), 3.2f + layer * .9f, .19f)
                        .Fall(new Vector2(0, -2.6f));
        }
        else if (beat == 1)
        {
            int tubes = Dense(Enraged ? 5 : 4);
            for (int side = 0; side < 2; side++)
            {
                var aim = CombatVisuals.Aim(Muzzle(side));
                for (int i = 0; i < tubes; i++)
                    Fire<SimpleRound>(Muzzle(side), CombatVisuals.Rotate(aim, (i - (tubes - 1) * .5f) * 18), .45f, .3f).Ramp(9, 1.0f, .35f + i * .14f);
            }
        }
        else
            Stream(Dense(12), .07f, n =>
            {
                float lane = n / 11f * 2 - 1;
                for (int mirror = -1; mirror <= 1; mirror += 2)
                    Fire<NeedleRound>(Muzzle(mirror < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, mirror * (12 + lane * 18)), 2.9f, .19f)
                        .Fall(new Vector2(-mirror * 1.4f, 0));
            });
    }
}
