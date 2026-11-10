using System.Collections;
using UnityEngine;

// Candy regent: one concept per phase (Miguel, 10 November 2026), all about its
// attention. The eye snaps to one thing at a time and the lollipop wands swing to point
// at it; bursts of excitement (the crown shivers, the heart races) alternate with calm
// moments when the eye daydreams. Hearts are its signature. Every shape is a line or an
// outline with open lanes, never a scatter. No minions; it stays in the upper arena.
// Phase one, Crush: the regent fixates. The eye snaps to a target (the pilot, an upper
//   corner, a wall, the ceiling, a lower corner, usually followed by its mirror image)
//   and both wands pour a ribbon at it: an arrow line flanked alternately by hearts and
//   circles. Walls and the ceiling rebound the ribbons. After each fixation the crown
//   shivers and throws a mirrored fan of stars and hearts off the ceiling, then it calms
//   for a moment. Its attention span shrinks as the phase goes on, and every fifth
//   fixation is a double take: a glance at the mirror image and straight back.
// Phase two, Heartbeat: lub-dub. On each beat the heart throbs and a heart outline grows
//   from the eye and sinks, followed by a smaller, faster one. Each outline is open where
//   the eye is looking, so the way through moves with the glance. Every fourth beat is a
//   flutter: four quick small hearts with the opening spinning round, then a long calm.
// Phase three (Hard), Sugar Crash: a rush, then a crash. The eye flicks between targets
//   ever faster while the wands spray short ribbons at each one, weaving a web across the
//   arena. On one shared beat the whole web stops dead and hangs; the regent slumps; then
//   every line droops away downward together and speeds up. A recovery heart follows.
//   Each rush has more flicks than the last.
// Tones: Crush alternates bright and dark ribbons fixation by fixation with a bright crown
//   burst; Heartbeat is a bright lub and a dark dub; Sugar Crash is a dark web and a
//   bright recovery heart.
public sealed partial class CandyRegentBoss
{
    protected override void Attack()
    {
        if (arrived)
            OpenCards();
    }

    protected override IEnumerator Cards()
    {
        int verse = 0;
        while (true)
        {
            if (phaseThree)
                yield return SugarCrash(verse++);
            else if (!phaseTwo)
                yield return Crush(verse++);
            else
                yield return Heartbeat(verse++);
        }
    }

    // What the regent fixates on, in arena coordinates (-1 to 1), mirror images following
    // one another. NaN is the pilot.
    static readonly Vector2[] Sights =
    {
        new(float.NaN, 0), new(-1, .9f), new(1, .9f), new(float.NaN, 0), new(-1, .15f),
        new(1, .15f), new(0, 1), new(float.NaN, 0), new(-.55f, -1), new(.55f, -1),
    };

    // A point to fixate on. Walls and the ceiling rebound the ribbons; the floor and the
    // pilot do not.
    Vector3 Sight(int k, out bool rebound)
    {
        var s = Sights[(k % Sights.Length + Sights.Length) % Sights.Length];
        rebound = !float.IsNaN(s.x) && s.y > -1;
        if (float.IsNaN(s.x))
            return player != null ? player.position : new Vector3(transform.position.x, Camera.DownLimit, 0);
        var edge = CombatRound.Border;
        return new Vector3(Mathf.Lerp(edge.xMin, edge.xMax, (s.x + 1) * .5f), Mathf.Lerp(edge.yMin, edge.yMax, (s.y + 1) * .5f), 0);
    }

    static HostileTone Opposite(HostileTone shade) => shade == HostileTone.Bright ? HostileTone.Dark : HostileTone.Bright;

    IEnumerator ChargeWands(float seconds)
    {
        for (float t = 0; t < seconds;)
        {
            if (!Holding)
                t += Time.deltaTime * LevelRun.EnemyTempoScale;
            Cue(WandPort(-1), seconds - t);
            Cue(WandPort(1), seconds - t);
            yield return null;
        }
    }

    // ---------------------------------------------------------------- Crush

    IEnumerator Crush(int k)
    {
        var sight = Sight(k, out bool rebound);
        var shade = k % 2 == 0 ? HostileTone.Bright : HostileTone.Dark;
        Fixate(sight);
        yield return ChargeWands(.3f);
        if (k % 5 == 4)
        {
            // Double take: a glance at the mirror image and straight back.
            yield return Stream(sight, rebound, .35f, shade);
            var mirror = new Vector3(-sight.x, sight.y, 0);
            Fixate(mirror);
            yield return Stream(mirror, rebound, .35f, Opposite(shade));
            Fixate(sight);
            yield return Stream(sight, rebound, .45f, shade);
        }
        else
            // The attention span shrinks as the phase goes on.
            yield return Stream(sight, rebound, Mathf.Max(.7f, 1.1f - k * .02f), shade);
        CrownBurst(k);
        LookAway();
        yield return Wait(Rest(.4f));
    }

    // Both wands pour a ribbon at the fixation: an arrow line with a heart on one flank or
    // a circle on the other, alternating beat by beat.
    IEnumerator Stream(Vector3 target, bool rebound, float seconds, HostileTone shade)
    {
        int beats = Mathf.Max(1, Mathf.RoundToInt(seconds / .07f));
        for (int n = 0; n < beats; n++)
        {
            UseTone(shade);
            for (int side = -1; side <= 1; side += 2)
            {
                var port = WandPort(side);
                var axis = Toward(port, target);
                Ribboned(Fire<NeedleRound>(port, axis, 3.6f, .19f), rebound, -1, 0);
                if (n % 2 == 0)
                    Ribboned(Fire<HeartRound>(port, CombatVisuals.Rotate(axis, side * 7), 3.3f, .19f), rebound, -1, 0);
                else
                    Ribboned(Fire<SimpleRound>(port, CombatVisuals.Rotate(axis, -side * 7), 3.3f, .16f), rebound, -1, 0);
            }
            yield return Wait(Rest(.07f));
        }
    }

    static Vector2 Toward(Vector3 from, Vector3 to)
    {
        Vector2 axis = to - from;
        return axis.sqrMagnitude > .0001f ? axis.normalized : Vector2.down;
    }

    // Rebounds off the walls and ceiling when the fixation lies there; in a rush, stops
    // dead on the shared crash beat, hangs, then droops away downward and speeds up.
    static void Ribboned(CombatRound round, bool rebound, float stopAt, float lean)
    {
        if (rebound)
            round.Bounce(2);
        if (stopAt > 0)
            round.Pause(stopAt, .7f, CombatRound.Turn.Down, lean, .45f).Ramp(1.8f, 1, stopAt + .9f);
    }

    // An excited shiver of the crown: a mirrored fan of stars and hearts thrown up off the
    // ceiling.
    void CrownBurst(int n)
    {
        Thrill();
        int count = Dense(3);
        UseTone(HostileTone.Bright);
        for (int i = 0; i < count; i++)
            for (int mirror = -1; mirror <= 1; mirror += 2)
            {
                var axis = CombatVisuals.Rotate(Vector2.up, -mirror * (18 + i * 20 + (n % 2) * 10));
                if (i % 2 == 0)
                    Fire<StarRound>(CrownPort(mirror), axis, 3.0f, .19f).Bounce(2);
                else
                    Fire<HeartRound>(CrownPort(mirror), axis, 3.0f, .19f).Bounce(2);
            }
    }

    // ---------------------------------------------------------------- Heartbeat

    // Where the eye glances on successive beats, in degrees from straight down.
    static readonly float[] Glances = { 0, -55, 35, -20, 70, -75, 15, 50 };

    IEnumerator Heartbeat(int k)
    {
        float glance = Glances[k % Glances.Length] * (k / Glances.Length % 2 == 0 ? 1 : -1);
        if (k % 4 == 3)
        {
            // Flutter: four quick small hearts, the opening spinning round, then a long calm.
            Thrill();
            for (int i = 0; i < 4; i++)
            {
                float turn = glance + i * 90;
                Fixate(EyePort + (Vector3)(CombatVisuals.Rotate(Vector2.down, turn) * 4));
                Beat();
                UseTone(i % 2 == 0 ? HostileTone.Bright : HostileTone.Dark);
                HeartWave(EyePort, Dense(20), 2.0f + i * .35f, .9f, turn, 26);
                yield return Wait(Rest(.18f));
            }
            LookAway();
            yield return Wait(Rest(1.2f));
            yield break;
        }
        Fixate(EyePort + (Vector3)(CombatVisuals.Rotate(Vector2.down, glance) * 4));
        yield return Charge(pupil, Vector3.zero, .35f);
        Beat();
        UseTone(HostileTone.Bright);
        HeartWave(EyePort, Dense(40), 2.5f, .8f, glance, 22);
        yield return Wait(.3f);
        Beat();
        UseTone(HostileTone.Dark);
        HeartWave(EyePort, Dense(28), 3.1f, 1.0f, glance, 26);
        LookAway();
        yield return Wait(Rest(.9f));
    }

    // The outline of a heart grows from the eye as it sinks: each bead's velocity is its
    // point on the curve plus a shared downward drift. Arrows draw the line with an upright
    // heart every fourth bead. The beads facing the glance are left out: the opening.
    void HeartWave(Vector3 port, int beads, float growth, float sink, float glance, float opening)
    {
        var shade = tone;
        for (int i = 0; i < beads; i++)
        {
            float a = (i + .5f) / beads * Mathf.PI * 2;
            float s = Mathf.Sin(a);
            var shape = new Vector2(16 * s * s * s,
                13 * Mathf.Cos(a) - 5 * Mathf.Cos(2 * a) - 2 * Mathf.Cos(3 * a) - Mathf.Cos(4 * a)) / 17f;
            if (Mathf.Abs(Mathf.DeltaAngle(Vector2.SignedAngle(Vector2.down, shape), glance)) < opening)
                continue;
            Vector2 velocity = shape * growth + Vector2.down * sink;
            UseTone(shade);
            if (i % 4 == 0)
                Fire<HeartRound>(port, velocity, velocity.magnitude, .19f);
            else
                Fire<NeedleRound>(port, velocity, velocity.magnitude, .18f);
        }
    }

    // ---------------------------------------------------------------- Sugar Crash

    IEnumerator SugarCrash(int k)
    {
        // Each rush has more flicks than the last.
        int flicks = Mathf.Min(8, 5 + k / 2);
        const int beats = 6;
        float tick = Rest(.06f);
        float tempo = Mathf.Max(.01f, LevelRun.EnemyTempoScale);
        int total = flicks * beats;
        float lean = (k % 2 == 0 ? 1 : -1) * 14;
        Thrill();
        yield return ChargeWands(.3f);
        int n = 0;
        for (int f = 0; f < flicks; f++)
        {
            var sight = Sight(k * 3 + f, out bool rebound);
            Fixate(sight);
            for (int b = 0; b < beats; b++, n++)
            {
                // Every round of the rush stops on one shared beat: the crash.
                float stopAt = ((total - n) * tick + .15f) / tempo;
                UseTone(HostileTone.Dark);
                for (int side = -1; side <= 1; side += 2)
                {
                    var port = WandPort(side);
                    var axis = Toward(port, sight);
                    Ribboned(Fire<NeedleRound>(port, axis, 2.6f, .19f), rebound, stopAt, lean);
                    Ribboned(Fire<HeartRound>(port, CombatVisuals.Rotate(axis, side * 8), 2.4f, .19f), rebound, stopAt, lean);
                    Ribboned(Fire<SimpleRound>(port, CombatVisuals.Rotate(axis, -side * 8), 2.4f, .16f), rebound, stopAt, lean);
                }
                yield return Wait(tick);
            }
        }
        // The crash: the web hangs while the regent slumps, then droops away.
        LookAway();
        slump = 1;
        yield return Wait(.85f);
        yield return Wait(Rest(.5f));
        // Recovery: the heart starts again.
        slump = 0;
        Fixate(EyePort + Vector3.down * 4);
        yield return Charge(pupil, Vector3.zero, .3f);
        Beat();
        UseTone(HostileTone.Bright);
        HeartWave(EyePort, Dense(34), 2.6f, .8f, k * 70 % 360 - 180, 22);
        LookAway();
        yield return Wait(Rest(.4f));
    }
}
