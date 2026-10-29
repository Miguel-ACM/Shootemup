using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class InfernalFerryman : DistrictMiniboss
{
    protected override void Configure()
    {
        hull = 59;
        hullScale = 1;
        health = 650;
        attackInterval = 1.0f;
        nextAttack = 1.0f;
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        int beat = volley % 10;
        if (beat < 3 || beat == 6 || beat == 7)
        {
            Cue(Muzzle(beat % 3), seconds);
            Cue(Muzzle(beat % 3 + 3), seconds);
        }
        else if (beat == 4)
        {
            Cue(Muzzle(2), seconds);
            Cue(Muzzle(5), seconds);
        }
    }

    // Hell infernal ferryman: scissoring arrow streams from both oar banks, souls that
    // stop, stare and dart, and a canopy of arrows rebounding off the ceiling.
    protected override void Attack()
    {
        int beat = volley % 3;
        if (beat == 0)
            Stream(Dense(10), .08f, n =>
            {
                float t = n / 9f;
                for (int side = -1; side <= 1; side += 2)
                {
                    var p = Hull.TransformPoint(new Vector3(side * .88f, -.1f, -.3f));
                    Fire<NeedleRound>(p, CombatVisuals.Rotate(CombatVisuals.Aim(p), side * Mathf.Lerp(24, -24, t)), 4.4f, .19f);
                }
            });
        else if (beat == 1)
        {
            int souls = Dense(Enraged ? 8 : 6);
            for (int i = 0; i < souls; i++)
            {
                var p = Hull.TransformPoint(new Vector3((i % 2 == 0 ? -1 : 1) * .88f, -.65f, -.3f));
                Fire<SimpleRound>(p, CombatVisuals.Rotate(Vector2.down, (i - (souls - 1) * .5f) * 22), 1.7f, .33f)
                    .Pause(.7f, .35f + i * .18f, CombatRound.Turn.Aim, 0, 2.6f);
            }
        }
        else
        {
            int count = Dense(8);
            for (int i = 0; i < count; i++)
                for (int mirror = -1; mirror <= 1; mirror += 2)
                    Fire<NeedleRound>(transform.position, CombatVisuals.Rotate(Vector2.up, mirror * (6 + i * 10)), 3.6f, .19f).Bounce(1);
        }
    }
}
