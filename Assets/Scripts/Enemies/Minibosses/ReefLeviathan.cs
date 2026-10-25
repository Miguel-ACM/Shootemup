using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ReefLeviathan : DistrictMiniboss
{
    protected override void Configure()
    {
        hull = 58;
        hullScale = 1;
        health = 560;
        attackInterval = .58f;
        nextAttack = 1.2f;
    }

    protected override void Attack()
    {
        int beat = (volley - 1) % 8;
        if (beat < 3)
            for (int side = -1; side <= 1; side += 2)
            {
                var p = Hull.TransformPoint(new Vector3(side * .82f, -.7f, -.25f));
                for (int j = -1; j <= 1; j++)
                    Fire<SuspendedRound>(p, CombatVisuals.Rotate(Vector2.down, side * 25 + j * 18 + (beat - 1) * 7), 3.5f, .18f);
            }
        else if (beat == 5 || beat == 6)
            Fan<NeedleRound>(Enraged ? 7 : 5, 14, 4.7f, .14f);
    }
}
