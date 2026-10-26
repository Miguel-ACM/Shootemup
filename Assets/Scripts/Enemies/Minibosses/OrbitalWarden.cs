using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class OrbitalWarden : DistrictMiniboss
{
    protected override void Configure()
    {
        hull = 60;
        hullScale = .95f;
        health = 590;
        attackInterval = Timing(0.72f,0.52f);
        nextAttack = 1.0f;
    }

    protected override void Attack()
    {
        int beat = (volley - 1) % 10;
        if (beat < 3)
            Fan<RicochetBolt>(Shots(4,6), 25, 3.8f, .23f);
        else if (beat == 4)
            for (int side = -1; side <= 1; side += 2)
            {
                var local = new Vector3(side * .95f, -.85f, -.3f);
                var p = Hull.TransformPoint(local);
                var beam = Fire<PulseLaser>(p, Vector2.down, 0);
                if (beam != null)
                {
                    beam.Bind(Hull, local);
                    beam.warning = Timing(1.4f,1.1f);
                    beam.activeTime = 1.15f;
                    beam.sweep = 0;
                    beam.width = .19f;
                }
            }
        else if (beat == 7 || beat == 8)
            Fan<NeedleRound>(Shots(Enraged?5:4,Enraged?8:6), 17, 4.8f, .19f);
    }
}
