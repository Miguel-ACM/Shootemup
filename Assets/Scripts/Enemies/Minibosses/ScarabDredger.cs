using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ScarabDredger : DistrictMiniboss
{
    protected override void Configure()
    {
        hull = 56;
        hullScale = .96f;
        health = 510;
        attackInterval = 0.66f;
        nextAttack = 1.1f;
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        int beat = volley % 8;
        if (beat < 3)
        {
            Cue(Muzzle0, seconds);
            Cue(Muzzle1, seconds);
        }
        else if (beat == 5)
            Cue(Muzzle2, seconds);
    }

    protected override void Attack()
    {
        int beat = (volley - 1) % 8;
        if (beat < 3)
            for (int side = -1; side <= 1; side += 2)
                FanPort(new Vector3(side * .85f, -.45f, -.2f), 5, 3.5f, (beat - 1) * 8);
        else if (beat == 5)
            for (int i = 0; i < 16; i++)
                Fire<SimpleRound>(Hull.TransformPoint(new Vector3(0, -.65f, -.3f)), CombatVisuals.Rotate(Vector2.down, i * 360f / 16 + volley * 4), 3.6f, .15f);
    }

    void FanPort(Vector3 local, int count, float speed, float turn)
    {
        var p = Hull.TransformPoint(local);
        for (int j = 0; j < count; j++)
            Fire<NeedleRound>(p, CombatVisuals.Rotate(CombatVisuals.Aim(p), (j - (count - 1) * .5f) * 15 + turn), speed, .14f);
    }
}
