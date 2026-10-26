using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class InfernalFerryman : DistrictMiniboss
{
    protected override void Configure()
    {
        hull = 59;
        hullScale = 1;
        health = 650;
        attackInterval = Timing(0.72f, 0.54f);
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
        if (beat < 3 || beat == 6 || beat == 7)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var p = Hull.TransformPoint(new Vector3(side * .88f, .45f - (beat % 3) * .55f, -.3f));
                for (int i = 0; i < Shots(3, 5); i++)
                    Fire<SimpleRound>(p, CombatVisuals.Rotate(CombatVisuals.Aim(p), (i - (Shots(3, 5) - 1) * .5f) * 21 + (beat % 3 - 1) * 6), 3.6f, .23f);
            }
        }
        else if (beat == 4)
            FanFrom<NeedleRound>(Shots(5, 7), 17, 4.8f, .19f, new Vector3(-.88f, -.65f, -.3f), new Vector3(.88f, -.65f, -.3f));
    }
}
