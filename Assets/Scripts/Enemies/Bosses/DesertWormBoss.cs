using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed partial class DesertWormBoss : DistrictBoss
{
    public const int BodyCount = 7;
    readonly Transform[] cores = new Transform[BodyCount + 1];
    readonly Transform[] sections = new Transform[BodyCount];
    protected override bool AtOpeningStation => sections[BodyCount - 1].position.y <= Camera.UpLimit - .5f;
    SandSpine spine;
    Vector3 previous;
    public Transform[] Cores => cores;
    PolygonCollider2D bodyContact;
    readonly Vector2[] bodyOutline = new Vector2[(BodyCount + 1) * 2 + 2];
    readonly MeshRenderer[] reactors = new MeshRenderer[BodyCount + 1];
    MaterialPropertyBlock reactorInk;
    int darkReactors;
    protected override void Configure()
    {
        hullScale = 1.10f;
        health = 2200;
        attackInterval = (.75f);
        nextAttack = 3;
    }

    protected override void Start()
    {
        base.Start();
        reactorInk = new MaterialPropertyBlock();
        var old = GetComponent<CircleCollider2D>();
        old.enabled = false;
        Destroy(old);
        bodyContact = gameObject.AddComponent<PolygonCollider2D>();
        bodyContact.isTrigger = true;
        GetComponent<CombatHullVisual>().enabled = false;
        BuildCircuit(Mathf.Max(1.85f, UnityEngine.Camera.main.orthographicSize * 405f / 720f), out circuit, out circuitDistance);
        transform.position = circuit[0];
        previous = transform.position + Vector3.up * .1f;
        Hull.GetComponent<MeshRenderer>().sharedMaterial = IndustrialMeshFactory.WhiteWormMaterial;
        cores[0] = Reactor(Hull, true, 0, new Vector3(0, .05f, -.44f));
        firingCue.Register(cores[0].transform, Vector3.zero);
        spine = new SandSpine(transform.position, 1.04f, BodyCount);
        for (int i = 0; i < BodyCount; i++)
        {
            sections[i] = SandSpine.Model("White armored vertebra " + (i + 1), transform, IndustrialMeshFactory.WhiteWormSection(), .91f - i * .018f);
            sections[i].GetComponent<MeshRenderer>().sharedMaterial = IndustrialMeshFactory.WhiteWormMaterial;
            sections[i].position = transform.position + Vector3.up * (i + 1) * 1.04f;
            cores[i + 1] = Reactor(sections[i], false, i + 1, new Vector3(0, 0, -.44f));
            firingCue.Register(cores[i + 1].transform, Vector3.zero);
        }
        PartHitFeedback.Bind(transform, false);
    }

    // A predictable closed circuit: a dive down the middle, a rise up the left lane, a sweep
    // in from the left side, a low sweep in from the right across both lower corners and a
    // diagonal rise back to the top. The head turns just outside the border, so most of the
    // body stays in the arena and the route never jumps.
    Vector3[] circuit;
    float[] circuitDistance;
    float travel;
    int circuitIndex;
    internal static void BuildCircuit(float w, out Vector3[] circuit, out float[] circuitDistance)
    {
        float top = Camera.UpLimit, bottom = Camera.DownLimit, mid = (top + bottom) / 2, o = 1.1f;
        Vector2[] keys =
        {
            new Vector2(0, top + o + .6f), new Vector2(.05f * w, top - 5), new Vector2(-.1f * w, bottom + 4),
            new Vector2(-.25f * w, bottom - o), new Vector2(-.45f * w, bottom - o - .6f), new Vector2(-.65f * w, bottom - o),
            new Vector2(-.5f * w, bottom + 5), new Vector2(-.65f * w, top - 5), new Vector2(-.6f * w, top + o - .2f),
            new Vector2(-.95f * w, top + o + .3f), new Vector2(-w - o, top - 1.6f),
            new Vector2(-.55f * w, top - 3.2f), new Vector2(0, top - 6), new Vector2(.6f * w, mid - 1.5f), new Vector2(w + o - .3f, mid - 2.4f),
            new Vector2(w + o + .5f, bottom + 3.4f),
            new Vector2(.62f * w, bottom + 2.3f), new Vector2(0, bottom + 3.2f), new Vector2(-.6f * w, bottom + 2.4f), new Vector2(-w - o, bottom + 1.9f),
            new Vector2(-w - o - .2f, bottom), new Vector2(-.75f * w, bottom - o - .2f),
            new Vector2(-.4f * w, bottom + 3), new Vector2(.1f * w, mid), new Vector2(.5f * w, top - 4), new Vector2(.55f * w, top + o),
            new Vector2(.3f * w, top + o + 1.1f),
        };
        const int steps = 24;
        circuit = new Vector3[keys.Length * steps + 1];
        circuitDistance = new float[circuit.Length];
        for (int i = 0; i < keys.Length; i++)
            for (int k = 0; k < steps; k++)
            {
                int n = keys.Length;
                Vector2 p = CentripetalSpline(keys[(i + n - 1) % n], keys[i], keys[(i + 1) % n], keys[(i + 2) % n], k / (float)steps);
                circuit[i * steps + k] = new Vector3(p.x, p.y, 1);
            }
        circuit[circuit.Length - 1] = circuit[0];
        for (int i = 1; i < circuit.Length; i++)
            circuitDistance[i] = circuitDistance[i - 1] + Vector3.Distance(circuit[i - 1], circuit[i]);
    }

    static Vector2 CentripetalSpline(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t1 = Mathf.Sqrt(Mathf.Max(1e-4f, Vector2.Distance(p0, p1)));
        float t2 = t1 + Mathf.Sqrt(Mathf.Max(1e-4f, Vector2.Distance(p1, p2)));
        float t3 = t2 + Mathf.Sqrt(Mathf.Max(1e-4f, Vector2.Distance(p2, p3)));
        float u = Mathf.Lerp(t1, t2, t);
        Vector2 a1 = Vector2.LerpUnclamped(p0, p1, u / t1);
        Vector2 a2 = Vector2.LerpUnclamped(p1, p2, (u - t1) / (t2 - t1));
        Vector2 a3 = Vector2.LerpUnclamped(p2, p3, (u - t2) / (t3 - t2));
        Vector2 b1 = Vector2.LerpUnclamped(a1, a2, u / t2);
        Vector2 b2 = Vector2.LerpUnclamped(a2, a3, (u - t1) / (t3 - t1));
        return Vector2.LerpUnclamped(b1, b2, (u - t1) / (t2 - t1));
    }

    internal static Vector3 AdvanceCircuit(Vector3[] circuit, float[] circuitDistance, ref float travel, ref int circuitIndex, float step)
    {
        float length = circuitDistance[circuitDistance.Length - 1];
        travel += step;
        if (travel >= length)
        {
            travel -= length;
            circuitIndex = 0;
        }
        while (circuitIndex < circuit.Length - 2 && circuitDistance[circuitIndex + 1] < travel)
            circuitIndex++;
        float span = circuitDistance[circuitIndex + 1] - circuitDistance[circuitIndex];
        return Vector3.Lerp(circuit[circuitIndex], circuit[circuitIndex + 1], span > 0 ? (travel - circuitDistance[circuitIndex]) / span : 0);
    }

    protected override void MoveBoss(float dt)
    {
        transform.position = AdvanceCircuit(circuit, circuitDistance, ref travel, ref circuitIndex, dt * (phaseTwo ? 4.3f : 3.3f));
        Vector2 tangent = transform.position - previous;
        previous = transform.position;
        if (tangent.sqrMagnitude > .00001f)
            Hull.rotation = Quaternion.FromToRotation(Vector3.down, tangent);
        spine?.Follow(transform.position, sections);
    }

    // The body keeps winding through a phase break; only its fire and damage pause.
    protected override bool RepositionsOnPhase => false;
    protected override void OnPhaseTwo()
    {
        nextAttack = age + 1.1f;
        volley = 0;
    }

    Transform Reactor(Transform parent, bool head, int index, Vector3 local)
    {
        var port = new GameObject(head ? "Head reactor" : "Body reactor").transform;
        port.SetParent(parent, false);
        port.localPosition = local;
        var visual = SandSpine.Model("Exposed red reactor", port, IndustrialMeshFactory.WhiteWormCore(head), 1);
        reactors[index] = visual.GetComponent<MeshRenderer>();
        reactors[index].sharedMaterial = IndustrialMeshFactory.WhiteWormMaterial;
        return port;
    }

    bool CanShoot(Transform core) => core != null && (core == cores[0] || System.Array.IndexOf(cores, core) > darkReactors) &&
        core.position.y < Camera.UpLimit - .7f && core.position.y > Camera.DownLimit + 1 &&
        (player == null || Vector2.Distance(core.position, player.position) > 2.4f);

    void LateUpdate()
    {
        if (bodyContact == null || health <= 0) return;
        // A single ribbon follows the articulated body, keeping the gaps outside its bends empty.
        int count = 0;
        bodyOutline[count++] = transform.InverseTransformPoint(Hull.TransformPoint(new Vector3(0, -1, 0)));
        for (int i = 0; i <= BodyCount; i++)
        {
            var section = i == 0 ? Hull : sections[i-1];
            float width = section.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x;
            bodyOutline[count++] = transform.InverseTransformPoint(section.TransformPoint(new Vector3(width, 0, 0)));
        }
        bodyOutline[count++] = transform.InverseTransformPoint(sections[BodyCount-1].TransformPoint(new Vector3(0, .6f, 0)));
        for (int i = BodyCount; i >= 0; i--)
        {
            var section = i == 0 ? Hull : sections[i-1];
            float width = section.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x;
            bodyOutline[count++] = transform.InverseTransformPoint(section.TransformPoint(new Vector3(-width, 0, 0)));
        }
        bodyContact.SetPath(0, bodyOutline);
        int lost = Mathf.Clamp(Mathf.FloorToInt((1-HealthFraction)*(BodyCount+1)), 0, BodyCount);
        if (lost == darkReactors) return;
        darkReactors = lost;
        for (int i = 1; i <= BodyCount; i++)
        {
            reactorInk.SetColor("_Tint", i <= lost ? new Color(.12f,.13f,.14f) : Color.white);
            reactors[i].SetPropertyBlock(reactorInk);
        }
    }
}
