using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Proposed black-hole book (ArtSource/BossPhaseProposals), flown only while
// DistrictBoss.ProposedCards is set so it can be recorded beside the shipping cards.
// Everything leaves the singularity or the two eyes inside its event horizon, except the
// inward fronts, which (as in the shipping book) are born on one circle beyond the whole
// screen and close on the core. Circles always move.
// Phase one
//   Accretion Weave: every circular front is woven from two strands: arrows tilted one
//     way and circles tilted the other. As a front closes, the strands swirl against
//     each other, so successive fronts cross into a shrinking lattice that the core
//     swallows. A slowly turning seam stays open. The eyes throw dark stars up over the
//     top of the arena; gravity bends them round and the core swallows them.
//   Eclipse: after a charge each eye throws a ring whose centre drifts outward as it
//     opens, so the two rings stop overlapping like an eclipse. They hold, fall back
//     through the black hole and open again inverted on the other side, crossing.
//     A comet with a bead tail leaves an eye at the pilot between eclipses.
// Phase two
//   Orrery: the core flings planets: small spinning rings of circles that travel out on
//     six mirrored headings, left planets turning one way and right planets the other,
//     then sling their moons off on the tangent all at once. Comets from the eyes deny
//     a still pilot.
//   Pulsar: star-shaped shells (five and six points, alternately tip down and tip up)
//     open, hold, then collapse through the core and open inverted. Mirrored pulsing
//     beams of arrows and crescents sweep round from the eyes; the centre laser cuts the
//     middle at the end.
// Phase three (Hard)
//   Galaxy Collision: woven fronts close in from the circle beyond the screen while the
//     core keeps flinging planets out through them and the eyes throw dark stars.
// Tones: the dominant tone flips card to card; dark stars are always dark.
//   Accretion Weave: bright arrow strand, dark circle strand.
//   Eclipse: dark rings with bright crescents, bright comets.
//   Orrery: bright planets, dark comets.
//   Pulsar: dark shells with bright beads, bright beams.
//   Galaxy Collision: bright fronts, dark planets.
public sealed partial class SpaceCitadelBoss
{
    int weaving;

    protected override IEnumerator Proposed()
    {
        // A phase break stops the fronts still closing in.
        weaving = 0;
        while (true)
        {
            if (phaseThree)
                yield return GalaxyCollision();
            else if (!phaseTwo)
            {
                yield return AccretionWeave();
                yield return Wait(.8f);
                yield return Eclipse();
                yield return Wait(.8f);
            }
            else
            {
                yield return Orrery();
                yield return Wait(.7f);
                yield return Pulsar();
                yield return Wait(.7f);
            }
        }
    }

    // The two eyes cut into the event horizon (SpaceBlackHole.shader).
    Vector3 Eye(int side) => CoreBody.TransformPoint(new Vector3(side * .31f, .065f, -.05f));

    IEnumerator ChargeEyes(float seconds)
    {
        for (float t = 0; t < seconds;)
        {
            if (!Holding)
                t += Time.deltaTime * LevelRun.EnemyTempoScale;
            Cue(Eye(-1), seconds - t);
            Cue(Eye(1), seconds - t);
            yield return null;
        }
    }

    // Accretion Weave: woven fronts on a steady beat, dark stars thrown over the top.
    IEnumerator AccretionWeave()
    {
        OpenWell();
        yield return Wait(.6f);
        yield return Every(5.5f, .45f, n =>
        {
            Phrase(WovenFront(n, Dense(40), 2.5f));
            if (n % 4 == 1)
                Phrase(DarkFountain(n / 4));
        });
        while (weaving > 0)
            yield return null;
        yield return Wait(.6f);
        CloseWell();
    }

    // One circular front born on the circle beyond the screen, like OuterFront, but woven:
    // even beads are arrows tilted one way, odd beads circles tilted the other. Each round
    // appears on the edge when its point of the shrinking circle arrives there.
    IEnumerator WovenFront(int n, int count, float velocity)
    {
        weaving++;
        if (count % 2 == 1)
            count++;
        Vector3 center = Core;
        Rect r = CombatRound.Border;
        const float Margin = .45f;
        Rect outside = Rect.MinMaxRect(r.xMin - Margin, r.yMin - Margin, r.xMax + Margin, r.yMax + Margin);
        float circle = 0;
        foreach (var corner in new[] { new Vector2(outside.xMin, outside.yMin), new Vector2(outside.xMin, outside.yMax),
            new Vector2(outside.xMax, outside.yMin), new Vector2(outside.xMax, outside.yMax) })
            circle = Mathf.Max(circle, Vector2.Distance(corner, center));
        float world = velocity / WorldSpeed(1);
        float seam = 180 + 30 * Mathf.Sin(n * .27f);
        var front = new List<(float delay, Vector3 point, Vector2 heading, bool arrow)>(count);
        for (int i = 0; i < count; i++)
        {
            float angle = (i + .5f) * 360f / count + n * 5;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, seam)) < 9)
                continue;
            bool arrow = i % 2 == 0;
            var radial = CombatVisuals.Rotate(Vector2.up, angle);
            var heading = CombatVisuals.Rotate(-radial, arrow ? 26 : -26);
            Vector2 start = (Vector2)center + radial * circle;
            if (!Entry(start, heading * world, outside, out float delay))
                continue;
            var point = start + heading * world * delay;
            front.Add((delay, new Vector3(point.x, point.y, center.z), heading, arrow));
        }
        front.Sort((a, b) => a.delay.CompareTo(b.delay));
        float clock = 0;
        for (int k = 0; k < front.Count;)
        {
            if (!Holding)
                clock += Time.deltaTime;
            for (; k < front.Count && front[k].delay <= clock; k++)
            {
                if (front[k].arrow)
                {
                    UseTone(HostileTone.Bright);
                    Fire<NeedleRound>(front[k].point, front[k].heading, velocity, .19f).Absorb(CoreBody, 5.2f, .6f).LivesFor(14);
                }
                else
                {
                    UseTone(HostileTone.Dark);
                    Fire<SimpleRound>(front[k].point, front[k].heading, velocity, .19f).Absorb(CoreBody, 5.2f, .6f).LivesFor(14);
                }
            }
            yield return null;
        }
        weaving = Mathf.Max(0, weaving - 1);
    }

    // A pair of dark stars thrown up and out from the eyes; the well bends them back
    // over the top of the arena into the core.
    IEnumerator DarkFountain(int n)
    {
        yield return ChargeEyes(.45f);
        UseTone(HostileTone.Dark);
        for (int mirror = -1; mirror <= 1; mirror += 2)
            Fire<GiantOrb>(Eye(mirror), CombatVisuals.Rotate(Vector2.up, -mirror * (28 + (n % 2) * 16)), 2.4f, .62f)
                .Absorb(CoreBody, 2.6f, .7f)
                .LivesFor(12);
    }

    // Eclipse: four charged pairs of eye rings, then time for the last to cross.
    IEnumerator Eclipse()
    {
        for (int k = 0; k < 4; k++)
        {
            yield return ChargeEyes(.45f);
            EclipseRings(k);
            yield return Wait(Rest(.7f));
            Comet(k % 2 == 0 ? -1 : 1, HostileTone.Bright);
            yield return Wait(Rest(.8f));
        }
        yield return Wait(1.2f);
    }

    // Each eye throws a ring whose centre drifts out to its own side while it opens. The
    // rings stop overlapping, hold, then retrace their paths through the black hole and
    // open inverted beyond it, so the two rings cross.
    void EclipseRings(int k)
    {
        int beads = Dense(20);
        const float travel = .9f;
        float radius = 2.6f + (k % 2) * .5f;
        for (int mirror = -1; mirror <= 1; mirror += 2)
        {
            var eye = Eye(mirror);
            var drift = new Vector2(mirror * 1.3f, -.3f);
            for (int i = 0; i < beads; i++)
            {
                var radial = CombatVisuals.Rotate(Vector2.down, mirror * (k * 7 + i * 360f / beads));
                Vector2 velocity = (radial * radius + drift) / (travel - .175f);
                bool moon = i % 4 == 0;
                UseTone(moon ? HostileTone.Bright : HostileTone.Dark);
                MoonShot(moon, eye, velocity, WorldSpeed(velocity.magnitude))
                    .Pause(travel, .6f, CombatRound.Turn.Rotate, 180, 1.1f)
                    .LivesFor(9);
            }
        }
    }

    // A comet from one eye at the pilot, shedding a short tail of beads.
    void Comet(int side, HostileTone shade)
    {
        UseTone(shade);
        var eye = Eye(side);
        var comet = Fire<SimpleRound>(eye, CombatVisuals.Aim(eye), 2.6f, .26f);
        if (comet != null)
            comet.LeaveTrail(.4f, 2.5f);
    }

    // Orrery: planets flung on a steady beat after one charge.
    IEnumerator Orrery()
    {
        OpenWell();
        yield return Charge(Hull, WeaponPort(), .5f);
        yield return Every(7.5f, .7f, n =>
        {
            Planets(n, HostileTone.Bright);
            if (n % 2 == 1)
            {
                Comet(-1, HostileTone.Dark);
                Comet(1, HostileTone.Dark);
            }
        });
        CloseWell();
    }

    // Six mirrored planets: rings of circles that bloom from the core, spin as they
    // travel out (left ones one way, right ones the other) and sling their moons off
    // on the tangent together.
    void Planets(int n, HostileTone shade)
    {
        int moons = Dense(9);
        UseTone(shade);
        foreach (float course in new[] { 30f, 90f, 150f })
            for (int mirror = -1; mirror <= 1; mirror += 2)
            {
                var heading = CombatVisuals.Rotate(Vector2.down, mirror * (course + (n % 3) * 8));
                for (int i = 0; i < moons; i++)
                {
                    var moon = Fire<SimpleRound>(Core, heading, 1.35f, .19f);
                    if (moon != null)
                        moon.Orbit(.8f, i * 360f / moons, mirror * 110, 12, 0, 0, 0, .7f).Sling(2.6f, .9f);
                }
            }
    }

    // Pulsar: five charged star shells under sweeping beams, then the centre laser.
    IEnumerator Pulsar()
    {
        Phrase(PulsarBeams(7.4f));
        for (int k = 0; k < 5; k++)
        {
            yield return Charge(Hull, WeaponPort(), .35f);
            StarShell(k);
            yield return Wait(Rest(1.0f));
        }
        Laser();
        yield return Wait(2.2f);
    }

    // A shell whose radius follows a star: five points tip down, then six points tip up.
    // It holds, then shrinks through the core at a speed proportional to its reach, so it
    // opens again as the same star turned over.
    void StarShell(int k)
    {
        int beads = Dense(30);
        int points = k % 2 == 0 ? 5 : 6;
        float turn = k % 2 == 0 ? 0 : 180f / points;
        const float travel = .95f;
        for (int i = 0; i < beads; i++)
        {
            float angle = i * 360f / beads;
            float reach = 3.4f * (.62f + .38f * Mathf.Cos(points * (angle - turn) * Mathf.Deg2Rad));
            bool bead = i % 3 == 0;
            UseTone(bead ? HostileTone.Bright : HostileTone.Dark);
            ScoredShot(bead, Core, CombatVisuals.Rotate(Vector2.down, angle), WorldSpeed(reach / (travel - .175f)))
                .Pause(travel, .55f, CombatRound.Turn.Rotate, 180, 1.15f)
                .LivesFor(9);
        }
    }

    // Mirrored beams from the two eyes sweeping round in opposite directions, pulsing
    // four rounds on and two off.
    IEnumerator PulsarBeams(float seconds)
    {
        yield return Every(seconds, .09f, n =>
        {
            if (n % 6 >= 4)
                return;
            float sweep = n * 6f;
            UseTone(HostileTone.Bright);
            for (int mirror = -1; mirror <= 1; mirror += 2)
                MoonShot(n % 6 == 0, Eye(mirror), CombatVisuals.Rotate(Vector2.down, mirror * sweep), 2.8f);
        });
    }

    // Hard final phase, Galaxy Collision: woven fronts closing in through planets flung out.
    IEnumerator GalaxyCollision()
    {
        OpenWell();
        yield return Wait(.5f);
        yield return Every(10, .5f, n =>
        {
            Phrase(WovenFront(n, Dense(40), 2.5f));
            if (n % 2 == 1)
                Planets(n, HostileTone.Dark);
            if (n % 6 == 3)
                Phrase(DarkFountain(n / 6));
        });
        while (weaving > 0)
            yield return null;
        CloseWell();
        yield return Wait(.6f);
    }
}
