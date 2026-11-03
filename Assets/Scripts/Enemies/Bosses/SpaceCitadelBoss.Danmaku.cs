using UnityEngine;
using System.Collections;

// The black hole draws rounds in from a visible orbit and flings them out of its core.
// Phase one (central reactor open)
//   Accretion: arrows appear around the disk and spiral in until the core swallows
//     them. Dark stars (giant orbs) curl in from the same orbit on alternate beats.
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
//   Big Crunch: the core breathes, swallowing accretion rings from its orbit and
//     flinging spiral arms back out, both always running and trading dominance.
public sealed partial class SpaceCitadelBoss
{
    Vector3 Core => Reactor != null ? Reactor.transform.position : transform.position;
    MaterialPropertyBlock wellInk;
    LineRenderer accretionOrbit;
    float orbitStarted;
    protected override void Attack()
    {
        if (Reactor != null)
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

    Transform CoreBody => Reactor != null ? Reactor.transform : transform;

    // Accretion brightens the disk itself; the singularity stays dark.
    void OpenWell(bool inward)
    {
        if (inward)
        {
            orbitStarted = motionAge;
            if (accretionOrbit == null)
            {
                var tint = LevelProjectilePalette.Accent(LevelId.Space);
                tint.a = .32f;
                accretionOrbit = CombatVisuals.Line(transform, tint, .023f);
                accretionOrbit.useWorldSpace = false;
                accretionOrbit.sortingLayerName = "Enemy";
                CombatVisuals.Ring(accretionOrbit, Vector3.zero, 1);
            }
            UpdateOrbitTell();
            accretionOrbit.enabled = true;
        }
        SetAccretion(1);
    }

    void CloseWell()
    {
        if (accretionOrbit != null)
            accretionOrbit.enabled = false;
        SetAccretion(0);
    }
    void SetAccretion(float strength)
    {
        if (Hull == null) return;
        if (wellInk == null) wellInk = new MaterialPropertyBlock();
        var renderer = Hull.GetComponent<MeshRenderer>();
        renderer.GetPropertyBlock(wellInk);
        wellInk.SetFloat("_Feeding", strength);
        renderer.SetPropertyBlock(wellInk);
    }

    // Fit the entire orbit, including the outer half of the largest star, in view.
    static float AccretionRadius(Vector3 center)
    {
        Rect view = CombatRound.Border;
        float clearance = Mathf.Min(Mathf.Min(center.x - view.xMin, view.xMax - center.x),
                                    Mathf.Min(center.y - view.yMin, view.yMax - center.y));
        return Mathf.Clamp(clearance - .75f, 0, 3.6f);
    }

    void UpdateOrbitTell()
    {
        var center = Core;
        accretionOrbit.transform.position = center;
        accretionOrbit.transform.localScale = Vector3.one * AccretionRadius(center);
    }

    void AccretionRing(int n, int pairs)
    {
        var center = Core;
        float radius = AccretionRadius(center);
        for (int i = 0; i < pairs * 2; i++)
        {
            var radial = CombatVisuals.Rotate(Vector2.up, (i + .5f + n * .37f) * 180f / pairs);
            var heading = CombatVisuals.Rotate(-radial, radial.x < 0 ? 28 : -28);
            Fire<NeedleRound>(center + (Vector3)radial * radius, heading, .6f + radius * .22f, .19f)
                .Absorb(CoreBody, 7, .6f).LivesFor(12);
        }
    }

    void AccretionStars(int n, int pairs)
    {
        var center = Core;
        float radius = AccretionRadius(center);
        for (int i = 0; i < pairs * 2; i++)
        {
            var radial = CombatVisuals.Rotate(Vector2.up, (i + .5f + n * .137f) * 180f / pairs);
            var heading = CombatVisuals.Rotate(-radial, radial.x < 0 ? -20 : 20);
            Fire<GiantOrb>(center + (Vector3)radial * radius, heading, .4f + radius * .14f, .75f)
                .Absorb(CoreBody, 4, 1).LivesFor(14);
        }
    }

    // Mirrored spiral arms flung from the core; they accelerate out and straighten.
    void Expel(int n, int arms, float twist)
    {
        for (int arm = 0; arm < arms; arm++)
            foreach (int mirror in new[] { 1, -1 })
            {
                var radial = CombatVisuals.Rotate(Vector2.down, mirror * (n * 9 + arm * 360f / arms));
                Fire<NeedleRound>(Core, CombatVisuals.Rotate(radial, mirror * twist), .5f, .19f).Repel(Core, 7).LivesFor(9);
            }
    }

    // A full ring flung out between the arms, offset every time.
    void ExpelRing(int n, int beads)
    {
        for (int i = 0; i < beads; i++)
            Fire<NeedleRound>(Core, CombatVisuals.Rotate(Vector2.down, n * 7.3f + i * 360f / beads), .7f, .19f).Repel(Core, 6).LivesFor(9);
    }

    void ExpelStars(int n, int count)
    {
        for (int i = 0; i < count; i++)
            Fire<GiantOrb>(Core, CombatVisuals.Rotate(Vector2.down, n * 11 + i * 360f / count), .4f, .75f).Repel(Core, 3).LivesFor(13);
    }

    IEnumerator Accretion()
    {
        OpenWell(inward: true);
        yield return Wait(.6f);
        yield return Every(8, Rest(.4f), n =>
        {
            AccretionRing(n, Dense(18));
            if (n % 2 == 1)
                AccretionStars(n, Dense(4));
        });
        yield return Wait(1.2f);
        CloseWell();
    }

    // Rings thrown out that stop, hold, then fall back through the core and burst out beyond it.
    void SupernovaRing(int beads, float radius, float offset)
    {
        float travel = .9f;
        float speed = radius / (travel - .175f);
        for (int i = 0; i < beads; i++)
            Fire<NeedleRound>(Core, CombatVisuals.Rotate(Vector2.down, offset + i * 360f / beads), WorldSpeed(speed), .19f)
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
        OpenWell(inward: false);
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
    // dominant side flips: full accretion rings with a thin pair of arms, then full
    // spiral arms with sparse rings, dark stars travelling both ways.
    IEnumerator FinalSpell()
    {
        OpenWell(inward: true);
        yield return Wait(.5f);
        yield return Every(10, .1f, n =>
        {
            bool inward = n / 25 % 2 == 0;
            if (n % (inward ? 4 : 8) == 0)
                AccretionRing(n / 4, Dense(inward ? 16 : 10));
            if (!inward || n % 3 == 0)
                Expel(n, inward ? 2 : 4, 55);
            if (!inward && n % 15 == 5)
                ExpelRing(n, Dense(18));
            if (n % 8 == 4)
            {
                AccretionStars(n, Dense(inward ? 4 : 2));
                ExpelStars(n, Dense(inward ? 4 : 7));
            }
        });
        CloseWell();
        yield return Wait(.6f);
    }
}
