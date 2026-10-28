using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ReefLeviathan : DistrictMiniboss
{
    protected override void Configure()
    {
        hull = 58;
        hullScale = 1;
        health = 560;
        attackInterval = (0.68f);
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

    protected override void Attack()
    {
        int beat = (volley - 1) % 8;
        if (beat < 3)
        {
            // Volleys caught in a current that carries them sideways in arcs.
            int count = Dense(3);
            for (int side = -1; side <= 1; side += 2)
            {
                var p = Hull.TransformPoint(new Vector3(side * .82f, -.7f, -.25f));
                for (int j = 0; j < count; j++)
                    Fire<SimpleRound>(p, CombatVisuals.Rotate(Vector2.down, side * 20 + (j - (count - 1) * .5f) * 22 + (beat - 1) * 7), 2.6f, .22f)
                        .Fall(new Vector2(-side * 1.3f, 0));
            }
        }
        else if (beat == 5 || beat == 6)
        {
            // Torpedoes leave almost still and accelerate along their aim.
            int count = Dense(Enraged ? 4 : 3);
            var aim = Aim;
            for (int i = 0; i < count; i++)
                Fire<SimpleRound>(Muzzle(i), CombatVisuals.Rotate(aim, (i - (count - 1) * .5f) * 20), .45f, .3f).Ramp(9, 1.0f, .4f + i * .15f);
        }
    }
}
