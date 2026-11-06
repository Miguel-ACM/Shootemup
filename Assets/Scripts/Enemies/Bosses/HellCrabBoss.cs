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

    protected override void MoveBoss(float dt)
    {
        station = new Vector3(Mathf.Sin(motionAge * .58f) * .70f, Camera.UpLimit - 2.55f, 1);
        transform.position = Vector3.MoveTowards(transform.position, station, dt * 3.4f);
        if (claws[0] == null)
            return;
        float before = clawClock;
        clawClock += dt * (phaseTwo ? 1.18f : 1);
        float t = Mathf.Repeat(clawClock, clawPeriod);
        float shoulderY = transform.position.y;
        if (before == 0 || t < Mathf.Repeat(before, clawPeriod))
            lockedY = Mathf.Clamp(player != null ? player.position.y : shoulderY - 2, shoulderY - 2.7f, shoulderY - .8f);
        lockedY = Mathf.Clamp(lockedY, shoulderY - 2.7f, shoulderY - .8f);
        float reach = Mathf.Min(Camera.RightLimit - 1.5f, 2.9f), sweep = 0, y = shoulderY - .75f;
        if (t < 1.1f)
            y = Mathf.Lerp(y, y + .45f, Mathf.Sin(t / 1.1f * Mathf.PI));
        else if (t < 2.4f)
            y = Mathf.Lerp(y, lockedY, Mathf.SmoothStep(0, 1, (t - 1.1f) / 1.3f));
        else if (t < 3.4f)
        {
            y = lockedY;
            sweep = Mathf.Sin((t - 2.4f) * Mathf.PI) * .32f;
        }
        else if (t < 4.6f)
            y = Mathf.Lerp(lockedY, y, Mathf.SmoothStep(0, 1, (t - 3.4f) / 1.2f));
        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? -1 : 1;
            var p = new Vector3(side * reach * (1 - sweep), y, 0);
            claws[i].position = p;
            claws[i].rotation = Quaternion.Euler(0, 0, -side * (12 + sweep * 50));
            var shoulder = Hull.TransformPoint(new Vector3(side * 1.55f, .25f, 0));
            var elbow = (shoulder + p) * .5f + new Vector3(side * .45f, .25f, .15f);
            Vector3 Point(float u) => Vector3.Lerp(Vector3.Lerp(shoulder, elbow, u), Vector3.Lerp(elbow, p, u), u);
            for (int segment = 0; segment < ArmSections; segment++)
            {
                var a = Point(segment / (float)ArmSections);
                var b = Point((segment + 1) / (float)ArmSections);
                var bone = arms[i, segment];
                bone.position = (a + b) * .5f + Vector3.forward * .12f;
                bone.rotation = Quaternion.FromToRotation(Vector3.up, b - a);
                bone.localScale = new Vector3(1.05f - segment * .025f, Mathf.Max(.42f, Vector3.Distance(a, b) / .95f), 1);
            }
            if (t > 1.1f && t < 4.6f && player != null && Vector2.Distance(player.position, claws[i].TransformPoint(new Vector3(0, -.45f, 0))) < 1.25f)
                player.SendMessage("ReceiveHazardDamage", 18f, SendMessageOptions.DontRequireReceiver);
        }
        if (player != null && Vector2.Distance(player.position, transform.position) < 1.55f)
            player.SendMessage("ReceiveHazardDamage", 18f, SendMessageOptions.DontRequireReceiver);
    }

    protected override void OnPhaseTwo()
    {
        volley = 0;
        clawClock = 0;
        nextAttack = age + 1.5f;
    }

}
