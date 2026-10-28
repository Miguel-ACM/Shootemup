using System.Collections;
using UnityEngine;

// City duo: rush hour around a hub. The light interceptor circles the heavy
// hauler and fires arrows straight out from the hub, so its streams wind into
// spiral arms; between arms it sends headlight rays (one aim, many speeds). The
// hauler lays a mirrored street grid, lobs paired cargo that bursts at the player
// and fans layered arrow wavefronts. A lone survivor inherits its partner's moves.
public sealed partial class CityDuoBoss
{
    protected override void Attack()
    {
        int beat = (volley - 1) % 12;
        if (heavy)
        {
            if (beat == 0 || beat == 6)
                Phrase(Gridlock(beat == 0 ? 1 : -1));
            else if (beat == 3 || enraged && beat == 9)
                CargoLob();
            else if (beat == 10)
                Wavefront(Muzzle(1));
            else if (enraged && (beat == 2 || beat == 8))
                Headlights(Muzzle(1));
        }
        else
        {
            if (beat == 1 || beat == 7)
                Phrase(SpiralArm());
            else if (beat == 4 || beat == 11)
                Headlights(Muzzle());
            else if (enraged && (beat == 5 || beat == 9))
                Phrase(Gridlock(beat == 5 ? 1 : -1));
        }
    }

    // Arrows fired straight out from the hub while circling it: a winding spiral arm.
    IEnumerator SpiralArm()
    {
        int shots = Dense(enraged ? 34 : 26);
        for (int i = 0; i < shots; i++)
        {
            Vector2 hub = partner != null ? partner.transform.position : transform.position + Vector3.up;
            Vector2 outward = ((Vector2)transform.position - hub).normalized;
            if (outward.sqrMagnitude < .01f)
                outward = Vector2.down;
            foreach (float spread in new[] { -9f, 9f })
                Fire<NeedleRound>(Muzzle(), CombatVisuals.Rotate(outward, spread), 3.6f, .19f);
            yield return Wait(Rest(.055f));
        }
    }

    // Three mirrored rays, each a line of arrows at falling speeds.
    void Headlights(Vector3 port)
    {
        int beads = Dense(enraged ? 9 : 7);
        var aim = CombatVisuals.Aim(port);
        foreach (float ray in new[] { -22f, 0f, 22f })
            for (int b = 0; b < beads; b++)
                Fire<NeedleRound>(port, CombatVisuals.Rotate(aim, ray), 5.4f - b * .45f, .18f);
    }

    // Two mirrored diagonal families; the diamonds between them are the streets.
    IEnumerator Gridlock(int sign)
    {
        int rows = Dense(6), lanes = Dense(enraged ? 11 : 9);
        for (int row = 0; row < rows; row++)
        {
            foreach (int family in new[] { -1, 1 })
            {
                var axis = CombatVisuals.Rotate(Vector2.down, family * 36);
                for (int i = 0; i < lanes; i++)
                    Fire<SimpleRound>(Muzzle(1), CombatVisuals.Rotate(axis, (i - (lanes - 1) * .5f) * 6.5f + (row % 2) * 3.25f * sign), 2.2f, .2f);
            }
            yield return Wait(Rest(.38f));
        }
    }

    // Mirrored crates lobbed high, falling under gravity, bursting into arrows at the player.
    void CargoLob()
    {
        int shards = Dense(7);
        foreach (float lob in enraged ? new[] { -60f, -30f, 30f, 60f } : new[] { -45f, 0f, 45f })
            Fire<SimpleRound>(Muzzle(1), CombatVisuals.Rotate(Vector2.up, lob), 2.7f, .42f)
                .Fall(new Vector2(0, -2.7f))
                .SplitAt(1.9f + Mathf.Abs(lob) / 150f, shards, 9, 3.6f, .17f, true);
    }

    // A symmetric arrow fan at two speeds: two nested arcs that separate.
    void Wavefront(Vector3 port)
    {
        int lanes = Dense(enraged ? 23 : 19);
        for (int layer = 0; layer < 2; layer++)
            for (int i = 0; i < lanes; i++)
            {
                float step = 140f / (lanes - 1);
                Fire<NeedleRound>(port, CombatVisuals.Rotate(Vector2.down, (i - (lanes - 1) * .5f) * step + layer * step * .5f), 2.6f + layer * .8f, .19f);
            }
    }

    protected override void PrepareScore(float seconds)
    {
        int beat = volley % 12;
        if (heavy && (beat == 0 || beat == 6 || beat == 3 || beat == 10 || enraged && (beat == 9 || beat == 2 || beat == 8)))
            Cue(Muzzle(1), seconds);
        else if (!heavy && (beat == 1 || beat == 7 || beat == 4 || beat == 11 || enraged && (beat == 5 || beat == 9)))
            Cue(Muzzle(), seconds);
    }
}
