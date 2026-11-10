using System.Collections;
using UnityEngine;

// Proposed icebreaker card book (ArtSource/BossPhaseProposals), flown only while
// DistrictBoss.ProposedCards is set. Every round still leaves the prow or one of the
// two shoulder ports.
// Phase one (pack ice)
//   Bow Wave: the ship surges through the arena on a W, down and up twice, and its prow
//     throws both arms of a bow wave as it goes. The wake trails behind it as a V; each
//     crest freezes, then slides off down its own side, starting slow and accelerating.
//     Diving legs leave the wake over the top of the arena, rising legs below.
//   Shattered Icicles: lined up over the pilot, all three ports drive parallel lances at
//     the pilot. The lances freeze, then shatter: every link splits into a mirrored pair,
//     the deeper links splaying wider, so each lance opens into a tree of chevrons. A
//     crystal at each tip re-aims at the pilot.
// Phase two (open water)
//   Aurora Curtain: drifting broadside, each shoulder lets down a braided ribbon of
//     arrows, two strands swaying against each other, dashed with gaps that the other
//     shoulder fills half a beat later. The curtains ripple as the ship drifts; the prow
//     fires fans that freeze and re-aim.
//   Closing Pack: the shoulders throw ice along both side walls, a column the full height
//     of the arena on each side with one opening. The left column breaks off and closes
//     in across the arena, the right one half a throw later, so they cross off centre.
//     Both openings walk along the walls throw by throw (down on one pass, up on the
//     next), the right one a little behind. The prow fires slow, accelerating pellets.
// Hard phase three, Glacial Breach: the bow-wave W again, with a fresh pair of pack walls
//   thrown at the foot of each dive and a lance driven at the pilot from each crest.
// Tones: the dominant tone alternates card by card. A bright wake with dark crystal
// darts; dark lances with bright tips; bright curtains with dark prow fans; dark pack
// walls with bright pellets. Glacial Breach mixes a bright wake with dark walls and lances.
public sealed partial class SnowIcebreakerBoss
{
    protected override IEnumerator Proposed()
    {
        drifting = false;
        cruise = Cruise;
        while (true)
        {
            if (phaseThree)
            {
                yield return GlacialBreach();
                yield return Wait(.8f);
            }
            else if (!phaseTwo)
            {
                yield return BowWave();
                yield return Wait(1);
                yield return ShatteredIcicles();
                yield return Wait(1);
            }
            else
            {
                yield return AuroraCurtain();
                yield return Wait(.9f);
                yield return ClosingPack();
                yield return Wait(.9f);
            }
            side = -side;
        }
    }

    // ---- Phase one ----

    IEnumerator BowWave()
    {
        float reach = CombatRound.Border.xMax - 1.4f;
        float top = Camera.UpLimit - 2.2f, low = Camera.UpLimit - 6.2f;
        yield return Sail(new Vector3(-side * reach, top, 1), 5.5f);
        yield return PrimeAll(.45f);
        // A W across the arena: down, up, down, up.
        yield return Surge(new Vector3(-side * reach * .45f, low, 1));
        yield return Surge(new Vector3(0, top - .8f, 1));
        yield return Surge(new Vector3(side * reach * .45f, low, 1));
        yield return Surge(new Vector3(side * reach, top, 1));
        cruise = Cruise;
    }

    int wakeStep;

    // One leg of the run: the prow throws both arms of the bow wave at an even spacing.
    IEnumerator Surge(Vector3 to)
    {
        station = to;
        cruise = 4.4f;
        Vector2 heading = ((Vector2)(to - transform.position)).normalized;
        float interval = Rest(.1f), clock = interval;
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
    // as a V behind the ship. Each crest freezes, then slides off down its own side,
    // starting slow. Every fourth crest is round; every eighth step both shoulders throw
    // a dark crystal dart that crawls at the pilot and accelerates.
    void Wake(Vector2 heading, int step)
    {
        UseTone(HostileTone.Bright);
        var port = Muzzle();
        bool round = step % 4 == 0;
        for (int arm = -1; arm <= 1; arm += 2)
        {
            var axis = CombatVisuals.Rotate(heading, arm * 118);
            ScoredShot(round, port, axis, 1.5f, round ? .22f : .19f)
                .Pause(.8f, .35f, CombatRound.Turn.Down, Mathf.Sign(axis.x) * 14, .7f)
                .Ramp(3.2f, 1.2f, 1.15f);
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

    IEnumerator ShatteredIcicles()
    {
        for (int lance = 0; lance < 4; lance++)
        {
            yield return LineUp(.6f);
            // The prow and both shoulders drive parallel lances along the prow's aim.
            var aim = CombatVisuals.Aim(Muzzle());
            for (int port = 0; port < 3; port++)
                Lance(Muzzle(port), aim, HostileTone.Dark);
            yield return Wait(Rest(.9f));
        }
        station.y = Camera.UpLimit - 2.2f;
        cruise = Cruise;
    }

    // A lance of paired arrows launched together at rising speeds. It freezes, then
    // every pair splits mirror-wise, the deeper links splaying wider: a tree of chevrons.
    // The crystal tip, in the other tone, re-aims at the pilot after the shatter.
    void Lance(Vector3 port, Vector2 axis, HostileTone body)
    {
        UseTone(body);
        int links = Mathf.Max(3, Dense(5));
        const float At = .85f;
        for (int link = 0; link < links; link++)
        {
            float speed = 1.8f + .5f * link;
            float hold = .35f + link * .06f;
            float splay = 22 + 16 * link;
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

    // ---- Phase two ----

    IEnumerator AuroraCurtain()
    {
        cruise = 2.4f;
        drifting = true;
        yield return PrimePair(.45f);
        Phrase(Toned(HostileTone.Dark, ProwFrost()));
        const int Beats = 112;
        for (int n = 0; n < Beats; n++)
        {
            UseTone(HostileTone.Bright);
            for (int mirror = -1; mirror <= 1; mirror += 2)
            {
                // Dashed: on for nine beats, off for seven. The right curtain runs half a
                // period behind the left, so its openings fall between the left one's.
                int phase = (n + (mirror > 0 ? 8 : 0)) % 16;
                if (phase >= 9)
                    continue;
                var port = Muzzle(mirror < 0 ? 1 : 2);
                bool round = phase == 4;
                // Two strands sway against each other into a braid.
                for (int strand = -1; strand <= 1; strand += 2)
                    ScoredShot(round, port, Vector2.down, 1.4f, round ? .22f : .19f)
                        .Sway(strand * .55f, .32f)
                        .Ramp(2.2f, 1.4f, .3f);
            }
            yield return Wait(Rest(.07f));
        }
        drifting = false;
        cruise = Cruise;
        station = new Vector3(transform.position.x, Camera.UpLimit - 2.3f, 1);
    }

    // Prow fans while the curtains fall: they fly out, freeze, re-aim at the pilot and
    // accelerate, so standing between two curtains is never safe.
    IEnumerator ProwFrost()
    {
        while (drifting)
        {
            yield return Charge(Hull, WeaponPort(0), .4f);
            if (!drifting)
                yield break;
            int count = Dense(5);
            var aim = CombatVisuals.Aim(Muzzle());
            for (int i = 0; i < count; i++)
            {
                float lane = i - (count - 1) * .5f;
                Fire<SimpleRound>(Muzzle(), CombatVisuals.Rotate(aim, lane * 13), 2.4f, .22f)
                    .Pause(.5f, .35f, CombatRound.Turn.Aim, lane * 8, .9f)
                    .Ramp(2.2f, 1.0f, .85f);
            }
            yield return Wait(Rest(1.2f));
        }
    }

    IEnumerator ClosingPack()
    {
        cruise = 3.0f;
        var edge = CombatRound.Border;
        float span = edge.height - 3.0f;
        for (int throwIndex = 0; throwIndex < 5; throwIndex++)
        {
            station = new Vector3(Mathf.Sin(throwIndex * 1.3f) * 1.4f, Camera.UpLimit - 2.3f, 1);
            yield return PrimePair(.45f);
            // Both openings walk along the arena throw by throw (down on one pass, up on
            // the next), the right one a little behind the left. The right wall breaks off
            // half a throw later, so the two columns cross off the centre line.
            float walk = throwIndex * 2.4f;
            PackWall(-1, Opening(walk, span), HostileTone.Dark);
            PackWall(1, Opening(walk + 3.0f, span), HostileTone.Dark, 3.05f);
            // Slow bright pellets from the prow crawl at the pilot and accelerate.
            UseTone(HostileTone.Bright);
            int pellets = Dense(3);
            var aim = CombatVisuals.Aim(Muzzle());
            for (int i = 0; i < pellets; i++)
                Fire<SimpleRound>(Muzzle(), CombatVisuals.Rotate(aim, (i - (pellets - 1) * .5f) * 12), 1.2f, .22f)
                    .Ramp(2.6f, 1.0f, .2f);
            yield return Wait(Rest(1.25f));
        }
        yield return Wait(Rest(.8f));
        cruise = Cruise;
    }

    // A column of ice along one side wall, the full height of the arena, with one
    // opening. Each piece is thrown from that side's shoulder and settles on the wall;
    // the whole column breaks off at once and closes in across the arena, slowly at
    // first. Every third piece is round, the rest are shards pointing inward.
    void PackWall(int wall, float gapAt, HostileTone shade, float release = 2.2f)
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
            float hold = Mathf.Max(.1f, release - at);
            float launch;
            CombatRound ice = row % 3 == 1
                ? (CombatRound)Place<SimpleRound>(port, point, at, .22f, out launch)
                : Place<NeedleRound>(port, point, at, .19f, out launch);
            ice.Pause(at, hold, CombatRound.Turn.Down, -wall * 90, 1.25f / launch)
                .Ramp(1.7f, 1.2f, at + hold + .6f);
        }
    }

    // The height of a pack wall's opening, a given distance along this pass's walk.
    float Opening(float walk, float span)
    {
        var edge = CombatRound.Border;
        float along = Mathf.Repeat(walk, span);
        return side > 0 ? edge.yMax - 1.5f - along : edge.yMin + 1.5f + along;
    }

    // ---- Hard phase three ----

    // Glacial Breach: the bow-wave W, with a fresh pair of pack walls thrown at the foot
    // of each dive and a lance driven at the pilot from each crest.
    IEnumerator GlacialBreach()
    {
        float reach = CombatRound.Border.xMax - 1.4f;
        float top = Camera.UpLimit - 2.2f, low = Camera.UpLimit - 6.0f;
        var edge = CombatRound.Border;
        yield return Sail(new Vector3(-side * reach, top, 1), 5.5f);
        yield return PrimeAll(.4f);
        for (int leg = 0; leg < 2; leg++)
        {
            yield return Surge(new Vector3(side * reach * (leg == 0 ? -.45f : .45f), low, 1));
            float span = edge.height - 3.0f;
            PackWall(-1, Opening(2 + leg * 5, span), HostileTone.Dark);
            PackWall(1, Opening(5 + leg * 5, span), HostileTone.Dark, 3.05f);
            yield return Surge(new Vector3(leg == 0 ? 0 : side * reach, leg == 0 ? top - .8f : top, 1));
            yield return PrimeAll(.3f);
            Lance(Muzzle(), CombatVisuals.Aim(Muzzle()), HostileTone.Dark);
        }
        cruise = Cruise;
    }
}
