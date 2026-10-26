using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class DesertWormBoss : DistrictBoss
{
    public const int BodyCount = 7;
    readonly DesertWormCore[] cores = new DesertWormCore[BodyCount + 1];
    readonly Transform[] sections = new Transform[BodyCount];
    SandSpine spine;
    Vector3 previous;
    float routeWidth, routeHeight, routeCentre;
    float routeClock;
    bool entered;
    Vector3 rippleTarget;
    Vector2 jawAxis = Vector2.down;
    static readonly Vector2[] Route ={new Vector2(0,1),new Vector2(-1,.72f),new Vector2(-.86f,-.84f),
        new Vector2(.80f,-1),new Vector2(1,.63f),new Vector2(-.70f,1),new Vector2(-.12f,-.92f),
        new Vector2(.95f,-.48f),new Vector2(.72f,.95f),new Vector2(-1,.14f),new Vector2(.18f,-.65f),new Vector2(.36f,1)};
    bool completing;
    public int RemainingParts { get; private set; } = BodyCount + 1;
    public DesertWormCore[] Cores => cores;
    protected override void Configure()
    {
        hull = 33;
        hullScale = 1.10f;
        health = 2200;
        attackInterval = Timing(.98f, .75f);
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
        routeHeight = camera.orthographicSize - 1.35f;
        routeCentre = camera.transform.position.y;
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

    protected override void MoveBoss(float dt)
    {
        var entry = new Vector3(0, routeCentre + routeHeight, 1);
        if (!entered)
        {
            transform.position = Vector3.MoveTowards(transform.position, entry, dt * 5.2f);
            entered = Vector3.Distance(transform.position, entry) < .02f;
        }
        else
        {
            int leg = Mathf.FloorToInt(routeClock) % Route.Length;
            Vector2 a = Route[leg], b = Route[(leg + 1) % Route.Length];
            float distance = Vector2.Scale(b - a, new Vector2(routeWidth, routeHeight)).magnitude;
            routeClock += dt * (phaseTwo ? 7.3f : 5.8f) / Mathf.Max(3, distance);
            leg = Mathf.FloorToInt(routeClock) % Route.Length;
            float t = routeClock - Mathf.Floor(routeClock);
            // Rounded corners keep the entire articulated body flowing through a full-arena route.
            a = Route[leg];
            b = Route[(leg + 1) % Route.Length];
            Vector2 before = Route[(leg + Route.Length - 1) % Route.Length], after = Route[(leg + 2) % Route.Length];
            Vector2 p = .5f * ((2 * a) + (-before + b) * t + (2 * before - 5 * a + 4 * b - after) * t * t + (-before + 3 * a - 3 * b + after) * t * t * t);
            p.x = Mathf.Clamp(p.x, -1, 1);
            p.y = Mathf.Clamp(p.y, -1, 1);
            var target = new Vector3(p.x * routeWidth, routeCentre + p.y * routeHeight, 1);
            transform.position = Vector3.MoveTowards(transform.position, target, dt * (phaseTwo ? 8.4f : 6.8f));
        }
        Vector2 tangent = transform.position - previous;
        previous = transform.position;
        if (tangent.sqrMagnitude > .00001f)
            Hull.rotation = Quaternion.FromToRotation(Vector3.down, tangent);
        spine?.Follow(transform.position, sections);
    }

    protected override bool PhaseTwoReady => RemainingParts <= 4;
    protected override void OnPhaseTwo()
    {
        nextAttack = age + 1.1f;
        volley = 0;
    }

    protected override void Attack()
    {
        int beat = (volley - 1) % 12;
        if (beat == 0)
            rippleTarget = player != null ? player.position : transform.position + Vector3.down * 8;
        // Body ripple, pause, jaw burst, pause, crossed body fans. Dead sections are permanently silent.
        if (beat < 4)
        {
            for (int i = 1; i < cores.Length; i++)
                if ((i - 1) % 4 == beat && CanShoot(cores[i]))
                {
                    Vector3 port = cores[i].transform.position;
                    Vector2 aim = (rippleTarget - port).normalized;
                    PetalFan<SimpleRound>(port, aim, Shots(3, 5), 25, 2.7f, .28f, .22f);
                }
        }
        else if ((beat == 5 || beat == 6) && CanShoot(cores[0]))
        {
            Vector3 jaw = Hull.TransformPoint(new Vector3(0, -.92f, -.18f));
            if (beat == 5)
                jawAxis = Hull.TransformDirection(Vector3.down);
            PetalFan<NeedleRound>(jaw, CombatVisuals.Rotate(jawAxis, beat == 5 ? -7 : 7), Shots(phaseTwo ? 5 : 3, phaseTwo ? 7 : 5), 15, 4.5f, .21f);
        }
        else if (beat == 9 || beat == 10)
        {
            for (int i = 1; i < cores.Length; i++)
                if (i % 2 == beat % 2 && CanShoot(cores[i]))
                {
                    Vector3 port = cores[i].transform.position;
                    int count = Shots(2, 3);
                    for (int j = 0; j < count; j++)
                        Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, (j - (count - 1) * .5f) * 33 + (beat == 9 ? -12 : 12)), 2.3f, .31f);
                }
        }
        // When only a few reactors remain, each surviving part owns a readable answer on the shared rest.
        if (phaseTwo && beat == 7)
            for (int i = 0; i < cores.Length; i++)
                if (i % 2 == (volley / 12) % 2 && CanShoot(cores[i]))
                {
                    Vector3 port = cores[i].transform.position;
                    Vector2 aim = CombatVisuals.Aim(port);
                    PetalFan<SimpleRound>(port, aim, Shots(3, 5), 29, 1.8f, .32f, .18f);
                }
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        int beat = volley % 12;
        if (beat == 5 || beat == 6)
        {
            Cue(Muzzle(), seconds);
            PointWeapon(Muzzle(), Hull.TransformDirection(Vector3.down));
        }
        else
            for (int i = 1; i < cores.Length; i++)
                if (cores[i] != null && CanShoot(cores[i]) && (beat < 4 && (i - 1) % 4 == beat || (beat == 9 || beat == 10) && i % 2 == beat % 2 || phaseTwo && beat == 7 && i % 2 == ((volley + 1) / 12) % 2))
                    Cue(cores[i].transform.position, seconds);
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
