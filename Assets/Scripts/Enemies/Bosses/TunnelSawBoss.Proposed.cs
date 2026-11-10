using System.Collections;
using UnityEngine;

// Proposed Tunnel saw card book (ArtSource/BossPhaseProposals), flown only while
// DistrictBoss.ProposedCards is set. Every round still leaves the centre of the blade.
// Phase one (stations, no bouncing)
//   Gear Train: at each new station the saw throws pairs of widening cogs. The bright
//     cog wears its teeth outward and turns one way; the dark cog wears them inward,
//     turns the other way and widens more slowly, so the two openings sweep past each
//     other like meshing gears. A fast rice tracer flies at the pilot with each pair.
//   Spiral Stair: a fast ring of small beads and a slow ring of large teeth leave
//     together with one shared opening; each pair's opening turns a step further round,
//     so the openings climb a spiral staircase around the saw. The first faces the pilot.
// Phase two (ricochet; it fires only when it strikes a wall)
//   Buzzsaw Bloom: each impact opens a full turn of hooked blades, rice grains launched
//     together at rising speeds, each lagging further behind the spin, with slow round
//     teeth between the blades; some blade tips rebound once. A fast trio flies at the
//     pilot, so an impact near the pilot is a threat.
//   Spark Fountain: each impact throws a fountain of fast and slow rice sparks that
//     arc back down under gravity, a full turn of slow teeth (half of them rebound once)
//     and runners that grind away along the struck wall in both directions.
// Hard phase three, Chain Reaction: a faster ricochet. Impacts alternate blades and
//   fountains, and every impact adds a crown of slow shrapnel teeth that crack into
//   small fast fans as they widen.
// Tones: Gear Train bright outer cogs, dark inner cogs and tracers; Spiral Stair dark
// slow teeth with bright fast beads; Buzzsaw Bloom bright blades with dark teeth; Spark
// Fountain dark sparks with bright teeth and runners; Chain Reaction adds dark shrapnel.
public sealed partial class TunnelSawBoss
{
    bool patrolling;

    protected override IEnumerator Proposed()
    {
        spinner = GetComponent<CombatHullVisual>();
        while (true)
        {
            if (phaseThree)
                yield return Patrol(12, ChainReaction, 1.15f);
            else if (!phaseTwo)
            {
                yield return GearTrain();
                yield return SpiralStair();
            }
            else
            {
                yield return Patrol(9, BuzzsawBloom);
                yield return Patrol(9, SparkFountain);
            }
        }
    }

    // Review switch only: keep the shipping patrol, and so its impact bursts, still
    // through a phase break until the proposed book takes the saw back.
    protected override void OnPhaseEntered(int phase)
    {
        base.OnPhaseEntered(phase);
        if (ProposedCards && !miniature)
            haltUntil = float.MaxValue;
    }

    // ---- Phase one ----

    IEnumerator GearTrain()
    {
        spinner.spin = 720;
        yield return Centre(Stations[nextStation++ % Stations.Length]);
        yield return Charge(Hull, Vector3.zero, .5f);
        int outer = Dense(36), inner = Dense(30);
        int turn = nextStation % 2 == 0 ? 1 : -1;
        for (int pair = 0; pair < 3; pair++)
        {
            UseTone(HostileTone.Bright);
            Cog(outer, pair * 11, turn, .42f, 1.3f, .24f, .19f);
            UseTone(HostileTone.Dark);
            Cog(inner, pair * 7 + inner / 2, -turn, -.3f, 1.0f, .26f, .2f);
            Fire<RiceRound>(transform.position, CombatVisuals.Aim(transform.position), 4.2f, .19f);
            yield return Wait(Rest(1.1f));
        }
        yield return Wait(.6f);
    }

    // A widening ring with six raised teeth (or six notches, for a negative tooth) and
    // one four-bead opening. It turns at 26 degrees a second and its rim never travels
    // faster than 2.8 units a second as it widens.
    void Cog(int positions, int gap, int sign, float tooth, float expand, float crownRadius, float rootRadius)
    {
        for (int i = 0; i < positions; i++)
        {
            if ((i - gap % positions + positions) % positions < 4)
                continue;
            bool crown = i * 12 / positions % 2 == 0;
            var bead = Fire<SimpleRound>(transform.position, Vector2.down, 0, crown ? crownRadius : rootRadius);
            if (bead != null)
                bead.Orbit(Radius * .9f + (crown ? tooth : 0), i * 360f / positions, sign * 26, 10, 0, 0, 0, .05f)
                    .Expand(expand, 2.8f);
        }
    }

    IEnumerator SpiralStair()
    {
        spinner.spin = 720;
        yield return Centre(Stations[nextStation++ % Stations.Length]);
        yield return Charge(Hull, Vector3.zero, .5f);
        int slow = Dense(28), fast = Dense(16);
        float turn = nextStation % 2 == 0 ? 16 : -16;
        float opening = Vector2.SignedAngle(Vector2.down, CombatVisuals.Aim(transform.position));
        for (int step = 0; step < 5; step++)
        {
            UseTone(HostileTone.Bright);
            OpenRing(fast, 2, opening, 2.1f, .17f);
            UseTone(HostileTone.Dark);
            OpenRing(slow, 4, opening, 1.05f, .27f);
            opening += turn;
            yield return Wait(Rest(.75f));
        }
        yield return Wait(.6f);
    }

    // A ring that only widens, with its opening centred on a heading.
    void OpenRing(int count, int missing, float opening, float expand, float radius)
    {
        float step = 360f / count;
        for (int i = 0; i < count - missing; i++)
        {
            var bead = Fire<SimpleRound>(transform.position, Vector2.down, 0, radius);
            if (bead != null)
                bead.Orbit(Radius * .9f, opening + (i + missing * .5f + .5f) * step, 0, 10, 0, 0, 0, .05f)
                    .Expand(expand);
        }
    }

    // ---- Phase two and three ----

    // The saw's own ricochet, driven here so each impact releases the proposed card's
    // burst: the same speed, walls, impact warning and contact damage as the shipping
    // patrol, which stands down (haltUntil) meanwhile. Silent between impacts.
    IEnumerator Patrol(float seconds, System.Action<Vector2> burst, float pace = 1)
    {
        if (!patrolling)
        {
            patrolling = true;
            velocity = new Vector2(impacts % 2 == 0 ? 1 : -1, -.75f).normalized;
        }
        haltUntil = float.MaxValue;
        spinner.spin = 720;
        for (float t = 0; t < seconds;)
        {
            if (!Holding)
            {
                float step = Time.deltaTime * LevelRun.EnemyTempoScale;
                t += step;
                Glide(step * MovementScale, pace, burst);
            }
            yield return null;
        }
    }

    void Glide(float dt, float pace, System.Action<Vector2> burst)
    {
        float speed = (2.2f + (1 - HealthFraction) * 1.8f) * (phaseTwo ? 1.12f : 1) * pace;
        float r = Radius;
        float etaX = (velocity.x > 0 ? arena.xMax - r - transform.position.x : transform.position.x - arena.xMin - r) / Mathf.Max(.01f, Mathf.Abs(velocity.x) * speed);
        float etaY = (velocity.y > 0 ? arena.yMax - r - transform.position.y : transform.position.y - arena.yMin - r) / Mathf.Max(.01f, Mathf.Abs(velocity.y) * speed);
        firingCue.Warn(Mathf.Min(etaX, etaY));
        var p = transform.position + (Vector3)velocity * speed * dt;
        Vector2 inward = Vector2.zero;
        if ((p.x < arena.xMin + r && velocity.x < 0) || (p.x > arena.xMax - r && velocity.x > 0))
        {
            velocity.x = -velocity.x;
            inward.x = Mathf.Sign(velocity.x);
        }
        if ((p.y < arena.yMin + r && velocity.y < 0) || (p.y > arena.yMax - r && velocity.y > 0))
        {
            velocity.y = -velocity.y;
            inward.y = Mathf.Sign(velocity.y);
        }
        p.x = Mathf.Clamp(p.x, arena.xMin + r, arena.xMax - r);
        p.y = Mathf.Clamp(p.y, arena.yMin + r, arena.yMax - r);
        transform.position = p;
        if (inward.sqrMagnitude > 0)
        {
            using var sample = burstMarker.Auto();
            impacts++;
            burst(inward.normalized);
        }
        if (player != null && Vector2.Distance(player.position, p) < r)
            player.SendMessage("ReceiveHazardDamage", 14f, SendMessageOptions.DontRequireReceiver);
    }

    // A full turn of hooked blades. Each blade is three rice grains launched together at
    // rising speeds, each lagging further behind the spin, so the burst opens as a
    // turning saw; slow round teeth sit between the blades.
    void BuzzsawBloom(Vector2 inward)
    {
        var centre = transform.position;
        int blades = Dense(12);
        int spin = impacts % 2 == 0 ? 1 : -1;
        float step = 360f / blades;
        for (int b = 0; b < blades; b++)
        {
            float root = b * step + impacts * 9;
            UseTone(HostileTone.Bright);
            for (int k = 0; k < 3; k++)
            {
                var grain = Fire<RiceRound>(centre, CombatVisuals.Rotate(inward, root - spin * k * 8), 1.8f + 1.3f * k, .19f);
                if (k == 2 && b % 3 == 0)
                    grain.Bounce(1);
            }
            UseTone(HostileTone.Dark);
            Fire<SimpleRound>(centre, CombatVisuals.Rotate(inward, root + step * .5f), 1.2f, .3f);
        }
        AimedTrio(centre, HostileTone.Bright);
    }

    // A fountain of rice sparks thrown into the arena that arc back down under gravity,
    // fast and slow alternately; a full turn of slow teeth; runners grinding both ways
    // along the struck wall.
    void SparkFountain(Vector2 inward)
    {
        var centre = transform.position;
        int sparks = Dense(24);
        UseTone(HostileTone.Dark);
        for (int i = 0; i < sparks; i++)
        {
            float lane = i - (sparks - 1) * .5f;
            Fire<RiceRound>(centre, CombatVisuals.Rotate(inward, lane * 150f / Mathf.Max(1, sparks - 1)), i % 2 == 0 ? 4.4f : 3.0f, .19f)
                .Fall(Vector2.down * 1.6f);
        }
        int teeth = Dense(20);
        UseTone(HostileTone.Bright);
        for (int i = 0; i < teeth; i++)
        {
            var tooth = Fire<SimpleRound>(centre, CombatVisuals.Rotate(inward, (i + .5f) * 360f / teeth + impacts * 7), 1.4f, .3f);
            if (i % 2 == 0)
                tooth.Bounce(1);
        }
        for (int direction = -1; direction <= 1; direction += 2)
            for (int k = 0; k < 2; k++)
                Fire<SimpleRound>(centre, -inward, 4.0f + k * 1.2f, .22f)
                    .RunWalls(5, direction);
    }

    // Hard: impacts alternate blades and fountains; each adds a crown of slow shrapnel
    // teeth that crack into small fast fans as they widen.
    void ChainReaction(Vector2 inward)
    {
        if (impacts % 2 == 0)
            BuzzsawBloom(inward);
        else
            SparkFountain(inward);
        int shrapnel = Dense(8);
        UseTone(HostileTone.Dark);
        for (int i = 0; i < shrapnel; i++)
            Fire<SimpleRound>(transform.position, CombatVisuals.Rotate(inward, (i + .5f) * 360f / shrapnel), 1.0f, .34f)
                .SplitAt(1.6f, 3, 22, 3.4f, .16f);
    }

    void AimedTrio(Vector3 centre, HostileTone shade)
    {
        UseTone(shade);
        var aim = CombatVisuals.Aim(centre);
        for (int i = -1; i <= 1; i++)
            Fire<RiceRound>(centre, CombatVisuals.Rotate(aim, i * 10), 4.6f, .19f);
    }
}
