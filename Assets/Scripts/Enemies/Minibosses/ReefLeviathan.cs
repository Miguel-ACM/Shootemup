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
            for (int side = -1; side <= 1; side += 2)
            {
                var p = Hull.TransformPoint(new Vector3(side * .82f, -.7f, -.25f));
                for (int j = 0; j < (3); j++)
                    Fire<SuspendedRound>(p, CombatVisuals.Rotate(Vector2.down, side * 25 + (j - ((3) - 1) * .5f) * 25 + (beat - 1) * 7), 2.7f, .25f);
            }
        else if (beat == 5 || beat == 6)
            Fan<NeedleRound>((Enraged ? 6 : 5), 21, 3.9f, .2f);
    }
}
