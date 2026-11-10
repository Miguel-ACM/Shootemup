using UnityEngine;
using System.Collections;

// The black hole draws circular fronts across the whole arena into its core. It has no
// laser and no aimed rounds (Miguel, 10 November 2026): its pressure comes from gravity.
// Phase one (central reactor open)
//   Accretion: every front is an actual circle centred on the black hole, born beyond
//     the visible screen. Its rounds fall straight in, so the circle stays round as it
//     shrinks, and the core swallows it. The black hole holds still while a front is
//     closing, so the circle stays centred on it. Dark stars (giant orbs) close in from
//     the same circle on alternate beats. A slowly turning seam stays open. No nested
//     inner circles; the black hole keeps its upper station.
//   Supernova: the core throws dense rings of arrows outward. Each ring stops, holds,
//     then collapses back through the core and bursts out the other side. Every ring
//     carries an opening that jumps round from ring to ring and turns over as the ring
//     passes through the core, so the safe place keeps moving.
// Phase two (reactor overload at half health)
//   Expulsion: the core flings mirrored spiral arms of arrows that accelerate away
//     and bend straight as they leave the screen, with full rings and rings of dark
//     stars flung out between them.
//   Binary Collapse: two supernova rings at once with counter-rotating offsets and
//     openings on opposite sides.
// Phase three (Hard)
//   Big Crunch: the core breathes, swallowing fronts that close in from the circle
//     beyond the screen and flinging spiral arms back out, trading dominance.
// Tones (Miguel, 9 November 2026); dark stars are always dark.
//   Accretion: bright inward fronts.
//   Supernova: dark ring arrows with bright circular beads; Binary Collapse pairs a
//     dark outer ring with a bright inner ring.
//   Expulsion: bright spiral arms with dark rings flung between them.
//   Big Crunch: bright inward fronts, dark outward arms and bright rings, so the
//     dominant tone flips with the dominant direction.
public sealed partial class SpaceCitadelBoss
{
    Vector3 Core => CoreVisual != null ? CoreVisual.position : transform.position;
    MaterialPropertyBlock wellInk;
    protected override void Attack()
    {
        if (CoreVisual != null)
            OpenCards();
    }

    protected override IEnumerator Cards()
    {
        while (true)
        {
            if (phaseThree)
                yield return FinalSpell();
            else if (!phaseTwo)
            {
                yield return Accretion();
                yield return Wait(.8f);
                yield return Supernova(false);
                yield return Wait(.8f);
            }
            else
            {
                yield return Expulsion();
                yield return Wait(.7f);
                yield return Supernova(true);
                yield return Wait(.7f);
            }
        }
    }

    Transform CoreBody => CoreVisual != null ? CoreVisual : transform;

    // Accretion brightens the disk itself; the singularity stays dark.
    void OpenWell() => SetAccretion(1);

    void CloseWell() => SetAccretion(0);

    void SetAccretion(float strength)
    {
        if (Hull == null) return;
        if (wellInk == null) wellInk = new MaterialPropertyBlock();
        var renderer = Hull.GetComponent<MeshRenderer>();
        renderer.GetPropertyBlock(wellInk);
        wellInk.SetFloat("_Feeding", strength);
        renderer.SetPropertyBlock(wellInk);
    }

    // Mirrored spiral arms flung from the core; they accelerate out and straighten.
    void Expel(int n, int arms, float twist, HostileTone shade)
    {
        UseTone(shade);
        for (int arm = 0; arm < arms; arm++)
            for (int mirror = -1; mirror <= 1; mirror += 2)
            {
                var radial = CombatVisuals.Rotate(Vector2.down, mirror * (n * 9 + arm * 360f / arms));
                ScoredShot(n % 9 < 3, Core, CombatVisuals.Rotate(radial, mirror * twist), .5f).Repel(Core, 7).LivesFor(9);
            }
    }

    // A full ring flung out between the arms, offset every time.
    void ExpelRing(int n, int beads, HostileTone shade)
    {
        UseTone(shade);
        for (int i = 0; i < beads; i++)
            MoonShot(i % 3 == 0, Core, CombatVisuals.Rotate(Vector2.down, n * 7.3f + i * 360f / beads), .7f).Repel(Core, 6).LivesFor(9);
    }

    void ExpelStars(int n, int count)
    {
        UseTone(HostileTone.Dark);
        for (int i = 0; i < count; i++)
            Fire<GiantOrb>(Core, CombatVisuals.Rotate(Vector2.down, n * 11 + i * 360f / count), .4f, .75f).Repel(Core, 3).LivesFor(13);
    }

    IEnumerator Accretion()
    {
        // Brighten the disk without the inward dive or the nested orbit tells.
        OpenWell();
        yield return Wait(.6f);
        yield return Every(5.5f, .38f, n =>
        {
            Phrase(OuterFront<NeedleRound>(n, Dense(44), 2.6f, .19f, 8, .6f, HostileTone.Bright));
            if (n % 2 == 1)
                Phrase(OuterFront<GiantOrb>(n, Dense(10), 1.5f, .62f, 15, 1, HostileTone.Dark));
        });
        // Let the last fronts close in before the disk dims.
        while (closing > 0)
            yield return null;
        yield return Wait(.6f);
        CloseWell();
    }

    // Fronts still closing on the core. The black hole holds still until they arrive.
    int closing;
    // One actual circle centred on the core, beginning just outside the screen corner
    // farthest from it. Each round falls straight in and is released on the edge at the
    // moment its point of the shrinking circle arrives there, so the whole front stays a
    // circle centred on the black hole until the core swallows it.
    IEnumerator OuterFront<T>(int n, int count, float velocity, float radius, float seam, float reach, HostileTone shade) where T : CombatRound
    {
        closing++;
        Vector3 center = Core;
        Rect r = CombatRound.Border;
        const float Margin = .45f;
        Rect outside = Rect.MinMaxRect(r.xMin - Margin, r.yMin - Margin, r.xMax + Margin, r.yMax + Margin);
        float circle = 0;
        foreach (var corner in new[] { new Vector2(outside.xMin, outside.yMin), new Vector2(outside.xMin, outside.yMax),
            new Vector2(outside.xMax, outside.yMin), new Vector2(outside.xMax, outside.yMax) })
            circle = Mathf.Max(circle, Vector2.Distance(corner, center));
        float world = velocity / WorldSpeed(1);
        var front = new System.Collections.Generic.List<(float delay, Vector3 point, Vector2 heading)>(count);
        for (int i = 0; i < count; i++)
        {
            float angle = (i + .5f) * 360f / count + n * 7 * (n % 2 == 0 ? 1 : -1);
            // All fronts share one slowly turning radial escape seam.
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 180 + 24 * Mathf.Sin(n * .22f))) < seam)
                continue;
            var radial = CombatVisuals.Rotate(Vector2.up, angle);
            var heading = -radial;
            Vector2 start = (Vector2)center + radial * circle;
            if (!Entry(start, heading * world, outside, out float delay))
                continue;
            var point = start + heading * world * delay;
            front.Add((delay, new Vector3(point.x, point.y, center.z), heading));
        }
        front.Sort((a, b) => a.delay.CompareTo(b.delay));
        float clock = 0;
        float arrival = circle / Mathf.Max(.01f, world);
        for (int k = 0; k < front.Count || clock < arrival;)
        {
            if (!Holding)
                clock += Time.deltaTime;
            UseTone(shade);
            // A straight fall keeps the circle round; the core swallows it on arrival.
            for (; k < front.Count && front[k].delay <= clock; k++)
                Fire<T>(front[k].point, front[k].heading, velocity, radius).Absorb(CoreBody, 0, reach).LivesFor(14);
            yield return null;
        }
        closing = Mathf.Max(0, closing - 1);
    }

    // When a straight path from outside first crosses into the rectangle.
    static bool Entry(Vector2 start, Vector2 velocity, Rect box, out float time)
    {
        float enter = 0, exit = float.MaxValue;
        time = 0;
        for (int axis = 0; axis < 2; axis++)
        {
            float p = start[axis], v = velocity[axis], lo = axis == 0 ? box.xMin : box.yMin, hi = axis == 0 ? box.xMax : box.yMax;
            if (Mathf.Abs(v) < 1e-5f)
            {
                if (p < lo || p > hi)
                    return false;
                continue;
            }
            float a = (lo - p) / v, b = (hi - p) / v;
            enter = Mathf.Max(enter, Mathf.Min(a, b));
            exit = Mathf.Min(exit, Mathf.Max(a, b));
        }
        time = enter;
        return enter <= exit;
    }

    // A dense ring thrown out that stops, holds, then falls back through the core and
    // bursts out beyond it. An opening of three beads faces the given angle; it turns
    // over as the ring passes through the core. A single ring is dark arrows with bright
    // circular beads; a uniform shade colours the whole ring (Binary Collapse).
    void SupernovaRing(int beads, float radius, float offset, float opening, HostileTone? shade = null)
    {
        const float travel = .75f;
        float speed = radius / (travel - .175f);
        float step = 360f / beads;
        for (int i = 0; i < beads; i++)
        {
            float angle = offset + i * step;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, opening)) < step * 1.5f)
                continue;
            UseTone(shade ?? (i % 3 == 0 ? HostileTone.Bright : HostileTone.Dark));
            ScoredShot(i % 3 == 0, Core, CombatVisuals.Rotate(Vector2.down, angle), WorldSpeed(speed))
                .Pause(travel, .5f, CombatRound.Turn.Rotate, 180, 3.2f / speed)
                .LivesFor(9);
        }
    }

    // Seven charged rings; their radii alternate so the collapsing rings cross the
    // opening ones, and the opening jumps round by 137 degrees each ring.
    IEnumerator Supernova(bool binary)
    {
        int beads = Dense(binary ? 32 : 38);
        for (int n = 0; n < 7; n++)
        {
            yield return Charge(Hull, WeaponPort(), .3f);
            float opening = n * 137;
            SupernovaRing(beads, 3.0f + (n % 2) * 1.4f, n * 6, opening, binary ? HostileTone.Dark : (HostileTone?)null);
            if (binary)
                SupernovaRing(beads, 2.2f, -n * 6 + 180f / beads, opening + 180, HostileTone.Bright);
            yield return Wait(Rest(.85f));
        }
        yield return Wait(1.2f);
    }

    IEnumerator Expulsion()
    {
        OpenWell();
        yield return Charge(Hull, WeaponPort(), .5f);
        yield return Every(8, Rest(.09f), n =>
        {
            Expel(n, 4, 55, HostileTone.Bright);
            if (n % 13 == 0)
                ExpelRing(n, Dense(20), HostileTone.Dark);
            // Dark stars every 0.36 s, alternating between a big and a small ring.
            if (n % 4 == 1)
                ExpelStars(n, Dense(n % 8 == 1 ? 12 : 8));
        });
        CloseWell();
    }

    // Hard final phase, Big Crunch: absorbing and expelling at once. Every 2.5 s the
    // dominant side flips: full inward fronts closing from the circle beyond the screen
    // with a thin pair of arms, then full spiral arms with sparse fronts, dark stars
    // travelling both ways. Like phase one, nothing is born on inner circles.
    IEnumerator FinalSpell()
    {
        OpenWell();
        yield return Wait(.5f);
        yield return Every(10, .1f, n =>
        {
            bool inward = n / 25 % 2 == 0;
            if (n % (inward ? 3 : 6) == 0)
                Phrase(OuterFront<NeedleRound>(n / 3, Dense(inward ? 44 : 30), 2.6f, .19f, 8, .6f, HostileTone.Bright));
            if (!inward || n % 3 == 0)
                Expel(n, inward ? 4 : 6, 55, HostileTone.Dark);
            if (!inward && n % 15 == 5)
                ExpelRing(n, Dense(30), HostileTone.Bright);
            if (n % 8 == 4)
            {
                Phrase(OuterFront<GiantOrb>(n, Dense(inward ? 10 : 7), 1.5f, .62f, 15, 1, HostileTone.Dark));
                ExpelStars(n, Dense(inward ? 6 : 9));
            }
        });
        while (closing > 0)
            yield return null;
        CloseWell();
        yield return Wait(.6f);
    }
}
