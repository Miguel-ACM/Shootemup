using System.Collections;
using UnityEngine;

// Desert worm: it winds a fixed circuit of dives, rises and side sweeps, turning just
// outside the border, attacking all the way. Everything comes off its body.
// One concept per phase (Miguel, 10 November 2026). Each concept develops verse by verse:
// variations of the same idea, different reactors taking it up and rests of its own.
// Desert grammar throughout: fire spreads out from the body, mines stop then lunge or
// carry on, heat haze quickens, nothing fades and very little falls. Anything that
// winds does so as a sandworm: a chain of rounds leaving one burrow along one heading,
// one after another, so the whole chain follows a single predictable path.
// Phase one, Mine Caravan: the worm sows its own track with mines while sandworms of
//   haze crawl from the body, and the track wakes. Four verses, then again with the
//   turns reversed and lunges more frequent:
//   1. Shared beat: head and tail sow; after a charge at the head every mine wakes at
//      once, carrying on outward, one in five lunging.
//   2. Caravan: each mine wakes soon after it is sown and turns to march along the
//      track, so the trail behind the worm becomes a moving caravan.
//   3. Three sowers: the middle of the body joins; a shared beat again, quickening.
//   4. Undertow: the newest mines wake first, the wake running back down the trail.
// Phase two, Simoom: a sandstorm blowing in a world direction, the wind swinging to the
//   other side after a charge every verse. A gust runs along the body: reactor after
//   reactor releases sandworms downwind that wind like dune lines and quicken; mines in
//   the lee stop, then carry on with the wind or lunge.
//   1. Gust: one wave, tail to head.  2. Crossing gusts: a second wave head to tail.
//   3. Squall: three worms per reactor and more lee mines.  4. Crosswind: each reactor
//   sends one worm each way.
// Phase three (Hard), Lotus Mirage: lotuses bloom and pass along the body.
//   1. Head lotus: charged mirrored blooms around the head's line that hang and open.
//   2. Flank buds: alternate reactors charge and open half-lotuses on both flanks.
//   3. Chain bloom: buds open reactor by reactor, tail to head, ending in a head lotus.
//   4. Closing lotus: head blooms whose petals swing inward and cross before spreading.
// Tones: Mine Caravan dark mines with bright lungers and bright haze worms; Simoom
//   bright gust worms with dark lee mines; Lotus Mirage bright arrow petals over dark
//   circles and dark haze worms.
public sealed partial class DesertWormBoss
{
    // Sequences run tail to head: step 0 is the tail reactor, later steps climb the
    // body. A reactor that cannot fire hands the step to the next one toward the head.
    int TailToHead(int step, bool head = true)
    {
        int span = head ? cores.Length : cores.Length - 1;
        int wanted = cores.Length - 1 - step % span;
        for (int n = 0; n < cores.Length; n++)
        {
            int i = ((wanted - n) % cores.Length + cores.Length) % cores.Length;
            if (CanShoot(cores[i]))
                return i;
        }
        return -1;
    }

    protected override void Attack()
    {
        OpenCards();
    }

    protected override IEnumerator Cards()
    {
        int verse = 0;
        while (true)
        {
            if (phaseThree)
                yield return LotusMirage(verse++);
            else if (!phaseTwo)
                yield return MineCaravan(verse++);
            else
                yield return Simoom(verse++);
        }
    }

    // The head's hull points down the worm's own path.
    Vector2 HeadForward => -(Vector2)Hull.up;

    // A charge at several reactors at once.
    IEnumerator ChargeReactors(int first, int step, float seconds)
    {
        for (float t = 0; t < seconds;)
        {
            if (!Holding)
                t += Time.deltaTime * LevelRun.EnemyTempoScale;
            for (int i = first; i < cores.Length; i += step)
                if (CanShoot(cores[i]))
                    Cue(cores[i].position, seconds - t);
            yield return null;
        }
    }

    // A sandworm: a rice head and a tapering body of circles leave one burrow along one
    // heading, one after another, with the same sway and the same quickening, so every
    // segment follows the head along a single winding path.
    IEnumerator Sandworm(Vector3 burrow, Vector2 heading, int segments, float speed, float sway, float cycles, float quicken, HostileTone shade)
    {
        for (int s = 0; s < segments; s++)
        {
            UseTone(shade);
            CombatRound segment = s == 0
                ? (CombatRound)Fire<RiceRound>(burrow, heading, speed, .19f)
                : Fire<SimpleRound>(burrow, heading, speed, Mathf.Lerp(.17f, .13f, s / (float)segments));
            segment.Sway(sway, cycles).Ramp(1.7f, .9f, quicken);
            yield return Wait(.09f);
        }
    }

    // ---------------------------------------------------------------- Mine Caravan

    IEnumerator MineCaravan(int verse)
    {
        const int steps = 15;
        const float charge = .5f;
        int movement = verse % 4;
        int pass = verse / 4;
        bool sharedBeat = movement == 0 || movement == 2;
        float interval = Rest(.15f);
        float tempo = Mathf.Max(.01f, LevelRun.EnemyTempoScale);
        int lungeEvery = pass % 2 == 0 ? 5 : 4;
        // The caravan marches back along the trail, or forward on later passes.
        int march = pass % 2 == 0 ? 1 : -1;
        if (!sharedBeat)
            yield return Charge(cores[0], Vector3.zero, charge);
        for (int step = 0; step < steps; step++)
        {
            // Real seconds until this mine wakes.
            float release;
            if (sharedBeat)
                release = ((steps - step) * interval + charge) / tempo;
            else if (movement == 1)
                release = 1.3f / tempo;
            else
                release = ((steps - step) * interval + (steps - 1 - step) * interval * .6f + .3f) / tempo;
            var carry = movement == 1 ? CombatRound.Turn.Rotate : CombatRound.Turn.Keep;
            SowMine(0, step, release, carry, march * 90, lungeEvery);
            int tail = TailToHead(0, false);
            if (tail > 0)
                SowMine(tail, step + 1, release, carry, march * 90, lungeEvery);
            if (movement == 2)
                SowMine(MiddleReactor(), step + 2, release, carry, 0, lungeEvery);
            if (step % 4 == 0)
                HazeWorms(step / 4, HostileTone.Bright);
            yield return Wait(interval);
        }
        if (sharedBeat)
            yield return Charge(cores[0], Vector3.zero, charge);
        yield return Wait(Rest(.7f));
    }

    int MiddleReactor()
    {
        for (int n = 0; n < 3; n++)
            foreach (int i in new[] { 4 - n, 4 + n })
                if (i > 0 && i < cores.Length && CanShoot(cores[i]))
                    return i;
        return -1;
    }

    // One mine to each flank, a near one and a far one swapping sides every step, so the
    // two rows on either side of the track stagger like a zipper. A turning mine swings
    // by the same angle on both flanks, so both rows march the same way along the track.
    void SowMine(int core, int step, float release, CombatRound.Turn carry, float turn, int lungeEvery)
    {
        if (core < 0 || !CanShoot(cores[core]))
            return;
        var t = cores[core];
        for (int side = -1; side <= 1; side += 2)
        {
            bool far = (step + (side > 0 ? 1 : 0)) % 2 == 0;
            bool lunges = (step * 2 + (side > 0 ? 1 : 0)) % lungeEvery == 0;
            float speed = far ? 3.6f : 2.2f;
            UseTone(lunges ? HostileTone.Bright : HostileTone.Dark);
            Fire<SimpleRound>(t.position, (Vector2)t.right * side, speed, .22f)
                .Pause(.5f, Mathf.Max(.1f, release - .5f), lunges ? CombatRound.Turn.Aim : carry, lunges ? 0 : side * turn, (lunges ? 2.7f : 2.1f) / speed);
        }
    }

    // Heat haze: alternate body reactors send a slow sandworm out of each flank that
    // winds, then quickens.
    void HazeWorms(int parity, HostileTone shade)
    {
        for (int i = 2 + parity % 2; i < cores.Length - 1; i += 2)
        {
            if (!CanShoot(cores[i]))
                continue;
            var t = cores[i];
            for (int side = -1; side <= 1; side += 2)
                Phrase(Sandworm(t.position, (Vector2)t.right * side, 4, 1.3f, side * .3f, .6f, 1.4f, shade));
        }
    }

    // ---------------------------------------------------------------- Simoom

    IEnumerator Simoom(int verse)
    {
        int movement = verse % 4;
        // The wind blows away from the side the head starts on, then swings every verse.
        int wind = (transform.position.x > 0 ? -1 : 1) * (verse % 2 == 0 ? 1 : -1);
        yield return Charge(cores[0], Vector3.zero, .45f);
        yield return Every(3.4f, .3f, n =>
        {
            int up = cores.Length - 1 - n % cores.Length;
            Gust(up, wind, movement);
            int down = n % cores.Length;
            if (movement == 1 && down != up)
                Gust(down, wind, movement);
            if (n % (movement == 2 ? 2 : 3) == 1)
                LeeMines(TailToHead(n / 2, false), wind, n / 2);
        });
        yield return Wait(Rest(.6f));
    }

    // Sandworms downwind that wind like dune lines and quicken: two from a reactor, three
    // in a squall, or one each way in a crosswind.
    void Gust(int core, int wind, int movement)
    {
        if (core < 0 || !CanShoot(cores[core]))
            return;
        Vector3 p = cores[core].position;
        if (movement == 3)
        {
            for (int way = -1; way <= 1; way += 2)
                Phrase(Sandworm(p, CombatVisuals.Rotate(Vector2.right * wind * way, -wind * way * 14), 4, 2.1f, wind * way * .3f, .5f, .9f, HostileTone.Bright));
            return;
        }
        Vector2 downwind = CombatVisuals.Rotate(Vector2.right * wind, -wind * 14);
        int lanes = movement == 2 ? 3 : 2;
        for (int lane = 0; lane < lanes; lane++)
            Phrase(Sandworm(p, CombatVisuals.Rotate(downwind, (lane - (lanes - 1) * .5f) * (lanes == 3 ? 10 : 14)), 4, 2.1f, wind * .3f, .5f, .9f, HostileTone.Bright));
    }

    // Mines dropped in the lee: they stop, then carry on with the wind; one in three lunges.
    void LeeMines(int core, int wind, int n)
    {
        if (core <= 0 || !CanShoot(cores[core]))
            return;
        var t = cores[core];
        for (int side = -1; side <= 1; side += 2)
        {
            bool lunges = (n + (side > 0 ? 1 : 0)) % 3 == 0;
            UseTone(lunges ? HostileTone.Bright : HostileTone.Dark);
            Fire<SimpleRound>(t.position, (Vector2)t.right * side, 2.4f, .22f)
                .Pause(.5f, .7f, lunges ? CombatRound.Turn.Aim : CombatRound.Turn.Down, lunges ? 0 : wind * 76, 1.05f);
        }
    }

    // ---------------------------------------------------------------- Lotus Mirage

    IEnumerator LotusMirage(int verse)
    {
        int movement = verse % 4;
        if (movement == 0 || movement == 3)
        {
            // Head lotus, or the closing lotus whose petals swing inward first.
            for (int k = 0; k < 3; k++)
            {
                yield return Charge(cores[0], Vector3.zero, .35f);
                Bloom(movement == 3 ? -38 : 38);
                if (k == 1)
                    HazeWorms(k, HostileTone.Dark);
                yield return Wait(Rest(.65f));
            }
        }
        else if (movement == 1)
        {
            // Flank buds on alternate reactors.
            for (int k = 0; k < 3; k++)
            {
                int first = 1 + k % 2;
                yield return ChargeReactors(first, 2, .4f);
                for (int i = first; i < cores.Length; i += 2)
                    Bud(i);
                yield return Wait(Rest(.6f));
            }
        }
        else
        {
            // Chain bloom: buds open reactor by reactor up the body, then the head blooms.
            for (int i = cores.Length - 1; i > 0; i--)
            {
                yield return Charge(cores[i], Vector3.zero, .25f);
                Bud(i);
            }
            yield return Charge(cores[0], Vector3.zero, .35f);
            Bloom(38);
        }
        yield return Wait(Rest(.8f));
    }

    // Mirrored petals across 300 degrees around the head's line; after the hang each half
    // swings away from (or across) that line and quickens. Each petal is a tip of three
    // arrows over two circles. The back of the bloom is left to the body.
    void Bloom(float swing)
    {
        if (!CanShoot(cores[0]))
            return;
        Vector3 p = cores[0].position;
        Vector2 forward = HeadForward;
        int petals = Dense(10);
        if (petals % 2 == 1)
            petals++;
        float step = 300f / petals;
        for (int k = 0; k < petals; k++)
        {
            float angle = (k - (petals - 1) * .5f) * step;
            int side = angle < 0 ? -1 : 1;
            UseTone(HostileTone.Bright);
            for (int lane = -1; lane <= 1; lane++)
                Fire<NeedleRound>(p, CombatVisuals.Rotate(forward, angle + lane * 5), lane == 0 ? 3.1f : 2.7f, .19f)
                    .Pause(.55f, .45f, CombatRound.Turn.Rotate, side * swing, 1)
                    .Ramp(1.6f, .9f, 1.1f);
            UseTone(HostileTone.Dark);
            for (int lane = -1; lane <= 1; lane += 2)
                Fire<SimpleRound>(p, CombatVisuals.Rotate(forward, angle + lane * 3), 2.0f, .2f)
                    .Pause(.55f, .6f, CombatRound.Turn.Rotate, side * swing, 1)
                    .Ramp(1.6f, .9f, 1.25f);
        }
    }

    // A half-lotus on each flank of one reactor: three petals that hang, then open.
    void Bud(int core)
    {
        if (core < 0 || !CanShoot(cores[core]))
            return;
        var t = cores[core];
        int petals = Dense(3);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 flank = (Vector2)t.right * side;
            for (int k = 0; k < petals; k++)
            {
                float angle = (k - (petals - 1) * .5f) * 24;
                float open = Mathf.Abs(angle) < 1 ? 0 : Mathf.Sign(angle) * 30;
                UseTone(HostileTone.Bright);
                Fire<NeedleRound>(t.position, CombatVisuals.Rotate(flank, angle), 2.9f, .19f)
                    .Pause(.5f, .4f, CombatRound.Turn.Rotate, open, 1)
                    .Ramp(1.6f, .9f, 1.0f);
                UseTone(HostileTone.Dark);
                for (int lane = -1; lane <= 1; lane += 2)
                    Fire<SimpleRound>(t.position, CombatVisuals.Rotate(flank, angle + lane * 4), 2.0f, .2f)
                        .Pause(.5f, .55f, CombatRound.Turn.Rotate, open, 1)
                        .Ramp(1.6f, .9f, 1.15f);
            }
        }
    }
}
