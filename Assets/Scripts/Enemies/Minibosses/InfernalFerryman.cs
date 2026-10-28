using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class InfernalFerryman : DistrictMiniboss
{
    protected override void Configure()
    {
        hull = 59;
        hullScale = 1;
        health = 650;
        attackInterval = (0.54f);
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

    protected override void Attack()
    {
        int beat = (volley - 1) % 10;
        if (beat < 3)
        {
            int count = Dense(5);
            for (int side = -1; side <= 1; side += 2)
            {
                var p = Hull.TransformPoint(new Vector3(side * .88f, .45f - (beat % 3) * .55f, -.3f));
                for (int i = 0; i < count; i++)
                    Fire<SimpleRound>(p, CombatVisuals.Rotate(CombatVisuals.Aim(p), (i - (count - 1) * .5f) * 21 + (beat % 3 - 1) * 6), 3.6f, .23f);
            }
        }
        else if (beat == 4)
        {
            // Souls that stop, stare, then dart at the player one after another.
            int souls = Dense(6);
            for (int i = 0; i < souls; i++)
            {
                var p = Hull.TransformPoint(new Vector3((i % 2 == 0 ? -1 : 1) * .88f, -.65f, -.3f));
                Fire<SimpleRound>(p, CombatVisuals.Rotate(Vector2.down, (i - (souls - 1) * .5f) * 24), 1.7f, .33f)
                    .Pause(.75f, .4f + i * .2f, CombatRound.Turn.Aim, 0, 2.5f);
            }
        }
        else if (beat == 6 || beat == 7)
        {
            // Braided threads from both oars, swaying in antiphase.
            int beads = Dense(5);
            for (int side = -1; side <= 1; side += 2)
            {
                var p = Hull.TransformPoint(new Vector3(side * .88f, -.65f, -.3f));
                for (int i = 0; i < beads; i++)
                    Fire<SimpleRound>(p, Vector2.down, 2.2f + i * .35f, .2f).Sway(side * .6f, .7f);
            }
        }
    }
}
