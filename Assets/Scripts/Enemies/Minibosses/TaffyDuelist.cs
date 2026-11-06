using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class TaffyDuelist : DistrictMiniboss
{
    protected override void Configure()
    {
        hullScale = .9f;
        health = 480;
        attackInterval = 0.8f;
        nextAttack = 1.1f;
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        Cue(Muzzle(0), seconds);
        Cue(Muzzle(1), seconds);
    }

    // Candy taffy duelist: taffy pulls that curl in mirror, mirrored ricocheting
    // sprinkles, giant gumdrops shedding sideways sprinkles, and a ceiling canopy.
    protected override void Attack()
    {
        int beat = volley % 4;
        if (beat == 0)
        {
            int beads = Dense(11);
            for (int hatch = 0; hatch < 2; hatch++)
            {
                var port = Muzzle(hatch);
                var axis = CombatVisuals.Rotate(CombatVisuals.Aim(port), hatch == 0 ? -12 : 12);
                for (int i = 0; i < beads; i++)
                {
                    var bead = Fire<SimpleRound>(port, axis, Mathf.Lerp(1.5f, 4.6f, i / (beads - 1f)), .19f);
                    if (bead != null)
                        bead.Curl(hatch == 0 ? 26 : -26, 1.3f);
                }
            }
        }
        else if (beat == 1)
        {
            int count = Dense(Enraged ? 9 : 7);
            for (int i = 0; i < count; i++)
                for (int mirror = -1; mirror <= 1; mirror += 2)
                    Fire<SimpleRound>(Muzzle(mirror < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, mirror * (12 + i * 13)), 2.9f + (i % 2) * .5f, .15f).Bounce(2);
        }
        else if (beat == 2)
        {
            for (int mirror = -1; mirror <= 1; mirror += 2)
                Fire<GiantOrb>(Muzzle(mirror < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, mirror * 20), 2.2f, .59f)
                    .SideSpray(1.3f);
        }
        else
        {
            int count = Dense(6);
            for (int i = 0; i < count; i++)
                for (int mirror = -1; mirror <= 1; mirror += 2)
                    Fire<NeedleRound>(Muzzle(mirror < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.right * mirror, mirror * (-10 + i * 10)), 3.4f, .19f).Bounce(3);
        }
    }
}
