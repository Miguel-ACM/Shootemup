using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed partial class HellCrabBoss : DistrictBoss
{
    readonly Transform[] claws = new Transform[2];
    const int ArmSections = 4;
    readonly Transform[,] arms = new Transform[2, ArmSections];
    readonly Transform[] eyes = new Transform[2];
    float clawClock, lockedY;
    [EnemyTunable("Claws / Cycle seconds", 5, 12)] public float clawPeriod = 7.4f;
    [EnemyTunable("Claws / Fan count", 3, 9)] public int clawShots = 7;
    protected override void Configure()
    {
        hullScale = 1;
        health = 3000;
        attackInterval = (.72f);
        nextAttack = 2.2f;
    }

    protected override void Start()
    {
        base.Start();
        GetComponent<CombatHullVisual>().enabled = false;
        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? -1 : 1;
            eyes[i] = Model("Amber firing eye", IndustrialMeshFactory.CombatHull(55), Hull);
            eyes[i].localPosition = new Vector3(side * .67f, -.52f, -.96f);
            claws[i] = Model("Jointed side pincer", IndustrialMeshFactory.CombatHull(54), Hull);
            claws[i].localScale = Vector3.one * 1.75f;
            // The Terra pincer's moving jaw is a separate animated part; without it each claw lost its jaw.
            IndustrialMeshFactory.AttachTerraParts("Hull54", claws[i], CombatMaterials.Fleet);
            firingCue.Register(claws[i], new Vector3(0, -.32f, -.4f));
            firingCue.Register(eyes[i], Vector3.zero);
            for (int segment = 0; segment < ArmSections; segment++)
                arms[i, segment] = Model("Articulated pincer vertebra " + segment, IndustrialMeshFactory.CrabArm(), Hull);
        }
        PrepareSideWarnings();
    }

    static Transform Model(string name, Mesh mesh, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CombatMaterials.Fleet;
        renderer.sortingLayerName = "Enemy";
        return go.transform;
    }

    // The claw stroke (Miguel, 10 November 2026): a physical wind-up, then each pincer
    // traces one figure around a point beside the body, mirrored by the other, and eases
    // back to rest. Stroke by stroke the pair draws a heart (crescent sweeps), a vertical
    // figure eight, then two loops turning opposite ways. The pincers turn along their
    // path, so streams and laces fired from them draw the same figures, and while they
    // draw they leave a trail of flames (ClawTrail).
    const float StrokeStart = 1;
    const float StrokeEnd = 4.6f;
    readonly float[] clawTurn = new float[2];
    float nextTrail;
    int Figure => Mathf.FloorToInt(clawClock / clawPeriod) % 3;

    protected override void MoveBoss(float dt)
    {
        station = new Vector3(Mathf.Sin(motionAge * .58f) * .70f, Camera.UpLimit - 2.55f, 1);
        transform.position = Vector3.MoveTowards(transform.position, station, dt * 3.4f);
        if (claws[0] == null)
            return;
        float before = clawClock;
        // Phase one belongs entirely to the slow side flames; the pincers stay at rest.
        if (phaseTwo) clawClock += dt * 1.18f;
        float t = Mathf.Repeat(clawClock, clawPeriod);
        float shoulderY = transform.position.y;
        if (before == 0 || t < Mathf.Repeat(before, clawPeriod))
            lockedY = Mathf.Clamp(player != null ? player.position.y : shoulderY - 2, shoulderY - 2.7f, shoulderY - .8f);
        lockedY = Mathf.Clamp(lockedY, shoulderY - 2.7f, shoulderY - .8f);
        // The wind-up lifts and opens the claws; the figure fades in and out of the rest pose.
        float windUp = t < StrokeStart ? Mathf.Sin(t / StrokeStart * Mathf.PI) : 0;
        float draw = Mathf.SmoothStep(0, 1, (t - StrokeStart) / .6f) * (1 - Mathf.SmoothStep(0, 1, (t - (StrokeEnd - .6f)) / .6f));
        float u = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(StrokeStart, StrokeEnd, t));
        // The figure sits beside the body, a little toward the pilot's height.
        float centreY = Mathf.Clamp(lockedY + .9f, shoulderY - 1.9f, shoulderY - 1.2f);
        var figure = FigurePoint(Figure, u);
        bool drawing = t > StrokeStart && t < StrokeEnd;
        bool trail = drawing && draw > .6f && enteredFight && openingComplete && age >= nextTrail;
        if (trail)
            nextTrail = age + .16f;
        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? -1 : 1;
            var rest = new Vector3(side * (2.9f + windUp * .3f), shoulderY - .75f + windUp * .45f, 0);
            var drawn = new Vector3(side * (2.2f + figure.x), centreY + figure.y, 0);
            var p = Vector3.Lerp(rest, drawn, draw);
            // The pincer's tip follows its path, never turning past sideways.
            var motion = (Vector2)(p - claws[i].position);
            float heading = motion.sqrMagnitude > dt * dt * .16f ? Mathf.Clamp(Vector2.SignedAngle(Vector2.down, motion), -100, 100) : -side * 12;
            float target = Mathf.LerpAngle(-side * 12, heading, draw);
            clawTurn[i] = Mathf.LerpAngle(clawTurn[i], target, 1 - Mathf.Exp(-7 * dt));
            claws[i].position = p;
            claws[i].rotation = Quaternion.Euler(0, 0, clawTurn[i]);
            var shoulder = Hull.TransformPoint(new Vector3(side * 1.55f, .25f, 0));
            var elbow = (shoulder + p) * .5f + new Vector3(side * .45f, .25f, .15f);
            Vector3 Point(float k) => Vector3.Lerp(Vector3.Lerp(shoulder, elbow, k), Vector3.Lerp(elbow, p, k), k);
            for (int segment = 0; segment < ArmSections; segment++)
            {
                var a = Point(segment / (float)ArmSections);
                var b = Point((segment + 1) / (float)ArmSections);
                var bone = arms[i, segment];
                bone.position = (a + b) * .5f + Vector3.forward * .12f;
                bone.rotation = Quaternion.FromToRotation(Vector3.up, b - a);
                bone.localScale = new Vector3(1.05f - segment * .025f, Mathf.Max(.42f, Vector3.Distance(a, b) / .95f), 1);
            }
            if (trail)
                ClawTrail(i);
            if (drawing && player != null && Vector2.Distance(CombatVisuals.PlayerHitPoint, claws[i].TransformPoint(new Vector3(0, -.45f, 0))) < 1.25f)
                player.SendMessage("ReceiveHazardDamage", 18f, SendMessageOptions.DontRequireReceiver);
        }
        if (player != null && Vector2.Distance(CombatVisuals.PlayerHitPoint, transform.position) < 1.55f)
            player.SendMessage("ReceiveHazardDamage", 18f, SendMessageOptions.DontRequireReceiver);
    }

    // One pincer's figure, in outward/up units around its centre, for stroke progress u.
    static Vector2 FigurePoint(int figure, float u)
    {
        const float across = .95f;
        const float tall = .85f;
        if (figure == 0)
        {
            // Crescent: out, down, in and up again; the pair draws a heart.
            float a = Mathf.Lerp(70, -250, u) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a) * across, Mathf.Sin(a) * tall);
        }
        if (figure == 1)
        {
            // A vertical figure eight.
            float a = u * Mathf.PI * 2;
            return new Vector2(Mathf.Sin(2 * a) * across * .7f, Mathf.Sin(a) * tall * 1.1f);
        }
        // A full loop from the top; mirrored, the two pincers turn opposite ways.
        float b = (90 - 360 * u) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(b) * across, Mathf.Sin(b) * tall);
    }

    protected override void OnPhaseTwo()
    {
        HideSideWarnings();
        volley = 0;
        clawClock = 0;
        nextAttack = age + 1.5f;
    }

    protected override void Die()
    {
        HideSideWarnings();
        base.Die();
    }

}
