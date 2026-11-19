using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// City switchyard: an engine whose three signal heads control its own traffic. Each head
// sits above a gun (nose, left shoulder, right shoulder) and shows its signal. Before danger it
// blinks; at danger it stays lit and stops the cars it sent out; when it clears it flashes
// and the stopped cars set off on the route the signal sets. Every stopped car is a long
// round that turns to face its next heading while it waits, so the pilot can read it.
// Phase one, Red Signal: a head at danger freezes its rails where they are and sends
//   nothing more; the cars turn onto posted diagonals and leave in a ripple from the head.
// Phase two, Stop Line: a head at danger draws a stop line across the yard while its gun
//   keeps running; its cars queue along the line and leave as a row on the head's route,
//   across the yard from a shoulder or split down the middle from the nose.
// Final, Interlocking: the shoulder heads freeze and redirect their cars while the nose
//   head runs its stop line, and each aspect clears as the other goes to danger.
// Tones: phase one's nose is dark and its shoulders bright; phase two swaps them; the
// interlocking shoulders are bright and the nose dark. Running cars are green, braking
// approaches amber, and held cars red. Each turns green on its own departure beat.
[UnityEngine.Scripting.Preserve]
public sealed class CitySwitchyardBoss : ArmoredHullBoss
{
    static readonly Vector2[] shape = { new(-1,.86f),new(-.88f,1.2f),new(.84f,1.2f),new(1,.88f),new(1.55f,.68f),new(1.55f,.32f),
        new(1.26f,.27f),new(1.26f,-.46f),new(1,-.46f),new(1.05f,-1.2f),new(.66f,-1.30f),new(-.66f,-1.30f),new(-1.05f,-1.20f),
        new(-1,-.46f),new(-1.26f,-.46f),new(-1.26f,.29f),new(-1.32f,.33f),new(-1.32f,.66f) };
    protected override Vector2[] Outline => shape;
    protected override void Configure() { health = 2600; hullScale = 1.12f; attackInterval = .9f; }

    // Signal heads in hull space, one above each gun: nose, left shoulder, right shoulder.
    // The hull mesh carries their housings; the lenses are lit at runtime.
    public static readonly Vector3[] SignalHeads = { new Vector3(0, -.84f, -.42f), new Vector3(-1.12f, -.02f, -.42f), new Vector3(1.12f, -.02f, -.42f) };
    const int Nose = 0, Left = 1, Right = 2;
    static readonly Color Green = new Color(.15f, .94f, .49f);
    static readonly Color Amber = new Color(1f, .63f, .09f);
    static readonly Color Red = new Color(1f, .17f, .23f);

    protected override void MoveBoss(float dt)
    {
        // Brake into each signal box, then wait there for the timetable.
        float distance = Vector3.Distance(transform.position, station);
        transform.position = Vector3.MoveTowards(transform.position, station, dt * Mathf.Clamp(distance * 2.6f, .8f, 6.6f));
    }

    struct Car
    {
        public CombatRound round;
        public int serial;
        public bool Running => round != null && round.isActiveAndEnabled && round.Serial == serial;
        public static readonly System.Predicate<Car> Gone = car => !car.Running;
    }

    sealed class Signal
    {
        public int port;
        public HostileTone tone;
        public SpriteRenderer lens, glow;
        public LineRenderer line;
        public bool danger, warning, holdFire;
        public float flash;
        // Stop line height while at danger in the Stop Line aspect; NaN when there is none.
        public float stopLine = float.NaN, route;
        public bool splitRoute;
        public readonly List<Car> cars = new List<Car>();
        public readonly List<Car> held = new List<Car>();
    }

    readonly Signal[] signals = new Signal[3];

    protected override void Start()
    {
        base.Start();
        for (int i = 0; i < signals.Length; i++)
        {
            var s = new Signal { port = i };
            s.glow = ItemSpark.Sprite(Hull, "Signal glow", "standard", Color.white, .9f);
            s.glow.transform.localPosition = SignalHeads[i] + new Vector3(0, 0, -.05f);
            s.lens = ItemSpark.Sprite(Hull, "Signal lens", "standard", Color.white, .44f);
            s.lens.transform.localPosition = SignalHeads[i] + new Vector3(0, 0, -.1f);
            s.lens.sortingOrder = 24;
            s.line = CombatVisuals.Line(transform, Red, .045f);
            s.line.enabled = false;
            signals[i] = s;
            Paint(s);
        }
    }

    protected override void Update()
    {
        base.Update();
        foreach (var s in signals)
            if (s != null)
            {
                WatchStopLine(s);
                Paint(s);
            }
    }

    // The lens dims when clear, blinks before danger, stays lit at danger and flashes on clearing.
    void Paint(Signal s)
    {
        s.flash = Mathf.Max(0, s.flash - Time.deltaTime * 3);
        float pulse = .5f + .5f * Mathf.Sin(Time.time * 9);
        Color aspect = s.warning ? Amber : s.danger ? Red : Green;
        float lit = s.warning ? Mathf.Lerp(.45f, 1, pulse) : s.danger ? 1 : .74f;
        Color lens = aspect * Mathf.Min(1, lit + s.flash);
        lens.a = 1;
        s.lens.color = lens;
        s.glow.color = new Color(aspect.r, aspect.g, aspect.b, .18f * lit + s.flash * .32f);
        s.glow.transform.localScale = Vector3.one * (.75f + .25f * lit + .4f * s.flash);
        bool line = s.danger && !float.IsNaN(s.stopLine);
        s.line.enabled = line;
        if (line)
        {
            var c = Red;
            c.a = .23f + .12f * pulse;
            s.line.startColor = s.line.endColor = c;
            s.line.SetPosition(0, new Vector3(CombatRound.Border.xMin, s.stopLine, -.3f));
            s.line.SetPosition(1, new Vector3(CombatRound.Border.xMax, s.stopLine, -.3f));
        }
    }

    // A head at danger with a stop line halts each of its cars as it reaches the line.
    void WatchStopLine(Signal s)
    {
        if (!s.danger || float.IsNaN(s.stopLine))
            return;
        for (int i = s.cars.Count - 1; i >= 0; i--)
        {
            var car = s.cars[i];
            if (!car.Running)
            {
                s.cars.RemoveAt(i);
                continue;
            }
            if (car.round.transform.position.y > s.stopLine)
                continue;
            float route = s.splitRoute && s.held.Count % 2 == 1 ? -s.route : s.route;
            car.round.SetAccent(Red);
            car.round.Halt(.05f, CombatRound.Turn.Down, route, 1.05f);
            s.held.Add(car);
            s.cars.RemoveAt(i);
        }
    }

    protected override IEnumerator Cards()
    {
        ResetSignals();
        if (phaseThree)
            return Interlocking();
        if (phaseTwo)
            return StopLines();
        return RedSignals();
    }

    void ResetSignals()
    {
        foreach (var s in signals)
            if (s != null)
            {
                s.danger = s.warning = s.holdFire = false;
                s.stopLine = float.NaN;
                s.cars.Clear();
                s.held.Clear();
            }
    }

    // Each gun runs its rails on a steady beat; a head holding fire sends nothing.
    IEnumerator Rails(float interval, float[] nose, float[] shoulder, float speed, bool ceiling)
    {
        for (int beat = 0; ; beat++)
        {
            float sway = 8 * Mathf.Sin(beat * interval * .7f);
            foreach (var s in signals)
            {
                s.cars.RemoveAll(Car.Gone);
                if (s.holdFire || (beat % 2 == 0) != (s.port == Nose))
                    continue;
                UseTone(s.tone);
                var port = Muzzle(s.port);
                var angles = s.port == Nose ? nose : shoulder;
                int sign = s.port == Left ? -1 : 1;
                for (int i = 0; i < angles.Length; i++)
                {
                    float angle = s.port == Nose ? angles[i] + sway : sign * angles[i];
                    var direction = CombatVisuals.Rotate(Vector2.down, angle);
                    // Every sixth beat a kunai leads the nose train.
                    var car = beat % 6 == 0 && i == 0 ? Fire<KunaiRound>(port, direction, speed + .5f, .2f) : (CombatRound)Fire<NeedleRound>(port, direction, speed, .19f);
                    if (car == null)
                        continue;
                    car.SetAccent(s.warning || s.danger ? Amber : Green);
                    car.Bounce(1, ceiling);
                    s.cars.Add(new Car { round = car, serial = car.Serial });
                }
            }
            yield return Wait(Rest(interval));
        }
    }

    static float[] Spread(int count, float span)
    {
        var angles = new float[count];
        for (int i = 0; i < count; i++)
            angles[i] = count == 1 ? 0 : (i / (count - 1f) - .5f) * span;
        return angles;
    }

    static float[] Range(int count, float from, float to)
    {
        var angles = new float[count];
        for (int i = 0; i < count; i++)
            angles[i] = count == 1 ? from : Mathf.Lerp(from, to, i / (count - 1f));
        return angles;
    }

    // The head blinks before it goes to danger.
    IEnumerator Warn(params Signal[] heads)
    {
        foreach (var s in heads)
        {
            s.warning = true;
            ColorRunning(s, Amber);
        }
        for (float t = 0; t < .45f;)
        {
            if (!Holding)
                t += Time.deltaTime * LevelRun.EnemyTempoScale;
            foreach (var s in heads)
                Cue(Muzzle(s.port), .45f - t);
            yield return null;
        }
        foreach (var s in heads)
            s.warning = false;
    }

    static void ColorRunning(Signal s, Color color)
    {
        foreach (var car in s.cars)
            if (car.Running)
                car.round.SetAccent(color);
    }

    void Freeze(Signal s, float angle)
    {
        int lane = 0;
        foreach (var car in s.cars)
        {
            if (!car.Running) continue;
            if (!InArena(car.round.transform.position))
            {
                car.round.SetAccent(Green);
                continue;
            }
            // Shoulders cross inward. The nose forks into two opposing diagonals.
            float route = s.port == Left ? angle : s.port == Right ? -angle : (lane++ % 2 == 0 ? angle : -angle);
            car.round.SetAccent(Red);
            car.round.Halt(.3f, CombatRound.Turn.Down, route, 1.08f);
            s.held.Add(car);
        }
        s.cars.Clear();
    }

    static bool InArena(Vector3 p) => p.y > Camera.DownLimit + 1.2f && p.y < Camera.UpLimit && p.x > Camera.LeftLimit && p.x < Camera.RightLimit;

    // The head clears with a flash, and its stopped cars set off in a ripple spreading from it.
    IEnumerator Clear(Signal s, float ripple)
    {
        s.danger = false;
        s.stopLine = float.NaN;
        s.flash = 1;
        ColorRunning(s, Green);
        var head = (Vector2)Hull.TransformPoint(SignalHeads[s.port]);
        var queue = new List<Car>(s.held);
        s.held.Clear();
        queue.Sort((a, b) => Distance(a, head).CompareTo(Distance(b, head)));
        int released = 0;
        for (float t = 0; released < queue.Count;)
        {
            if (!Holding)
                t += Time.deltaTime * LevelRun.EnemyTempoScale;
            int upTo = Mathf.Min(queue.Count, Mathf.CeilToInt(queue.Count * t / ripple));
            for (; released < upTo; released++)
                if (queue[released].Running)
                {
                    queue[released].round.SetAccent(Green);
                    queue[released].round.Proceed();
                }
            yield return null;
        }
    }

    static float Distance(Car car, Vector2 point) => car.round != null ? ((Vector2)car.round.transform.position - point).sqrMagnitude : 0;

    // A head at danger blinks, holds its gun, posts its route, waits and clears.
    IEnumerator RedAspect(float hold, float routeAngle, params Signal[] heads)
    {
        yield return Warn(heads);
        for (int i = 0; i < heads.Length; i++)
        {
            heads[i].danger = heads[i].holdFire = true;
            Freeze(heads[i], routeAngle);
        }
        yield return Wait(hold);
        foreach (var s in heads)
        {
            s.holdFire = false;
            Phrase(Clear(s, .75f));
        }
    }

    // Red Signal (phase one), in four movements of four steps each: one head at a time, the
    // shoulders together before the nose, paired diagonals opening a V, and a quick chase.
    IEnumerator RedSignals()
    {
        signals[Nose].tone = HostileTone.Dark;
        signals[Left].tone = HostileTone.Bright;
        signals[Right].tone = HostileTone.Bright;
        yield return PrimePair(.5f);
        Phrase(Rails(.28f, Spread(Dense(3), 56), Range(Dense(2), 23, 63), 3.0f, true));
        yield return Wait(Rest(.7f));
        for (int step = 0; ; step++)
        {
            int movement = step / 4 % 4;
            station.x = Lane(step % 2 == 0 ? -.45f : .45f);
            station.y = Camera.UpLimit - 2.4f;
            var l = signals[Left];
            var n = signals[Nose];
            var r = signals[Right];
            switch (movement)
            {
                case 0:
                    yield return RedAspect(1.0f, 32, step % 3 == 0 ? l : step % 3 == 1 ? n : r);
                    break;
                case 1:
                    yield return RedAspect(1.1f, 43, step % 2 == 0 ? new[] { l, r } : new[] { n });
                    break;
                case 2:
                    yield return RedAspect(1.0f, 26, step % 2 == 0 ? r : l);
                    break;
                default:
                    yield return RedAspect(.8f, 36, step % 3 == 0 ? l : step % 3 == 1 ? n : r);
                    break;
            }
            yield return Wait(Rest(movement == 3 ? .3f : .55f));
        }
    }

    // A head at danger draws its stop line; its gun keeps running and the queue builds on the
    // line, then leaves as a row on the head's route when it clears.
    IEnumerator StopAspect(Signal s, float height, float route, bool split, float hold)
    {
        yield return Warn(s);
        s.route = route;
        s.splitRoute = split;
        // Cars already through the junction keep running when the light changes.
        for (int i = s.cars.Count - 1; i >= 0; i--)
        {
            var car = s.cars[i];
            if (!car.Running || car.round.transform.position.y >= height) continue;
            car.round.SetAccent(Green);
            s.cars.RemoveAt(i);
        }
        s.stopLine = height;
        s.danger = true;
        yield return Wait(hold);
        yield return Clear(s, .65f);
    }

    // Stop Line (phase two), in four movements of four steps each: single heads at changing
    // heights, both shoulders as crossing rows, the nose splitting its row under a shoulder
    // row, and lines that step lower each time.
    IEnumerator StopLines()
    {
        signals[Nose].tone = HostileTone.Bright;
        signals[Left].tone = HostileTone.Dark;
        signals[Right].tone = HostileTone.Dark;
        yield return PrimePair(.5f);
        Phrase(Rails(.27f, Spread(Dense(3), 64), Range(Dense(2), 14, 52), 3.0f, false));
        float middle = (Camera.UpLimit + Camera.DownLimit) * .5f;
        float[] heights = { middle + 2.6f, middle + 1.0f, middle + 1.8f, middle - .2f };
        for (int step = 0; ; step++)
        {
            int movement = step / 4 % 4;
            station.x = Lane(step % 2 == 0 ? .45f : -.45f);
            station.y = Camera.UpLimit - 2.3f;
            float height = heights[step % heights.Length];
            var l = signals[Left];
            var n = signals[Nose];
            var r = signals[Right];
            switch (movement)
            {
                case 0:
                    var head = step % 3 == 0 ? l : step % 3 == 1 ? r : n;
                    yield return StopAspect(head, height, head == l ? 62 : head == r ? -62 : 28, head == n, 1.5f);
                    break;
                case 1:
                    Phrase(StopAspect(l, height, 62, false, 1.5f));
                    yield return StopAspect(r, height - .8f, -62, false, 1.5f);
                    break;
                case 2:
                    Phrase(StopAspect(step % 2 == 0 ? l : r, height + .6f, step % 2 == 0 ? 62 : -62, false, 1.6f));
                    yield return StopAspect(n, height - 1.0f, 28, true, 1.6f);
                    break;
                default:
                    yield return StopAspect(step % 2 == 0 ? r : l, middle + 2.4f - (step % 4) * .9f, step % 2 == 0 ? -62 : 62, false, 1.2f);
                    break;
            }
            yield return Wait(Rest(.45f));
        }
    }

    // Interlocking (Hard final): the shoulder heads go to danger together, freezing their cars
    // onto crossing diagonals, while the nose clears its queue; then the nose draws its stop line as
    // the shoulders clear. The stop line steps through four heights and the shoulder release
    // alternates shallow and steep crossings.
    IEnumerator Interlocking()
    {
        signals[Nose].tone = HostileTone.Dark;
        signals[Left].tone = HostileTone.Bright;
        signals[Right].tone = HostileTone.Bright;
        yield return PrimePair(.5f);
        Phrase(Rails(.24f, Spread(Dense(4), 66), Range(Dense(3), 21, 67), 3.1f, true));
        float middle = (Camera.UpLimit + Camera.DownLimit) * .5f;
        float[] heights = { middle + 2.2f, middle + .8f, middle + 1.6f, middle + .2f };
        var l = signals[Left];
        var n = signals[Nose];
        var r = signals[Right];
        for (int step = 0; ; step++)
        {
            station.x = Lane((step % 3 - 1) * .5f);
            station.y = Camera.UpLimit - 2.4f;
            Phrase(StopAspect(n, heights[step % heights.Length], 26, true, 1.3f));
            yield return Wait(.6f);
            yield return RedAspect(1.0f, step % 2 == 0 ? 29 : 46, l, r);
            yield return Wait(Rest(.35f));
        }
    }
}

public static partial class IndustrialMeshFactory
{
    static Mesh switchyardHull, switchyardBody;

    // The Switchyard engine's baked hull with three large hooded signal heads, one above each
    // gun: a dark backplate, a bright bezel, a dark lens recess and a visor, on a bracket.
    public static Mesh SwitchyardSignalHull(Mesh body)
    {
        if (switchyardHull != null && switchyardBody == body)
            return switchyardHull;
        var b = new Builder();
        foreach (var head in CitySwitchyardBoss.SignalHeads)
        {
            b.Drum(head + new Vector3(0, 0, .08f), .36f, .1f, 11, 10);
            b.Drum(head, .3f, .07f, 10, 10);
            b.Drum(head + new Vector3(0, 0, -.04f), .23f, .05f, 9, 10);
            b.Box(head + new Vector3(0, .32f, -.1f), new Vector3(.66f, .09f, .22f), 11);
            foreach (int side in new[] { -1, 1 })
                b.Box(head + new Vector3(side * .33f, .17f, -.08f), new Vector3(.07f, .3f, .16f), 11);
            b.Box(head + new Vector3(0, .5f, .06f), new Vector3(.14f, .2f, .12f), 6);
        }
        var heads = b.Finish("Switchyard signal heads");
        var combined = new Mesh { name = "Switchyard engine with signal heads", indexFormat = body.indexFormat };
        combined.CombineMeshes(new[] { new CombineInstance { mesh = body }, new CombineInstance { mesh = heads } }, true, false);
        switchyardBody = body;
        return switchyardHull = combined;
    }
}
