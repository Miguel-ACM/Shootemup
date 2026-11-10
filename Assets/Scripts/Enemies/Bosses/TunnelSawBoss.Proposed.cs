using System.Collections;
using UnityEngine;

// Proposed Tunnel saw card book (ArtSource/BossPhaseProposals), flown only while
// DistrictBoss.ProposedCards is set. One concept per phase; every round still leaves
// the centre of the blade.
// Phase one, Gearbox (stations, no bouncing): at each new station the saw throws widening
//   cogs, rings with six raised teeth and one opening, turning at most 26 degrees a
//   second. Station by station the gearbox shifts: a drive gear (single cogs turning in
//   alternate directions), a meshing pair (a bright cog with teeth out and a dark cog
//   with teeth in, counter-rotating), a ratchet (still cogs, fast and slow alternately,
//   whose opening steps round from the pilot like a spiral stair), and an idler pair (a
//   fast and a slow cog turning together). Later rounds raise the teeth and the fast
//   tracer becomes a trio.
// Phase two, Buzzsaw Bloom (ricochet; it fires only when it strikes a wall): every
//   impact opens a full turn of hooked blades, rice grains launched together at rising
//   speeds, each lagging further behind the spin, with slow round teeth between the
//   blades and two sparks grinding along the struck wall. Impacts cycle through a
//   ripsaw (every blade hooked with the spin), a crosscut (alternate blades hooked
//   against it) and a rebound (near-straight spokes whose tips ricochet); the hook
//   deepens as the saw wears down. A fast trio flies at the pilot from every impact.
// Hard phase three, Chain Reaction: a faster ricochet; every impact throws two crowns of
//   slow shrapnel teeth that crack into small fast fans: a forward cone, wide petals,
//   then teeth that rebound before cracking and pairs that crack toward the pilot. The
//   cracks come sooner as the saw wears down.
// Tones: phase one bright outer cogs over dark inner cogs and tracers; phase two bright
// blades over dark teeth and sparks; phase three dark large teeth, bright small ones.
public sealed partial class TunnelSawBoss
{
    bool patrolling;

    protected override IEnumerator Proposed()
    {
        spinner = GetComponent<CombatHullVisual>();
        while (true)
        {
            if (phaseThree)
                yield return Patrol(30, ChainReaction, 1.15f);
            else if (!phaseTwo)
                yield return Gearbox();
            else
                yield return Patrol(30, BuzzsawBloom);
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

    // ---- Phase one: Gearbox ----

    IEnumerator Gearbox()
    {
        for (int movement = 0; !phaseTwo; movement++)
        {
            spinner.spin = 720;
            yield return Centre(Stations[nextStation++ % Stations.Length]);
            yield return Charge(Hull, Vector3.zero, .5f);
            int turn = movement / 4 % 2 == 0 ? 1 : -1;
            float tooth = movement >= 4 ? .6f : .42f;
            int positions = Dense(36);
            float opening = Vector2.SignedAngle(Vector2.down, CombatVisuals.Aim(transform.position));
            switch (movement % 4)
            {
                case 0:
                    // Drive gear: single cogs turning in alternate directions, the opening sliding.
                    for (int ring = 0; ring < 6; ring++)
                    {
                        UseTone(ring % 2 == 0 ? HostileTone.Bright : HostileTone.Dark);
                        Cog(positions, opening + ring * 75, (ring % 2 == 0 ? 1 : -1) * turn * 26, tooth, 1.3f, .24f, .19f);
                        if (ring % 2 == 0)
                            Tracer(movement);
                        yield return Wait(Rest(.55f));
                    }
                    break;
                case 1:
                    // Meshing pair: teeth out and teeth in, counter-rotating, the inner cog slower.
                    for (int pair = 0; pair < 3; pair++)
                    {
                        UseTone(HostileTone.Bright);
                        Cog(positions, opening + pair * 100, turn * 26, tooth, 1.3f, .24f, .19f);
                        UseTone(HostileTone.Dark);
                        Cog(Dense(30), opening + 180 + pair * 60, -turn * 26, -tooth * .7f, 1.0f, .26f, .2f);
                        Tracer(movement);
                        yield return Wait(Rest(1.1f));
                    }
                    break;
                case 2:
                    // Ratchet: still cogs, fast and slow alternately, the opening stepping round.
                    for (int ring = 0; ring < 6; ring++)
                    {
                        bool fast = ring % 2 == 0;
                        UseTone(fast ? HostileTone.Bright : HostileTone.Dark);
                        Cog(positions, opening + ring * 16 * turn, 0, tooth, fast ? 2.0f : 1.05f, fast ? .2f : .27f, fast ? .17f : .22f);
                        yield return Wait(Rest(.55f));
                    }
                    Tracer(movement);
                    break;
                default:
                    // Idler pair: a fast and a slow cog turning together, the fast one first.
                    for (int pair = 0; pair < 3; pair++)
                    {
                        float at = opening + pair * 120;
                        UseTone(HostileTone.Bright);
                        Cog(positions, at, turn * 22, tooth, 2.0f, .2f, .17f);
                        UseTone(HostileTone.Dark);
                        Cog(positions, at, turn * 22, tooth, 1.05f, .27f, .22f);
                        Tracer(movement);
                        yield return Wait(Rest(1.1f));
                    }
                    break;
            }
            // The gearbox idles before it shifts to the next station.
            yield return Wait(.6f);
        }
    }

    // A widening ring with six raised teeth (or notches, for a negative tooth) and one
    // four-bead opening centred on a heading. Its rim never travels faster than 2.8 units
    // a second as it widens.
    void Cog(int positions, float opening, float rate, float tooth, float expand, float crownRadius, float rootRadius)
    {
        const int Missing = 4;
        float step = 360f / positions;
        for (int i = 0; i < positions - Missing; i++)
        {
            bool crown = i * 12 / positions % 2 == 0;
            var bead = Fire<SimpleRound>(transform.position, Vector2.down, 0, crown ? crownRadius : rootRadius);
            if (bead != null)
                bead.Orbit(Radius * .9f + (crown ? tooth : 0), opening + (i + Missing * .5f + .5f) * step, rate, 10, 0, 0, 0, .05f)
                    .Expand(expand, 2.8f);
        }
    }

    // A fast rice tracer at the pilot; from the second round of stations, a trio.
    void Tracer(int movement)
    {
        UseTone(HostileTone.Dark);
        var aim = CombatVisuals.Aim(transform.position);
        int lanes = movement >= 4 ? 1 : 0;
        for (int i = -lanes; i <= lanes; i++)
            Fire<RiceRound>(transform.position, CombatVisuals.Rotate(aim, i * 9), 4.2f, .19f);
    }

    // ---- Phase two and three: the ricochet ----

    // The saw's own ricochet, driven here so each impact releases the proposed burst:
    // the same speed, walls, impact warning and contact damage as the shipping patrol,
    // which stands down (haltUntil) meanwhile. Silent between impacts.
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

    // How far the saw has worn through the current phase, 0 at its start and 1 at its end.
    float Wear => Mathf.InverseLerp(PhaseEnd(CurrentPhase - 1), PhaseEnd(CurrentPhase), HealthFraction);

    // A full turn of hooked blades: four rice grains per blade launched together at rising
    // speeds, each lagging further behind the spin, with a slow round tooth between blades.
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
            // Ripsaw hooks every blade with the spin; crosscut alternates; rebound runs
            // nearly straight and its tips ricochet.
            float lag = kind == 0 ? spin * hook : kind == 1 ? (b % 2 == 0 ? spin : -spin) * hook : spin * hook * .3f;
            UseTone(HostileTone.Bright);
            for (int k = 0; k < 4; k++)
            {
                var grain = Fire<RiceRound>(centre, CombatVisuals.Rotate(inward, root - lag * k), 1.6f + 1.1f * k, .19f);
                if (k == 3 && (kind == 2 || b % 4 == 0))
                    grain.Bounce(1);
            }
            UseTone(HostileTone.Dark);
            Fire<SimpleRound>(centre, CombatVisuals.Rotate(inward, root + step * .5f), 1.2f, .3f);
        }
        // Two sparks grind away along the struck wall, one each way.
        UseTone(HostileTone.Dark);
        for (int direction = -1; direction <= 1; direction += 2)
            Fire<SimpleRound>(centre, -inward, 4.4f, .22f)
                .RunWalls(5, direction);
        AimedTrio(centre, HostileTone.Bright);
    }

    // Two crowns of shrapnel teeth that crack into small fast fans as they widen. The
    // large dark crown cracks forward, into wide petals, or after a rebound; the small
    // bright crown cracks into pairs, aimed at the pilot on every third impact.
    void ChainReaction(Vector2 inward)
    {
        var centre = transform.position;
        int kind = impacts % 3;
        float crack = Mathf.Lerp(1.7f, 1.1f, Wear);
        int large = Dense(12), small = Dense(12);
        UseTone(HostileTone.Dark);
        for (int i = 0; i < large; i++)
        {
            var tooth = Fire<SimpleRound>(centre, CombatVisuals.Rotate(inward, (i + .5f) * 360f / large + impacts * 7), 1.0f, .34f)
                .SplitAt(kind == 2 ? crack + .6f : crack, 3, kind == 1 ? 60 : 24, 3.4f, .16f);
            if (kind == 2)
                tooth.Bounce(1);
        }
        UseTone(HostileTone.Bright);
        for (int i = 0; i < small; i++)
            Fire<SimpleRound>(centre, CombatVisuals.Rotate(inward, i * 360f / small + impacts * 7), 1.7f, .24f)
                .SplitAt(crack * .6f, 2, kind == 2 ? 14 : 90, 3.0f, .15f, kind == 2);
        AimedTrio(centre, HostileTone.Bright);
    }

    void AimedTrio(Vector3 centre, HostileTone shade)
    {
        UseTone(shade);
        var aim = CombatVisuals.Aim(centre);
        for (int i = -1; i <= 1; i++)
            Fire<RiceRound>(centre, CombatVisuals.Rotate(aim, i * 10), 4.6f, .19f);
    }
}
