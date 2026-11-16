using System.Collections;
using UnityEngine;

public sealed class MushroomWarden : DistrictBoss
{
    protected override void Configure() { health = 1980; hullScale = .92f; attackInterval = .28f; nextAttack = 1.5f; }

    protected override void MoveBoss(float dt)
    {
        Vector3 p = station;
        if (openingComplete)
        {
            p.x = Mathf.Sin(motionAge * (phaseThree ? 1.12f : .74f)) * (phaseThree ? 2.17f : 1.6f);
            p.y += Mathf.Sin(motionAge * 1.53f) * .65f - .55f;
        }
        transform.position = Vector3.MoveTowards(transform.position, p, dt * (openingComplete ? 4.2f : 5.8f));
    }

    protected override void Attack()
    {
        if (openingComplete) OpenCards();
    }

    // Fungus spreads through tendrils, cap edges and returning volleys.
    protected override IEnumerator Cards()
    {
        int beat = 0;
        while (true)
        {
            float warning = phaseThree ? .33f : .48f;
            Cue(Muzzle(1), warning);
            yield return Charge(Hull, WeaponPort(0), warning);
            int strokes = phaseThree ? 6 : phaseTwo ? 4 : 3;
            for (int row = 0; row < strokes; row++)
            {
                float swing = 24 * Mathf.Sin(beat * .73f + row * .41f);
                for (int side = -1; side <= 1; side += 2)
                {
                    int count = Dense(phaseTwo ? 7 : 6);
                    for (int i = 0; i < count; i++)
                    {
                        var port = Muzzle(side < 0 ? 0 : 1);
                        float angle = side * (18 + 11 * i + swing);
                        Vector2 axis = CombatVisuals.Rotate(Vector2.down, angle);
                        var mark = Fire<SimpleRound>(port, axis, 3.38f + .17f * i, .22f, i == 0)
                            .Bounce(phaseTwo ? 2 : 1, true);
                        if (phaseThree && row % 2 == 1)
                            mark.Pause(.86f, .34f, CombatRound.Turn.Rotate, side * 35, 4.2f);
                    }
                }
                if (phaseTwo && row % 2 == 0)
                {
                    int count = Dense(11);
                    for (int i = 0; i < count; i++)
                        Fire<NeedleRound>(Muzzle(2), CombatVisuals.Rotate(Vector2.down,
                            (i - (count - 1) * .5f) * 16 - swing), 4.8f, .18f, i == 0).Ramp(.61f, 1.18f, .27f);
                }
                yield return Wait(Rest(.24f));
            }
            beat++;
            yield return Wait(Rest(phaseThree ? .40f : .62f));
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
