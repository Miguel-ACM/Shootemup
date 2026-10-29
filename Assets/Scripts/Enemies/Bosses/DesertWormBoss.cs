using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed partial class DesertWormBoss : DistrictBoss
{
    public const int BodyCount = 7;
    readonly DesertWormCore[] cores = new DesertWormCore[BodyCount + 1];
    readonly Transform[] sections = new Transform[BodyCount];
    SandSpine spine;
    Vector3 previous;
    float routeWidth;
    bool completing;
    public int RemainingParts { get; private set; } = BodyCount + 1;
    public DesertWormCore[] Cores => cores;
    protected override void Configure()
    {
        hull = 33;
        hullScale = 1.10f;
        health = 2200;
        attackInterval = (.75f);
        nextAttack = 3;
    }

    protected override void Start()
    {
        base.Start();
        GetComponent<Collider2D>().enabled = false;
        GetComponent<CombatHullVisual>().enabled = false;
        // Only the red reactors are damage targets, including homing, burn and chain weapons.
        Live.Remove(this);
        previous = transform.position;
        var camera = UnityEngine.Camera.main;
        routeWidth = Mathf.Max(.6f, camera.orthographicSize * 405f / 720f - 1.25f);
        Hull.GetComponent<MeshRenderer>().sharedMaterial = IndustrialMeshFactory.WhiteWormMaterial;
        cores[0] = DesertWormCore.Create(this, Hull, true, health * 670 / 2700, new Vector3(0, .05f, -.44f));
        firingCue.Register(cores[0].transform, Vector3.zero);
        spine = new SandSpine(transform.position, 1.04f, BodyCount);
        for (int i = 0; i < BodyCount; i++)
        {
            sections[i] = SandSpine.Model("White armored vertebra " + (i + 1), transform, IndustrialMeshFactory.WhiteWormSection(), .91f - i * .018f);
            sections[i].GetComponent<MeshRenderer>().sharedMaterial = IndustrialMeshFactory.WhiteWormMaterial;
            sections[i].position = transform.position + Vector3.up * (i + 1) * 1.04f;
            cores[i + 1] = DesertWormCore.Create(this, sections[i], false, health * 290 / 2700, new Vector3(0, 0, -.44f));
            firingCue.Register(cores[i + 1].transform, Vector3.zero);
        }
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

    protected override bool PhaseTwoReady => RemainingParts <= 4;
    protected override void OnPhaseTwo()
    {
        nextAttack = age + 1.1f;
        volley = 0;
    }

    bool CanShoot(DesertWormCore core) => core != null && core.health > 0 && core.transform.position.y < Camera.UpLimit - .7f &&
        core.transform.position.y > Camera.DownLimit + 1 && (player == null || Vector2.Distance(core.transform.position, player.position) > 2.4f);
    public void PartDamaged()
    {
        if (completing)
            return;
        float sum = 0;
        RemainingParts = 0;
        foreach (var core in cores)
            if (core != null && core.health > 0)
            {
                sum += core.health;
                RemainingParts++;
            }
        if (RemainingParts == 0)
        {
            completing = true;
            // Use the ordinary boss death path exactly once: score, drops, health-bar teardown and route completion.
            base.damage(health / Mathf.Max(.001f, GameRules.enemyDamageReceived) + 1);
        }
        else
            health = sum;
    }

    public override void damage(float amount)
    {
    } // Armor and the aggregate health object cannot bypass the reactors.
}

public sealed class DesertWormCore : AEnemy
{
    DesertWormBoss owner;
    MeshRenderer mesh;
    public static DesertWormCore Create(DesertWormBoss owner, Transform parent, bool head, float health, Vector3 local)
    {
        var t = new GameObject(head ? "Head weakpoint" : "Body weakpoint").transform;
        t.SetParent(parent, false);
        var visual = SandSpine.Model("Exposed red reactor", t, IndustrialMeshFactory.WhiteWormCore(head), 1);
        t.localPosition = local;
        t.gameObject.layer = 8;
        t.gameObject.tag = "Enemy";
        var sprite = t.gameObject.AddComponent<SpriteRenderer>();
        sprite.enabled = false;
        var hit = t.gameObject.AddComponent<CircleCollider2D>();
        hit.isTrigger = true;
        hit.radius = head ? .51f : .35f;
        var part = t.gameObject.AddComponent<DesertWormCore>();
        part.owner = owner;
        part.health = health;
        part.mesh = visual.GetComponent<MeshRenderer>();
        part.mesh.sharedMaterial = IndustrialMeshFactory.WhiteWormMaterial;
        PartHitFeedback.Bind(visual);
        part.scoreOnKill = 0;
        part.lifeDropChance = part.manaDropChance = part.moneyDropChance = 0;
        return part;
    }

    public override void damage(float amount)
    {
        if (health <= 0 || owner == null)
            return;
        base.damage(amount);
        if (owner != null)
            owner.PartDamaged();
    }

    protected override void Die()
    {
        health = 0;
        GetComponent<Collider2D>().enabled = false;
        PSXImpactEffects.Burst(transform.position, new Color(1, .25f, .08f), true, Vector2.up);
        var tint = new MaterialPropertyBlock();
        tint.SetColor("_Tint", new Color(.12f, .13f, .14f));
        mesh.SetPropertyBlock(tint);
        enabled = false; // Keep the dead, dark reactor attached to the articulated wreck.
    }

    protected override void OnBecameInvisible()
    {
    }
}
