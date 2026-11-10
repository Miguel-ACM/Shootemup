using System.Collections;
using UnityEngine;

// Proposed card book for the Plains bomber (ArtSource/BossPhaseProposals). It flies only
// while DistrictBoss.ProposedCards is set, so it can be recorded beside Cards(). One idea
// per phase, developing over the phase rather than alternating with another card.
// Phase one, Seed Drill: the six bays sow an arc of dark bombs. Every bomb flies the same
//   distance, so they settle on a smile under the bomber, then burst 360 along a fuse. Each
//   drill burns the fuse differently: left to right, right to left, both ends inward, the
//   centre outward, two leaning sweeps, a double arc taken up by the lower guns and a crown
//   that bursts all at once. Bright petals tossed from the wingtips hang and flutter down,
//   and bright aimed arrows bracket the pilot while the fuse burns.
// Phase two, Skywriting: the wingtips write curling contrails of dark arrows that brake and
//   hang over the upper arena, then fall together; each pass ends with dark full-stop bombs.
//   Passes vary the writing (curling out, curling in), the way it falls (inward, out toward
//   the walls, peeling round) and the hands: the lower guns join in on later passes.
// Phase three (Hard), Harvest Fireworks: a continuous show. The wingtips loft bright shells
//   that burst 360 along the skyline while it sweeps out and back; the bays drop dark pairs
//   below, and every fourth salvo is a grand crown of all six bays.
// Tones: dark bombs and contrails, bright petals, arrows, tracers and fireworks.
public sealed partial class FieldsBomberBoss
{
    protected override IEnumerator Proposed()
    {
        while (true)
        {
            if (phaseThree)
                yield return HarvestFireworks();
            else if (!phaseTwo)
                yield return SeedDrill();
            else
                yield return Skywriting();
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

    // A dark bomb: one speed and one braking time, so every bomb stops the same distance
    // from its port, glints for `hold` and bursts in a full ring.
    void Bomb(Vector3 port, float angle, float hold, int shards, float burst)
    {
        UseTone(HostileTone.Dark);
        Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, angle), 2.6f, .42f)
            .Pause(.95f, hold)
            .SplitAt(.95f + hold, shards, 360f / shards, burst, .19f);
    }

    // Seed Drill: eight drills, each burning its fuse a different way.
    IEnumerator SeedDrill()
    {
        for (int drill = 0; drill < 8; drill++)
        {
            yield return PrimePorts(.45f, drill == 6 ? 2 : 4, 9);
            SowArc(drill);
            TossPetals(drill);
            yield return Every(.9f, .15f, n => Talon(n));
            TossPetals(drill + 1);
            yield return Wait(Rest(1.25f));
        }
    }

    // A bomb's place on the fuse, by drill: left to right, right to left, both ends inward,
    // the centre outward, two leaning sweeps, the double arc, then the crown.
    static int FuseRank(int drill, int i, int count)
    {
        int last = count - 1;
        switch (drill % 8)
        {
            case 1:
            case 5:
                return last - i;
            case 2:
                return Mathf.Min(i, last - i);
            case 3:
                return last / 2 - Mathf.Min(i, last - i);
            case 7:
                return 0;
            default:
                return i;
        }
    }

    // Six bombs fanned across 80 degrees settle on an arc; each bursts on its place on the
    // fuse, alternate places at two ring speeds. On the double-arc drill the lower guns add
    // two bombs each on a wider arc that burns after the bays' arc.
    void SowArc(int drill)
    {
        int shards = Dense(20);
        float lean = drill % 8 == 4 ? -14 : drill % 8 == 5 ? 14 : 0;
        for (int i = 0; i < bays.Length; i++)
        {
            int rank = FuseRank(drill, i, bays.Length);
            Bomb(Hull.TransformPoint(bays[i]), Mathf.Lerp(-40, 40, i / (bays.Length - 1f)) + lean,
                .3f + rank * .2f, shards, rank % 2 == 0 ? 2.2f : 2.8f);
        }
        if (drill % 8 == 6)
            for (int side = 2; side <= 3; side++)
            {
                int mirror = side == 2 ? -1 : 1;
                for (int j = 0; j < 2; j++)
                    Bomb(Muzzle(side), mirror * (52 + j * 16), .3f + (6 + j) * .2f, shards, j == 0 ? 2.4f : 2.6f);
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

    // How a contrail falls when it lets go.
    enum ContrailFall
    {
        Inward, Outward, Peel
    }

    // Skywriting: six passes. Passes 2-4 change how the writing falls; the lower guns
    // join on passes 3 and 5, writing the opposite curl, and the last pass doubles the stops.
    IEnumerator Skywriting()
    {
        for (int pass = 0; pass < 6; pass++)
        {
            bool lowerGuns = pass == 3 || pass == 5;
            bool curlIn = pass % 2 == 1;
            var fall = pass == 2 ? ContrailFall.Outward : pass == 4 ? ContrailFall.Peel : ContrailFall.Inward;
            yield return PrimePorts(.4f, 0, lowerGuns ? 3 : 1);
            for (int side = 0; side < 2; side++)
            {
                Phrase(Contrail(side, 1f, 1.5f, curlIn, fall));
                Phrase(Contrail(side, 1.7f, 1.85f, curlIn, fall));
                if (lowerGuns)
                {
                    Phrase(Contrail(2 + side, 1.2f, 1.65f, !curlIn, fall));
                    Phrase(Contrail(2 + side, 1.9f, 2.0f, !curlIn, fall));
                }
            }
            FullStops(pass == 5 ? 2 : 1, 1.85f);
            yield return Every(2.2f, .3f, n => Tracer(n));
            yield return Wait(Rest(.15f));
        }
    }

    // One contrail written in .8 s from a wingtip or lower gun, sweeping from below the port
    // out and over it (or, curling in, from over it down): later arrows fly further before
    // braking. Every arrow hangs until the shared release; odd arrows fall faster.
    IEnumerator Contrail(int port, float reach, float release, bool curlIn, ContrailFall fall)
    {
        int mirror = port % 2 == 0 ? -1 : 1;
        int count = Mathf.Max(6, Dense(20));
        float step = .8f / count;
        for (int i = 0; i < count; i++)
        {
            float f = i / (count - 1f);
            float at = .3f + .45f * f * reach;
            float sweep = curlIn ? 1 - f : f;
            float hold = Mathf.Max(.1f, release - i * step - at);
            float speed = i % 2 == 0 ? .7f : 1.05f;
            UseTone(HostileTone.Dark);
            var arrow = Fire<NeedleRound>(Muzzle(port), CombatVisuals.Rotate(Vector2.down, mirror * (20 + 150 * sweep)), 3.2f, .19f);
            if (fall == ContrailFall.Inward)
                arrow.Pause(at, hold, CombatRound.Turn.Down, -mirror * (10 + 26 * f), speed);
            else if (fall == ContrailFall.Outward)
                arrow.Pause(at, hold, CombatRound.Turn.Down, mirror * (8 + 20 * f), speed);
            else
                arrow.Pause(at, hold, CombatRound.Turn.Rotate, -mirror * 100, speed);
            yield return Wait(step);
        }
    }

    // Full stops: dark bombs from the inner bays punctuate each pass. They travel the same
    // distance as every bomb and burst just after the contrails let go.
    void FullStops(int pairs, float release)
    {
        int shards = Dense(16);
        for (int pair = 0; pair < pairs; pair++)
            for (int mirror = -1; mirror <= 1; mirror += 2)
                Bomb(Hull.TransformPoint(bays[mirror < 0 ? 2 - pair : 3 + pair]), mirror * (10 + pair * 14),
                    release + .2f - .95f, shards, 2.5f);
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

    // Harvest Fireworks: twelve salvos; the skyline sweeps out and back, and every fourth
    // salvo the bays fire the grand crown instead of a pair.
    IEnumerator HarvestFireworks()
    {
        for (int salvo = 0; salvo < 12; salvo++)
        {
            yield return PrimePorts(.3f, 0, 1);
            Loft(24 * Mathf.Sin(salvo * .55f));
            yield return Wait(Rest(.35f));
            if (salvo % 4 == 3)
                SowArc(7);
            else
                DropPairs(salvo);
            yield return Wait(Rest(.55f));
        }
    }

    // Three bright shells from each wingtip, thrown up and out at one speed and braking at
    // one time, so they all burst 360 the same distance away, over the top of the arena.
    void Loft(float lean)
    {
        UseTone(HostileTone.Bright);
        int shards = Dense(16);
        for (int side = 0; side < 2; side++)
        {
            int mirror = side == 0 ? -1 : 1;
            for (int j = 0; j < 3; j++)
                Fire<SimpleRound>(Muzzle(side), CombatVisuals.Rotate(Vector2.up, -mirror * (20 + 24 * j + lean)), 2.6f, .4f)
                    .Pause(.85f, .3f)
                    .SplitAt(1.15f, shards, 360f / shards, 2.3f, .19f);
        }
    }

    // Two mirrored pairs of dark bombs from neighbouring bays, rotating outer, middle, inner:
    // the low rings open under the falling high ones.
    void DropPairs(int salvo)
    {
        int shards = Dense(16);
        for (int pair = 0; pair < 2; pair++)
        {
            int bay = (salvo + pair) % 3;
            for (int mirror = -1; mirror <= 1; mirror += 2)
                Bomb(Hull.TransformPoint(bays[mirror < 0 ? bay : bays.Length - 1 - bay]), mirror * (6 + bay * 10),
                    .4f + pair * .15f, shards, 2.6f);
        }
    }
}
