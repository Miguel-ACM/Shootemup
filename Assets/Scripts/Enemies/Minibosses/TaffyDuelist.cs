using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class TaffyDuelist : DistrictMiniboss
{
    protected override void Configure()
    {
        hull = 57;
        hullScale = .9f;
        health = 480;
        attackInterval = (0.4f);
        nextAttack = 1.1f;
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        int beat = volley % 10;
        if (beat != 3 && beat != 4 && beat != 9)
            Cue(Muzzle(beat % 2), seconds);
    }

    protected override void Attack()
    {
        int beat = (volley - 1) % 10;
        if (beat == 3 || beat == 4 || beat == 9)
            return;
        var port = Hull.TransformPoint(new Vector3((beat % 2 == 0 ? -1 : 1) * .74f, -.55f, -.25f));
        if (beat == 6)
        {
            // Pinball sprinkles off the walls and ceiling.
            int count = Dense(Enraged ? 7 : 5);
            for (int i = 0; i < count; i++)
                for (int mirror = -1; mirror <= 1; mirror += 2)
                    Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, mirror * (15 + i * 18)), 2.9f + (i % 2) * .5f, .15f).Bounce(2);
            return;
        }
        if (beat == 8)
        {
            // A taffy pull: one aim, slow to fast, bending as it stretches.
            int beads = Dense(8);
            var aim = CombatVisuals.Aim(port);
            for (int i = 0; i < beads; i++)
            {
                var bead = Fire<SimpleRound>(port, aim, Mathf.Lerp(1.5f, 4.4f, i / (beads - 1f)), .19f);
                if (bead != null)
                    bead.Curl(volley % 20 < 10 ? 24 : -24, 1.3f);
            }
            return;
        }
        for (int i = 0; i < 3; i++)
            Fire<NeedleRound>(port, CombatVisuals.Rotate(CombatVisuals.Aim(port), (i - 1) * (Enraged ? 22 : 26)), 4.8f, .19f);
    }
}
