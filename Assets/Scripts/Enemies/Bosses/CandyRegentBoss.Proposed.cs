using System.Collections;
using UnityEngine;

// Proposed Candy regent book (ArtSource/BossPhaseProposals), flown only while
// DistrictBoss.ProposedCards is set so it can be recorded beside the shipping cards.
// The regent keeps darting through the upper arena with no minions. Every card keeps a
// ricocheting canopy over the top: folded ribbons, or stars thrown up from the crown.
// Ports: the two prongs at the keel, the two mint hatches on the wings and the sugar
// crown on top.
// Phase one
//   Ribbon Candy: both hatches pull two strands of taffy, fast and slow, along one aim
//     that sweeps from down-and-out to up-and-out; the walls and ceiling fold them into
//     ribbon candy draped over the top. Hearts are threaded through the fast strand and
//     the prongs flick an aimed sweet now and then.
//   Gumdrop Garden: the hatches swell and plant a mirrored bed of gumdrops across the
//     arena. They brake, glint, start to sink and pop into small downward cones, the
//     outer drops first and the innermost pair popping at the pilot. Crown stars ricochet
//     off the ceiling between beds.
// Phase two
//   Candy Cane: each hatch lays diagonal stripes of arrows and circles, alternating
//     bright and dark like a cane, that rebound off the side walls into a crossing
//     lattice. One bead is missing from every stripe and the opening walks along it.
//   Sweetheart: after a charge both prongs release an outline of a heart, leaning apart,
//     that grows as it sinks, so every point of the arena is crossed once by a widening
//     line with room between its beads. Crown stars keep the ceiling busy.
// Phase three (Hard)
//   Heartbreak: a large heart grows from the prongs, stops, and breaks down the middle,
//     both halves swinging apart, over candy-cane stripes and crown stars.
// Tones: the dominant tone flips card to card.
//   Ribbon Candy: bright ribbons with dark hearts and dark sweets.
//   Gumdrop Garden: dark gumdrops (their pops inherit it), bright crown stars.
//   Candy Cane: stripes alternate bright and dark; dark crown stars.
//   Sweetheart: dark hearts, bright crown stars.
//   Heartbreak: a bright heart over dark-led stripes.
public sealed partial class CandyRegentBoss
{
    protected override IEnumerator Proposed()
    {
        while (true)
        {
            if (phaseThree)
                yield return Heartbreak();
            else if (!phaseTwo)
            {
                yield return RibbonCandy();
                yield return Wait(.7f);
                yield return GumdropGarden();
                yield return Wait(.7f);
            }
            else
            {
                yield return CandyCane();
                yield return Wait(.7f);
                yield return Sweetheart();
                yield return Wait(.7f);
            }
        }
    }

    const int LeftProng = 0, RightProng = 1, LeftHatch = 2, RightHatch = 3;

    // A visible charge at a mirrored pair of ports.
    IEnumerator ChargePair(int left, int right, float seconds)
    {
        for (float t = 0; t < seconds;)
        {
            if (!Holding)
                t += Time.deltaTime * LevelRun.EnemyTempoScale;
            Cue(Muzzle(left), seconds - t);
            Cue(Muzzle(right), seconds - t);
            yield return null;
        }
    }

    // Ribbon Candy: one aim per hatch sweeping up and down, folded by the walls.
    IEnumerator RibbonCandy()
    {
        yield return ChargePair(LeftHatch, RightHatch, .45f);
        yield return Every(6.3f, .09f, n =>
        {
            Ribbon(n);
            if (n % 14 == 7)
                Sweets();
        });
    }

    void Ribbon(int n)
    {
        // From 25 degrees below the horizon to 55 above, one fold every 26 beats.
        float lift = 15 + 40 * Mathf.Sin(n / 26f * Mathf.PI * 2);
        bool heart = n % 5 == 0;
        for (int mirror = -1; mirror <= 1; mirror += 2)
        {
            var port = Muzzle(mirror < 0 ? LeftHatch : RightHatch);
            var axis = CombatVisuals.Rotate(Vector2.right * mirror, mirror * lift);
            if (heart)
            {
                UseTone(HostileTone.Dark);
                Fire<HeartRound>(port, axis, 3.2f, .19f).Bounce(3);
            }
            else
            {
                UseTone(HostileTone.Bright);
                Fire<NeedleRound>(port, axis, 3.4f, .19f).Bounce(3);
            }
            UseTone(HostileTone.Bright);
            Fire<NeedleRound>(port, axis, 2.3f, .19f).Bounce(3);
        }
    }

    // A wrapped sweet flicked at the pilot from each prong, so the open floor never rests.
    void Sweets()
    {
        if (!Clear)
            return;
        UseTone(HostileTone.Dark);
        foreach (int prong in new[] { LeftProng, RightProng })
            Fire<CandyRound>(Muzzle(prong), CombatVisuals.Aim(Muzzle(prong)), 2.7f, .2f);
    }

    // Gumdrop Garden: three beds, crown stars ricocheting between them.
    IEnumerator GumdropGarden()
    {
        for (int bed = 0; bed < 3; bed++)
        {
            yield return ChargePair(LeftHatch, RightHatch, .45f);
            Plant(bed);
            yield return Every(1.9f, .55f, n => CrownStars(bed * 4 + n, HostileTone.Bright));
        }
    }

    // A mirrored bed: gumdrops thrown out at widening angles brake to a glinting stop at
    // staggered distances, start to sink, and pop into small downward cones. The outer
    // drops pop first; the innermost pair pops at the pilot.
    void Plant(int bed)
    {
        int drops = Dense(5);
        int shards = Dense(5);
        UseTone(HostileTone.Dark);
        for (int i = 0; i < drops; i++)
        {
            float u = drops == 1 ? .5f : i / (drops - 1f);
            float angle = Mathf.Lerp(18, 78, u) + (bed % 2) * 6;
            float pauseAt = .6f + u * .4f;
            float hold = .45f + (drops - 1 - i) * .12f;
            for (int mirror = -1; mirror <= 1; mirror += 2)
                Fire<SimpleRound>(Muzzle(mirror < 0 ? LeftHatch : RightHatch), CombatVisuals.Rotate(Vector2.down, mirror * angle), 3.4f + u * 1.8f, .36f)
                    .Pause(pauseAt, hold, CombatRound.Turn.Down, 0, .45f)
                    .SplitAt(pauseAt + hold + .2f, shards, 15, 3.3f, .17f, i == 0);
        }
    }

    // Stars thrown up and out from the crown; they ricochet along the ceiling and walls.
    void CrownStars(int n, HostileTone shade)
    {
        int count = Dense(3);
        UseTone(shade);
        for (int i = 0; i < count; i++)
            for (int mirror = -1; mirror <= 1; mirror += 2)
            {
                var port = Hull.TransformPoint(new Vector3(mirror * .31f, .95f, -.31f));
                var axis = CombatVisuals.Rotate(Vector2.up, -mirror * (20 + i * 22 + (n % 2) * 11));
                Fire<StarRound>(port, axis, 3.0f, .19f).Bounce(3);
            }
    }

    // Candy Cane: stripes from both hatches after a charge, crown stars between them.
    IEnumerator CandyCane()
    {
        yield return ChargePair(LeftHatch, RightHatch, .4f);
        yield return Every(6.5f, .34f, n =>
        {
            CaneStripe(n);
            if (n % 3 == 1)
                CrownStars(n, HostileTone.Dark);
        });
    }

    // One diagonal stripe from each hatch, mirrored, its tone alternating
    // stripe by stripe. The missing bead walks along the stripe; the side walls turn
    // each stripe back across the other hatch's.
    void CaneStripe(int n)
    {
        int beads = Dense(8);
        int gap = n % beads;
        UseTone(n % 2 == 0 ? HostileTone.Bright : HostileTone.Dark);
        for (int i = 0; i < beads; i++)
        {
            if (i == gap)
                continue;
            float across = (i - (beads - 1) * .5f) * 56f / Mathf.Max(1, beads - 1);
            for (int mirror = -1; mirror <= 1; mirror += 2)
                ScoredShot(i % 2 == 0, Muzzle(mirror < 0 ? LeftHatch : RightHatch), CombatVisuals.Rotate(Vector2.down, mirror * (32 + across)), 2.7f)
                    .Bounce(1, false);
        }
    }

    // Sweetheart: four pairs of growing hearts, each after a charge at the prongs.
    IEnumerator Sweetheart()
    {
        for (int k = 0; k < 4; k++)
        {
            yield return ChargePair(LeftProng, RightProng, .45f);
            if (Clear)
            {
                UseTone(HostileTone.Dark);
                HeartOutline(Muzzle(LeftProng), 14, Dense(24), 2.4f, .9f, 0);
                HeartOutline(Muzzle(RightProng), -14, Dense(24), 2.4f, .9f, 0);
            }
            if (k % 2 == 1)
                CrownStars(k, HostileTone.Bright);
            yield return Wait(Rest(1.05f));
        }
    }

    // The outline of a heart grows from one port as it sinks: each bead's velocity is its
    // point on the curve plus a shared downward drift. Upright hearts alternate with
    // circles. A break swings the two halves apart after a stop.
    void HeartOutline(Vector3 port, float tilt, int beads, float growth, float sink, float breakAngle)
    {
        for (int i = 0; i < beads; i++)
        {
            float a = (i + .5f) / beads * Mathf.PI * 2;
            float s = Mathf.Sin(a);
            var shape = new Vector2(16 * s * s * s,
                13 * Mathf.Cos(a) - 5 * Mathf.Cos(2 * a) - 2 * Mathf.Cos(3 * a) - Mathf.Cos(4 * a)) / 17f;
            Vector2 velocity = CombatVisuals.Rotate(shape, tilt) * growth + Vector2.down * sink;
            CombatRound bead = i % 2 == 0
                ? (CombatRound)Fire<HeartRound>(port, velocity, velocity.magnitude, .19f)
                : Fire<SimpleRound>(port, velocity, velocity.magnitude, .17f);
            if (breakAngle != 0)
                bead.Pause(.85f, .4f, CombatRound.Turn.Rotate, shape.x < 0 ? breakAngle : -breakAngle, 1.3f);
        }
    }

    // Hard final phase, Heartbreak: a large heart that stops and breaks in two over
    // candy-cane stripes and crown stars.
    IEnumerator Heartbreak()
    {
        Phrase(Every(9, .38f, n =>
        {
            CaneStripe(n);
            if (n % 3 == 0)
                CrownStars(n, HostileTone.Bright);
        }));
        for (int k = 0; k < 6; k++)
        {
            yield return ChargePair(LeftProng, RightProng, .4f);
            if (Clear)
            {
                UseTone(HostileTone.Bright);
                HeartOutline((Muzzle(LeftProng) + Muzzle(RightProng)) * .5f, 0, Dense(36), 2.8f, .6f, 40);
            }
            yield return Wait(Rest(1.0f));
        }
        yield return Wait(.6f);
    }
}
