using UnityEngine;
using System.Collections;

// The black hole draws circular fronts across the whole arena into its moving core.
// Phase one (central reactor open)
//   Accretion: every front is born on one circle beyond the visible screen and closes
//     in on the core, which swallows it; each round appears at the edge just as the
//     circle reaches it. Dark stars (giant orbs) close in from the same circle on
//     alternate beats. No nested inner circles, and the black hole keeps its upper
//     station as in phase two (Miguel).
//   Supernova: the core throws rings of arrows outward; each ring stops, holds, then
//     collapses back through the core and bursts out the other side. The centre
//     laser cuts the middle at the end.
// Phase two (reactor overload at half health)
//   Expulsion: the core flings mirrored spiral arms of arrows that accelerate away
//     and bend straight as they leave the screen, with full rings and rings of dark
//     stars flung out between them.
//   Binary Collapse: two supernova rings at once, counter-rotating offsets, with the
//     laser between them.
// Phase three (Hard)
//   Big Crunch: the core breathes, swallowing fronts that close in from the circle
//     beyond the screen and flinging spiral arms back out, trading dominance.
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
    void Expel(int n, int arms, float twist)
    {
        for (int arm = 0; arm < arms; arm++)
            for(int mirror=-1;mirror<=1;mirror+=2)
            {
                var radial = CombatVisuals.Rotate(Vector2.down, mirror * (n * 9 + arm * 360f / arms));
                ScoredShot(n%9<3,Core, CombatVisuals.Rotate(radial, mirror * twist), .5f).Repel(Core, 7).LivesFor(9);
            }
    }

    // A full ring flung out between the arms, offset every time.
    void ExpelRing(int n, int beads)
    {
        // A comet down the pilot's current line keeps the rotating seam transient.
        Fire<SimpleRound>(Core,CombatVisuals.Aim(Core),2.7f,.24f).Repel(Core,1.2f).LivesFor(10);
        for (int i = 0; i < beads; i++)
            MoonShot(i%3==0,Core, CombatVisuals.Rotate(Vector2.down, n * 7.3f + i * 360f / beads), .7f).Repel(Core, 6).LivesFor(9);
    }

    void ExpelStars(int n, int count)
    {
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
            Phrase(OuterFront<NeedleRound>(n, Dense(44), 2.6f, .19f, n % 2 == 0 ? 23 : -23, 8, 5.2f, .6f));
            if (n % 2 == 1)
                Phrase(OuterFront<GiantOrb>(n, Dense(10), 1.5f, .62f, n % 4 == 1 ? -18 : 18, 15, 3.4f, 1));
            // A comet down the pilot's line keeps a still pilot from waiting out the fronts.
            if (n % 3 == 2)
                Fire<SimpleRound>(Core, CombatVisuals.Aim(Core), 2.7f, .24f).LivesFor(10);
        });
        // Let the last fronts close in before the disk dims.
        while (emerging > 0)
            yield return null;
        yield return Wait(.6f);
        CloseWell();
    }

    int emerging;
    // One circular front centered on the core, beginning just outside the screen corner
    // farthest from it. Each round is released on the edge at the moment its point of
    // the shrinking circle would arrive there, so the whole front stays circular.
    IEnumerator OuterFront<T>(int n, int count, float velocity, float radius, float tilt, float seam, float pull, float reach) where T : CombatRound
    {
        emerging++;
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
            var heading = CombatVisuals.Rotate(-radial, tilt);
            Vector2 start = (Vector2)center + radial * circle;
            if (!Entry(start, heading * world, outside, out float delay))
                continue;
            var point = start + heading * world * delay;
            front.Add((delay, new Vector3(point.x, point.y, center.z), heading));
        }
        front.Sort((a, b) => a.delay.CompareTo(b.delay));
        float clock = 0;
        for (int k = 0; k < front.Count;)
        {
            if (!Holding)
                clock += Time.deltaTime;
            for (; k < front.Count && front[k].delay <= clock; k++)
                Fire<T>(front[k].point, front[k].heading, velocity, radius).Absorb(CoreBody, pull, reach).LivesFor(14);
            yield return null;
        }
        emerging = Mathf.Max(0, emerging - 1);
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

    // Rings thrown out that stop, hold, then fall back through the core and burst out beyond it.
    void SupernovaRing(int beads, float radius, float offset)
    {
        float travel = .9f;
        float speed = radius / (travel - .175f);
        for (int i = 0; i < beads; i++)
            ScoredShot(i%3==0,Core, CombatVisuals.Rotate(Vector2.down, offset + i * 360f / beads), WorldSpeed(speed))
                .Pause(travel, .7f, CombatRound.Turn.Rotate, 180, 2.6f / speed)
                .LivesFor(9);
    }

    IEnumerator Supernova(bool binary)
    {
        int beads = Dense(binary ? 26 : 30);
        for (int n = 0; n < 5; n++)
        {
            yield return Charge(Hull, WeaponPort(), .35f);
            SupernovaRing(beads, 3.4f + (n % 2) * .8f, n * 6);
            if (binary)
                SupernovaRing(beads, 2.2f, -n * 6 + 180f / beads);
            yield return Wait(Rest(1.1f));
        }
        Laser();
        yield return Wait(2.2f);
    }

    IEnumerator Expulsion()
    {
        OpenWell();
        yield return Charge(Hull, WeaponPort(), .5f);
        yield return Every(8, Rest(.09f), n =>
        {
            Expel(n, 4, 55);
            if (n % 13 == 0)
                ExpelRing(n, Dense(20));
            // Dark stars every 0.36 s, alternating between a big and a small ring.
            if (n % 4 == 1)
                ExpelStars(n, Dense(n % 8 == 1 ? 12 : 8));
        });
        CloseWell();
    }

    void Laser()
    {
        centralLaser = Fire<PulseLaser>(Muzzle(), Vector2.down, 0);
        if (centralLaser == null)
            return;
        centralLaser.Bind(Hull, WeaponPort());
        centralLaser.warning = 1.1f;
        centralLaser.activeTime = phaseTwo ? 1.8f : 1.4f;
        centralLaser.width = phaseTwo ? .42f : .34f;
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
                Phrase(OuterFront<NeedleRound>(n / 3, Dense(inward ? 44 : 30), 2.6f, .19f, n / 3 % 2 == 0 ? 23 : -23, 8, 5.2f, .6f));
            if (!inward || n % 3 == 0)
                Expel(n, inward ? 4 : 6, 55);
            if (!inward && n % 15 == 5)
                ExpelRing(n, Dense(30));
            if (n % 8 == 4)
            {
                Phrase(OuterFront<GiantOrb>(n, Dense(inward ? 10 : 7), 1.5f, .62f, n % 16 == 4 ? -18 : 18, 15, 3.4f, 1));
                ExpelStars(n, Dense(inward ? 6 : 9));
            }
        });
        while (emerging > 0)
            yield return null;
        CloseWell();
        yield return Wait(.6f);
    }
}
