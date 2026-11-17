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
