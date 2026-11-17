using System.Collections;
using UnityEngine;

public sealed class MushroomWarden : DistrictBoss
{
    MushroomMothRig moth;
    Vector3 glideStart, glideEnd;
    float glideTime, glideDuration, dip, fold = .72f, wantedFold = .72f;

    protected override void Configure()
    {
        health = 1980;
        hullScale = .92f;
        attackInterval = .28f;
        nextAttack = 1.5f;
    }

    // Bind before the hull, hit feedback and death sequence cache their renderers.
    internal void BindWings()
    {
        moth = MushroomMothRig.Attach(Hull);
        for (int i = 0; i < 3; i++)
            firingCue.FollowPort(i, moth.Muzzle(i));
        moth.Pose(fold);
    }

    protected override void Start()
    {
        base.Start();
        SporeCloud.Dissipate();
        station = new Vector3(0, Camera.UpLimit - 2.75f, 1);
    }

    protected override void OnPhaseEntered(int phase)
    {
        glideDuration = 0;
        wantedFold = .72f;
    }

    void Glide(float x, float height, float depth, float seconds)
    {
        glideStart = transform.position;
        glideEnd = station + new Vector3(x, height, 0);
        glideTime = 0;
        glideDuration = seconds;
        dip = depth;
    }

    protected override void MoveBoss(float dt)
    {
        if (!openingComplete)
            transform.position = Vector3.MoveTowards(transform.position, station, dt * 5.3f);
        else if (glideTime < glideDuration)
        {
            glideTime = Mathf.Min(glideTime + dt, glideDuration);
            float t = Mathf.SmoothStep(0, 1, glideTime / glideDuration);
            transform.position = Vector3.Lerp(glideStart, glideEnd, t) + Vector3.down * (dip * Mathf.Sin(t * Mathf.PI));
        }
        float idleBeat = .055f * Mathf.Sin(motionAge * 4.1f);
        fold = Mathf.MoveTowards(fold, Mathf.Clamp01(wantedFold + idleBeat), dt * (wantedFold < fold ? 5.5f : 2.1f));
        moth.Pose(fold);
    }

    protected override void Attack()
    {
        if (openingComplete) OpenCards();
    }

    protected override IEnumerator Cards() => phaseThree ? LanternRun() : phaseTwo ? SporePrint() : Eyespots();

    IEnumerator RaiseWings(float duration)
    {
        wantedFold = .86f;
        for (float t = 0; t < duration;)
        {
            Cue(moth.Muzzle(0).position, duration - t);
            Cue(moth.Muzzle(1).position, duration - t);
            if (!Holding) t += Time.deltaTime * LevelRun.EnemyTempoScale;
            yield return null;
        }
        wantedFold = 0;
        // Let the opening stroke carry the guns before the first round departs.
        yield return Wait(.11f);
    }

    // The bright forewings and dark hindwings share one pinched waist. The entire
    // silhouette is drawn in velocity space, so it opens as it floats downfield.
    IEnumerator Eyespots()
    {
        for (int beat = 0; ; beat++)
        {
            int mirror = (beat & 1) == 0 ? 1 : -1;
            float lean = 12 * Mathf.Sin(beat * .86f);
            Glide(mirror * 1.18f, -.12f, .66f, Rest(1.92f));
            yield return RaiseWings(.52f);
            for (int wing = 0; wing < 2; wing++)
            {
                int side = wing * 2 - 1;
                Vector3 port = moth.Muzzle(wing).position;
                for (int lobe = 0; lobe < 2; lobe++)
                {
                    bool upper = lobe == 0;
                    UseTone(upper ? HostileTone.Bright : HostileTone.Dark);
                    int count = Mathf.Max(8, Dense(upper ? 29 : 21));
                    for (int i = 0; i < count; i++)
                    {
                        float a = i * (Mathf.PI * 2 / count);
                        float reach = (1 - Mathf.Cos(a)) * .5f;
                        float edge = Mathf.Sin(a) * (upper ? .81f : .58f);
                        var v = new Vector2(side * (-.68f + reach * (upper ? 2.28f : 1.32f)),
                            -3.63f + reach * (upper ? .94f : -1.14f) + edge);
                        v = CombatVisuals.Rotate(v, lean);
                        Fire<SimpleRound>(port, v, v.magnitude, upper ? .17f : .15f, i == 0 && upper);
                    }
                }
            }
            // An inset pair of pupils hangs inside the outlines on alternate claps.
            if ((beat & 1) != 0)
            {
                yield return Wait(Rest(.18f));
                UseTone(HostileTone.Bright);
                for (int wing = 0; wing < 2; wing++)
                {
                    int side = wing * 2 - 1;
                    Vector2 axis = CombatVisuals.Rotate(new Vector2(side * .53f, -3.15f), lean);
                    Fire<SimpleRound>(moth.Muzzle(wing).position, axis, axis.magnitude, .29f);
                }
            }
            yield return Wait(Rest(1.08f));
        }
    }

    // Four ribs of a spore print. A ripple moves from stem to rim, but all the ribs
    // share the same falling direction so their missing sections form a traversable slit.
    IEnumerator SporePrint()
    {
        for (int beat = 0; ; beat++)
        {
            float swing = Mathf.Sin(beat * .81f);
            float slit = 2.0f * swing;
            float wind = 5f * Mathf.Cos(beat * .81f);
            Glide(-swing * 1.29f, .12f, .34f, 1.65f);
            yield return RaiseWings(.50f);
            int lanes = Mathf.Max(9, Dense(27));
            float halfWidth = (Camera.RightLimit - Camera.LeftLimit) * .5f - .18f;
            for (int rib = 0; rib < 4; rib++)
            {
                UseTone((rib & 1) == 0 ? HostileTone.Bright : HostileTone.Dark);
                for (int i = 0; i < lanes; i++)
                {
                    float x = Mathf.Lerp(-halfWidth, halfWidth, (i + .5f) / lanes);
                    if (Mathf.Abs(x - slit) < .72f) continue;
                    int wing = x < slit ? 0 : 1;
                    Vector3 port = moth.Muzzle(wing).position;
                    float span = wing == 0 ? halfWidth + slit : halfWidth - slit;
                    float along = Mathf.Abs(x - slit) / span;
                    float arch = Mathf.Sin(along * Mathf.PI);
                    float y = station.y + .95f - .31f * rib + arch * (1.22f - .53f * rib) - .9f * along;
                    Vector2 offset = new Vector2(x, y) - (Vector2)port;
                    const float travel = .85f;
                    float speed = WorldSpeed(Mathf.Max(.15f, offset.magnitude / (travel - .175f)));
                    Fire<NeedleRound>(port, offset, speed, .15f, i == 0 || i == lanes - 1)
                        .Pause(travel, .43f + .50f * along, CombatRound.Turn.Down, wind, 4.65f / speed)
                        .ShowTurn();
                }
                if (rib < 3) yield return Wait(.13f);
            }
            yield return Wait(Rest(1.18f));
        }
    }

    // The bank draws two scalloped garlands. Each hanging lantern marks a crest.
    // The nose locks its aim before the silk opens on either side of that bearing.
    IEnumerator LanternRun()
    {
        for (int pass = 0; ; pass++)
        {
            int sign = (pass & 1) == 0 ? 1 : -1;
            yield return RaiseWings(.48f);
            Glide(sign * 2.18f, -.14f, 3.22f, 2.16f);
            int strokes = Mathf.Max(9, Dense(23));
            int tines = Mathf.Max(3, Dense(5));
            float opening = .5f + .16f * Mathf.Sin(pass * 1.37f);
            for (int stroke = 0; stroke < strokes; stroke++)
            {
                float t = (stroke + .5f) / strokes;
                while (Holding || glideTime < t * glideDuration) yield return null;
                if (Mathf.Abs(t - opening) < .092f) continue;
                float scallop = Mathf.Cos(t * Mathf.PI * 4);
                for (int wing = 0; wing < 2; wing++)
                {
                    int side = wing * 2 - 1;
                    UseTone(wing == (pass & 1) ? HostileTone.Bright : HostileTone.Dark);
                    var port = moth.Muzzle(wing).position;
                    for (int k = 0; k < tines; k++)
                    {
                        float tooth = k / (tines - 1f);
                        Vector2 axis = new Vector2(side * (.22f + .85f * tooth + .20f * scallop), 1);
                        Fire<NeedleRound>(port, axis, 2.9f + tooth + .70f * scallop, .145f, k == 0)
                            .Pause(.59f, .40f + .31f * (1 - t), CombatRound.Turn.Down,
                                side * (9 + 12 * scallop + 8 * tooth), 1.32f).ShowTurn();
                    }
                    if (stroke % 5 == 0)
                        Fire<SimpleRound>(port, new Vector2(side * .54f, .93f), 2.4f, .30f, false)
                            .Fall(new Vector2(-side * .19f, -2.12f));
                }
            }
            var nose = moth.Muzzle(2);
            Vector2 lockOn = CombatVisuals.Aim(nose.position);
            wantedFold = .73f;
            PointWeapon(nose.position, lockOn);
            yield return Charge(nose, Vector3.zero, .53f);
            UseTone(HostileTone.Bright);
            int stitches = Mathf.Max(3, Dense(9));
            for (int i = 0; i < stitches; i++)
            {
                float t = i / (stitches - 1f);
                float flare = 7 + 22 * Mathf.Sin(t * Mathf.PI);
                for (int side = -1; side <= 1; side += 2)
                    Fire<SimpleRound>(nose.position, CombatVisuals.Rotate(lockOn, side * flare),
                        5.45f - .8f * t, .18f, side < 0);
                yield return Wait(.067f);
            }
            yield return Wait(Rest(.38f));
        }
    }
}

public sealed class DreamPainter : DistrictBoss
{
    readonly Transform[] hands = new Transform[2];
    static Vector3[] Fingers => IndustrialMeshFactory.DreamNibs;
    float handAge, turnStart, turnClock;
    bool turning;
    const float SwapSeconds = 1.36f;
    protected override bool RepositionsOnPhase => false;
    protected override void Configure() { health = 3060; hullScale = .97f; attackInterval = .24f; nextAttack = 1.5f; }

    protected override void Start()
    {
        base.Start();
        station = new Vector3(0, Camera.UpLimit - 3.45f, 1);
        for (int i = 0; i < hands.Length; i++)
        {
            var hand = new GameObject(i == 0 ? "Left painted hand" : "Right painted hand").transform;
            hand.SetParent(Hull, false);
            hand.localPosition = new Vector3(i == 0 ? -2.15f : 2.15f, .41f, -.20f);
            hand.localScale = new Vector3(i == 0 ? 1.18f : -1.18f, 1.18f, 1.18f);
            hand.gameObject.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.DreamHandMesh();
            var renderer = hand.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Hull.GetComponent<MeshRenderer>().sharedMaterial;
            renderer.sortingLayerName = "Enemy";
            foreach (var finger in Fingers)
                firingCue.Register(hand, finger);
            hands[i] = hand;
        }
    }

    protected override void OnPhaseEntered(int phase)
    {
        handAge = turnStart = turnClock = 0;
        turning = false;
    }

    protected override void MoveBoss(float dt)
    {
        transform.position = Vector3.MoveTowards(transform.position, station, dt * 6f);
        if (!openingComplete)
            return;
        handAge += dt;
        if (turning)
        {
            turnClock += dt;
            if (turnClock >= SwapSeconds)
            {
                turnStart = Mathf.Repeat(turnStart + Mathf.PI, 2 * Mathf.PI);
                turning = false;
            }
        }
        for (int i = 0; i < hands.Length; i++)
        {
            int side = i == 0 ? -1 : 1;
            float x = side * 2.9f, y = -.44f + i * .63f, angle = side * 14;
            if (phaseTwo)
            {
                float turn = handAge * .73f + i * Mathf.PI;
                if (phaseThree)
                {
                    float arc = turning ? Mathf.SmoothStep(0, 1, turnClock / SwapSeconds) : 0;
                    turn = turnStart + (arc + i) * Mathf.PI;
                }
                x = 2.98f * Mathf.Cos(turn);
                y = 1.44f * Mathf.Sin(turn) - 1.09f;
                angle = Mathf.Cos(turn) * -33f;
            }
            var hand = hands[i];
            hand.localPosition = Vector3.MoveTowards(hand.localPosition, new Vector3(x, y, -.35f), dt * 6f);
            hand.localRotation = Quaternion.RotateTowards(hand.localRotation, Quaternion.Euler(0, 0, angle), dt * 115);
        }
    }

    protected override void Attack()
    {
        if (openingComplete) OpenCards();
    }

    protected override IEnumerator Cards() => phaseThree ? ShatteredRose() : phaseTwo ? NightLoom() : CompassRose();

    IEnumerator ChargeFingers(float seconds)
    {
        for (float t = 0; t < seconds;)
        {
            foreach (var hand in hands)
                foreach (var finger in Fingers)
                    Cue(hand.TransformPoint(finger), seconds - t);
            if (!Holding) t += Time.deltaTime * LevelRun.EnemyTempoScale;
            yield return null;
        }
    }

    // The last .35 seconds of a programmed approach brake linearly. Compensating
    // for that distance lets marks from eight different fingers land on one canvas.
    void PaintMoon(Vector3 port, Vector2 target, float travel, float hold, float angle,
        float fallSpeed, float radius, bool flash)
    {
        Vector2 offset = target - (Vector2)port;
        float launch = WorldSpeed(Mathf.Max(.20f, offset.magnitude / (travel - .175f)));
        Fire<CrescentRound>(port, offset, launch, radius, flash)
            .Pause(travel, hold, CombatRound.Turn.Down, angle, fallSpeed / launch);
    }

    // A four-point compass is drawn in velocity space. Each tip belongs to one
    // finger, so the two roses open from the palms and grow through each other.
    static Vector2 RosePoint(int vertex, float cant)
    {
        float angle = (vertex * 45 + cant) * Mathf.Deg2Rad;
        float radius = (vertex & 1) == 0 ? 3.72f : 1.25f;
        return new Vector2(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius);
    }

    IEnumerator CompassRose()
    {
        for (int beat = 0; ; beat++)
        {
            yield return ChargeFingers(.46f);
            float cant = 24 * Mathf.Sin(beat * .67f);
            for (int echo = 0; echo < 2; echo++)
            {
                int marks = Mathf.Max(4, Dense(echo == 0 ? 8 : 6));
                for (int h = 0; h < 2; h++)
                {
                    // One hand signs the echo while the opposite star stays open.
                    if (echo == 1 && h != (beat & 1)) continue;
                    UseTone((h + beat + echo) % 2 == 0 ? HostileTone.Bright : HostileTone.Dark);
                    int side = h * 2 - 1;
                    for (int f = 0; f < 4; f++)
                    {
                        Vector3 port = hands[h].TransformPoint(Fingers[f]);
                        float turn = side * (cant + echo * 18);
                        Vector2 tip = RosePoint(f * 2, turn);
                        for (int flank = -1; flank <= 1; flank += 2)
                        {
                            Vector2 notch = RosePoint(f * 2 + flank + 8, turn);
                            for (int i = 0; i < marks; i++)
                            {
                                float t = (i + .5f) / marks;
                                // A break in one flank turns with the rose.
                                if (f == (beat + h) % 4 && flank == side && t > .28f && t < .64f) continue;
                                Vector2 v = Vector2.Lerp(tip, notch, t) * (echo == 0 ? 1 : .76f);
                                v += new Vector2(-side * .57f, -1.46f);
                                Fire<CrescentRound>(port, v, v.magnitude, echo == 0 ? .176f : .15f, i == 0 && flank == -1)
                                    .Ramp(1.13f, .78f, .68f);
                            }
                        }
                    }
                }
                if (echo == 0) yield return Wait(Rest(.38f));
            }
            yield return Wait(Rest(1.23f));
        }
    }

    // Four brush angles follow each orbiting wrist. The fingers sweep a pleat
    // into the silk; the next wrist turns the other way through the same space.
    IEnumerator NightLoom()
    {
        for (int pass = 0; ; pass++)
        {
            yield return ChargeFingers(.41f);
            int marks = Mathf.Max(12, Dense(29));
            for (int i = 0; i < marks; i++)
            {
                float t = i / (marks - 1f);
                float pleat = 46 * Mathf.Sin(Mathf.PI * t) + 14 * Mathf.Sin(2 * Mathf.PI * t);
                for (int h = 0; h < 2; h++)
                {
                    int side = h * 2 - 1;
                    float orbit = handAge * .73f + h * Mathf.PI;
                    UseTone(((pass + h) & 1) == 0 ? HostileTone.Bright : HostileTone.Dark);
                    for (int f = 0; f < 4; f++)
                    {
                        float angle = orbit * Mathf.Rad2Deg + f * 90 + side * pleat;
                        Vector2 radial = CombatVisuals.Rotate(Vector2.down, angle);
                        Vector2 v = radial * (3.82f + .50f * Mathf.Sin(t * Mathf.PI)) + Vector2.down * .98f;
                        Vector3 port = hands[h].TransformPoint(Fingers[f]);
                        Fire<CrescentRound>(port, v, v.magnitude, f % 2 == 0 ? .21f : .165f, i % 6 == 0)
                            .Fall(new Vector2(-side * radial.y * .46f, -.34f));
                    }
                }
                yield return Wait(Rest(1.92f) / marks);
            }
            yield return Wait(Rest(.52f));
        }
    }

    // A rose window has eight individually cut lozenges. It assembles from
    // the fingertips above the player. When the wrists turn, the lead seams
    // split and the panes peel off in different directions.
    IEnumerator ShatteredRose()
    {
        for (int beat = 0; ; beat++)
        {
            while (turning || Holding) yield return null;
            yield return ChargeFingers(.48f);
            float twist = Mathf.Sin(beat * .74f) * 18 + 12;
            Vector2 center = new Vector2(.54f * Mathf.Sin(beat * .89f), station.y - 1.13f);
            for (int f = 0; f < 4; f++)
            {
                float travel = 1.28f - f * .13f;
                for (int h = 0; h < 2; h++)
                {
                    int side = hands[h].position.x < station.x ? -1 : 1;
                    float a = (side * (22.5f + f * 45f) + twist) * Mathf.Deg2Rad;
                    Vector2 spoke = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
                    Vector2 tangent = new Vector2(-spoke.y, spoke.x);
                    Vector3 nib = hands[h].TransformPoint(Fingers[f]);
                    for (int edge = 0; edge < 4; edge++)
                    {
                        bool outer = edge == 1 || edge == 2;
                        float flank = edge < 2 ? -1 : 1;
                        Vector2 end = spoke * (outer ? 1f : .17f);
                        Vector2 shoulder = spoke * .56f + tangent * (.19f * flank);
                        int count = Mathf.Max(3, Dense(outer ? 9 : 6));
                        UseTone(outer ? HostileTone.Bright : HostileTone.Dark);
                        for (int i = 0; i < count; i++)
                        {
                            float t = (i + .5f) / count;
                            Vector2 glass = Vector2.Lerp(end, shoulder, t);
                            Vector2 target = center + Vector2.Scale(glass, new Vector2(4.1f, 3.5f));
                            float direction = spoke.y > .68f ? 157 * Mathf.Sign(spoke.x) :
                                -spoke.x * 20 + Mathf.Sin(beat * .89f) * 9;
                            PaintMoon(nib, target, travel, .49f + .32f * (1 - t), direction + flank * 5,
                                outer ? 4.95f : 4.45f, outer ? .185f : .155f, edge == 0 && i == 0);
                        }
                    }
                }
                if (f < 3) yield return Wait(.13f);
            }
            yield return Wait(.83f);
            turnClock = 0;
            turning = true;
            yield return ChargeFingers(.32f);
            // The two signing strokes are aimed once. The upper stroke folds
            // over the ceiling, the lower one parts around the locked bearing.
            Vector2 aim = CombatVisuals.Aim(center);
            int marks = Mathf.Max(6, Dense(16));
            for (int i = 0; i < marks; i++)
            {
                float t = i / (marks - 1f);
                for (int h = 0; h < 2; h++)
                {
                    int side = h * 2 - 1;
                    float flare = side * (7 + 25 * Mathf.Sin(Mathf.PI * t));
                    UseTone(h == (beat & 1) ? HostileTone.Bright : HostileTone.Dark);
                    Fire<CrescentRound>(hands[h].TransformPoint(Fingers[(beat + h) % 4]),
                        CombatVisuals.Rotate(aim, flare), 4.55f - .55f * t, .21f, i % 4 == 0);
                    Fire<CrescentRound>(hands[h].TransformPoint(Fingers[(beat + h + 2) % 4]),
                        CombatVisuals.Rotate(Vector2.up, side * (19 + 40 * t)), 3.75f, .16f, i % 4 == 0)
                        .Bounce(1);
                }
                yield return Wait(.99f / marks);
            }
            yield return Wait(Rest(.45f));
        }
    }
}
