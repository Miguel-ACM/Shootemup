using System.Collections;
using UnityEngine;

// Candy regent: a sugar rush. Taffy pulls stretch one aimed line from slow to
// fast and bend it; sprinkles ricochet off the walls and ceiling like a pinball
// table; gumdrops brake and then pop into a tight forward shotgun; wiggling
// candy-worm streams follow the darting hull. Brood minions still hatch.
public sealed partial class CandyRegentBoss
{
    protected override void Attack()
    {
        if (!arrived || player != null && Vector2.Distance(player.position, transform.position) < 2.5f)
            return;
        int beat = (volley - 1) % 16;
        if (beat == 0 || beat == 8)
            TaffyPull(beat == 0 ? 1 : -1);
        else if (beat == 3 || phaseTwo && beat == 12)
            Sprinkles();
        else if (beat == 5 || beat == 13)
            Gumdrops();
        else if (beat == 10)
            Phrase(CandyWorms());
        else if (beat == 15)
            Phrase(MirrorLace(Hull, WeaponPort(0), WeaponPort(1), Dense(phaseTwo ? 26 : 20), 2, 65, 22, 3.6f, .065f));
    }

    // One aim per hatch, a line of rounds from slow to fast that curls as it stretches.
    void TaffyPull(int sign)
    {
        int beads = Dense(phaseTwo ? 16 : 13);
        for (int hatch = 0; hatch < 2; hatch++)
        {
            var port = Muzzle(hatch);
            var axis = CombatVisuals.Rotate(CombatVisuals.Aim(port), (hatch == 0 ? -1 : 1) * 14);
            int bend = (hatch == 0 ? 1 : -1) * sign;
            for (int i = 0; i < beads; i++)
            {
                var bead = Fire<SimpleRound>(port, axis, Mathf.Lerp(1.4f, 4.6f, i / (beads - 1f)), .2f);
                if (bead != null)
                    bead.Curl(bend * 26, 1.4f);
            }
        }
    }

    // Mirrored sprinkles thrown in a wide spread, ricocheting off the walls and ceiling.
    void Sprinkles()
    {
        int count = Dense(phaseTwo ? 13 : 10);
        for (int i = 0; i < count; i++)
        {
            float angle = 12 + i * 88f / Mathf.Max(1, count - 1);
            for (int mirror = -1; mirror <= 1; mirror += 2)
                Fire<SimpleRound>(Muzzle(mirror < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, mirror * angle), 2.8f + (i % 3) * .45f, .16f).Bounce(2);
        }
    }

    // Gumdrops brake hard, then pop into a narrow fast fan along their path.
    void Gumdrops()
    {
        int drops = Dense(5), shards = Dense(phaseTwo ? 9 : 7);
        var aim = Aim;
        for (int i = 0; i < drops; i++)
            Fire<SimpleRound>(Muzzle(i % 2), CombatVisuals.Rotate(aim, (i - (drops - 1) * .5f) * 32), 3.2f, .4f)
                .Ramp(.15f, .7f)
                .SplitAt(.95f, shards, 9, 4.6f, .16f, true);
    }

    // Wiggling streams that follow the regent's darting path.
    IEnumerator CandyWorms()
    {
        int length = Dense(phaseTwo ? 20 : 15);
        for (int i = 0; i < length; i++)
        {
            for (int hatch = 0; hatch < 2; hatch++)
                Fire<SimpleRound>(Muzzle(hatch), CombatVisuals.Rotate(Vector2.down, (hatch == 0 ? -1 : 1) * 18), 2.9f, .19f)
                    .Sway(hatch == 0 ? .45f : -.45f, .8f);
            yield return Wait(Rest(.08f));
        }
    }

    protected override void PrepareScore(float seconds)
    {
        int beat = volley % 16;
        if (beat == 0 || beat == 8 || beat == 3 || phaseTwo && beat == 12 || beat == 5 || beat == 13 || beat == 10 || beat == 15)
        {
            Cue(Muzzle(0), seconds);
            Cue(Muzzle(1), seconds);
        }
    }
}
