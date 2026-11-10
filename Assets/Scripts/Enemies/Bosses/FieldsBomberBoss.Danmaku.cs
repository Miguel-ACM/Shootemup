using System.Collections;
using UnityEngine;

// Plains bomber, a working aircraft that flies its sorties as spell cards.
// Phase one
//   Wingtip Crossfire: both wingtips stream triple arrows at the player while the
//     bays drop mirrored bombs that travel the same distance, stop and explode 360.
//   Harvest Lace: a two-arm mirrored lace folds open and shut from the wings,
//     with slow orb wavefronts from the nose rolling under it.
// Phase two, Skywriting: the wingtips write curling contrails of dark arrows that brake
//   and hang over the upper arena, then fall together; each pass ends with dark full-stop
//   bombs. Passes vary the writing (curling out, curling in), the way it falls (inward,
//   out toward the walls, peeling round) and the hands: the lower guns join on later passes.
//   Bright tracers from the lower guns keep the pilot moving.
// Phase three (Hard), Harvest Fireworks: a continuous show. The wingtips loft bright shells
//   that burst 360 along the skyline while it sweeps out and back; the bays drop dark
//   pairs below, and every fourth salvo is a grand crown of all six bays.
// Tones: the bombs and contrails are dark. Phase one alternates bright crossfire and a
// dark lace; the tracers and fireworks are bright.
public sealed partial class FieldsBomberBoss
{
    protected override void Attack()
    {
        OpenCards();
    }

    protected override IEnumerator Cards()
    {
        while (true)
        {
            if (phaseThree)
                yield return HarvestFireworks();
            else if (phaseTwo)
                yield return Skywriting();
            else
            {
                yield return WingtipCrossfire();
                yield return Wait(1.1f);
                yield return HarvestLace();
                yield return Wait(1.1f);
            }
        }
    }

    // Bright aimed wingtip arrows; dark bombs and tracer.
    IEnumerator WingtipCrossfire()
    {
        yield return Charge(Hull, WeaponPort(0), .5f);
        // The triple streams run for 3.5 s (was 7), so the card does not drag.
        yield return Every(3.5f, .1f, n =>
        {
            UseTone(HostileTone.Bright);
            for (int side = 0; side < 2; side++)
            {
                var port = Muzzle(side);
                var axis = CombatVisuals.Aim(port);
                // All three streams are arrows, the middle one included (Miguel).
                foreach (float spread in new[] { -10f, 0f, 10f })
                    Fire<NeedleRound>(port, CombatVisuals.Rotate(axis, spread), 4.6f, .19f);
            }
            if (n % 12 == 6)
                CarpetBombs(2);
        });
    }

    // Dark lace; bright orb arcs and nose tracer rolling under it.
    IEnumerator HarvestLace()
    {
        Phrase(Every(7, 1.4f, n => OrbWave(n)));
        // Two arms per wing; the third was one animated fan too many (Miguel).
        yield return Toned(HostileTone.Dark, MirrorLace(Hull, WeaponPort(0), WeaponPort(1), Dense(96), 2, 70, 24, 3.4f, .07f, 1));
    }

    // Two layers of slow orbs from the nose: an arc that rolls under the lace.
    void OrbWave(int n)
    {
        UseTone(HostileTone.Bright);
        Fire<SimpleRound>(Muzzle(4),CombatVisuals.Aim(Muzzle(4)),2.8f,.23f);
        int lanes = Dense(15);
        for (int layer = 0; layer < 2; layer++)
            for (int i = 0; i < lanes; i++)
            {
                float step = 120f / (lanes - 1);
                Fire<PetalRound>(Muzzle(4), CombatVisuals.Rotate(Vector2.down, (i - (lanes - 1) * .5f) * step + (layer + n) % 2 * step * .5f), 1.7f + layer * .5f, .26f);
            }
    }

    // Mirrored bombs brake at matching distances, glint, then explode in a full ring.
    void CarpetBombs(int pairs)
    {
        UseTone(HostileTone.Dark);
        // A nose tracer punctuates each payload release, including above the wings.
        Fire<SimpleRound>(Muzzle(4),CombatVisuals.Aim(Muzzle(4)),2.9f,.22f);
        int shards = Dense(16);
        for (int i = 0; i < Dense(pairs); i++)
            for (int mirror = 0; mirror < 2; mirror++)
            {
                var port = Hull.TransformPoint(bays[mirror == 0 ? i : bays.Length - 1 - i]);
                // Every bomb travels the same distance before it stops and explodes all round.
                Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, (mirror == 0 ? -1 : 1) * (8 + i * 14)), 2.5f, .42f)
                    .Pause(1.0f, .45f)
                    .SplitAt(1.45f, shards, 360f / shards, 2.6f, .19f);
            }
    }

    protected override void OnPhaseTwo()
    {
        volley = 0;
        nextAttack = age + 1;
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
                Crown();
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

    // The grand crown: all six bays fanned across 80 degrees burst together, alternate
    // rings at two speeds.
    void Crown()
    {
        int shards = Dense(20);
        for (int i = 0; i < bays.Length; i++)
            Bomb(Hull.TransformPoint(bays[i]), Mathf.Lerp(-40, 40, i / (bays.Length - 1f)), .3f, shards, i % 2 == 0 ? 2.2f : 2.8f);
    }
}
