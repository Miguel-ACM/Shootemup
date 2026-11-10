using System.Collections;
using UnityEngine;

// Snow icebreaker: an armoured bow that smashes through pack ice, flying its passes as
// spell cards. Every round leaves the prow or one of the two shoulder ports.
// Phase one (pack ice)
//   Ramming Speed: it lines up over the pilot while the bow charges, then rams down
//     into the ice. The impact cracks a mirrored fan of fracture lines, arrows that
//     start slow and accelerate, while both shoulders throw bow spray up and out that
//     freezes, re-aims at the pilot and gathers speed. Fans alternate, so the gaps move.
//   Fracture Sheet: the ship glides over the fracture it is about to open and freezes
//     a whole-width sheet of ice in two courses. The sheet cracks under its bow (or
//     under both shoulders): the lower course falls apart either side of the crack,
//     gathering speed, and the upper course heaves up over the top of the arena. Pellets
//     from the shoulders freeze and re-aim between sheets.
// Phase two (open water)
//   Cut Channel: the ship cuts a Z down and back up across the arena, lining its lead
//     with frozen walls. A moment after each pair freezes the ice closes behind it like
//     a zip: the walls cross the channel and carry on, one to the top, one down at the
//     pilot. Prow pellets stop and re-aim.
//   Calving Bergs: drifting broadside, the shoulders throw bergs up and out that arc,
//     rebound from the walls and calve at the top of their flight into fans aimed at
//     the pilot, over mirrored pressure ridges from the prow that cross under the bow,
//     start slow and accelerate.
// Hard phase three, Icequake: rams at the pilot whose impacts crack a fracture fan and
//   a fresh sheet beneath the ship at once, then a zipped channel cut back up to the
//   opposite corner.
// Tones: the dominant tone alternates card by card. Bright fracture lines over dark bow
// spray; a dark sheet with bright pellets; bright channel walls with dark pellets; dark
// ridges under bright bergs. Icequake mixes bright fractures and walls with a dark sheet.
[UnityEngine.Scripting.Preserve]
public sealed partial class SnowIcebreakerBoss : ArmoredHullBoss
{
    static readonly Vector2[] shape = { new(0,-1.53f),new(-.78f,-1.05f),new(-1.14f,-.55f),new(-1.02f,.65f),new(-.79f,1.08f),
        new(.79f,1.08f),new(1.02f,.65f),new(1.14f,-.55f),new(.78f,-1.05f) };
    protected override Vector2[] Outline => shape;
    protected override void Configure() { health = 2100; hullScale = 1.14f; attackInterval = 1.05f; }

    const float Cruise = 4.1f;
    float cruise = Cruise;
    bool drifting;
    int side = 1;

    protected override void MoveBoss(float dt)
    {
        if (drifting)
        {
            // Broadside drift: a slow figure across the upper arena.
            station.x = Mathf.Sin(motionAge * .55f) * (CombatRound.Border.xMax - 2.0f);
            station.y = Camera.UpLimit - 2.7f + Mathf.Sin(motionAge * 1.1f) * .45f;
        }
        transform.position = Vector3.MoveTowards(transform.position, station, dt * cruise);
    }

    protected override IEnumerator Cards()
    {
        drifting = false;
        cruise = Cruise;
        while (true)
        {
            if (phaseThree)
            {
                yield return Icequake();
                yield return Wait(.8f);
            }
            else if (!phaseTwo)
            {
                yield return RammingSpeed();
                yield return Wait(1);
                yield return FractureSheet();
                yield return Wait(1);
            }
            else
            {
                yield return CutChannel();
                yield return Wait(.9f);
                yield return CalvingBergs();
                yield return Wait(.9f);
            }
            side = -side;
        }
    }

    // ---- Phase one ----

    IEnumerator RammingSpeed()
    {
        for (int ram = 0; ram < 3; ram++)
        {
            yield return LineUp(.7f);
            yield return Ram(Camera.UpLimit - 6.3f);
            UseTone(HostileTone.Bright);
            FractureLines(Dense(9), 200, ram % 2 == 1);
            UseTone(HostileTone.Dark);
            BowSpray();
            yield return Wait(Rest(.35f));
            // Back off astern before lining up again.
            station.y = Camera.UpLimit - 2.2f;
            cruise = 4.6f;
            yield return Wait(Rest(.55f));
        }
    }

    // Track the pilot along the top while every port charges.
    IEnumerator LineUp(float seconds)
    {
        cruise = 5.2f;
        station.y = Camera.UpLimit - 2.2f;
        for (float t = 0; t < seconds;)
        {
            if (!Holding)
                t += Time.deltaTime * LevelRun.EnemyTempoScale;
            station.x = Mathf.Clamp(PilotX, Lane(-1), Lane(1));
            for (int port = 0; port < 3; port++)
                Cue(Muzzle(port), seconds - t);
            yield return null;
        }
    }

    IEnumerator Ram(float depth)
    {
        station = new Vector3(transform.position.x, depth, 1);
        cruise = 13;
        while (transform.position.y > depth + .06f)
            yield return null;
        cruise = 4.6f;
    }

    // Mirrored fracture lines from the prow. Each line is a string of arrows launched
    // together at rising speeds with a round tip, so the crack lengthens as it runs;
    // all of them start slow and accelerate. Odd fans sit in the previous fan's gaps.
    void FractureLines(int lines, float arc, bool interleave)
    {
        var port = Muzzle();
        int links = Mathf.Max(2, Dense(5));
        if (interleave)
            lines = Mathf.Max(2, lines - 1);
        float step = arc / Mathf.Max(1, interleave ? lines : lines - 1);
        for (int line = 0; line < lines; line++)
        {
            float angle = -arc * .5f + (line + (interleave ? .5f : 0)) * step;
            var axis = CombatVisuals.Rotate(Vector2.down, angle);
            for (int link = 0; link < links; link++)
                ScoredShot(link == links - 1, port, axis, 1.0f + .42f * link, link == links - 1 ? .22f : .19f)
                    .Ramp(2.4f, 1.0f, .3f);
        }
    }

    // Spray thrown up and out from both shoulders: it freezes, re-aims around the
    // pilot and crawls before accelerating, so the top of the arena is not a refuge.
    void BowSpray()
    {
        int drops = Dense(5);
        for (int mirror = -1; mirror <= 1; mirror += 2)
        {
            var port = Muzzle(mirror < 0 ? 1 : 2);
            for (int k = 0; k < drops; k++)
            {
                float lane = k - (drops - 1) * .5f;
                float elevation = 15 + 60f * k / Mathf.Max(1, drops - 1);
                Fire<NeedleRound>(port, CombatVisuals.Rotate(Vector2.right * mirror, mirror * elevation), 3.2f, .19f)
                    .Pause(.5f, .4f + k * .06f, CombatRound.Turn.Aim, mirror * lane * 4, .35f)
                    .Ramp(3.0f, 1.1f, .9f + k * .06f);
            }
        }
    }

    IEnumerator FractureSheet()
    {
        cruise = 3.6f;
        for (int sheet = 0; sheet < 4; sheet++)
        {
            // The bow glides over the fracture it is about to open.
            bool shoulders = sheet % 2 == 1;
            float over = Lane(side * (sheet == 0 ? -.55f : sheet == 1 ? .35f : sheet == 2 ? .85f : -.2f));
            yield return Sail(new Vector3(over, Camera.UpLimit - 2.4f, 1), 3.6f);
            yield return PrimeAll(.45f);
            float bow = transform.position.x;
            LaySheet(Camera.UpLimit - 6.8f, shoulders ? bow - 1.7f : bow, shoulders ? bow + 1.7f : bow);
            yield return Wait(Rest(.7f));
            // Small fans from both shoulders freeze and re-aim around the pilot: an ice
            // crystal in the middle, round pellets either side.
            int pellets = Dense(3);
            for (int k = 0; k < pellets; k++)
            {
                UseTone(HostileTone.Bright);
                for (int port = 1; port <= 2; port++)
                {
                    var from = Muzzle(port);
                    var axis = CombatVisuals.Aim(from);
                    for (int lane = -1; lane <= 1; lane++)
                        (lane == 0 ? (CombatRound)Fire<CrystalRound>(from, axis, 2.5f, .2f) : Fire<SimpleRound>(from, CombatVisuals.Rotate(axis, lane * 14), 2.5f, .22f))
                            .Pause(.42f, .45f, CombatRound.Turn.Aim, lane * 9, 1.1f)
                            .Ramp(2.1f, .9f, .9f);
                }
                yield return Wait(Rest(.28f));
            }
            yield return Wait(Rest(.35f));
        }
    }

    // Two courses of dark ice frozen right across the arena from all three ports.
    // Columns beside a fracture are left out, so the crack reads before it opens. When
    // the sheet cracks, each floe takes its own heading: the lower course falls apart
    // either side of the fracture and gathers speed, while the upper course heaves up
    // over the top. Between two fractures the middle floe falls (and heaves) straight.
    void LaySheet(float depth, float leftCrack, float rightCrack)
    {
        UseTone(HostileTone.Dark);
        var edge = CombatRound.Border;
        int columns = Mathf.Max(8, Dense(22));
        const float At = .8f, Hold = .55f;
        for (int course = 0; course < 2; course++)
            for (int i = 0; i < columns; i++)
            {
                float x = Mathf.Lerp(edge.xMin + .3f, edge.xMax - .3f, (i + course * .5f) / (columns - .5f));
                if (Mathf.Abs(x - leftCrack) < .25f || Mathf.Abs(x - rightCrack) < .25f)
                    continue;
                int floe = x < leftCrack ? -1 : x > rightCrack ? 1 : 0;
                float lateral = x - transform.position.x;
                var port = Muzzle(lateral < -1.7f ? 1 : lateral > 1.7f ? 2 : 0);
                var point = new Vector2(x, depth + course * .62f);
                if (course == 0)
                    Place<SimpleRound>(port, point, At, .21f, out float launch)
                        .Pause(At, Hold, CombatRound.Turn.Down, floe * 22, 1.1f / launch)
                        .Ramp(2.6f, 1.5f, At + Hold);
                else
                    Place<NeedleRound>(port, point, At, .19f, out float launch)
                        .Pause(At, Hold + .2f, CombatRound.Turn.Down, floe == 0 ? 180 : floe * 152, 1.5f / launch)
                        .Ramp(1.6f, 1.2f, At + Hold + .2f);
            }
    }

    // ---- Phase two ----

    IEnumerator CutChannel()
    {
        float reach = CombatRound.Border.xMax - 1.25f;
        float top = Camera.UpLimit - 2.0f, low = Camera.UpLimit - 5.6f;
        yield return Sail(new Vector3(-side * reach, top, 1), 5.5f);
        yield return PrimeAll(.45f);
        // Down the Z, then back up its mirror image.
        yield return Cut(new Vector3(side * reach, top, 1));
        yield return Cut(new Vector3(-side * reach, low, 1));
        yield return Cut(new Vector3(side * reach, low, 1));
        yield return Wait(Rest(.4f));
        yield return Cut(new Vector3(-side * reach, low, 1));
        yield return Cut(new Vector3(side * reach, top, 1));
        yield return Cut(new Vector3(-side * reach, top, 1));
        cruise = Cruise;
    }

    // One straight leg of the channel, laying a pair of walls at an even spacing.
    IEnumerator Cut(Vector3 to)
    {
        station = to;
        cruise = 6;
        Vector2 direction = ((Vector2)(to - transform.position)).normalized;
        float interval = Rest(.13f), clock = interval;
        while (Vector2.Distance(transform.position, to) > .08f)
        {
            if (!Holding)
                clock += Time.deltaTime * LevelRun.EnemyTempoScale;
            if (clock >= interval)
            {
                clock -= interval;
                LayWalls(direction, channelStep++);
            }
            yield return null;
        }
    }

    int channelStep;
    const float WallAt = .3f, ZipHold = .75f;

    // Each wall freezes beside the lead, then the zip closes it: the wall turns back
    // across the channel, starting slow and accelerating, and carries on through it.
    // The wall on the pilot's side leaves the prow; the far wall the trailing shoulder.
    void LayWalls(Vector2 direction, int step)
    {
        int courses = Dense(2);
        for (int sign = -1; sign <= 1; sign += 2)
        {
            var normal = CombatVisuals.Rotate(direction, 90 * sign);
            bool prow = normal.y < 0;
            var port = Muzzle(prow ? 0 : direction.x >= 0 ? 1 : 2);
            for (int course = 0; course < courses; course++)
            {
                UseTone(HostileTone.Bright);
                float offset = (prow ? 2.1f : 1.3f) + course * .8f;
                var point = (Vector2)transform.position + normal * offset - direction * (course * .4f);
                if (course == 0)
                    Place<NeedleRound>(port, point, WallAt, .19f, out float launch)
                        .Pause(WallAt, ZipHold, CombatRound.Turn.Rotate, 180, .7f / launch)
                        .Ramp(3.6f, 1.3f, WallAt + ZipHold);
                else
                    Place<SimpleRound>(port, point, WallAt, .21f, out float launch)
                        .Pause(WallAt, ZipHold + .1f, CombatRound.Turn.Rotate, 180, .7f / launch)
                        .Ramp(3.6f, 1.3f, WallAt + ZipHold + .1f);
            }
        }
        if (step % 5 == 0)
        {
            UseTone(HostileTone.Dark);
            Fire<SimpleRound>(Muzzle(), CombatVisuals.Aim(Muzzle()), 2.6f, .22f)
                .Pause(.4f, .35f, CombatRound.Turn.Aim, 0, 1.2f)
                .Ramp(2.0f, .8f, .75f);
        }
    }

    IEnumerator CalvingBergs()
    {
        cruise = 2.2f;
        drifting = true;
        Phrase(Toned(HostileTone.Dark, PressureRidges()));
        for (int pair = 0; pair < 6; pair++)
        {
            yield return PrimePair(.4f);
            UseTone(HostileTone.Bright);
            int bergs = Dense(2);
            for (int mirror = -1; mirror <= 1; mirror += 2)
                for (int berg = 0; berg < bergs; berg++)
                    ThrowBerg(mirror, 34 + pair % 3 * 10 + berg * 22);
            yield return Wait(Rest(.9f));
        }
        drifting = false;
        cruise = Cruise;
        station = new Vector3(transform.position.x, Camera.UpLimit - 2.3f, 1);
    }

    // A berg thrown up and out under gravity. It rebounds once from a side wall and
    // calves at the top of its flight into a fan aimed at the pilot.
    void ThrowBerg(int mirror, float elevation)
    {
        const float Toss = 3.0f, Gravity = 1.7f;
        float apex = Toss * Mathf.Sin(elevation * Mathf.Deg2Rad) / Gravity;
        Fire<SimpleRound>(Muzzle(mirror < 0 ? 1 : 2), CombatVisuals.Rotate(Vector2.right * mirror, mirror * elevation), WorldSpeed(Toss), .36f)
            .Fall(new Vector2(0, -Gravity))
            .Bounce(1, false)
            .SplitAt(apex, Dense(7), 8, 2.4f, .17f, true);
    }

    // Mirrored arrows from the prow swing out and back through the centre line, so the
    // space under the bow is crossed too; each arrow starts slow and accelerates.
    IEnumerator PressureRidges()
    {
        for (int n = 0; drifting; n++)
        {
            float swing = -8 + 56 * (.5f + .5f * Mathf.Sin(n * .21f));
            for (int mirror = -1; mirror <= 1; mirror += 2)
                ScoredShot(n % 4 == 0, Muzzle(), CombatVisuals.Rotate(Vector2.down, mirror * swing), 1.0f)
                    .Ramp(3.4f, 1.2f, .35f);
            yield return Wait(Rest(.11f));
        }
    }

    // ---- Hard phase three ----

    // Icequake: each ram cracks a fracture fan and a fresh sheet beneath the ship. The
    // sheet's middle floe drops onto the pilot's line; the cracks open under both
    // shoulders, between the fracture lines. Then a zipped channel back to a top corner.
    IEnumerator Icequake()
    {
        float reach = CombatRound.Border.xMax - 1.25f;
        for (int quake = 0; quake < 2; quake++)
        {
            yield return LineUp(.55f);
            yield return Ram(Camera.UpLimit - 5.9f);
            float bow = transform.position.x;
            UseTone(HostileTone.Bright);
            FractureLines(Mathf.Max(2, Dense(8) & ~1), 180, false);
            LaySheet(transform.position.y - 3.6f, bow - 2.2f, bow + 2.2f);
            yield return Wait(Rest(.5f));
            int heading = bow > 0 ? -1 : 1;
            yield return Cut(new Vector3(heading * reach, Camera.UpLimit - 2.0f, 1));
            yield return Wait(Rest(.45f));
        }
        cruise = Cruise;
    }

    // ---- Helpers ----

    float PilotX
    {
        get
        {
            var pilot = CombatVisuals.Player;
            return pilot != null ? SpectralDecoy.AimPosition(pilot.position).x : 0;
        }
    }

    IEnumerator Sail(Vector3 to, float speed)
    {
        station = to;
        cruise = speed;
        while (Vector2.Distance(transform.position, to) > .08f)
            yield return null;
    }

    IEnumerator PrimeAll(float seconds)
    {
        for (float t = 0; t < seconds; t += Holding ? 0 : Time.deltaTime * LevelRun.EnemyTempoScale)
        {
            for (int port = 0; port < 3; port++)
                Cue(Muzzle(port), seconds - t);
            yield return null;
        }
    }

    // Launch a round from a port so it brakes to a stop exactly on a point of the arena
    // (the braking ramp covers half its distance). A later resume speed is relative to
    // the launch speed, so the caller divides by the returned launch value.
    T Place<T>(Vector3 port, Vector2 point, float at, float radius, out float launch) where T : CombatRound
    {
        var path = point - (Vector2)port;
        launch = Mathf.Max(.1f, WorldSpeed(path.magnitude / (at - Mathf.Min(.35f, at) * .5f)));
        return Fire<T>(port, path.sqrMagnitude > .0001f ? path.normalized : Vector2.down, launch, radius);
    }
}
