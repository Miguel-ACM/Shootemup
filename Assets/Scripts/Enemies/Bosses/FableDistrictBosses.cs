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

    // Eyespots. A single clap draws two hollow, pointed wing markings in velocity
    // space. The following clap crosses the first pair, leaving an eye to fly through.
    IEnumerator Eyespots()
    {
        for (int beat = 0; ; beat++)
        {
            int sign = (beat & 1) == 0 ? 1 : -1;
            Glide(sign * 1.38f, -.12f, .72f, Rest(1.75f));
            yield return RaiseWings(.48f);
            for (int echo = 0; echo < 2; echo++)
            {
                bool cross = echo != 0;
                UseTone(cross ? HostileTone.Dark : HostileTone.Bright);
                int beads = Mathf.Max(8, Dense(cross ? 20 : 29));
                for (int wing = 0; wing < 2; wing++)
                {
                    int side = wing * 2 - 1;
                    var port = moth.Muzzle(wing).position;
                    for (int i = 0; i < beads; i++)
                    {
                        float loop = 2f * i / beads;
                        float along = loop <= 1 ? loop : 2 - loop;
                        float width = Mathf.Sin(along * Mathf.PI) * (.72f + .48f * along);
                        float rim = loop <= 1 ? width : -width;
                        float inward = cross ? 1.68f : .30f;
                        Vector2 velocity = new Vector2(side * (rim - inward + .68f * along),
                            -Mathf.Lerp(cross ? 3.2f : 2.75f, cross ? 4.9f : 5.4f, along));
                        Fire<SimpleRound>(port, velocity, velocity.magnitude, cross ? .155f : .18f, i == 0);
                    }
                }
                if (!cross) yield return Wait(Rest(.39f));
            }
            yield return Wait(Rest(.77f));
        }
    }

    // Spore Print. The needles plant themselves in three arched gill lines above
    // the pilot. Their tips turn down while held, showing the fall and its opening.
    IEnumerator SporePrint()
    {
        for (int beat = 0; ; beat++)
        {
            float wave = Mathf.Sin(beat * .96f);
            float gate = wave * 2.35f;
            Glide(-wave * .98f, .08f, .20f, 1.55f);
            yield return RaiseWings(.44f);
            int lanes = Mathf.Max(9, Dense(32));
            float halfWidth = (Camera.RightLimit - Camera.LeftLimit) * .5f - .15f;
            for (int row = 0; row < 3; row++)
            {
                float slit = gate + (row - 1) * .20f * Mathf.Cos(beat * .96f);
                UseTone(row == 1 ? HostileTone.Dark : HostileTone.Bright);
                for (int i = 0; i < lanes; i++)
                {
                    float x = Mathf.Lerp(-halfWidth, halfWidth, (i + .5f) / lanes);
                    if (Mathf.Abs(x - slit) < .69f) continue;
                    int wing = x < 0 ? 0 : 1;
                    Vector2 port = moth.Muzzle(wing).position;
                    float arch = 1 - Mathf.Pow((Mathf.Abs(x) - halfWidth * .52f) / (halfWidth * .52f), 2);
                    float y = station.y - .55f + arch * 1.85f - row * .76f;
                    Vector2 offset = new Vector2(x, y) - port;
                    // Pause brakes linearly during the last .35 s. Give every lane
                    // the distance it needs before that brake, and one common fall speed.
                    const float travel = .90f;
                    float launch = Mathf.Max(.18f, offset.magnitude / (travel - .175f));
                    float authoredSpeed = WorldSpeed(launch);
                    Fire<NeedleRound>(moth.Muzzle(wing).position, offset, authoredSpeed, .15f, i == 0 || i == lanes - 1)
                        .Pause(travel, .45f + .12f * arch, CombatRound.Turn.Down, 0, 4.8f / authoredSpeed)
                        .ShowTurn();
                }
                if (row < 2) yield return Wait(Rest(.20f));
            }
            yield return Wait(Rest(1.15f));
        }
    }

    // Lantern Run. During each low banking pass the two guns comb a trail of
    // suspended needles along the curve. A missing stroke leaves a cut in the
    // wake. The nose locks its answer once, giving the pilot time to change course.
    IEnumerator LanternRun()
    {
        for (int pass = 0; ; pass++)
        {
            int sign = (pass & 1) == 0 ? 1 : -1;
            yield return RaiseWings(.44f);
            Glide(sign * 2.12f, -.12f, 3.05f, 1.92f);
            int strokes = Mathf.Max(9, Dense(21));
            int tines = Mathf.Max(3, Dense(6));
            float opening = (pass & 1) == 0 ? .38f : .64f;
            for (int stroke = 0; stroke < strokes; stroke++)
            {
                float t = (stroke + .5f) / strokes;
                while (Holding || glideTime < t * glideDuration) yield return null;
                if (Mathf.Abs(t - opening) < .09f) continue;
                for (int wing = 0; wing < 2; wing++)
                {
                    int side = wing * 2 - 1;
                    UseTone(wing == (pass & 1) ? HostileTone.Bright : HostileTone.Dark);
                    var port = moth.Muzzle(wing).position;
                    for (int k = 0; k < tines; k++)
                    {
                        float tooth = k / (tines - 1f);
                        Vector2 direction = new Vector2(side * Mathf.Lerp(.16f, 1.08f, tooth), 1);
                        Fire<NeedleRound>(port, direction, 3.1f + 1.25f * tooth, .14f, k == 0)
                            .Pause(.49f, .49f + .32f * (1 - t), CombatRound.Turn.Down, -side * (11 + 7 * tooth), 1.2f)
                            .ShowTurn();
                    }
                }
            }
            var nose = moth.Muzzle(2);
            Vector2 lockOn = CombatVisuals.Aim(nose.position);
            PointWeapon(nose.position, lockOn);
            wantedFold = .64f;
            yield return Charge(nose, Vector3.zero, .47f);
            UseTone(HostileTone.Bright);
            int darts = Mathf.Max(2, Dense(5));
            for (int i = 0; i < darts; i++)
            {
                for (int side = -1; side <= 1; side += 2)
                    Fire<SimpleRound>(nose.position, CombatVisuals.Rotate(lockOn, side * (4 + i * 3)), 5.9f, .20f, side < 0);
                yield return Wait(.075f);
            }
            yield return Wait(Rest(.35f));
        }
    }
}

public sealed class DreamPainter : DistrictBoss
{
    readonly Transform[] hands = new Transform[2];
    static readonly Vector3[] Fingers =
    {
        new Vector3(-.72f, -1.08f, -.12f), new Vector3(-.25f, -1.42f, -.12f),
        new Vector3(.25f, -1.49f, -.12f), new Vector3(.72f, -1.12f, -.12f)
    };
    float handAge;
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

    protected override void OnPhaseEntered(int phase) => handAge = 0;

    protected override void MoveBoss(float dt)
    {
        transform.position = Vector3.MoveTowards(transform.position, station, dt * 6f);
        if (!openingComplete)
            return;
        handAge += dt;
        for (int i = 0; i < hands.Length; i++)
        {
            int side = i == 0 ? -1 : 1;
            float x = side * 2.9f, y = -.44f + i * .63f, angle = side * 14;
            if (phaseTwo)
            {
                float turn = handAge * (phaseThree ? .93f : .73f) + i * Mathf.PI;
                if (phaseThree)
                {
                    const float cycle = 4.4f;
                    float t = handAge / cycle;
                    turn = (Mathf.Floor(t) + Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.36f, .88f, t % 1))) * Mathf.PI + i * Mathf.PI;
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

    // Still Life, Two Moons, and The Hands of Sleep. Every shot begins at a finger.
    protected override IEnumerator Cards()
    {
        int beat = 0;
        while (true)
        {
            float charge = phaseThree ? .29f : .44f;
            for (float time = 0; time < charge;)
            {
                if (!Holding) time += Time.deltaTime * LevelRun.EnemyTempoScale;
                foreach (var hand in hands)
                    foreach (var finger in Fingers)
                        Cue(hand.TransformPoint(finger), charge - time);
                yield return null;
            }
            int strokes = phaseThree ? 5 : phaseTwo ? 4 : 3;
            int n = Dense(phaseThree ? 5 : 4);
            for (int stroke = 0; stroke < strokes; stroke++)
            {
                for (int side = 0; side < hands.Length; side++)
                {
                    int sign = side == 0 ? -1 : 1;
                    for (int index = 0; index < Fingers.Length; index++)
                    {
                        if (Difficulty.Easy && (index + beat + stroke) % 3 == 0)
                            continue;
                        var port = hands[side].TransformPoint(Fingers[index]);
                        float lean = sign * (17 + index * 17 + 18 * Mathf.Sin(beat * .62f + stroke * .48f));
                        Vector2 down = phaseTwo && stroke % 3 == 2 ? Vector2.up : Vector2.down;
                        for (int i = 0; i < n; i++)
                        {
                            float lane = i - (n - 1) * .5f;
                            var moon = Fire<CrescentRound>(port, CombatVisuals.Rotate(down, lean + lane * 8),
                                3.1f + .40f * i, .20f + (i % 2) * .035f, i == 0);
                            if (down == Vector2.up) moon.Bounce(2);
                            else if (phaseThree && (i + index) % 3 == 0)
                                moon.Pause(.8f, .35f, CombatRound.Turn.Rotate, sign * 36, 4.2f);
                        }
                    }
                }
                yield return Wait(Rest(phaseThree ? .18f : .23f));
            }
            beat++;
            yield return Wait(Rest(phaseThree ? .44f : .68f));
        }
    }
}
