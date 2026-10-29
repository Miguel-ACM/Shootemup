using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ScarabDredger : DistrictMiniboss
{
    protected override void Configure()
    {
        hull = 56;
        hullScale = .96f;
        health = 510;
        attackInterval = 1.0f;
        nextAttack = 1.1f;
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        int beat = volley % 8;
        if (beat < 3)
        {
            Cue(Muzzle(0), seconds);
            Cue(Muzzle(1), seconds);
        }
        else if (beat == 5)
            Cue(Muzzle(2), seconds);
    }

    // Desert scarab dredger: mirrored rows of quicksand mines that stop, then carry on
    // outward or sink; twin mirrored arrow sandblasts either side of the player; and
    // a sand fountain thrown up from its scoop.
    protected override void Attack()
    {
        int beat = volley % 3;
        if (beat == 0)
        {
            int mines = Dense(8);
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < mines; i++)
                {
                    var axis = CombatVisuals.Rotate(Vector2.right * side, -side * (i * 15 - 30));
                    var mine = Fire<SimpleRound>(Muzzle(side < 0 ? 0 : 1), axis, 2.4f + (i % 2) * .5f, .22f)
                        .Pause(.5f, .8f + i * .06f, CombatRound.Turn.Keep, 0, 1.2f);
                    if (i % 2 == 1)
                        mine.FadeAt(1.3f + i * .06f);
                }
        }
        else if (beat == 1)
            Stream(Dense(14), .07f, n =>
            {
                var aim = CombatVisuals.Aim(Muzzle(2));
                float open = 10 + Mathf.Abs(Mathf.Sin(n * .45f)) * 30;
                foreach (int mirror in new[] { -1, 1 })
                    Fire<NeedleRound>(Muzzle(2), CombatVisuals.Rotate(aim, mirror * open), 3.8f, .19f);
            });
        else
        {
            int lanes = Dense(11);
            for (int i = 0; i < lanes; i++)
                Fire<NeedleRound>(Muzzle(2), CombatVisuals.Rotate(Vector2.up, (i - (lanes - 1) * .5f) * 12), 4.2f, .19f).Fall(new Vector2(0, -3f));
        }
    }
}
