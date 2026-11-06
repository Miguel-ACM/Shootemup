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
    float routeWidth;
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
        previous = transform.position;
        var camera = UnityEngine.Camera.main;
        routeWidth = Mathf.Max(.6f, camera.orthographicSize * 405f / 720f - 1.25f);
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

    // A predictable route: after one dive down through the arena, the worm keeps rising
    // from below the bottom edge to above the top in a gentle S, each pass in the next
    // lane, its body laid out offscreen before every new pass.
    static readonly float[] Lanes = { 0, -.55f, .55f, -.25f, .3f };
    int pass = -1;
    float passTravel;
    protected override void MoveBoss(float dt)
    {
        float body = BodyCount * 1.04f + 1.5f;
        float speed = phaseTwo ? 4.3f : 3.3f;
        if (pass < 0)
        {
            // Opening dive: straight down the middle and out of the bottom.
            transform.position += Vector3.down * dt * speed * 1.3f;
            if (transform.position.y < Camera.DownLimit - body)
                BeginPass();
        }
        else
        {
            passTravel += dt * speed;
            float lane = Lanes[pass % Lanes.Length] * routeWidth;
            float y = Camera.DownLimit - 1.2f + passTravel;
            float x = lane + Mathf.Sin(passTravel * .5f + pass) * routeWidth * .35f;
            transform.position = new Vector3(Mathf.Clamp(x, -routeWidth, routeWidth), y, 1);
            if (y > Camera.UpLimit + body)
                BeginPass();
        }
        Vector2 tangent = transform.position - previous;
        previous = transform.position;
        if (tangent.sqrMagnitude > .00001f)
            Hull.rotation = Quaternion.FromToRotation(Vector3.down, tangent);
        spine?.Follow(transform.position, sections);
    }

    void BeginPass()
    {
        pass++;
        passTravel = 0;
        transform.position = new Vector3(Lanes[pass % Lanes.Length] * routeWidth, Camera.DownLimit - 1.2f, 1);
        previous = transform.position + Vector3.down * .1f;
        spine?.Reset(transform.position, Vector3.down);
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
