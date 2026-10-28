using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ScarabDredger : DistrictMiniboss
{
    protected override void Configure()
    {
        hull = 56;
        hullScale = .96f;
        health = 510;
        attackInterval = (0.66f);
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

    protected override void Attack()
    {
        int beat = (volley - 1) % 8;
        if (beat < 3)
            for (int side = -1; side <= 1; side += 2)
                FanPort(new Vector3(side * .85f, -.45f, -.2f), Dense(5), 3.5f, (beat - 1) * 8);
        else if (beat == 5)
        {
            // Quicksand mines: they stop and glint; even mines lunge at the player, odd ones sink away.
            int mines = Dense(10);
            var p = Hull.TransformPoint(new Vector3(0, -.65f, -.3f));
            for (int i = 0; i < mines; i++)
            {
                float lane = i - (mines - 1) * .5f;
                var mine = Fire<SimpleRound>(p, CombatVisuals.Rotate(Vector2.down, lane * 150f / (mines - 1)), 2.6f, .2f)
                    .Pause(.5f, 1.0f + Mathf.Abs(lane) * .06f, CombatRound.Turn.Aim, 0, .9f);
                if (i % 2 == 1)
                    mine.FadeAt(1.5f + Mathf.Abs(lane) * .06f);
            }
        }
    }

    void FanPort(Vector3 local, int count, float speed, float turn)
    {
        var p = Hull.TransformPoint(local);
        for (int j = 0; j < count; j++)
            Fire<NeedleRound>(p, CombatVisuals.Rotate(CombatVisuals.Aim(p), (j - (count - 1) * .5f) * 15 + turn), speed, .14f);
    }
}
