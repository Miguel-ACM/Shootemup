using System.Collections;
using UnityEngine;

// Threshing Drum phases one and three.
// Phase one, Winnowing: from a new station for every toss, the drums throw grain and chaff
//   high in a mirrored dome that brakes and hangs over the upper arena; on one shared beat
//   heavy dark grain (arrows and kernels) falls fast and straight while light bright chaff
//   drifts slowly and sways, so the dome separates into two layers. The wind develops: still
//   air, gusts leaning the grain inward, gusts leaning it out to the walls, then doubled
//   tosses with a lower second dome. Nothing is aimed: the domes themselves are the threat.
// Phase three (Hard), Scything: both drums swing scythes, crescents of bright arrows whose
//   middle runs ahead of the tips, sweeping out, in and back so the blades cross under the
//   machine. The swing develops: mirrored strokes, strokes a half-sweep apart, backhand
//   strokes whose tips lead, then the machine walking the field from side to side. A dark
//   gleaning cup follows every other stroke.
public sealed partial class PlainsThreshingDrumBoss
{
    // Winnowing: eight tosses. Gust is the lean of the falling grain, outward positive:
    // still for two tosses, inward for two, outward for two; the last two are doubled.
    IEnumerator Winnowing()
    {
        const int Volleys = 7;
        const float Throw = .12f;
        // The last volley must still brake (.7 s) and glint (.1 s) before the shared beat.
        const float Release = .8f + (Volleys - 1) * Throw;
        for (int toss = 0; toss < 8; toss++)
        {
            station.x = Lane(.6f * Mathf.Sin(toss * 2.3f + .7f));
            while (((Vector2)(transform.position - station)).sqrMagnitude > .01f)
                yield return null;
            yield return PrimePair(.3f);
            float gust = toss < 2 ? 0 : toss < 4 ? -14 : toss < 6 ? 14 : 0;
            int domes = toss >= 6 ? 2 : 1;
            for (int dome = 0; dome < domes; dome++)
                for (int volley = 0; volley < Volleys; volley++)
                {
                    // Each dome lets go on one beat; the lower second dome is thrown after
                    // the first and lets go .9 s after it.
                    float thrown = dome * Volleys * Throw + volley * Throw;
                    Toss(volley, Volleys, Release + dome * .9f - thrown, gust, dome == 0 ? 1 : .72f, dome == 0);
                    yield return Wait(Throw);
                }
            yield return Wait(Rest(.6f));
        }
    }

    // One throw from both drums: lanes from 18 degrees inward to 72 outward of straight up,
    // the middle lanes thrown hardest so the hanging rounds form a dome. Every fourth lane of
    // alternate volleys of the first dome is bright chaff that drifts outward and sways (this
    // keeps the petals inside their warmed reserve); the rest is dark grain, arrows and
    // kernels in turn, that drops fast.
    void Toss(int volley, int volleys, float release, float gust, float lift, bool chaff)
    {
        int lanes = Mathf.Max(4, Dense(16));
        float hold = Mathf.Max(.1f, release - .7f);
        for (int drum = 1; drum <= 2; drum++)
        {
            int mirror = drum == 1 ? -1 : 1;
            for (int k = 0; k < lanes; k++)
            {
                float f = (k + volley / (float)volleys) / lanes;
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
}
