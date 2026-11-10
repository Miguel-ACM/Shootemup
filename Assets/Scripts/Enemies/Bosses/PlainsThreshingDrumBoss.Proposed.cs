using System.Collections;
using UnityEngine;

// Proposed card book for the Threshing Drum (ArtSource/BossPhaseProposals). It flies only
// while DistrictBoss.ProposedCards is set, so it can be recorded beside Cards(). One idea
// per phase, developing over the phase rather than alternating with another card.
// Phase one, Winnowing: from a new station for every toss, the drums throw grain and chaff
//   high in a mirrored dome that brakes and hangs over the upper arena; on one shared beat
//   heavy dark grain (arrows and kernels) falls fast and straight while light bright chaff
//   drifts slowly and sways, so the dome separates into two layers. The wind develops: still
//   air, gusts leaning the grain inward, gusts leaning it out to the walls, then doubled
//   tosses with a lower second dome. A dark grain trickle answers the pilot.
// Phase two, Scything: both drums swing scythes, crescents of bright arrows whose middle runs
//   ahead of the tips, sweeping out, in and back so the blades cross under the machine. The
//   swing develops: mirrored strokes, strokes a half-sweep apart, backhand strokes whose tips
//   lead, then the machine walking the field from side to side. A dark gleaning cup follows
//   every other stroke.
// Phase three (Hard), Bale Line: the machine traverses edge to edge and sets dark bales down at
//   a fixed spacing; when it stops, a fuse bursts them one fan at a time into flat downward
//   fans. The fuse changes every traverse (toward the machine, away from it, both ends in,
//   the centre out) and so does the row (zigzag, level, three tiers). Bright twine drops down
//   the sides and binds the pilot while the fuse burns.
public sealed partial class PlainsThreshingDrumBoss
{
    protected override IEnumerator Proposed()
    {
        station.y = Camera.UpLimit - 2.25f;
        while (true)
        {
            if (phaseThree)
                yield return BaleLine();
            else if (!phaseTwo)
                yield return Winnowing();
            else
                yield return Scything();
        }
    }

    // A dark handful of grain from the chute: a short column down the pilot's line.
    void Grain()
    {
        UseTone(HostileTone.Dark);
        var aim = CombatVisuals.Aim(Muzzle());
        int count = Mathf.Max(2, Dense(5));
        for (int k = 0; k < count; k++)
            Fire<SimpleRound>(Muzzle(), aim, 2.6f + k * .4f, .22f);
    }

    // Winnowing: eight tosses. Gust is the lean of the falling grain, outward positive:
    // still for two tosses, inward for two, outward for two; the last two are doubled.
    IEnumerator Winnowing()
    {
        for (int toss = 0; toss < 8; toss++)
        {
            station.x = Lane(.6f * Mathf.Sin(toss * 2.3f + .7f));
            while (((Vector2)(transform.position - station)).sqrMagnitude > .01f)
                yield return null;
            yield return PrimePair(.3f);
            float gust = toss < 2 ? 0 : toss < 4 ? -14 : toss < 6 ? 14 : 0;
            int domes = toss >= 6 ? 2 : 1;
            for (int dome = 0; dome < domes; dome++)
                for (int volley = 0; volley < 6; volley++)
                {
                    // Each dome lets go on one beat, 1.4 s after its first volley; the lower
                    // second dome is thrown .72 s later and lets go .9 s after the first.
                    Toss(volley, 1.4f + dome * (.9f - .72f) - volley * .12f, gust, dome == 0 ? 1 : .72f, dome == 0);
                    yield return Wait(.12f);
                }
            yield return Every(.6f, .2f, n => Grain());
        }
    }

    // One throw from both drums: lanes from 18 degrees inward to 72 outward of straight up,
    // the middle lanes thrown hardest so the hanging rounds form a dome. Every fourth lane of
    // alternate volleys of the first dome is bright chaff that drifts outward and sways (this
    // keeps the petals inside their warmed reserve); the rest is dark grain, arrows and
    // kernels in turn, that drops fast.
    void Toss(int volley, float release, float gust, float lift, bool chaff)
    {
        int lanes = Mathf.Max(4, Dense(14));
        float hold = Mathf.Max(.1f, release - .7f);
        for (int drum = 1; drum <= 2; drum++)
        {
            int mirror = drum == 1 ? -1 : 1;
            for (int k = 0; k < lanes; k++)
            {
                float f = (k + volley / 6f) / lanes;
                float speed = (3.4f + 1.8f * Mathf.Sin(f * Mathf.PI)) * lift;
                var heading = CombatVisuals.Rotate(Vector2.up, -mirror * (-18 + 90 * f));
                if (chaff && k % 4 == 1 && volley % 2 == 0)
                {
                    UseTone(HostileTone.Bright);
                    Fire<PetalRound>(Muzzle(drum), heading, speed, .26f)
                        .Pause(.7f, hold, CombatRound.Turn.Down, mirror * (8 + 10 * f + gust * .5f), 2.0f / speed)
                        .Sway(.5f, .35f)
                        .LivesFor(12);
                }
                else
                {
                    UseTone(HostileTone.Dark);
                    ScoredShot(k % 2 == 1, Muzzle(drum), heading, speed, .2f)
                        .Pause(.7f, hold, CombatRound.Turn.Down, mirror * (gust - 3), 3.1f / speed);
                }
            }
        }
    }

    // Scything: four swings of twelve strokes, each swing a variation.
    IEnumerator Scything()
    {
        for (int swing = 0; swing < 4; swing++)
        {
            yield return PrimePair(.45f);
            yield return ScytheStrokes(3.6f, .3f, Dense(14), swing);
        }
    }

    // Strokes sweep 55, 30, 5, -20, 5, 30 degrees from straight down, outward positive.
    // Swing 1 sets the right drum half a sweep behind the left; swing 2 strokes backhand;
    // swing 3 walks the machine to one side and then the other. A cup every other stroke.
    IEnumerator ScytheStrokes(float seconds, float interval, int arrows, int swing)
    {
        arrows = Mathf.Max(5, arrows);
        return Every(seconds, interval, n =>
        {
            UseTone(HostileTone.Bright);
            Scythe(Muzzle(1), -Sweep(n), arrows, swing == 2);
            Scythe(Muzzle(2), Sweep(n + (swing == 1 ? 3 : 0)), arrows, swing == 2);
            if (n % 2 == 1)
                Cup();
            station.x = swing == 3 ? Lane(n / 6 % 2 == 0 ? -.6f : .6f) : Lane(.35f * Mathf.Sin(n * interval * .6f));
        });
    }

    static float Sweep(int stroke)
    {
        int step = stroke % 6;
        return 55 - 25 * (step <= 3 ? step : 6 - step);
    }

    // One scythe stroke: a crescent of arrows across 55 degrees whose middle runs ahead of
    // its tips, so the blade bows forward and widens; backhand, the tips lead instead.
    void Scythe(Vector3 port, float angle, int arrows, bool backhand)
    {
        var heading = CombatVisuals.Rotate(Vector2.down, angle);
        for (int k = 0; k < arrows; k++)
        {
            float f = k / (arrows - 1f) - .5f;
            float bow = Mathf.Cos(f * Mathf.PI);
            Fire<NeedleRound>(port, CombatVisuals.Rotate(heading, f * 55), backhand ? 3.5f - 1.1f * bow : 2.4f + 1.1f * bow, .18f);
        }
    }

    // Gleaning cup: dark rounds aimed at the pilot, the rims faster than the middle, so the
    // cup closes round whoever waits for it.
    void Cup()
    {
        UseTone(HostileTone.Dark);
        var aim = CombatVisuals.Aim(Muzzle());
        int count = Mathf.Max(5, Dense(11));
        float half = (count - 1) * .5f;
        for (int k = 0; k < count; k++)
        {
            float lane = k - half;
            Fire<SimpleRound>(Muzzle(), CombatVisuals.Rotate(aim, lane * 7), 2.0f + .9f * Mathf.Abs(lane) / half, .23f);
        }
    }

    // Bale Line: eight traverses, each with its own fuse and row.
    IEnumerator BaleLine()
    {
        int side = transform.position.x > 0 ? 1 : -1;
        station.x = Reach(side);
        while (Mathf.Abs(transform.position.x - station.x) > .08f)
            yield return null;
        for (int pass = 0; pass < 8; pass++)
        {
            yield return Charge(Hull, WeaponPort(0), .4f);
            side = -side;
            yield return BaleTraverse(side, pass % 4);
            yield return Every(.9f, .15f, n => BindingTwine(n));
        }
    }

    // A bale's turn on the fuse (in fifths of a second), by traverse: from the first bale
    // toward the machine, from the machine back, both ends inward, the centre outward. The
    // paired fuses burn more slowly, so only the two far-apart ends ever burst together.
    static float FuseTime(int fuse, int mark, int marks)
    {
        int last = marks - 1;
        switch (fuse)
        {
            case 1:
                return (last - mark) * .2f;
            case 2:
                return Mathf.Min(mark, last - mark) * .3f;
            case 3:
                return (last / 2 - Mathf.Min(mark, last - mark)) * .3f;
            default:
                return mark * .2f;
        }
    }

    // Bales go down at a fixed spacing as the chute passes each mark, at depths set by the
    // traverse (a zigzag, a level row, three tiers); every other mark one drum adds a deeper,
    // smaller bale that bursts between its neighbours. The fuse starts just after the machine stops.
    IEnumerator BaleTraverse(int side, int fuse)
    {
        const float BaleStep = .75f;
        float start = transform.position.x;
        station.x = Reach(side);
        float rate = DrumSpeed * MovementScale * LevelRun.EnemyTempoScale;
        int marks = Mathf.Max(4, Mathf.CeilToInt(Mathf.Abs(station.x - start) / BaleStep) + 1);
        for (int mark = 0; mark < marks; mark++)
        {
            float x = Mathf.Lerp(start, station.x, mark / (marks - 1f));
            while (side * (x - transform.position.x) > .09f)
                yield return null;
            float burst = Mathf.Abs(station.x - transform.position.x) / rate + .35f + FuseTime(fuse, mark, marks);
            float depth = fuse % 2 == 0 ? (mark % 2 == 0 ? 2.6f : 3.4f) : fuse == 1 ? 3.0f : 2.4f + mark % 3 * .6f;
            Bale(Muzzle(), depth, burst, Dense(16));
            if (mark % 2 == 1)
                Bale(Muzzle(mark % 4 == 1 ? 1 : 2), 3.0f, burst + .1f, Dense(11));
            Twine(mark);
        }
    }

    // A dark bale dropped straight down: it brakes, glints until its turn on the fuse,
    // then bursts into a flat fan spread over 160 degrees below it.
    void Bale(Vector3 port, float speed, float burst, int shards)
    {
        UseTone(HostileTone.Dark);
        float hold = Mathf.Max(.1f, burst - .9f);
        Fire<SimpleRound>(port, Vector2.down, speed, .38f)
            .Pause(.9f, hold)
            .SplitAt(.9f + hold, shards, 160f / Mathf.Max(1, shards - 1), 2.5f, .17f);
    }

    // Bright twine flicked up and out from both drums; it brakes high and drops down the sides.
    void Twine(int mark)
    {
        UseTone(HostileTone.Bright);
        for (int drum = 1; drum <= 2; drum++)
        {
            int mirror = drum == 1 ? -1 : 1;
            Fire<NeedleRound>(Muzzle(drum), CombatVisuals.Rotate(Vector2.up, -mirror * (28 + mark % 3 * 22)), 4.2f, .18f)
                .Pause(.5f, .2f, CombatRound.Turn.Down, -mirror * 8, .75f);
        }
    }

    // While the fuse burns: bright aimed arrows from the drums in turn.
    void BindingTwine(int n)
    {
        UseTone(HostileTone.Bright);
        var port = Muzzle(1 + n % 2);
        var aim = CombatVisuals.Aim(port);
        int reach = Mathf.Max(1, Dense(5) / 2);
        for (int k = -reach; k <= reach; k++)
            Fire<NeedleRound>(port, CombatVisuals.Rotate(aim, k * 8), 4.1f - Mathf.Abs(k) * .25f, .18f);
    }
}
