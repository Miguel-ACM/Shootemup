using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class OrbitalWarden : DistrictMiniboss
{
    protected override void Configure()
    {
        hull = 60;
        hullScale = .95f;
        health = 590;
        attackInterval = (0.52f);
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

    protected override void Attack()
    {
        int beat = (volley - 1) % 10;
        if (beat == 0)
        {
            // A comet: a tight spinning ring that travels at the player as one body.
            int beads = Dense(9);
            var aim = Aim;
            for (int i = 0; i < beads; i++)
            {
                var bead = Fire<SimpleRound>(Muzzle(), aim, 1.9f, .19f);
                if (bead != null)
                    bead.Orbit(.75f, i * 360f / beads, 150, 10, 0, 0, 0, .4f);
            }
        }
        else if (beat == 1 || beat == 2)
            Fan<RicochetBolt>(Dense(5), 25, 3.8f, .23f);
        else if (beat == 4)
            for (int side = -1; side <= 1; side += 2)
            {
                var local = new Vector3(side * .95f, -.85f, -.3f);
                var p = Hull.TransformPoint(local);
                var beam = Fire<PulseLaser>(p, Vector2.down, 0);
                if (beam != null)
                {
                    beam.Bind(Hull, local);
                    beam.warning = 1.1f;
                    beam.activeTime = 1.15f;
                    beam.sweep = 0;
                    beam.width = .19f;
                }
            }
        else if (beat == 7)
        {
            // A small galaxy: stars wound round the warden, slung off one after another.
            int stars = Dense(Enraged ? 16 : 12);
            for (int i = 0; i < stars; i++)
            {
                var star = Fire<SimpleRound>(Muzzle(), Vector2.down, 0, .18f);
                if (star != null)
                    star.Orbit(.9f, i * 360f / stars, volley % 20 < 10 ? 90 : -90, 8, 0, 0, 0, .4f).Sling(.7f + i * .06f, .7f);
            }
        }
    }
}
