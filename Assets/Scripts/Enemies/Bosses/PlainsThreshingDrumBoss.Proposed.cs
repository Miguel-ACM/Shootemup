using System.Collections;
using UnityEngine;

// Proposed card book for the Threshing Drum (ArtSource/BossPhaseProposals). It flies only
// while DistrictBoss.ProposedCards is set, so it can be recorded beside Cards().
// Phase one
//   Reel: the two drums turn like a harvester's reel. Each beat a drum throws one bat, a
//     flat bar of bright arrows square to its heading that lengthens as it flies; the bats
//     sweep from over the top, out and down through the field, mirrored, while the machine
//     drifts. Dark grain from the chute answers the pilot every few beats.
//   Winnowing: the drums toss grain and chaff high in a mirrored dome that brakes and hangs
//     across the upper arena, then the wind takes all of it on one beat: heavy dark grain
//     falls fast and straight, light bright chaff drifts down slowly and sways, so one dome
//     separates into two layers. The machine moves to a new station for every toss.
// Phase two
//   Scything: both drums swing scythes. Each stroke is a crescent of bright arrows whose
//     middle runs ahead of its tips, so the blade bows forward and widens; the strokes sweep
//     from the outside inward and back, crossing under the machine. The chute lobs a dark
//     gleaning cup at the pilot whose rims run ahead of its middle.
//   Bale Line: the machine traverses edge to edge and sets dark bales down at a fixed spacing,
//     every bale braking at its own depth in a zigzag row. When the traverse ends, a fuse
//     burns along the row from the first bale toward the machine, each bale bursting into a
//     flat downward fan; bright twine flicked from the drums drops along both sides.
// Phase three (Hard)
//   Last Sheaf: the scythes keep stroking while the drums toss winnowing domes between them.
// Tones: bright reel, dark winnowing, bright scythes, dark bales; accents in the other tone.
public sealed partial class PlainsThreshingDrumBoss
{
    protected override IEnumerator Proposed()
    {
        station.y = Camera.UpLimit - 2.25f;
        while (true)
        {
            if (phaseThree)
                yield return LastSheaf();
            else if (!phaseTwo)
            {
                yield return Reel();
                yield return Wait(Rest(.3f));
                yield return Winnowing();
                yield return Wait(Rest(.3f));
            }
            else
            {
                yield return Scything();
                yield return Wait(Rest(.3f));
                yield return BaleLine();
                yield return Wait(Rest(.3f));
            }
        }
    }

    // Reel: eight bats per sweep and one silent beat while the reel turns over the top;
    // alternate sweeps shift half a step, so the bars do not stack into fixed lanes.
    IEnumerator Reel()
    {
        yield return PrimePair(.45f);
        float drift = transform.position.x > 0 ? Mathf.PI * .5f : -Mathf.PI * .5f;
        int arrows = Mathf.Max(3, Dense(9));
        yield return Every(6.4f, .16f, n =>
        {
            station.x = Lane(.55f * Mathf.Sin(n * .16f * .55f + drift));
            int sweep = n % 9;
            if (n % 3 == 0)
                Grain();
            if (sweep == 8)
                return;
            float angle = 130 - sweep * 20 + n / 9 % 2 * 10;
            UseTone(HostileTone.Bright);
            for (int drum = 1; drum <= 2; drum++)
                ReelBat(Muzzle(drum), (drum == 1 ? -1 : 1) * angle, arrows);
        });
    }

    // One reel bat: a flat bar of arrows square to its heading. The side arrows fly faster
    // in proportion, so the bar stays straight as it lengthens.
    void ReelBat(Vector3 port, float angle, int arrows)
    {
        var heading = CombatVisuals.Rotate(Vector2.down, angle);
        for (int k = 0; k < arrows; k++)
        {
            float offset = (k - (arrows - 1) * .5f) * 8;
            Fire<NeedleRound>(port, CombatVisuals.Rotate(heading, offset), 2.6f / Mathf.Cos(offset * Mathf.Deg2Rad), .18f);
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

    // Winnowing: four tosses from irregular stations.
    IEnumerator Winnowing()
    {
        for (int toss = 0; toss < 4; toss++)
            yield return WinnowToss(toss);
    }

    // Move to the toss's station, charge both drums, throw six interleaved volleys that
    // all let go on one shared beat, and trickle grain at the pilot while the dome hangs.
    IEnumerator WinnowToss(int toss)
    {
        station.x = Lane(.6f * Mathf.Sin(toss * 2.3f + .7f));
        while (((Vector2)(transform.position - station)).sqrMagnitude > .01f)
            yield return null;
        yield return PrimePair(.3f);
        for (int volley = 0; volley < 6; volley++)
        {
            Toss(volley, 1.3f - volley * .12f);
            yield return Wait(.12f);
        }
        yield return Every(.6f, .2f, n => Grain());
    }

    // One throw from both drums: lanes from 18 degrees inward to 72 outward of straight up,
    // the middle lanes thrown hardest so the hanging rounds form a dome. Every fourth lane of
    // alternate volleys is bright chaff that drifts outward and sways (this keeps the petals
    // inside their warmed reserve); the rest is dark grain that drops fast.
    void Toss(int volley, float release)
    {
        int lanes = Mathf.Max(4, Dense(14));
        for (int drum = 1; drum <= 2; drum++)
        {
            int mirror = drum == 1 ? -1 : 1;
            for (int k = 0; k < lanes; k++)
            {
                float f = (k + volley / 6f) / lanes;
                float speed = 3.4f + 1.8f * Mathf.Sin(f * Mathf.PI);
                var heading = CombatVisuals.Rotate(Vector2.up, -mirror * (-18 + 90 * f));
                float hold = Mathf.Max(.1f, release - .7f);
                if (k % 4 == 1 && volley % 2 == 0)
                {
                    UseTone(HostileTone.Bright);
                    Fire<PetalRound>(Muzzle(drum), heading, speed, .26f)
                        .Pause(.7f, hold, CombatRound.Turn.Down, mirror * (8 + 10 * f), 2.0f / speed)
                        .Sway(.5f, .35f)
                        .LivesFor(12);
                }
                else
                {
                    UseTone(HostileTone.Dark);
                    Fire<SimpleRound>(Muzzle(drum), heading, speed, .22f)
                        .Pause(.7f, hold, CombatRound.Turn.Down, -mirror * 3, 3.1f / speed);
                }
            }
        }
    }

    // Scything: one charge, then the strokes with a slow drift.
    IEnumerator Scything()
    {
        yield return PrimePair(.45f);
        yield return ScytheStrokes(6.6f, .3f, Dense(13), 2, true);
    }

    // Strokes sweep 55, 30, 5, -20, 5, 30 degrees from straight down, outward positive, from
    // both drums at once; a gleaning cup follows every `cupEvery` strokes.
    IEnumerator ScytheStrokes(float seconds, float interval, int arrows, int cupEvery, bool drift)
    {
        arrows = Mathf.Max(5, arrows);
        return Every(seconds, interval, n =>
        {
            int step = n % 6;
            float sweep = 55 - 25 * (step <= 3 ? step : 6 - step);
            UseTone(HostileTone.Bright);
            for (int drum = 1; drum <= 2; drum++)
                Scythe(Muzzle(drum), (drum == 1 ? -1 : 1) * sweep, arrows);
            if (n % cupEvery == cupEvery / 2)
                Cup();
            if (drift)
                station.x = Lane(.35f * Mathf.Sin(n * interval * .6f));
        });
    }

    // One scythe stroke: a crescent of arrows across 55 degrees, its middle running ahead of
    // its tips, so the blade bows forward and widens as it flies.
    void Scythe(Vector3 port, float angle, int arrows)
    {
        var heading = CombatVisuals.Rotate(Vector2.down, angle);
        for (int k = 0; k < arrows; k++)
        {
            float f = k / (arrows - 1f) - .5f;
            Fire<NeedleRound>(port, CombatVisuals.Rotate(heading, f * 55), 2.4f + 1.1f * Mathf.Cos(f * Mathf.PI), .18f);
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

    // Bale Line: two traverses, there and back; the first row's fuse burns during the return.
    IEnumerator BaleLine()
    {
        int side = transform.position.x > 0 ? 1 : -1;
        station.x = Reach(side);
        while (Mathf.Abs(transform.position.x - station.x) > .08f)
            yield return null;
        for (int pass = 0; pass < 2; pass++)
        {
            yield return Charge(Hull, WeaponPort(0), .4f);
            side = -side;
            yield return BaleTraverse(side);
            yield return Every(.9f, .15f, n => BindingTwine(n));
        }
    }

    // Bales go down at a fixed spacing as the chute passes each mark: chute bales alternate
    // two depths, and every other mark one drum adds a deeper, smaller bale. The fuse
    // starts just after the machine stops and runs from the first bale toward it, a fifth
    // of a second per chute bale, with each drum bale halfway between: one fan at a time.
    IEnumerator BaleTraverse(int side)
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
            float burst = Mathf.Abs(station.x - transform.position.x) / rate + .35f + mark * .2f;
            Bale(Muzzle(), mark % 2 == 0 ? 2.6f : 3.4f, burst, Dense(16));
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

    // Last Sheaf: slower scythe strokes run under four winnowing tosses.
    IEnumerator LastSheaf()
    {
        yield return PrimePair(.45f);
        Phrase(ScytheStrokes(6.5f, .42f, Dense(8), 4, false));
        for (int toss = 0; toss < 4; toss++)
            yield return WinnowToss(toss);
        yield return Wait(Rest(.5f));
    }
}
