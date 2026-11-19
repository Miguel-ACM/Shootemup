using System.Collections;
using UnityEngine;

// Tunnel saw. Every round leaves from the centre of the spinning blade.
// Phase one
//   Frogger Crossing, over and over: it glides to a new station, brakes, spins up and
//     throws six widening rings that turn in alternate directions.
// Phase two, Buzzsaw Bloom (no minions): it ricochets round the whole arena and fires
//   only when it hits a wall. Every impact opens a full turn of hooked blades, rice
//   grains launched together at rising speeds, each lagging further behind the spin,
//   with slow round teeth between the blades and two sparks grinding away along the
//   struck wall. Impacts cycle through a ripsaw (every blade hooked with the spin), a
//   crosscut (alternate blades hooked against it) and spokes (nearly straight blades,
//   fast and slow alternately); the hook deepens as the saw wears down. A fast trio
//   flies at the pilot from every impact. No round bounces off the walls.
// Phase three (Hard)
//   Overdrive: it ricochets faster, and every impact drives a glowing tooth into the
//     wall. The embedded teeth keep spitting straight fans of arrows back across the
//     arena for a few seconds, so the walls it has struck stay live while it hunts the
//     next one; the impact itself throws only round teeth.
// Tones: Frogger's rings alternate bright and dark with their direction, starting on
// the other tone at each new station, and each aimed tracer takes the tone its ring
// does not. Buzzsaw blades and the aimed trio are bright, teeth and sparks dark. In
// Overdrive the embedded teeth fire only bright arrows and the impacts only dark rounds.
public sealed partial class TunnelSawBoss
{
    CombatHullVisual spinner;
    protected override void Attack()
    {
        if (!miniature && entered)
            OpenCards();
    }

    protected override IEnumerator Cards()
    {
        spinner = GetComponent<CombatHullVisual>();
        while (true)
        {
            if (phaseThree)
                yield return FinalSpell();
            else if (!phaseTwo)
                yield return FroggerCrossing();
            else
                yield return RicochetStorm(10);
        }
    }

    void ImpactBurst(Vector2 inward)
    {
        using var sample = burstMarker.Auto();
        impacts++;
        if (phaseThree)
            Embed(inward);
        else if (phaseTwo)
            BuzzsawBloom(inward);
        else
            OpeningBurst(inward);
    }

    // Before the first station the saw crosses the arena once; each impact throws a full
    // 360-degree burst of fast rays and slow teeth.
    void OpeningBurst(Vector2 inward)
    {
        int rays = Dense(20);
        float step = 360f / rays;
        for (int i = 0; i < rays; i++)
        {
            var axis = CombatVisuals.Rotate(inward, i * step + impacts * 7);
            UseTone(Shape(i % 3 == 0));
            ScoredShot(i % 3 == 0, transform.position, axis, 4.8f);
            UseTone(HostileTone.Dark);
            var tooth = Fire<SimpleRound>(transform.position, CombatVisuals.Rotate(axis, step * .5f), 1.6f, .3f);
            if (i % 2 == 0)
                tooth.Bounce(2);
        }
    }

    static readonly Vector2[] Stations = { new Vector2(0, .25f), new Vector2(-.55f, .5f), new Vector2(.55f, .05f), new Vector2(-.5f, -.1f), new Vector2(.5f, .55f) };
    int nextStation;

    // Brake, glide to a station (the centre by default) and hold there while the card plays.
    IEnumerator Centre(Vector2? spot = null)
    {
        haltUntil = float.MaxValue;
        var at = spot ?? new Vector2(0, .25f);
        var centre = new Vector3(arena.center.x + at.x * (arena.width * .5f - Radius), arena.center.y + at.y * (arena.height * .5f - Radius), transform.position.z);
        while (Vector3.Distance(transform.position, centre) > .05f)
        {
            if (!Holding)
                transform.position = Vector3.MoveTowards(transform.position, centre, Time.deltaTime * LevelRun.EnemyTempoScale * 6);
            yield return null;
        }
        spinner.spin = 1600;
    }

    void ResumeBouncing()
    {
        haltUntil = 0;
        spinner.spin = 720;
        velocity = new Vector2(impacts % 2 == 0 ? 1 : -1, -.75f).normalized;
    }

    IEnumerator FroggerCrossing()
    {
        spinner.spin = 720;
        yield return Centre(Stations[nextStation++ % Stations.Length]);
        yield return Charge(Hull, Vector3.zero, .5f);
        int teeth = Dense(36);
        bool firstBright = nextStation % 2 == 1;
        for (int ring = 0; ring < 6; ring++)
        {
            var ringTone = (ring % 2 == 0) == firstBright ? HostileTone.Bright : HostileTone.Dark;
            UseTone(ringTone == HostileTone.Bright ? HostileTone.Dark : HostileTone.Bright);
            if (ring % 2 == 0)
                Fire<SimpleRound>(transform.position, CombatVisuals.Aim(transform.position), 2.8f, .23f);
            int sign = ring % 2 == 0 ? 1 : -1;
            UseTone(ringTone);
            for (int i = 0; i < teeth; i++)
            {
                var tooth = Fire<SimpleRound>(transform.position, Vector2.down, 0, ring % 2 == 0 ? .2f : .26f);
                if (tooth != null)
                    tooth.Orbit(Radius * .9f, i * 360f / teeth, sign * 30, 10, 0, 0, 0, .05f).Expand(1.3f, 2.8f);
            }
            yield return Wait(Rest(.55f));
        }
        yield return Wait(.6f);
    }

    // Silent while it travels: all of its fire comes from the impacts.
    IEnumerator RicochetStorm(float seconds)
    {
        if (haltUntil > age)
            ResumeBouncing();
        yield return Wait(seconds);
    }

    protected override void OnPhaseTwo()
    {
        if (miniature)
            return;
        // No minions: a brief pause, then the blade starts blooming. The shared phase break is
        // the only effect; its own yellow shockwave ring was an extra no other boss shows.
        haltUntil = age + 1.8f;
        GetComponent<CombatHullVisual>().spin = 720;
    }

    // How far the saw has worn through the current phase, 0 at its start and 1 at its end.
    float Wear => Mathf.InverseLerp(PhaseEnd(CurrentPhase - 1), PhaseEnd(CurrentPhase), HealthFraction);

    // Buzzsaw Bloom: a full turn of blades, four rice grains each launched together at
    // rising speeds and lagging further behind the spin with each grain, with a slow round
    // tooth between neighbouring blades.
    void BuzzsawBloom(Vector2 inward)
    {
        var centre = transform.position;
        int blades = Dense(12);
        int kind = impacts % 3;
        int spin = impacts / 3 % 2 == 0 ? 1 : -1;
        float hook = Mathf.Lerp(5, 12, Wear);
        float step = 360f / blades;
        for (int b = 0; b < blades; b++)
        {
            float root = b * step + impacts * 9;
            // Ripsaw hooks every blade with the spin; crosscut alternates; spokes run nearly
            // straight, alternate blades fast and slow.
            float lag = kind == 0 ? spin * hook : kind == 1 ? (b % 2 == 0 ? spin : -spin) * hook : spin * hook * .3f;
            float pace = kind == 2 && b % 2 == 1 ? .75f : 1;
            UseTone(HostileTone.Bright);
            for (int k = 0; k < 4; k++)
                Fire<RiceRound>(centre, CombatVisuals.Rotate(inward, root - lag * k), (1.6f + 1.1f * k) * pace, .19f);
            UseTone(HostileTone.Dark);
            Fire<SimpleRound>(centre, CombatVisuals.Rotate(inward, root + step * .5f), 1.2f, .3f);
        }
        // Two sparks grind away along the struck wall, one each way.
        for (int direction = -1; direction <= 1; direction += 2)
            Fire<SimpleRound>(centre, -inward, 4.4f, .22f)
                .RunWalls(5, direction);
        // A fast trio at the pilot: an impact nearby is a threat.
        UseTone(HostileTone.Bright);
        var aim = CombatVisuals.Aim(centre);
        for (int i = -1; i <= 1; i++)
            Fire<RiceRound>(centre, CombatVisuals.Rotate(aim, i * 10), 4.6f, .19f);
    }

    // Hard final phase, Overdrive. Each impact drives a tooth into the wall: a giant
    // round that stays where it struck, with a ring of dark round teeth thrown off the hit.
    const int EmbeddedTeeth = 4;
    readonly Vector2[] embeddedAt = new Vector2[EmbeddedTeeth], embeddedFacing = new Vector2[EmbeddedTeeth];
    readonly float[] embeddedUntil = new float[EmbeddedTeeth];

    void Embed(Vector2 inward)
    {
        int slot = impacts % EmbeddedTeeth;
        embeddedAt[slot] = transform.position - (Vector3)inward * Radius * .7f;
        embeddedFacing[slot] = inward;
        embeddedUntil[slot] = age + 4.2f;
        UseTone(HostileTone.Dark);
        Fire<GiantOrb>(embeddedAt[slot], inward, 0, .6f).LivesFor(4.2f);
        int rays = Dense(16);
        for (int i = 0; i < rays; i++)
            Fire<SimpleRound>(transform.position, CombatVisuals.Rotate(inward, i * 360f / rays + impacts * 11), 4.4f, .21f);
        // Only the impact may release this aimed tooth; travel remains silent.
        Fire<SimpleRound>(transform.position, CombatVisuals.Aim(transform.position), 3.0f, .23f);
    }

    IEnumerator FinalSpell()
    {
        if (haltUntil > age)
            ResumeBouncing();
        yield return Every(12, Rest(.3f), n =>
        {
            UseTone(HostileTone.Bright);
            for (int slot = 0; slot < EmbeddedTeeth; slot++)
            {
                if (age >= embeddedUntil[slot])
                    continue;
                // Each embedded tooth sweeps a narrow fan of straight arrows back across the arena.
                var axis = CombatVisuals.Rotate(embeddedFacing[slot], Mathf.Sin(n * .45f + slot * 1.7f) * 40);
                int fan = Dense(5);
                for (int i = 0; i < fan; i++)
                    Fire<NeedleRound>(embeddedAt[slot], CombatVisuals.Rotate(axis, (i - (fan - 1) * .5f) * 13), 3.1f, .19f);
            }
        });
    }

    // Round teeth are dark and arrows bright.
    static HostileTone Shape(bool circular) => circular ? HostileTone.Dark : HostileTone.Bright;
}
