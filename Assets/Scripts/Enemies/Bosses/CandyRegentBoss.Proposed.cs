using System.Collections;
using UnityEngine;

// Proposed Candy regent book (ArtSource/BossPhaseProposals), flown only while
// DistrictBoss.ProposedCards is set so it can be recorded beside the shipping cards.
// One concept per phase (Miguel, 10 November 2026). The concept develops verse by verse,
// passing between the regent's own parts: the two prongs at the keel, the two mint
// hatches on the wings and the sugar crown on top. The regent keeps darting through the
// upper arena with no minions, and a ricocheting canopy covers the top. Every shape is a
// line with open lanes, never a scatter.
// Phase one, Ribbon Candy: mirrored taffy ribbons, two strands (fast and slow) along one
//   sweeping aim, dashed five beats on and two off so each fold has open gaps. The walls
//   and ceiling fold them into ribbon candy. The folds tighten pass by pass.
//   1. Hatch ribbons sweep from below the horizon to above it, folding over the top.
//   2. Crown ribbons are thrown up and draped off the ceiling: the canopy.
//   3. Prong ribbons fold down and out off the side walls into the lower arena.
//   4. Braid: hatches and prongs alternate beats in antiphase, two ribbons interleaving.
// Phase two, Candy Cane: mirrored diagonal stripes alternating bright and dark, each with
//   one bead missing; the opening walks along the stripe (back the other way each pass)
//   and the walls rebound stripes across each other. Every fourth stripe is a crown
//   stripe thrown up off the ceiling.
//   1. Hatch stripes down and out.  2. Prong stripes crossing beneath the regent.
//   3. Crown stripes only: the whole canopy.  4. Hatches and crown alternate.
// Phase three (Hard), Heartbreak: heart outlines grow from the regent, sink, stop and
//   break down the middle, the halves swinging apart and leaving a lane that follows
//   the regent's dashes.
//   1. A large heart with a smaller one nested inside.  2. Twin hearts from the hatches.
//   3. The large heart between two hatch hearts.  4. A ripple of three hearts in quick
//   succession, each larger than the last.
// Tones: Ribbon Candy bright ribbons with dark hearts; Candy Cane alternating stripes;
//   Heartbreak bright outer hearts over dark inner and hatch hearts.
public sealed partial class CandyRegentBoss
{
    protected override IEnumerator Proposed()
    {
        int verse = 0;
        while (true)
        {
            if (phaseThree)
                yield return Heartbreak(verse++);
            else if (!phaseTwo)
                yield return RibbonCandy(verse++);
            else
                yield return CandyCane(verse++);
        }
    }

    const int LeftProng = 0, RightProng = 1, LeftHatch = 2, RightHatch = 3;

    Vector3 Crown(int side) => Hull.TransformPoint(new Vector3(side * .31f, .95f, -.31f));

    Vector3 Port(int part, int side)
    {
        if (part == 0)
            return Muzzle(side < 0 ? LeftProng : RightProng);
        if (part == 1)
            return Muzzle(side < 0 ? LeftHatch : RightHatch);
        return Crown(side);
    }

    // A visible charge at the mirrored pair of one part: 0 prongs, 1 hatches, 2 crown.
    IEnumerator ChargePart(int part, float seconds)
    {
        for (float t = 0; t < seconds;)
        {
            if (!Holding)
                t += Time.deltaTime * LevelRun.EnemyTempoScale;
            Cue(Port(part, -1), seconds - t);
            Cue(Port(part, 1), seconds - t);
            yield return null;
        }
    }

    // ---------------------------------------------------------------- Ribbon Candy

    IEnumerator RibbonCandy(int verse)
    {
        int movement = verse % 4;
        int pass = verse / 4;
        // The folds tighten pass by pass.
        float fold = Mathf.Max(16, 26 - pass * 4);
        int lead = movement == 0 ? 1 : movement == 1 ? 2 : 0;
        yield return ChargePart(lead, .45f);
        if (movement == 3)
            Phrase(ChargePart(1, .45f));
        yield return Every(4.5f, .08f, n =>
        {
            // Dashed: five beats on, two off.
            if (n % 7 >= 5)
                return;
            float wave = Mathf.Sin(n / fold * Mathf.PI * 2);
            if (movement == 0)
                Ribbon(Port(1, -1), Port(1, 1), Vector2.right, 15 + 40 * wave, 3, true, n);
            else if (movement == 1)
                Ribbon(Crown(-1), Crown(1), Vector2.up, -(35 + 25 * wave), 2, true, n);
            else if (movement == 2)
                Ribbon(Port(0, -1), Port(0, 1), Vector2.down, 40 + 30 * wave, 1, false, n);
            else if (n / 7 % 2 == 0)
                Ribbon(Port(1, -1), Port(1, 1), Vector2.right, 15 + 40 * wave, 3, true, n);
            else
                Ribbon(Port(0, -1), Port(0, 1), Vector2.down, 40 - 30 * wave, 1, false, n);
        });
        yield return Wait(Rest(.6f));
    }

    // Two strands from a mirrored pair of ports along one aim: the fast strand threaded
    // with hearts, the slow one plain. The right-hand aim is the base direction turned
    // by the angle (counterclockwise); the left-hand aim is its mirror image.
    void Ribbon(Vector3 left, Vector3 right, Vector2 rightBase, float angle, int bounces, bool ceiling, int n)
    {
        bool heart = n % 6 == 0;
        for (int mirror = -1; mirror <= 1; mirror += 2)
        {
            var port = mirror < 0 ? left : right;
            var baseAxis = new Vector2(rightBase.x * mirror, rightBase.y);
            var axis = CombatVisuals.Rotate(baseAxis, mirror * angle);
            if (heart)
            {
                UseTone(HostileTone.Dark);
                Fire<HeartRound>(port, axis, 3.2f, .19f).Bounce(bounces, ceiling);
            }
            else
            {
                UseTone(HostileTone.Bright);
                Fire<NeedleRound>(port, axis, 3.4f, .19f).Bounce(bounces, ceiling);
            }
            UseTone(HostileTone.Bright);
            Fire<NeedleRound>(port, axis, 2.3f, .19f).Bounce(bounces, ceiling);
        }
    }

    // ---------------------------------------------------------------- Candy Cane

    IEnumerator CandyCane(int verse)
    {
        int movement = verse % 4;
        int walk = verse / 4 % 2 == 0 ? 1 : -1;
        int lead = movement == 0 ? 1 : movement == 1 ? 0 : 2;
        yield return ChargePart(lead, .4f);
        if (movement == 3)
            Phrase(ChargePart(1, .4f));
        yield return Every(5.5f, .3f, n =>
        {
            int part = movement == 2 || n % 4 == 3 ? 2 : movement == 3 ? (n % 2 == 0 ? 1 : 2) : lead;
            CaneStripe(n, part, walk);
        });
        yield return Wait(Rest(.6f));
    }

    // One mirrored stripe from a pair of ports. Hatch stripes run down and out, prong
    // stripes cross beneath the regent, crown stripes go up off the ceiling. The tone
    // alternates stripe by stripe and the missing bead walks along the stripe.
    void CaneStripe(int n, int part, int walk)
    {
        int beads = Dense(8);
        int gap = ((n * walk) % beads + beads) % beads;
        float centre = part == 0 ? -14 : 32;
        float arc = part == 0 ? 40 : 56;
        Vector2 baseAxis = part == 2 ? Vector2.up : Vector2.down;
        if (part == 2)
            centre = -40;
        UseTone(n % 2 == 0 ? HostileTone.Bright : HostileTone.Dark);
        for (int i = 0; i < beads; i++)
        {
            if (i == gap)
                continue;
            float across = (i - (beads - 1) * .5f) * arc / Mathf.Max(1, beads - 1);
            for (int mirror = -1; mirror <= 1; mirror += 2)
            {
                var shot = ScoredShot(i % 2 == 0, Port(part, mirror), CombatVisuals.Rotate(baseAxis, mirror * (centre + across)), 2.7f);
                if (part == 2)
                    shot.Bounce(2);
                else
                    shot.Bounce(1, false);
            }
        }
    }

    // ---------------------------------------------------------------- Heartbreak

    IEnumerator Heartbreak(int verse)
    {
        int movement = verse % 4;
        for (int k = 0; k < 3; k++)
        {
            yield return ChargePart(movement == 1 ? 1 : 0, .4f);
            if (Clear)
            {
                var keel = (Port(0, -1) + Port(0, 1)) * .5f;
                if (movement == 0)
                {
                    UseTone(HostileTone.Bright);
                    HeartOutline(keel, 0, Dense(46), 2.8f, .6f, 40, .85f);
                    UseTone(HostileTone.Dark);
                    HeartOutline(keel, 0, Dense(32), 1.7f, .6f, 40, .85f);
                }
                else if (movement == 1)
                {
                    UseTone(HostileTone.Dark);
                    HeartOutline(Port(1, -1), 16, Dense(36), 2.3f, .7f, 35, .8f);
                    HeartOutline(Port(1, 1), -16, Dense(36), 2.3f, .7f, 35, .8f);
                }
                else if (movement == 2)
                {
                    UseTone(HostileTone.Bright);
                    HeartOutline(keel, 0, Dense(44), 2.8f, .6f, 40, .85f);
                    UseTone(HostileTone.Dark);
                    HeartOutline(Port(1, -1), 20, Dense(26), 1.9f, .8f, 30, .75f);
                    HeartOutline(Port(1, 1), -20, Dense(26), 1.9f, .8f, 30, .75f);
                }
                else
                    Phrase(HeartRipple(keel));
            }
            yield return Wait(Rest(.8f));
        }
        yield return Wait(Rest(.5f));
    }

    // Three hearts in quick succession, each larger, all breaking on the same beat.
    IEnumerator HeartRipple(Vector3 keel)
    {
        for (int r = 0; r < 3; r++)
        {
            UseTone(r == 1 ? HostileTone.Dark : HostileTone.Bright);
            HeartOutline(keel, 0, Dense(28), 1.8f + r * .5f, .6f, 40, .95f - r * .15f);
            yield return Wait(.15f);
        }
    }

    // The outline of a heart grows from one port as it sinks: each bead's velocity is its
    // point on the curve plus a shared downward drift. Arrows draw the line, with an
    // upright heart at every fourth bead. After a stop the two halves swing apart.
    void HeartOutline(Vector3 port, float tilt, int beads, float growth, float sink, float breakAngle, float breakAt)
    {
        var shade = tone;
        for (int i = 0; i < beads; i++)
        {
            float a = (i + .5f) / beads * Mathf.PI * 2;
            float s = Mathf.Sin(a);
            var shape = new Vector2(16 * s * s * s,
                13 * Mathf.Cos(a) - 5 * Mathf.Cos(2 * a) - 2 * Mathf.Cos(3 * a) - Mathf.Cos(4 * a)) / 17f;
            Vector2 velocity = CombatVisuals.Rotate(shape, tilt) * growth + Vector2.down * sink;
            UseTone(shade);
            CombatRound bead = i % 4 == 0
                ? (CombatRound)Fire<HeartRound>(port, velocity, velocity.magnitude, .19f)
                : Fire<NeedleRound>(port, velocity, velocity.magnitude, .18f);
            bead.Pause(breakAt, .4f, CombatRound.Turn.Rotate, shape.x < 0 ? breakAngle : -breakAngle, 1.3f);
        }
    }
}
