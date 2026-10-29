using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class OrbitalWarden : DistrictMiniboss
{
    protected override void Configure()
    {
        hull = 60;
        hullScale = .95f;
        health = 590;
        attackInterval = 1.2f;
        nextAttack = 1.0f;
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        int beat = volley % 10;
        if (beat < 3 || beat == 4 || beat == 7 || beat == 8)
            firingCue.Warn(seconds);
    }

    // Space orbital warden: supernova rings that stop, hold and fall back through it to
    // burst out the far side; arrow streams bent by a gravity well below it; and a
    // pair of charged lasers.
    protected override void Attack()
    {
        int beat = volley % 3;
        if (beat == 0)
        {
            int beads = Dense(Enraged ? 26 : 22);
            float radius = 2.8f, travel = .8f, speed = radius / (travel - .175f);
            for (int i = 0; i < beads; i++)
                Fire<NeedleRound>(transform.position, CombatVisuals.Rotate(Vector2.down, i * 360f / beads + volley * 5), WorldSpeed(speed), .19f)
                    .Pause(travel, .6f, CombatRound.Turn.Rotate, 180, 2.6f / speed)
                    .LivesFor(8);
        }
        else if (beat == 1)
        {
            Vector2 well = transform.position + Vector3.down * 4.5f;
            Stream(Dense(16), .09f, n =>
            {
                for (int side = -1; side <= 1; side += 2)
                    Fire<NeedleRound>(transform.position, CombatVisuals.Rotate(Vector2.right * side, -side * (n % 5) * 14), 3.4f, .19f).Attract(well, 8).LivesFor(8);
            });
        }
        else
            for (int side = -1; side <= 1; side += 2)
            {
                var local = new Vector3(side * .95f, -.85f, -.3f);
                var beam = Fire<PulseLaser>(Hull.TransformPoint(local), Vector2.down, 0);
                if (beam != null)
                {
                    beam.Bind(Hull, local);
                    beam.warning = 1.0f;
                    beam.activeTime = 1.2f;
                    beam.sweep = side * 14;
                    beam.width = .2f;
                }
            }
    }
}
