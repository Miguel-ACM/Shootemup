using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Proposed black-hole book (ArtSource/BossPhaseProposals), flown only while
// DistrictBoss.ProposedCards is set so it can be recorded beside the shipping cards.
// One concept per phase (Miguel, 10 November 2026), developing verse by verse.
// Everything leaves the singularity or the two eyes inside its event horizon, except the
// inward fronts, which (as in the shipping book) are born on one circle beyond the whole
// screen and close on the core. Circles always move.
// Phase one, Accretion Weave: every circular front is woven from two strands, arrows
//   tilted one way and circles tilted the other, so successive fronts cross into a
//   shrinking lattice that the core swallows. A slowly turning seam stays open. On the
//   second pass the strands and the seam turn the other way.
//   1. Weave.  2. Tightening weave: the tilt grows through the verse.
//   3. Tidal weave: arrows outrun circles, so each front splits into two crossing rings.
//   4. Fountain: the eyes throw dark stars up over the top that gravity bends back into
//      the core.
// Phase two, Orrery: the core flings planets, small spinning rings of circles that travel
//   out on six mirrored headings (left planets turning one way, right planets the other)
//   and sling their moons off on the tangent together. Comets from the eyes deny a
//   still pilot. Each pass reverses the spins.
//   1. Planets.  2. Epicycles: every moon carries a satellite, drawing spirograph loops.
//   3. Swelling worlds: the planets grow as they travel and sling late.
//   4. Slingshot galaxy: planets on the in-between headings sling early, close to the core.
// Phase three (Hard), Crunch and Rebound: woven fronts close from the circle beyond the
//   screen, and every front the core swallows comes back out on the beat it arrives.
//   1. Bounce: a ring of arrows thrown out.  2. Spin: a spinning ring of circles that
//   grows outward.  3. Echo: a ring that stops and falls back through the core.
//   4. Swirl: a woven ring of arrows and crescents leaving in two directions of tilt.
// Tones: Accretion Weave bright arrows, dark circles and stars; Orrery bright planets,
//   dark comets; Crunch and Rebound bright fronts, dark rebounds.
public sealed partial class SpaceCitadelBoss
{
    int weaving;

    protected override IEnumerator Proposed()
    {
        // A phase break stops the fronts still closing in.
        weaving = 0;
        int verse = 0;
        while (true)
        {
            if (phaseThree)
                yield return CrunchAndRebound(verse++);
            else if (!phaseTwo)
                yield return AccretionWeave(verse++);
            else
                yield return Orrery(verse++);
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

    // ---------------------------------------------------------------- Accretion Weave

    IEnumerator AccretionWeave(int verse)
    {
        int movement = verse % 4;
        int hand = verse / 4 % 2 == 0 ? 1 : -1;
        OpenWell();
        yield return ChargeEyes(.45f);
        yield return Every(6, .8f, n =>
        {
            float tilt = movement == 1 ? 12 + 4 * Mathf.Min(7, n) : 26;
            float arrows = movement == 2 ? 2.9f : 2.5f;
            float circles = movement == 2 ? 2.1f : 2.5f;
            Phrase(WovenFront(verse * 8 + n, Dense(40), arrows, circles, hand * tilt, hand, HostileTone.Bright, HostileTone.Dark));
            if (movement == 3 && n % 2 == 0)
                Phrase(DarkFountain(n / 2));
        });
        yield return Wait(Rest(.6f));
    }

    // The radius of the circle beyond the screen, centred on the core.
    static float FrontCircle(Vector3 center, out Rect outside)
    {
        Rect r = CombatRound.Border;
        const float Margin = .45f;
        outside = Rect.MinMaxRect(r.xMin - Margin, r.yMin - Margin, r.xMax + Margin, r.yMax + Margin);
        float circle = 0;
        foreach (var corner in new[] { new Vector2(outside.xMin, outside.yMin), new Vector2(outside.xMin, outside.yMax),
            new Vector2(outside.xMax, outside.yMin), new Vector2(outside.xMax, outside.yMax) })
            circle = Mathf.Max(circle, Vector2.Distance(corner, center));
        return circle;
    }

    // One circular front born on the circle beyond the screen, like OuterFront, but woven:
    // even beads are arrows tilted one way, odd beads circles tilted the other. Each round
    // appears on the edge when its point of the shrinking circle arrives there.
    IEnumerator WovenFront(int n, int count, float arrowSpeed, float circleSpeed, float tilt, int seamTurn, HostileTone arrowTone, HostileTone circleTone)
    {
        weaving++;
        if (count % 2 == 1)
            count++;
        Vector3 center = Core;
        float circle = FrontCircle(center, out Rect outside);
        float seam = 180 + seamTurn * 30 * Mathf.Sin(n * .27f);
        var front = new List<(float delay, Vector3 point, Vector2 heading, bool arrow)>(count);
        for (int i = 0; i < count; i++)
        {
            float angle = (i + .5f) * 360f / count + n * 5;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, seam)) < 9)
                continue;
            bool arrow = i % 2 == 0;
            float world = (arrow ? arrowSpeed : circleSpeed) / WorldSpeed(1);
            var radial = CombatVisuals.Rotate(Vector2.up, angle);
            var heading = CombatVisuals.Rotate(-radial, arrow ? tilt : -tilt);
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
                    UseTone(arrowTone);
                    Fire<NeedleRound>(front[k].point, front[k].heading, arrowSpeed, .19f).Absorb(CoreBody, 5.2f, .6f).LivesFor(14);
                }
                else
                {
                    UseTone(circleTone);
                    Fire<SimpleRound>(front[k].point, front[k].heading, circleSpeed, .19f).Absorb(CoreBody, 5.2f, .6f).LivesFor(14);
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

    // ---------------------------------------------------------------- Orrery

    IEnumerator Orrery(int verse)
    {
        int movement = verse % 4;
        int spin = verse / 4 % 2 == 0 ? 1 : -1;
        OpenWell();
        yield return Charge(Hull, WeaponPort(), .5f);
        yield return Every(6, .85f, n =>
        {
            Planets(n, movement, spin, HostileTone.Bright);
            if (n % 2 == 1)
            {
                Comet(-1, HostileTone.Dark);
                Comet(1, HostileTone.Dark);
            }
        });
        yield return Wait(Rest(.6f));
        CloseWell();
    }

    // Six mirrored planets: rings of circles that bloom from the core, spin as they
    // travel out (left ones one way, right ones the other) and sling their moons off on
    // the tangent together.
    void Planets(int n, int movement, int spin, HostileTone shade)
    {
        int moons = Dense(10);
        UseTone(shade);
        float offset = movement == 3 ? 30 : 0;
        foreach (float course in new[] { 30f, 90f, 150f })
            for (int mirror = -1; mirror <= 1; mirror += 2)
            {
                var heading = CombatVisuals.Rotate(Vector2.down, mirror * (course + offset + (n % 3) * 8));
                for (int i = 0; i < moons; i++)
                {
                    var moon = Fire<SimpleRound>(Core, heading, movement == 3 ? 1.6f : 1.35f, .19f);
                    if (moon == null)
                        continue;
                    float phase = i * 360f / moons;
                    float rate = spin * mirror * 110;
                    if (movement == 1)
                        moon.Orbit(.8f, phase, rate, 12, .32f, phase * 2, -rate * 3, .7f).Sling(2.6f, .9f);
                    else if (movement == 2)
                        moon.Orbit(.5f, phase, rate, 12, 0, 0, 0, .7f).Expand(.35f, 2.4f).Sling(3.0f, .9f);
                    else if (movement == 3)
                        moon.Orbit(.8f, phase, rate, 12, 0, 0, 0, .6f).Sling(1.5f, 1.15f);
                    else
                        moon.Orbit(.8f, phase, rate, 12, 0, 0, 0, .7f).Sling(2.6f, .9f);
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

    // ---------------------------------------------------------------- Crunch and Rebound

    IEnumerator CrunchAndRebound(int verse)
    {
        int movement = verse % 4;
        int hand = verse / 4 % 2 == 0 ? 1 : -1;
        OpenWell();
        yield return ChargeEyes(.4f);
        yield return Every(7, .55f, n =>
        {
            int count = Dense(48);
            float circle = FrontCircle(Core, out _);
            Phrase(WovenFront(verse * 13 + n, count, 2.5f, 2.5f, hand * 24, hand, HostileTone.Bright, HostileTone.Bright));
            // The front reaches the core one radius later; it comes straight back out.
            Phrase(Rebound(verse * 13 + n, movement, count, circle / (2.5f / WorldSpeed(1))));
        });
        yield return Wait(Rest(.5f));
    }

    IEnumerator Rebound(int n, int movement, int count, float delay)
    {
        for (float clock = 0; clock < delay;)
        {
            if (!Holding)
                clock += Time.deltaTime;
            if (clock > delay - .35f)
                Cue(Core, delay - clock);
            yield return null;
        }
        UseTone(HostileTone.Dark);
        float turn = n * 7.5f;
        for (int i = 0; i < count; i++)
        {
            var radial = CombatVisuals.Rotate(Vector2.down, turn + i * 360f / count);
            if (movement == 0)
                Fire<NeedleRound>(Core, radial, .8f, .19f).Repel(Core, 3).LivesFor(9);
            else if (movement == 1)
            {
                var moon = Fire<SimpleRound>(Core, Vector2.down, 0, .19f);
                if (moon != null)
                    moon.Orbit(.3f, turn + i * 360f / count, (n % 2 == 0 ? 1 : -1) * 50, 9, 0, 0, 0, .3f).Expand(1.9f, 2.6f);
            }
            else if (movement == 2)
                ScoredShot(i % 3 == 0, Core, radial, WorldSpeed(2.6f / .725f))
                    .Pause(.9f, .5f, CombatRound.Turn.Rotate, 180, .6f)
                    .LivesFor(9);
            else
                MoonShot(i % 4 == 0, Core, CombatVisuals.Rotate(radial, i % 2 == 0 ? 30 : -30), 2.2f).LivesFor(9);
        }
    }
}
