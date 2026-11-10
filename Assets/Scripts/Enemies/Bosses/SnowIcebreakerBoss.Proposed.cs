using System.Collections;
using UnityEngine;

// Proposed icebreaker card book (ArtSource/BossPhaseProposals), flown only while
// DistrictBoss.ProposedCards is set. One concept per phase, developed in movements;
// every round still leaves the prow or one of the two shoulder ports.
// Phase one, Bow Wave (pack ice): wherever the ship surges, its prow throws both arms of
//   a bow wave, so a V-shaped wake trails behind it; each crest freezes, then slides off
//   down its own side, starting slow and accelerating, and the shoulders now and then
//   throw a crystal dart. The course develops: a W through the arena, three rams at the
//   pilot (the wake laid on the dive and again backing astern), breakers across the
//   middle and back along the top, and a figure eight. From the second round of
//   movements alternate courses freeze their crests and re-aim them at the pilot.
// Phase two, Closing Pack (open water): the shoulders throw ice along the side walls, a
//   column the full height of the arena with one opening; the columns break off and close
//   in across the arena. Staggered pairs, then sheared columns that break off from the
//   top down and lean as they close, then a tide of single walls from one side with a
//   stepping opening, then a nip: both walls at once with their openings aligned. Prow
//   pellets with every throw and a freezing, re-aiming fan in the slack water between.
// Hard phase three, Shattered Icicles: lances of paired arrows freeze and shatter into
//   trees of chevrons, their crystal tips re-aiming at the pilot. A volley of parallel
//   lances, a fan of five from the prow, crossfire from both shoulders while the ship
//   slides, then a ram whose impact drives a fan of lances; later rounds splay wider.
// Tones: phase one a bright wake with dark darts; phase two dark walls with bright
// pellets and fans; phase three alternates dark and bright lances with opposite tips.
public sealed partial class SnowIcebreakerBoss
{
    protected override IEnumerator Proposed()
    {
        drifting = false;
        cruise = Cruise;
        while (true)
        {
            if (phaseThree)
                yield return ShatteredIcicles();
            else if (!phaseTwo)
                yield return BowWave();
            else
                yield return ClosingPack();
        }
    }

    // ---- Phase one: Bow Wave ----

    bool reaimCrests;
    int wakeStep;

    IEnumerator BowWave()
    {
        float reach = CombatRound.Border.xMax - 1.4f;
        float top = Camera.UpLimit - 2.2f, low = Camera.UpLimit - 6.2f, mid = (top + low) * .5f;
        for (int movement = 0; !phaseTwo; movement++)
        {
            reaimCrests = movement >= 4 && movement % 2 == 0;
            switch (movement % 4)
            {
                case 0:
                    // A W through the arena: down, up, down, up.
                    yield return Sail(new Vector3(-side * reach, top, 1), 5.5f);
                    yield return PrimeAll(.45f);
                    yield return Surge(new Vector3(-side * reach * .45f, low, 1));
                    yield return Surge(new Vector3(0, top - .8f, 1));
                    yield return Surge(new Vector3(side * reach * .45f, low, 1));
                    yield return Surge(new Vector3(side * reach, top, 1));
                    break;
                case 1:
                    // Rams at the pilot: a long wake on the dive, a short one backing astern.
                    for (int ram = 0; ram < 3; ram++)
                    {
                        yield return LineUp(.55f);
                        yield return Surge(new Vector3(transform.position.x, low, 1), 8);
                        yield return Surge(new Vector3(transform.position.x, top, 1), 3.2f);
                    }
                    break;
                case 2:
                    // Breakers: across the middle, a climb, then back along the top.
                    yield return Sail(new Vector3(-side * reach, low, 1), 5.5f);
                    yield return PrimeAll(.45f);
                    yield return Surge(new Vector3(side * reach, low, 1));
                    yield return Surge(new Vector3(side * reach * .3f, top, 1));
                    yield return Surge(new Vector3(-side * reach, top - .6f, 1));
                    break;
                default:
                    // A figure eight that crosses itself in the middle of the upper arena.
                    yield return Sail(new Vector3(0, mid, 1), 5.5f);
                    yield return PrimeAll(.45f);
                    yield return Surge(new Vector3(-side * reach * .9f, top, 1));
                    yield return Surge(new Vector3(-side * reach * .9f, low, 1));
                    yield return Surge(new Vector3(side * reach * .9f, top, 1));
                    yield return Surge(new Vector3(side * reach * .9f, low, 1));
                    yield return Surge(new Vector3(0, mid, 1));
                    break;
            }
            // The water settles before the next course.
            cruise = Cruise;
            yield return Wait(Rest(.6f));
            side = -side;
        }
    }

    // One leg of a course: the prow throws both arms of the bow wave at an even spacing.
    IEnumerator Surge(Vector3 to, float speed = 4.4f)
    {
        station = to;
        cruise = speed;
        Vector2 heading = ((Vector2)(to - transform.position)).normalized;
        float interval = Rest(.08f), clock = interval;
        while (Vector2.Distance(transform.position, to) > .08f)
        {
            if (!Holding)
                clock += Time.deltaTime * LevelRun.EnemyTempoScale;
            if (clock >= interval)
            {
                clock -= interval;
                Wake(heading, wakeStep++);
            }
            yield return null;
        }
    }

    // Both arms leave the prow a little behind square to the heading, so the wake opens
    // as a V behind the ship. Each crest freezes, then slides off down its own side (or
    // re-aims at the pilot), starting slow. Every fourth crest is round; every eighth step
    // both shoulders throw a dark crystal dart that crawls at the pilot and accelerates.
    void Wake(Vector2 heading, int step)
    {
        // Lighter difficulties leave out every third crest (Easy two in three).
        bool crest = Difficulty.Hard || step % 3 != 2 && (!Difficulty.Easy || step % 3 == 0);
        if (crest)
        {
            UseTone(HostileTone.Bright);
            var port = Muzzle();
            bool round = step % 4 == 0;
            for (int arm = -1; arm <= 1; arm += 2)
            {
                var axis = CombatVisuals.Rotate(heading, arm * 118);
                var shot = ScoredShot(round, port, axis, 1.5f, round ? .22f : .19f);
                if (reaimCrests)
                    shot.Pause(.8f, .35f, CombatRound.Turn.Aim, arm * 9, .6f);
                else
                    shot.Pause(.8f, .35f, CombatRound.Turn.Down, Mathf.Sign(axis.x) * 14, .7f);
                shot.Ramp(3.2f, 1.2f, 1.15f);
            }
        }
        if (step % 8 != 4)
            return;
        UseTone(HostileTone.Dark);
        for (int shoulder = 1; shoulder <= 2; shoulder++)
        {
            var from = Muzzle(shoulder);
            Fire<CrystalRound>(from, CombatVisuals.Aim(from), 1.0f, .2f)
                .Ramp(3.4f, 1.3f, .3f);
        }
    }

    // ---- Phase two: Closing Pack ----

    IEnumerator ClosingPack()
    {
        var edge = CombatRound.Border;
        float span = edge.height - 3.0f;
        for (int movement = 0; !phaseThree; movement++)
        {
            int kind = movement % 4;
            // Later rounds close faster.
            float closing = movement >= 4 ? 2.0f : 1.7f;
            cruise = 3.0f;
            if (kind == 2)
            {
                // Tide: single walls from one side in quick succession, the opening stepping.
                station = new Vector3(side * 1.6f, Camera.UpLimit - 2.3f, 1);
                yield return PrimePair(.45f);
                for (int wave = 0; wave < 6; wave++)
                {
                    PackWall(side, Opening(wave * 1.8f, span), HostileTone.Dark, closing);
                    Pellets(wave % 2 == 0 ? 2 : 3);
                    yield return Wait(Rest(.6f));
                }
            }
            else
            {
                for (int throwIndex = 0; throwIndex < 4; throwIndex++)
                {
                    station = new Vector3(Mathf.Sin((throwIndex + movement) * 1.3f) * 1.4f, Camera.UpLimit - 2.3f, 1);
                    yield return PrimePair(.45f);
                    float walk = throwIndex * 2.4f;
                    if (kind == 0)
                    {
                        // Staggered: the right column breaks off half a throw later, so the
                        // two cross off the centre line; its opening trails the left one's.
                        PackWall(-1, Opening(walk, span), HostileTone.Dark, closing);
                        PackWall(1, Opening(walk + 3.0f, span), HostileTone.Dark, closing, 3.05f);
                    }
                    else if (kind == 1)
                    {
                        // Shear: each column breaks off from the top down and leans as it closes.
                        PackWall(-1, Opening(walk, span), HostileTone.Dark, closing, 2.0f, .07f);
                        PackWall(1, Opening(walk + 3.0f, span), HostileTone.Dark, closing, 2.85f, .07f);
                    }
                    else
                    {
                        // Nip: both columns at once, their openings level, meeting mid-arena.
                        float opening = Opening(walk * 1.4f, span);
                        PackWall(-1, opening, HostileTone.Dark, closing);
                        PackWall(1, opening, HostileTone.Dark, closing);
                    }
                    Pellets(3);
                    yield return Wait(Rest(.95f));
                }
            }
            // Slack water: the prow charges and fans out frost that freezes and re-aims.
            yield return Charge(Hull, WeaponPort(0), .4f);
            ProwFan();
            yield return Wait(Rest(.9f));
            side = -side;
        }
    }

    // Slow bright pellets from the prow crawl at the pilot and accelerate.
    void Pellets(int count)
    {
        UseTone(HostileTone.Bright);
        int pellets = Dense(count);
        var aim = CombatVisuals.Aim(Muzzle());
        for (int i = 0; i < pellets; i++)
            Fire<SimpleRound>(Muzzle(), CombatVisuals.Rotate(aim, (i - (pellets - 1) * .5f) * 12), 1.2f, .22f)
                .Ramp(2.6f, 1.0f, .2f);
    }

    void ProwFan()
    {
        UseTone(HostileTone.Bright);
        int count = Dense(7);
        var aim = CombatVisuals.Aim(Muzzle());
        for (int i = 0; i < count; i++)
        {
            float lane = i - (count - 1) * .5f;
            Fire<SimpleRound>(Muzzle(), CombatVisuals.Rotate(aim, lane * 13), 2.4f, .22f)
                .Pause(.5f, .35f, CombatRound.Turn.Aim, lane * 8, .9f)
                .Ramp(2.2f, 1.0f, .85f);
        }
    }

    // A column of ice along one side wall, the full height of the arena, with one
    // opening. Each piece is thrown from that side's shoulder and settles on the wall;
    // the column breaks off (all at once, or from the top down with shear) and closes in
    // across the arena, slowly at first. Every third piece is round, the rest are shards
    // pointing inward.
    void PackWall(int wall, float gapAt, HostileTone shade, float closing, float release = 2.2f, float shear = 0)
    {
        UseTone(shade);
        var edge = CombatRound.Border;
        int rows = Mathf.Max(10, Dense(22));
        float x = wall < 0 ? edge.xMin + .35f : edge.xMax - .35f;
        var port = Muzzle(wall < 0 ? 1 : 2);
        for (int row = 0; row < rows; row++)
        {
            float y = Mathf.Lerp(edge.yMax - .45f, edge.yMin + .9f, row / (rows - 1f));
            if (Mathf.Abs(y - gapAt) < 1.0f)
                continue;
            var point = new Vector2(x, y);
            // Far pieces take longer to settle, so the column forms from the top down.
            float at = .6f + Vector2.Distance(port, point) * .09f;
            float hold = Mathf.Max(.1f, release + shear * row - at);
            float launch;
            CombatRound ice = row % 3 == 1
                ? (CombatRound)Place<SimpleRound>(port, point, at, .22f, out launch)
                : Place<NeedleRound>(port, point, at, .19f, out launch);
            ice.Pause(at, hold, CombatRound.Turn.Down, -wall * 90, 1.25f / launch)
                .Ramp(closing, 1.2f, at + hold + .6f);
        }
    }

    // The height of a pack wall's opening, a given distance along this pass's walk
    // (down the arena on one pass, up it on the next).
    float Opening(float walk, float span)
    {
        var edge = CombatRound.Border;
        float along = Mathf.Repeat(walk, span);
        return side > 0 ? edge.yMax - 1.5f - along : edge.yMin + 1.5f + along;
    }

    // ---- Hard phase three: Shattered Icicles ----

    IEnumerator ShatteredIcicles()
    {
        for (int movement = 0; ; movement++)
        {
            // Later rounds splay wider.
            float splay = movement >= 4 ? 1.25f : 1;
            var shade = movement % 2 == 0 ? HostileTone.Dark : HostileTone.Bright;
            switch (movement % 4)
            {
                case 0:
                    // Volley: three parallel lances along the prow's aim.
                    for (int salvo = 0; salvo < 4; salvo++)
                    {
                        yield return LineUp(.6f);
                        var aim = CombatVisuals.Aim(Muzzle());
                        for (int port = 0; port < 3; port++)
                            Lance(Muzzle(port), aim, shade, splay);
                        yield return Wait(Rest(.4f));
                    }
                    break;
                case 1:
                    // Fan: the prow drives five lances spread across the pilot.
                    for (int salvo = 0; salvo < 3; salvo++)
                    {
                        yield return LineUp(.6f);
                        var aim = CombatVisuals.Aim(Muzzle());
                        for (int lane = -2; lane <= 2; lane++)
                            Lance(Muzzle(), CombatVisuals.Rotate(aim, lane * 24), shade, splay);
                        yield return Wait(Rest(1.0f));
                    }
                    break;
                case 2:
                    // Crossfire: the ship slides across the top and both shoulders lance at the
                    // pilot from either side, the prow answering in the other tone.
                    var other = shade == HostileTone.Dark ? HostileTone.Bright : HostileTone.Dark;
                    for (int salvo = 0; salvo < 4; salvo++)
                    {
                        yield return Sail(new Vector3(Lane((salvo % 2 == 0 ? -.7f : .7f) * side), Camera.UpLimit - 2.3f, 1), 5.5f);
                        yield return PrimeAll(.4f);
                        for (int shoulder = 1; shoulder <= 2; shoulder++)
                            Lance(Muzzle(shoulder), CombatVisuals.Aim(Muzzle(shoulder)), shade, splay);
                        Lance(Muzzle(), CombatVisuals.Aim(Muzzle()), other, splay);
                        yield return Wait(Rest(.3f));
                    }
                    break;
                default:
                    // Ram: the impact drives a fan of lances straight down from the prow.
                    for (int ram = 0; ram < 3; ram++)
                    {
                        yield return LineUp(.55f);
                        yield return Ram(Camera.UpLimit - 5.6f);
                        for (int lane = -2; lane <= 2; lane++)
                            Lance(Muzzle(), CombatVisuals.Rotate(Vector2.down, lane * 30), shade, splay);
                        yield return Wait(Rest(.6f));
                        station.y = Camera.UpLimit - 2.2f;
                        cruise = 4.6f;
                    }
                    break;
            }
            cruise = Cruise;
            yield return Wait(Rest(.7f));
            side = -side;
        }
    }

    // A lance of paired arrows launched together at rising speeds. It freezes, then
    // every pair splits mirror-wise, the deeper links splaying wider: a tree of chevrons.
    // The crystal tip, in the other tone, re-aims at the pilot after the shatter.
    void Lance(Vector3 port, Vector2 axis, HostileTone body, float splayScale)
    {
        UseTone(body);
        int links = Mathf.Max(3, Dense(5));
        const float At = .85f;
        for (int link = 0; link < links; link++)
        {
            float speed = 1.8f + .5f * link;
            float hold = .35f + link * .06f;
            float splay = (22 + 16 * link) * splayScale;
            for (int mirror = -1; mirror <= 1; mirror += 2)
                Fire<NeedleRound>(port, axis, speed, .19f)
                    .Pause(At, hold, CombatRound.Turn.Rotate, mirror * splay, 1.3f / speed)
                    .Ramp(2.4f, 1.0f, At + hold);
        }
        UseTone(body == HostileTone.Dark ? HostileTone.Bright : HostileTone.Dark);
        float tip = 1.8f + .5f * links;
        Fire<CrystalRound>(port, axis, tip, .2f)
            .Pause(At, .35f + links * .06f + .15f, CombatRound.Turn.Aim, 0, 1.2f / tip)
            .Ramp(2.6f, 1.0f, At + .5f + links * .06f);
    }
}
