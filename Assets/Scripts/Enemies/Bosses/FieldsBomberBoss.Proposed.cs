using System.Collections;
using UnityEngine;

// Proposed card book for the Plains bomber (ArtSource/BossPhaseProposals). It flies only
// while DistrictBoss.ProposedCards is set, so it can be recorded beside Cards().
// Phase one
//   Seed Drill: the six bays sow an arc of dark bombs. Every bomb flies the same distance,
//     so they settle on a smile under the bomber, then burst 360 one after another along
//     it like a string of firecrackers; the next drill lights the fuse from the other end.
//     Bright petals tossed from the wingtips brake into a canopy, hang and flutter down.
//   Wind Through Wheat: the lower guns wave two mirrored bright arrow streams like wheat
//     in a gust. Each stream swings as a pendulum and every arrow sways, so the ribbons
//     cross into drifting diamonds; slow dark seed rows fall at the pilot between them.
// Phase two
//   Skywriting: each wingtip writes two curling contrails of dark arrows that brake where
//     they are written and hang over the upper arena, then fall inward together as curved
//     sheets that cross under the bomber. Alternate arrows fall faster, so every sheet
//     parts in two. Bright tracers from the lower guns keep the pilot moving.
//   Harvest Fireworks: the wingtips loft bright shells over the top of the arena while the
//     bays drop a dark pair below. Every shell and bomb travels the same distance, stops
//     and bursts 360; the high bursts rain through the low rings a beat later.
// Phase three (Hard)
//   Last Harvest: the wheat streams wave while contrails are written over them and a seed
//     drill burns along its fuse in between.
// Tones: dark drill, bright wheat, dark contrails, bright fireworks; each card keeps the
// other tone for its accents.
public sealed partial class FieldsBomberBoss
{
    protected override IEnumerator Proposed()
    {
        while (true)
        {
            if (phaseThree)
                yield return LastHarvest();
            else if (!phaseTwo)
            {
                yield return SeedDrill();
                yield return Wait(1f);
                yield return WindThroughWheat();
                yield return Wait(1f);
            }
            else
            {
                yield return Skywriting();
                yield return Wait(1f);
                yield return HarvestFireworks();
                yield return Wait(1f);
            }
        }
    }

    // Muzzle indices on the bomber's hull: wingtips 0-1, lower guns 2-3, bays 4-9.
    IEnumerator PrimePorts(float seconds, int first, int last)
    {
        for (float t = 0; t < seconds; t += Holding ? 0 : Time.deltaTime * LevelRun.EnemyTempoScale)
        {
            for (int port = first; port <= last; port++)
                Cue(Muzzle(port), seconds - t);
            yield return null;
        }
    }

    // Seed Drill: three drills, the fuse running left to right, then back.
    IEnumerator SeedDrill()
    {
        for (int drill = 0; drill < 3; drill++)
        {
            yield return PrimePorts(.45f, 4, 9);
            SowArc(drill % 2 == 0 ? 1 : -1);
            TossPetals(drill);
            yield return Every(.9f, .15f, n => Talon(n));
            TossPetals(drill + 1);
            yield return Wait(Rest(.7f));
        }
    }

    // Six dark bombs fanned across 80 degrees at one speed and one braking time, so they
    // stop on an arc; each waits for its place on the fuse, then bursts in a full ring.
    void SowArc(int order)
    {
        UseTone(HostileTone.Dark);
        int shards = Dense(20);
        for (int i = 0; i < bays.Length; i++)
        {
            float f = i / (bays.Length - 1f);
            int fuse = order > 0 ? i : bays.Length - 1 - i;
            float hold = .3f + fuse * .2f;
            Fire<SimpleRound>(Hull.TransformPoint(bays[i]), CombatVisuals.Rotate(Vector2.down, Mathf.Lerp(-40, 40, f)), 2.6f, .42f)
                .Pause(.95f, hold)
                .SplitAt(.95f + hold, shards, 360f / shards, fuse % 2 == 0 ? 2.2f : 2.8f, .19f);
        }
    }

    // Petals thrown up and out from both wingtips, farther the flatter they fly. They brake
    // into a canopy above the wings, hang, then fall slightly inward, swaying.
    void TossPetals(int toss)
    {
        UseTone(HostileTone.Bright);
        int lanes = Mathf.Max(3, Dense(9));
        for (int side = 0; side < 2; side++)
        {
            int mirror = side == 0 ? -1 : 1;
            for (int k = 0; k < lanes; k++)
            {
                float f = k / (lanes - 1f);
                float speed = 2.8f + 2.2f * f;
                float fall = 1.9f + (k + toss) % 2 * .5f;
                Fire<PetalRound>(Muzzle(side), CombatVisuals.Rotate(Vector2.up, -mirror * (12 + 66 * f)), speed, .26f)
                    .Pause(.6f, .35f + f * .3f, CombatRound.Turn.Down, -mirror * (6 + 10 * f), fall / speed)
                    .Sway(.35f, .45f)
                    .LivesFor(12);
            }
        }
    }

    // Bright aimed arrows that bracket the pilot, alternating lower guns.
    void Talon(int n)
    {
        UseTone(HostileTone.Bright);
        var port = Muzzle(2 + n % 2);
        var aim = CombatVisuals.Aim(port);
        int reach = Mathf.Max(1, Dense(5) / 2);
        for (int k = -reach; k <= reach; k++)
            Fire<NeedleRound>(port, CombatVisuals.Rotate(aim, k * 9), 4.3f - Mathf.Abs(k) * .25f, .19f);
    }

    // Wind Through Wheat: one charge, then the gust.
    IEnumerator WindThroughWheat()
    {
        yield return PrimePorts(.45f, 2, 3);
        yield return WheatStreams(6.4f, Dense(3), true);
    }

    // Two mirrored bright streams of arrows from the lower guns. The pendulum carries them
    // from 24 degrees outward to 36 degrees inward, where they cross under the bomber;
    // each arrow sways on its way, so the ribbons wave.
    IEnumerator WheatStreams(float seconds, int lanes, bool seeds)
    {
        return Every(seconds, .09f, n =>
        {
            float swing = 30 * Mathf.Sin(n * .09f * 1.2f) - 6;
            UseTone(HostileTone.Bright);
            for (int side = 2; side <= 3; side++)
            {
                int mirror = side == 2 ? -1 : 1;
                for (int k = 0; k < lanes; k++)
                {
                    float lane = k - (lanes - 1) * .5f;
                    Fire<NeedleRound>(Muzzle(side), CombatVisuals.Rotate(Vector2.down, mirror * (swing + lane * 8)), 4.0f, .19f)
                        .Sway(.3f, .55f);
                }
            }
            if (seeds && n % 10 == 5)
                SeedRow(n / 10);
            if (n % 14 == 0)
                FlickCorners();
        });
    }

    // Two interleaved short rows of slow dark seeds from the inner bays, centred on the pilot.
    void SeedRow(int row)
    {
        UseTone(HostileTone.Dark);
        int seeds = Dense(5);
        for (int side = 0; side < 2; side++)
        {
            var port = Hull.TransformPoint(bays[side == 0 ? 2 : 3]);
            var aim = CombatVisuals.Aim(port);
            for (int i = 0; i < seeds; i++)
            {
                float lane = i - (seeds - 1) * .5f;
                Fire<SimpleRound>(port, CombatVisuals.Rotate(aim, lane * 7 + (row + side) % 2 * 3.5f), 1.9f + Mathf.Abs(lane) * .12f, .24f);
            }
        }
    }

    // Three bright arrows flicked up and out from each wingtip into the top corners, where
    // they brake and drop back down along the sides.
    void FlickCorners()
    {
        UseTone(HostileTone.Bright);
        for (int side = 0; side < 2; side++)
        {
            int mirror = side == 0 ? -1 : 1;
            for (int k = 0; k < 3; k++)
                Fire<NeedleRound>(Muzzle(side), CombatVisuals.Rotate(Vector2.up, -mirror * (40 + k * 18)), 4.4f + k * .8f, .19f)
                    .Pause(.55f, .25f + k * .1f, CombatRound.Turn.Down, -mirror * (8 + k * 6), .7f);
        }
    }

    // Skywriting: three passes, a short and a long contrail from each wingtip.
    IEnumerator Skywriting()
    {
        for (int pass = 0; pass < 3; pass++)
        {
            yield return PrimePorts(.4f, 0, 1);
            for (int side = 0; side < 2; side++)
            {
                Phrase(Contrail(side, 1f, 1.5f));
                Phrase(Contrail(side, 1.7f, 1.85f));
            }
            yield return Every(2.2f, .3f, n => Tracer(n));
            yield return Wait(Rest(.15f));
        }
    }

    // One contrail written in .8 s, curling from below the wingtip out and over it: later
    // arrows sweep further round and fly further before braking. Every arrow hangs until
    // the shared release, then falls inward; odd arrows fall faster than even ones.
    IEnumerator Contrail(int side, float reach, float release)
    {
        int mirror = side == 0 ? -1 : 1;
        int count = Mathf.Max(6, Dense(20));
        float step = .8f / count;
        for (int i = 0; i < count; i++)
        {
            float f = i / (count - 1f);
            float at = .3f + .45f * f * reach;
            UseTone(HostileTone.Dark);
            Fire<NeedleRound>(Muzzle(side), CombatVisuals.Rotate(Vector2.down, mirror * (20 + 150 * f)), 3.2f, .19f)
                .Pause(at, Mathf.Max(.1f, release - i * step - at), CombatRound.Turn.Down, -mirror * (10 + 26 * f), i % 2 == 0 ? .7f : 1.05f);
            yield return Wait(step);
        }
    }

    // A bright aimed tracer and its flanking arrow pairs, alternating lower guns.
    void Tracer(int n)
    {
        UseTone(HostileTone.Bright);
        var port = Muzzle(2 + n % 2);
        var aim = CombatVisuals.Aim(port);
        Fire<SimpleRound>(port, aim, 3.6f, .22f);
        int pairs = Mathf.Max(1, Dense(4) / 2);
        for (int pair = 1; pair <= pairs; pair++)
            for (int mirror = -1; mirror <= 1; mirror += 2)
                Fire<NeedleRound>(port, CombatVisuals.Rotate(aim, mirror * 8 * pair), 3.9f - (pair - 1) * .3f, .19f);
    }

    // Harvest Fireworks: six salvos, each a high loft and a low drop.
    IEnumerator HarvestFireworks()
    {
        for (int salvo = 0; salvo < 6; salvo++)
        {
            yield return PrimePorts(.3f, 0, 1);
            Loft(salvo);
            yield return Wait(Rest(.35f));
            DropPair(salvo);
            yield return Wait(Rest(.55f));
        }
    }

    // Three bright shells from each wingtip, thrown up and out at one speed and braking at
    // one time, so they all burst 360 the same distance away, over the top of the arena.
    // Alternate salvos lean further out, moving the bursts along the skyline.
    void Loft(int salvo)
    {
        UseTone(HostileTone.Bright);
        int shards = Dense(16);
        for (int side = 0; side < 2; side++)
        {
            int mirror = side == 0 ? -1 : 1;
            for (int j = 0; j < 3; j++)
                Fire<SimpleRound>(Muzzle(side), CombatVisuals.Rotate(Vector2.up, -mirror * (14 + 24 * j + salvo % 2 * 12)), 2.6f, .4f)
                    .Pause(.85f, .3f)
                    .SplitAt(1.15f, shards, 360f / shards, 2.3f, .19f);
        }
    }

    // A mirrored pair of dark bombs from the bays, working outer, middle, inner, at the
    // same speed and braking time: the low rings open under the falling high ones.
    void DropPair(int salvo)
    {
        UseTone(HostileTone.Dark);
        int shards = Dense(16);
        int bay = salvo % 3;
        for (int mirror = -1; mirror <= 1; mirror += 2)
            Fire<SimpleRound>(Hull.TransformPoint(bays[mirror < 0 ? bay : bays.Length - 1 - bay]),
                    CombatVisuals.Rotate(Vector2.down, mirror * (6 + bay * 10)), 2.6f, .42f)
                .Pause(1.0f, .4f)
                .SplitAt(1.4f, shards, 360f / shards, 2.6f, .19f);
    }

    // Last Harvest: the wheat streams run under three contrail and drill passes.
    IEnumerator LastHarvest()
    {
        yield return PrimePorts(.5f, 0, 3);
        Phrase(WheatStreams(7.4f, Dense(4), false));
        for (int pass = 0; pass < 3; pass++)
        {
            for (int side = 0; side < 2; side++)
                Phrase(Contrail(side, 1.3f, 1.5f));
            yield return Wait(Rest(.9f));
            yield return PrimePorts(.3f, 4, 9);
            SowArc(pass % 2 == 0 ? 1 : -1);
            yield return Wait(Rest(1.1f));
        }
        yield return Wait(.6f);
    }
}
