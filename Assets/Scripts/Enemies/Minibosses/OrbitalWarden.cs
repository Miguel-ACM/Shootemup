using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class OrbitalWarden : DistrictMiniboss
{
    protected override void Configure()
    {
        hull = 60;
        hullScale = .95f;
        health = 590;
        attackInterval = .44f;
        nextAttack = 1.0f;
    }

    protected override void Attack()
    {
        int beat = (volley - 1) % 10;
        if (beat < 3)
            Fan<RicochetBolt>(7, 16, 5.1f, .16f);
        else if (beat == 4)
            for (int side = -1; side <= 1; side += 2)
            {
                var local = new Vector3(side * .95f, -.85f, -.3f);
                var p = Hull.TransformPoint(local);
                var beam = Fire<PulseLaser>(p, CombatVisuals.Rotate(Vector2.down, side * 25), 0);
                if (beam != null)
                {
                    beam.Bind(Hull, local);
                    beam.warning = .9f;
                    beam.activeTime = 1.15f;
                    beam.sweep = -side * 16;
                    beam.width = .19f;
                }
            }
        else if (beat == 7 || beat == 8)
            Fan<NeedleRound>(Enraged ? 9 : 7, 11, 6.2f, .14f);
    }
}
