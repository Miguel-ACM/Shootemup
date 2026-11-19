using System.Collections;
using UnityEngine;

public sealed class MushroomWarden : DistrictBoss
{
    MushroomMothRig moth;
    Vector3 glideStart, glideEnd;
    float glideTime, glideDuration, dip, fold = .72f, wantedFold = .72f;

    protected override void Configure()
    {
        health = 2350;
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

    protected override IEnumerator Cards() => phaseThree ? LastInstar() : phaseTwo ? SilkenShuttle() : AmanitaBloom();

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

    // Four lobes are traced in speed space. Both wings complete every layer.
    IEnumerator AmanitaBloom()
    {
        for (int bloom = 0; ; bloom++)
        {
            int sign = (bloom & 1) == 0 ? 1 : -1;
            Glide(sign * 1.65f, -.20f, .58f, 1.6f);
            yield return RaiseWings(.36f);
            for (int layer = 0; layer < 3; layer++)
            {
                int marks = Mathf.Max(layer == 1 ? 9 : 18, Dense(layer == 1 ? 18 : 36));
                for (int wing = 0; wing < 2; wing++)
                {
                    int side = 2 * wing - 1;
                    Vector3 lip = moth.Muzzle(wing).position;
                    UseTone((wing + layer) % 2 == 0 ? HostileTone.Bright : HostileTone.Dark);
                    for (int n = 0; n < marks; n++)
                    {
                        float a = (n + .5f) * 360 / marks;
                        float petal = .5f + .5f * Mathf.Cos((4*a + 24*layer) * Mathf.Deg2Rad);
                        Vector2 shape = CombatVisuals.Rotate(Vector2.down, side*(a + 8*layer)) *
                            (1.3f + 2.45f*petal) + new Vector2(-side*.34f, -1.72f);
                        Fire<SimpleRound>(lip, shape, shape.magnitude, layer == 0 ? .21f : .155f, n == 0);
                    }
                }
                yield return Wait(Rest(.24f));
            }
            yield return Wait(Rest(.69f));
        }
    }

    // Each bank lays a length of silk ahead of the other. The tips turn and zip across
    // the screen while the longer inner threads fall diagonally through their wake.
    IEnumerator SilkenShuttle()
    {
        for (int pass = 0; ; pass++)
        {
            int side = (pass & 1) == 0 ? 1 : -1;
            Glide(side * 2.35f, -.35f, 1.3f, 1.65f);
            yield return RaiseWings(.34f);
            int stitches = Mathf.Max(10, Dense(21));
            for (int i = 0; i < stitches; i++)
            {
                float t = i / (stitches - 1f);
                for (int wing = 0; wing < 2; wing++)
                {
                    int hand = wing * 2 - 1;
                    float knot = Mathf.Sin(2 * Mathf.PI * t);
                    Vector3 lip = moth.Muzzle(wing).position;
                    UseTone(wing == (pass & 1) ? HostileTone.Bright : HostileTone.Dark);
                    for (int j = 0; j < 3; j++)
                    {
                        float bearing = hand * (112 - 150*t + 10*j);
                        Fire<NeedleRound>(lip, CombatVisuals.Rotate(Vector2.down, bearing), 3.8f + j*.28f,
                            .165f, j == 0).Pause(.53f + .20f*t, .24f + j*.06f,
                            CombatRound.Turn.Down, hand*(19 + knot*23 + 11*j), 1.28f);
                    }
                    if (i % 4 == 0)
                        Fire<SimpleRound>(lip, new Vector2(-hand * (1.18f-.6f*t), .7f), 3.6f, .22f, false)
                            .Fall(new Vector2(-hand*.32f,-2.6f));
                }
                yield return Wait(Rest(1.46f) / stitches);
            }
            yield return Wait(Rest(.40f));
        }
    }

    // The moth banks through a figure eight. Two combs trace the moving wings while
    // the keel sends a slower scalloped skirt underneath the next pass.
    IEnumerator LastInstar()
    {
        for (int pass = 0; ; pass++)
        {
            int sign = (pass & 1) == 0 ? 1 : -1;
            Glide(sign*2.35f, -.35f, (pass & 1) == 0 ? 2.0f : .65f, 2.28f);
            yield return RaiseWings(.32f);
            int steps = Mathf.Max(13,Dense(26));
            for (int i=0;i<steps;i++)
            {
                float t=i/(steps-1f);
                for (int wing=0;wing<2;wing++)
                {
                    int side=wing*2-1;
                    float wave=Mathf.Sin(2*Mathf.PI*t + wing*.45f);
                    Vector3 lip=moth.Muzzle(wing).position;
                    UseTone(wing==0?HostileTone.Dark:HostileTone.Bright);
                    for (int comb=0;comb<4;comb++)
                    {
                        Vector2 axis=CombatVisuals.Rotate(Vector2.down, side*(27 + 49*wave + 13*comb));
                        Fire<NeedleRound>(lip,axis,3.45f+.33f*comb,.17f,comb==0);
                    }
                }
                if (i%4==0)
                {
                    Vector3 lip=moth.Muzzle(2).position;
                    int marks=Mathf.Max(10,Dense(20));
                    for (int n=0;n<marks;n++)
                    {
                        float a=(n+.5f)*360/marks;
                        Fire<SimpleRound>(lip,CombatVisuals.Rotate(Vector2.down,a),
                            2.0f+.72f*Mathf.Cos((a*3+pass*23)*Mathf.Deg2Rad),.20f,n==0)
                            .Fall(new Vector2(sign*.17f,-.71f));
                    }
                }
                yield return Wait(Rest(2.15f)/steps);
            }
        }
    }
}

public sealed class DreamPainter : DistrictBoss
{
    readonly Transform[] hands = new Transform[2];
    readonly Transform[] halos = new Transform[2];
    Transform gaze;
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
        Hull.GetComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.DreamFaceMesh();
        gaze = ArtPart("Watching eye", IndustrialMeshFactory.DreamGazeMesh());
        gaze.localPosition = IndustrialMeshFactory.DreamEyeSeat;
        for (int i = 0; i < halos.Length; i++) halos[i] = ArtPart("Turning aureole", IndustrialMeshFactory.DreamHaloMesh(i));
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

    Transform ArtPart(string label, Mesh mesh)
    {
        var part = new GameObject(label).transform;
        part.SetParent(Hull, false);
        part.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var surface = part.gameObject.AddComponent<MeshRenderer>();
        surface.sharedMaterial = IndustrialMeshFactory.DreamPorcelain;
        surface.sortingLayerName = "Enemy";
        return part;
    }

    protected override void OnPhaseEntered(int phase)
    {
        handAge = turnStart = turnClock = 0;
        turning = false;
    }

    protected override void MoveBoss(float dt)
    {
        Vector3 perch = station;
        if (openingComplete)
            perch += new Vector3(.60f * Mathf.Sin(handAge * .78f), .27f * Mathf.Sin(handAge * 1.56f), 0);
        transform.position = Vector3.MoveTowards(transform.position, perch, dt * 6f);
        if (!openingComplete)
            return;
        handAge += dt;
        float tilt = 11 * Mathf.Sin(handAge * .81f);
        float blink = Mathf.Pow(Mathf.Max(0, Mathf.Cos(handAge * 1.45f)), 76);
        Vector2 look = Hull.InverseTransformPoint(CombatVisuals.PlayerHitPoint);
        gaze.localPosition = IndustrialMeshFactory.DreamEyeSeat + (Vector3)(look.normalized * .065f);
        gaze.localScale = new Vector3(1, 1 - .90f * blink, 1);
        halos[0].localRotation = Quaternion.Euler(tilt, 0, handAge * (phaseThree ? 31 : 16));
        halos[1].localRotation = Quaternion.Euler(-tilt, tilt, -handAge * (phaseThree ? 42 : 23));
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
            hand.localPosition = Vector3.MoveTowards(hand.localPosition, new Vector3(x, y + .20f * Mathf.Sin(handAge * 2.1f + i), -.35f), dt * 6f);
            hand.localRotation = Quaternion.RotateTowards(hand.localRotation, Quaternion.Euler(8 * Mathf.Sin(handAge * 2 + i), side * (13 + 16 * firingCue.ChargeLevel), angle + side * (12 * firingCue.ChargeLevel - 9 * firingCue.ReleaseLevel)), dt * 115);
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
    void EtchNeedle(Vector3 port, Vector2 target, float travel, float hold, float angle,
        float fallSpeed, float radius, bool flash)
    {
        Vector2 offset = target - (Vector2)port;
        float launch = WorldSpeed(Mathf.Max(.20f, offset.magnitude / (travel - .175f)));
        Fire<NeedleRound>(port, offset, launch, radius, flash)
            .Pause(travel, hold, CombatRound.Turn.Down, angle, fallSpeed / launch).ShowTurn();
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
                                Vector2 v = Vector2.Lerp(tip, notch, t) * (echo == 0 ? 1 : .76f);
                                v += new Vector2(-side * .57f, -1.46f);
                                Fire<NeedleRound>(port, v, v.magnitude, echo == 0 ? .176f : .15f, i == 0 && flank == -1)
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
                        CombatRound mark = f % 2 == 0
                            ? (CombatRound)Fire<CrescentRound>(port, v, v.magnitude, .21f, i % 6 == 0)
                            : Fire<StarRound>(port, v, v.magnitude, .17f, i % 6 == 0);
                        mark.Fall(new Vector2(-side * radial.y * .46f, -.34f));
                    }
                }
                yield return Wait(Rest(1.92f) / marks);
            }
            yield return Wait(Rest(.52f));
        }
    }

    // Opposed streams keep threading the rose as the hands exchange places.
    // The inner dot leads each slowly drifting lane; every fourth stitch is a larger star.
    IEnumerator StarField()
    {
        yield return ChargeFingers(.48f);
        for (int beat = 0; ; beat++)
        {
            for (int h = 0; h < 2; h++)
            {
                int side = h * 2 - 1;
                Vector3 lip = hands[h].TransformPoint(Fingers[(beat / 4 + h) % 4]);
                UseTone(h == 0 ? HostileTone.Dark : HostileTone.Bright);
                int lanes = Mathf.Max(3, Dense(5));
                for (int lane = 0; lane < lanes; lane++)
                {
                    float angle = side * (23 * Mathf.Sin(beat*.15f) + (lane-(lanes-1)*.5f)*21);
                    var dir = CombatVisuals.Rotate(Vector2.down, angle);
                    if ((lane + beat) % 4 == 0)
                        Fire<StarRound>(lip, dir, 2.7f, .23f, lane == 0);
                    else
                        Fire<SimpleRound>(lip, dir, 3.42f, .15f, lane == 0);
                }
            }
            yield return Wait(Rest(.30f));
        }
    }

    // A rose window has eight individually cut lozenges. It assembles from
    // the fingertips above the player. When the wrists turn, the lead seams
    // split and the panes peel off in different directions.
    IEnumerator ShatteredRose()
    {
        Phrase(StarField());
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
                            EtchNeedle(nib, target, travel, .49f + .32f * (1 - t), direction + flank * 5,
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
