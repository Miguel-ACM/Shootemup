using UnityEngine;
using System.Collections;

// Space citadel: gravity is the weapon. Rounds come from the edges of space or the
// station's own core.
// Phase one (shield generators up)
//   Accretion: mirrored rings of arrows appear just outside every edge of the screen
//     and spiral in, accelerating, until the core swallows them; swarms of dark stars
//     (giant orbs) drift in from the edges among them.
//   Supernova: the core throws rings of arrows outward; each ring stops, holds, then
//     collapses back through the core and bursts out the other side. The centre
//     laser cuts the middle at the end.
// Phase two (core exposed)
//   Expulsion: the core flings mirrored spiral arms of arrows that accelerate away
//     and bend straight as they leave the screen, with full rings and rings of dark
//     stars flung out between them.
//   Binary Collapse: two supernova rings at once, counter-rotating offsets, with the
//     laser between them.
// Phase three (Hard)
//   Big Crunch: the core breathes, swallowing accretion rings from the edges and
//     flinging spiral arms back out, both always running and trading dominance.
public sealed partial class SpaceCitadelBoss
{
    Vector3 Core => parts[4] != null ? parts[4].transform.position : transform.position;
    Transform well;
    LineRenderer horizon;
    protected override void Attack()
    {
        if (parts[0] != null)
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

    Transform CoreBody => parts[4] != null ? parts[4].transform : transform;

    // The event horizon: a pulsing ring on the core while it absorbs or expels.
    void OpenWell()
    {
        if (well == null)
        {
            well = new GameObject("Gravity well").transform;
            horizon = CombatVisuals.Line(well, LevelProjectilePalette.Accent(LevelId.Space), .06f);
            ItemSpark.Sprite(well, "Well core", "standard", LevelProjectilePalette.Accent(LevelId.Space), .6f);
        }
        well.gameObject.SetActive(true);
        well.position = new Vector3(Core.x, Core.y, -.4f);
    }

    void CloseWell()
    {
        if (well != null)
            well.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (completing || health <= 0)
            CloseWell();
        if (well != null && well.gameObject.activeSelf)
        {
            well.position = new Vector3(Core.x, Core.y, -.4f);
            CombatVisuals.Ring(horizon, well.position, .55f + .12f * Mathf.Sin(Time.time * 6));
        }
    }

    // A point just outside the screen, walking its edge clockwise from the top centre.
    static Vector2 EdgePoint(float u)
    {
        const float Margin = .7f;
        float left = Camera.LeftLimit - Margin, right = Camera.RightLimit + Margin, bottom = Camera.DownLimit - Margin, top = Camera.UpLimit + Margin;
        float width = right - left, height = top - bottom;
        float d = Mathf.Repeat(Mathf.Repeat(u, 1) * 2 * (width + height) + width * .5f, 2 * (width + height));
        if (d < width)
            return new Vector2(left + d, top);
        d -= width;
        if (d < height)
            return new Vector2(right, top - d);
        d -= height;
        if (d < width)
            return new Vector2(right - d, bottom);
        return new Vector2(left, bottom + d - width);
    }

    // A mirrored ring around the whole screen edge that spirals in to the core.
    void AccretionRing(int n, int beads, float spin)
    {
        var into = CoreBody;
        for (int i = 0; i < beads; i++)
        {
            // The ring creeps round the edge every time, so no stretch of it stays empty.
            var p = EdgePoint((i + Mathf.Repeat(n * .37f, 1)) / (beads * 2f));
            foreach (int mirror in new[] { 1, -1 })
            {
                var from = new Vector2(p.x * mirror, p.y);
                var toward = (Vector2)Core - from;
                // Far rounds (the bottom edge) start faster, so the whole ring arrives together.
                Fire<NeedleRound>(new Vector3(from.x, from.y, 1), CombatVisuals.Rotate(toward.normalized, mirror * spin), .6f + toward.magnitude * .22f, .19f)
                    .Absorb(into, 7, .6f).LivesFor(12);
            }
        }
    }

    // Dark stars drifting in from mirrored points on the edge.
    void AccretionStars(int n, int pairs)
    {
        var into = CoreBody;
        for (int k = 0; k < pairs; k++)
        {
            var p = EdgePoint(Mathf.Repeat(n * .137f + k * .5f / pairs, .5f));
            foreach (int mirror in new[] { 1, -1 })
            {
                var from = new Vector2(p.x * mirror, p.y);
                var toward = (Vector2)Core - from;
                Fire<GiantOrb>(new Vector3(from.x, from.y, 1), CombatVisuals.Rotate(toward.normalized, mirror * 20), .4f + toward.magnitude * .14f, .75f)
                    .Absorb(into, 4, 1).LivesFor(14);
            }
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
        OpenWell();
        yield return Wait(.6f);
        yield return Every(8, Rest(.4f), n =>
        {
            AccretionRing(n, Dense(18), 28);
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

    void OnDestroy()
    {
        if (well != null)
            Destroy(well.gameObject);
    }

    // Hard final phase, Big Crunch: absorbing and expelling at once. Every 2.5 s the
    // dominant side flips: full accretion rings with a thin pair of arms, then full
    // spiral arms with sparse rings, dark stars travelling both ways.
    IEnumerator FinalSpell()
    {
        OpenWell();
        yield return Wait(.5f);
        yield return Every(10, .1f, n =>
        {
            bool inward = n / 25 % 2 == 0;
            if (n % (inward ? 4 : 8) == 0)
                AccretionRing(n / 4, Dense(inward ? 16 : 10), 28);
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
