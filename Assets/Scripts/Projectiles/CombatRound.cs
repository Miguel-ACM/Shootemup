using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

// Every hostile round is bright or dark. The tone is authored per attack, never random.
public enum HostileTone
{
    Bright, Dark
}

public abstract class CombatRound : AEnemyBullet
{
    // The tone of the next launch. Enemies set it around their own fire; a round sets
    // it to its own tone while it updates, so splits, shards and sprays inherit it.
    public static HostileTone LaunchTone;
    public HostileTone Tone
    {
        get; private set;
    }
    static readonly Unity.Profiling.ProfilerMarker launchMarker = new Unity.Profiling.ProfilerMarker("CombatRound.Launch");
    static readonly Unity.Profiling.ProfilerMarker createMarker = new Unity.Profiling.ProfilerMarker("CombatRound.Create");
    // Dense late-phase cards (bomb shards, laces and curtains together) outgrew 420
    // live rounds; at the cap Launch drops rounds and rings and rows open holes.
    public const int Limit = 3600;
    public static readonly HashSet<CombatRound> Live = new HashSet<CombatRound>();
    static readonly Dictionary<Type, Stack<CombatRound>> pools = new Dictionary<Type, Stack<CombatRound>>();
    static int pooled;
    const int PoolBudget = 7400;
    const int ReserveScale = 4;
    const int StandardReserve = 800 * ReserveScale;
    // Older rounds always draw above rounds launched after them: each launch, reused
    // or new, sits a hair further from the camera than the previous one. The sequence
    // restarts whenever the arena is clear, or after a long unbroken stream.
    const int DepthSlots = 16384;
    const float DepthStep = .000004f;
    static int launchSequence;
    static int launches;
    static Rect border;
    static int borderFrame = -1;

    // The visible combat area: what the camera actually shows between the HUD margins.
    // Bounces and wall runners use it, so rounds turn on the real screen edges rather
    // than on the gameplay limits a little inside them.
    public static Rect Border
    {
        get
        {
            if (borderFrame == Time.frameCount)
                return border;
            borderFrame = Time.frameCount;
            var view = UnityEngine.Camera.main;
            if (view == null || !view.orthographic)
                border = Rect.MinMaxRect(Camera.LeftLimit, Camera.DownLimit, Camera.RightLimit, Camera.UpLimit);
            else
            {
                float h = view.orthographicSize, w = h * 405f / 720f;
                var c = view.transform.position;
                border = new Rect(c.x - w, c.y - h, w * 2, h * 2);
            }
            return border;
        }
    }
    public static int CreatedCount
    {
        get; private set;
    }

    protected Vector2 heading;
    protected Vector3 origin;
    protected float age;
    protected SpriteRenderer visual;
    protected Rigidbody2D body;
    protected Collider2D collision;
    ProjectileSpriteTint tint;
    Vector2 orientedHeading;
    bool needsOrientation;
    float localCollisionRadius, collisionReach;
    Rect[] pixelBoxes;
    public bool UsesPixelContact
    {
        get; private set;
    }

    public virtual float HitboxScale => ProjectilePixelContact.HitboxScale;
    public bool PixelOverlaps(Collider2D target) => !UsesPixelContact || ProjectilePixelContact.Overlap(transform, pixelBoxes, target, HitboxScale);
    protected float lifetime = 9;
    bool explicitLifetime, programBudgeted;
    bool leaving;
    Vector2 lastVelocity, exitVelocity;
    internal void SetLifetime(float seconds)
    {
        lifetime = seconds;
        explicitLifetime = true;
    }
    protected bool ended;
    public int generation;
    // Identifies one launch of this pooled object, so an owner holding a reference can tell
    // whether the round it fired is still the one flying.
    public int Serial
    {
        get; private set;
    }
    public Vector2 Heading => heading;
    public Color Accent => HostileProjectileArt.Accent(GetType());
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPools()
    {
        pools.Clear();
        Live.Clear();
        pooled = CreatedCount = launchSequence = 0;
        LaunchTone = HostileTone.Bright;
        SceneManager.sceneUnloaded -= PrunePools;
        SceneManager.sceneUnloaded += PrunePools;
    }

    static void PrunePools(Scene scene)
    {
        pooled = 0;
        foreach (var pool in pools.Values)
        {
            var retained = pool.ToArray();
            pool.Clear();
            foreach (var shot in retained)
                if (shot != null)
                {
                    pool.Push(shot);
                    pooled++;
                }
        }
    }

    public static T Launch<T>(Vector3 p, Vector2 direction, float velocity = 4, float radius = .18f) where T : CombatRound
    {
        using var sample = launchMarker.Auto();
        if (Live.Count >= Limit)
            return null;
        T shot = null;
        if (pools.TryGetValue(typeof(T), out var pool))
            while (pool.Count > 0 && shot == null)
            {
                shot = pool.Pop() as T;
                pooled--;
            }
        if (shot == null)
            shot = Create<T>();
        shot.gameObject.layer = ProjectileCollisionLayers.Hostile;
        // Never insert a whole newborn pixel-fixture crown into the contact tree.
        // Arm each body's exact collider only when it approaches a valid target.
        shot.body.simulated = false;
        // Beams are not rounds: they stay bright and are never absorbed.
        shot.Tone = shot is PulseLaser ? HostileTone.Bright : LaunchTone;
        shot.tint.SetTone(shot.Tone);
        shot.needsOrientation = true;
        if (Live.Count == 0 || launchSequence >= DepthSlots)
            launchSequence = 0;
        shot.transform.SetPositionAndRotation(new Vector3(p.x, p.y, -.5f + launchSequence++ * DepthStep), Quaternion.identity);
        shot.trailFrom = p;
        shot.transform.localScale = Vector3.one * radius * 2;
        shot.visual.sprite = HostileProjectileArt.Sprite(typeof(T), radius >= .25f);
        shot.visual.color = Color.white;
        shot.visual.enabled = true;
        shot.collision.enabled = true;
        shot.origin = shot.transform.position;
        shot.heading = direction.sqrMagnitude > .001f ? direction.normalized : Vector2.down;
        shot.speed = velocity * GameRules.enemyBulletSpeed * LevelRun.EnemyShotSpeedScale;
        shot.damage = 10 * GameRules.enemyDamage;
        shot.age = 0;
        shot.generation = 0;
        shot.Serial = ++launches;
        // Large slow geometry must still reach the bottom of the arena on Easy.
        // Offscreen culling and each delivery's own lifetime keep residency bounded.
        shot.lifetime = Mathf.Clamp((Camera.UpLimit - Camera.DownLimit + 2) / Mathf.Max(.75f, shot.speed), 9, 18);
        shot.ended = false;
        shot.enabled = true;
        shot.gameObject.SetActive(true);
        Live.Add(shot);
        shot.ResetProgram();
        shot.Initialize();
        shot.collisionReach = shot.localCollisionRadius * Mathf.Max(Mathf.Abs(shot.transform.localScale.x), Mathf.Abs(shot.transform.localScale.y));
        if (shot.UsesPixelContact && shot.collision is CircleCollider2D proxy)
            proxy.radius = shot.localCollisionRadius + ProjectilePixelContact.PolygonSkin / Mathf.Max(.00001f, Mathf.Abs(shot.transform.localScale.x));
        shot.UpdateSimulation();
        SoundManager.PlayEnemyLaunch(typeof(T), p);
        return shot;
    }

    static T Create<T>() where T : CombatRound
    {
        using var sample = createMarker.Auto();
        var go = new GameObject(typeof(T).Name);
        go.SetActive(false);
        go.tag = "EnemyBullet";
        go.layer = ProjectileCollisionLayers.Hostile;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = false;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CombatVisuals.Projectile();
        sr.sharedMaterial = CombatVisuals.ProjectileMaterial;
        sr.sortingOrder = 15;
        sr.sortingLayerName = "Bullet";
        var collider = go.AddComponent<CustomCollider2D>();
        collider.isTrigger = true;
        sr.sprite = HostileProjectileArt.Sprite(typeof(T));
        ProjectileSprites.Fit(collider, sr.sprite);
        var shot = go.AddComponent<T>();
        shot.body = rb;
        shot.visual = sr;
        shot.collision = collider;
        shot.localCollisionRadius = sr.sprite.bounds.extents.magnitude;
        // Giant orbs have a deliberately small damaging core. Keep their native
        // broad phase compact too, while retaining the exact scaled pixel mask.
        if (shot is GiantOrb) shot.localCollisionRadius *= shot.HitboxScale;
        shot.pixelBoxes = ProjectileSprites.Boxes(sr.sprite);
        if (!(shot is PlasmaOrb))
        {
            // Keep the authored compound shape as the collision reference, but
            // do not insert its 9-30 fixtures per round into Box2D's contact tree.
            collider.enabled = false;
            var contact = go.AddComponent<CircleCollider2D>();
            contact.isTrigger = true;
            contact.radius = shot.localCollisionRadius;
            shot.collision = contact;
        }
        // Plasma keeps its full shootable shell, but only its reduced core hurts the pilot.
        shot.UsesPixelContact = true;
        ProjectileSpriteTint.Apply(go, shot.Accent);
        shot.tint = go.GetComponent<ProjectileSpriteTint>();
        CreatedCount++;
        return shot;
    }

    public static IEnumerator PrepareBursts()
    {
        if (LevelRun.Current == LevelId.Void)
        {
            // Reserve every authored delivery behind the curtain, including its special
            // renderer and collider. The final score shares the existing global budget.
            // Counts are multiplied by ReserveScale inside both helpers: 1850 base slots.
            // Fracture lattice needs ricochets; scythe ribbons and one-side
            // interludes share warmed chevrons without changing total capacity.
            TrimReserve<SimpleRound>(800);TrimReserve<NeedleRound>(352-StyledReserveTotal(LevelId.Void));
            TrimReserve<RicochetBolt>(304);TrimReserve<BurstShell>(8);TrimReserve<ChevronRound>(112);
            TrimReserve<GuidedMissile>(8);TrimReserve<ScatterCarrier>(4);TrimReserve<HelixRound>(20);
            TrimReserve<PlasmaOrb>(20);TrimReserve<ProximityMine>(6);
            TrimReserve<PulseLaser>(12);TrimReserve<LaserSideRound>(4);TrimReserve<GiantOrb>(200);
            yield return Warm<SimpleRound>(800);yield return Warm<NeedleRound>(352-StyledReserveTotal(LevelId.Void));
            yield return Warm<RicochetBolt>(304);yield return Warm<BurstShell>(8);yield return Warm<ChevronRound>(112);
            yield return Warm<GuidedMissile>(8);yield return Warm<ScatterCarrier>(4);yield return Warm<HelixRound>(20);
            yield return Warm<PlasmaOrb>(20);yield return Warm<ProximityMine>(6);
            yield return Warm<PulseLaser>(12);yield return Warm<LaserSideRound>(4);yield return Warm<GiantOrb>(200);
            yield return WarmStyled(LevelId.Void);
            yield break;
        }
        // These districts now sustain arrow-heavy boss scores. Give their unused
        // specialist reserves to needles behind the curtain, not during a finale.
        // Scaled reserves retain 3200 standard rounds, 800 giant orbs and at least
        // thirty-two of every delivery within the 7400-object budget.
        bool arrowDistrict = LevelRun.Current == LevelId.Fields || LevelRun.Current == LevelId.Snow || LevelRun.Current == LevelId.City ||
            LevelRun.Current == LevelId.Heaven || LevelRun.Current == LevelId.Space;
        // Celestial scores trade unused specialist deliveries for prepared needles.
        // Space retains its ordinary carriers' ricochets; total reserve stays below 7400.
        int needles = LevelRun.Current == LevelId.Space ? 560 : arrowDistrict ? 700 : 300;
        // Arrow districts pay for their styled silhouettes from the needles they replace;
        // the other districts have spare room in the non-standard reserve.
        if (arrowDistrict)
            needles -= StyledReserveTotal(LevelRun.Current);
        int ricochets = LevelRun.Current == LevelId.Space ? 96 : arrowDistrict ? 8 : 96;
        int shells = arrowDistrict ? (LevelRun.Current == LevelId.Snow ? 40 : 8) : 40;
        // Chevron volleys reuse the retired disc reserve and a small share of
        // City/Snow's needles. Total warm capacity stays unchanged.
        bool chevronDistrict = LevelRun.Current == LevelId.City || LevelRun.Current == LevelId.Snow;
        int chevrons = arrowDistrict ? (LevelRun.Current == LevelId.City ? 32 : 16) : 72;
        if (chevronDistrict) { needles -= 32; chevrons += 32; }
        int missiles = arrowDistrict ? 8 : 24;
        int carriers = arrowDistrict ? (LevelRun.Current == LevelId.Fields ? 24 : 8) : 24;
        int helixes = arrowDistrict ? 8 : 32;
        int plasma = arrowDistrict ? (LevelRun.Current == LevelId.Snow ? 24 : 8) : 24;
        int mines = arrowDistrict ? 8 : 16;
        int sideRounds = arrowDistrict ? 8 : 16;
        int lasers = arrowDistrict ? (LevelRun.Current == LevelId.City ? 16 : 8) : 16;
        // Return this space on district changes so later districts can warm all
        // of their specialist deliveries within the same bounded cache.
        TrimReserve<NeedleRound>(needles);
        if (arrowDistrict)
        {
            TrimReserve<RicochetBolt>(ricochets); TrimReserve<BurstShell>(shells);
            TrimReserve<ChevronRound>(chevrons); TrimReserve<GuidedMissile>(missiles);
            TrimReserve<ScatterCarrier>(carriers); TrimReserve<HelixRound>(helixes);
            TrimReserve<PlasmaOrb>(plasma); TrimReserve<ProximityMine>(mines);
            TrimReserve<LaserSideRound>(sideRounds);
            TrimReserve<PulseLaser>(lasers);
        }
        // Reserve the pool budget across every delivery.
        // Previously only SimpleRound was ready: a first needle/ricochet volley
        // constructed its GameObjects, pixel colliders and special effects in combat.
        yield return Warm<SimpleRound>(800);
        yield return Warm<NeedleRound>(needles);
        yield return Warm<RicochetBolt>(ricochets);
        yield return Warm<BurstShell>(shells);
        yield return Warm<ChevronRound>(chevrons);
        yield return Warm<GuidedMissile>(missiles);
        yield return Warm<ScatterCarrier>(carriers);
        yield return Warm<HelixRound>(helixes);
        yield return Warm<PlasmaOrb>(plasma);
        yield return Warm<GiantOrb>(200);
        yield return Warm<ProximityMine>(mines);
        yield return Warm<LaserSideRound>(sideRounds);
        yield return Warm<PulseLaser>(lasers);
        yield return WarmStyled(LevelRun.Current);
    }

    // The silhouettes each district actually fires (Miguel's approved set, 9 November 2026).
    static int StyledReserve(LevelId district, Type type)
    {
        switch (district)
        {
            case LevelId.Fields: return type == typeof(PetalRound) ? 48 : 0;
            case LevelId.Snow: return type == typeof(CrystalRound) ? 40 : type == typeof(SnowflakeRound) ? 64 : 0;
            case LevelId.City: return type == typeof(KunaiRound) ? 48 : type == typeof(GemRound) ? 32 : 0;
            case LevelId.Tunnel: return type == typeof(RiceRound) ? 64 : 0;
            case LevelId.Thunder:
            case LevelId.Desert: return type == typeof(RiceRound) ? 48 : 0;
            case LevelId.Candyland: return type == typeof(HeartRound) || type == typeof(CandyRound) || type == typeof(StarRound) ? 40 : 0;
            case LevelId.Space: return type == typeof(CrescentRound) ? 48 : 0;
            case LevelId.Underwater: return type == typeof(DropletRound) ? 64 : 0;
            case LevelId.Hell: return type == typeof(FlameRound) ? 64 : 0;
            case LevelId.Heaven: return type == typeof(FeatherRound) ? 64 : type == typeof(StarRound) ? 32 : 0;
            case LevelId.Void: return type == typeof(KunaiRound) ? 40 : type == typeof(CrescentRound) ? 32 : type == typeof(GemRound) ? 24 : 0;
            default: return 0;
        }
    }

    static readonly Type[] StyledTypes = { typeof(RiceRound), typeof(KunaiRound), typeof(CrystalRound), typeof(DropletRound),
        typeof(StarRound), typeof(SnowflakeRound), typeof(HeartRound), typeof(CandyRound), typeof(PetalRound),
        typeof(FlameRound), typeof(FeatherRound), typeof(CrescentRound), typeof(GemRound) };

    static int StyledReserveTotal(LevelId district)
    {
        int total = 0;
        foreach (var type in StyledTypes)
            total += StyledReserve(district, type);
        return total;
    }

    // Return unused styled reserves to the budget first, then warm this district's.
    static IEnumerator WarmStyled(LevelId district)
    {
        TrimReserve<RiceRound>(StyledReserve(district, typeof(RiceRound)));
        TrimReserve<KunaiRound>(StyledReserve(district, typeof(KunaiRound)));
        TrimReserve<CrystalRound>(StyledReserve(district, typeof(CrystalRound)));
        TrimReserve<DropletRound>(StyledReserve(district, typeof(DropletRound)));
        TrimReserve<StarRound>(StyledReserve(district, typeof(StarRound)));
        TrimReserve<SnowflakeRound>(StyledReserve(district, typeof(SnowflakeRound)));
        TrimReserve<HeartRound>(StyledReserve(district, typeof(HeartRound)));
        TrimReserve<CandyRound>(StyledReserve(district, typeof(CandyRound)));
        TrimReserve<PetalRound>(StyledReserve(district, typeof(PetalRound)));
        TrimReserve<FlameRound>(StyledReserve(district, typeof(FlameRound)));
        TrimReserve<FeatherRound>(StyledReserve(district, typeof(FeatherRound)));
        TrimReserve<CrescentRound>(StyledReserve(district, typeof(CrescentRound)));
        TrimReserve<GemRound>(StyledReserve(district, typeof(GemRound)));
        yield return Warm<RiceRound>(StyledReserve(district, typeof(RiceRound)));
        yield return Warm<KunaiRound>(StyledReserve(district, typeof(KunaiRound)));
        yield return Warm<CrystalRound>(StyledReserve(district, typeof(CrystalRound)));
        yield return Warm<DropletRound>(StyledReserve(district, typeof(DropletRound)));
        yield return Warm<StarRound>(StyledReserve(district, typeof(StarRound)));
        yield return Warm<SnowflakeRound>(StyledReserve(district, typeof(SnowflakeRound)));
        yield return Warm<HeartRound>(StyledReserve(district, typeof(HeartRound)));
        yield return Warm<CandyRound>(StyledReserve(district, typeof(CandyRound)));
        yield return Warm<PetalRound>(StyledReserve(district, typeof(PetalRound)));
        yield return Warm<FlameRound>(StyledReserve(district, typeof(FlameRound)));
        yield return Warm<FeatherRound>(StyledReserve(district, typeof(FeatherRound)));
        yield return Warm<CrescentRound>(StyledReserve(district, typeof(CrescentRound)));
        yield return Warm<GemRound>(StyledReserve(district, typeof(GemRound)));
    }

    static void TrimReserve<T>(int count) where T : CombatRound
    {
        count *= ReserveScale;
        if (!pools.TryGetValue(typeof(T), out var pool)) return;
        while (pool.Count > count)
        {
            var spare = pool.Pop();
            pooled--;
            if (spare != null) Destroy(spare.gameObject);
        }
    }

    static IEnumerator Warm<T>(int count) where T : CombatRound
    {
        count *= ReserveScale;
        if (!pools.TryGetValue(typeof(T), out var pool))
            pools[typeof(T)] = pool = new Stack<CombatRound>();
        while (pool.Count < count && pooled < PoolBudget)
        {
            for (int i = 0; i < 8 && pool.Count < count && pooled < PoolBudget; i++)
            {
                var shot = Create<T>();
                shot.heading = Vector2.down;
                shot.transform.localScale = Vector3.one * .3f;
                shot.Initialize();
                shot.ended = true;
                pool.Push(shot);
                pooled++;
            }
            yield return null;
        }
    }

    // Every cleared round dissolves in a pop sized to itself rather than blinking out.
    public static void ClearAll()
    {
        if (Live.Count > 0)
            ClearEffects.ScreenClear();
        foreach (var shot in new List<CombatRound>(Live))
            if (shot != null)
                shot.Vanish();
    }

    protected override void Start()
    {
    }

    protected virtual void Initialize()
    {
    }

    protected virtual float ClockRate => 1;
    // The three-argument params overload allocated an array for every live round,
    // every frame. Keep the same conservative contact horizon without that churn.
    protected virtual float ContactSpeed => speed * Mathf.Max(1, Mathf.Max(rampTo, resumeSpeed)) + drift.magnitude + Mathf.Abs(swayAmplitude) * swayRate * 6.3f;
    protected override void Update()
    {
        if (ended || WorldTimeStop.Frozen || Time.deltaTime <= 0)
            return;
        float dt = Time.deltaTime * ClockRate * (this is PulseLaser ? 1 : CombatUpgrades.HostileSpeedAt(transform.position));
        age += dt;
        var ambient = LaunchTone;
        LaunchTone = Tone;
        UpdateRound(dt);
        LaunchTone = ambient;
    }

    void UpdateRound(float dt)
    {
        BudgetProgramFlight();
        if (transform.position.y < Camera.DownLimit - 2 || transform.position.y > Camera.UpLimit + 5 ||
            transform.position.x < Camera.LeftLimit - 3 || transform.position.x > Camera.RightLimit + 3)
        {
            Release();
            return;
        }
        if (age > lifetime && !(this is PulseLaser)) LeaveScene();
        Vector3 wallFrom = transform.position;
        if (leaving)
        {
            // Very slow spent beads ease into their departure instead of lingering
            // for a minute and consuming the live budget of the next phrase.
            exitVelocity = Vector2.MoveTowards(exitVelocity, exitVelocity.normalized * Mathf.Max(1.2f, exitVelocity.magnitude), dt * .75f);
            transform.position += (Vector3)exitVelocity * dt;
        }
        else
            Tick(dt);
        if (dt > 0) lastVelocity = (transform.position - wallFrom) / dt;
        if (!ended && !leaving)
            Program();
        if (!ended && !(this is PulseLaser) && PocketWind.Current != null)
            transform.position += PocketWind.Velocity(transform.position) * Time.deltaTime;
        if (!ended && !(this is PulseLaser) && collision.enabled && CombatWalls.Sweep(wallFrom, transform.position, .04f, out _, out _))
        {
            Release();
            return;
        }
        if (!ended)
            UpdateSimulation();
        if (!ended && Erratic && visual.enabled)
            RoundTrails.Follow(ref trailFrom, transform.position, tint != null ? tint.Tint : Accent, Mathf.Abs(transform.localScale.x) * .5f);
    }

    // Finishing a score never erases a visible round. Keep its current travel (the
    // tangent for an orbit), cease further turns/releases, and cull only offscreen.
    void LeaveScene()
    {
        if (leaving) return;
        leaving = true;
        exitVelocity = lastVelocity.sqrMagnitude > .04f ? lastVelocity :
            (heading.sqrMagnitude > .001f ? heading : Vector2.down) * Mathf.Max(.8f, speed);
        heading = exitVelocity.normalized;
        showTurn = false;
        needsOrientation = true;
        Orient();
    }

    // A launch-time straight-flight estimate cannot cover a brake, hold, turn and return
    // across the screen. Budget the configured journey once, using the slowest travel leg.
    void BudgetProgramFlight()
    {
        if (programBudgeted) return;
        programBudgeted = true;
        if (explicitLifetime || (pauseAt < 0 && bounces <= 0 && rampTo >= 1 && wallRunFor <= 0)) return;
        var edge = Border;
        float across = Mathf.Sqrt(edge.width * edge.width + edge.height * edge.height) + 4;
        float slowest = Mathf.Max(.25f, speed * Mathf.Min(1, Mathf.Min(rampTo, pauseAt >= 0 ? resumeSpeed : 1)));
        float hold = pauseAt >= 0 && float.IsFinite(pauseHold) ? Mathf.Max(0, pauseAt + pauseHold - age) : 0;
        float journey = hold + .5f + across * (1 + Mathf.Min(bounces, 3)) / slowest + wallRunFor;
        lifetime = Mathf.Max(lifetime, Mathf.Min(60, age + journey));
    }

    // Rounds whose path wanders leave a short trail in their colour.
    protected virtual bool Erratic => gravity != Vector2.zero || pull != 0 || swayAmplitude != 0 || zigEvery > 0 || bounces > 0 || wallRunFor > 0;
    Vector2 trailFrom;

    void UpdateSimulation()
    {
        // Destructible plasma must remain hittable by player fire anywhere.
        // Beams do their own analytic collision; mines keep their arming rules.
        bool simulate = this is PlasmaOrb || (!(this is PulseLaser) && collision.enabled &&
            ProjectileCollisionWindow.Relevant(transform.position, collisionReach, ContactSpeed));
        if (body.simulated != simulate)
            body.simulated = simulate;
    }

    protected virtual void Tick(float dt)
    {
        Advance(dt);
        Orient();
    }

    // Authored danmaku behaviours, configured through RoundProgram after launch.
    // Every field resets on launch; nothing allocates and no child objects are added.
    public enum Turn
    {
        Keep, Aim, Rotate, Scatter, Down
    }

    internal float rampAt, rampOver, rampTo;
    internal float pauseAt, pauseHold, resumeAngle, resumeSpeed;
    internal Turn resumeTurn;
    internal bool resumed;
    // While held, the round turns to face the heading it will resume on.
    internal bool showTurn;
    float scatterAngle;
    bool scattered;
    internal float fadeAt;
    internal int bounces;
    internal bool bounceCeiling;
    internal Vector2 gravity, drift, well;
    internal float pull;
    internal Transform sink;
    internal float sinkRadius;
    internal float swayAmplitude, swayRate;
    internal float zigEvery, zigAngle, nextZig;
    internal int zigSign;
    internal float splitAt, splitSpread, splitSpeed, splitRadius;
    internal int splitCount;
    internal bool splitAimed;
    internal float wallRunFor, wallRunStart;
    internal int wallDirection, wallSide;
    Vector2 travel;
    public bool Paused => pauseAt >= 0 && age >= pauseAt && age < pauseAt + pauseHold;
    // A purely visual round, such as the spray thrown up to make snow: no contact at all.
    // Launch re-enables the collider for the next use of the pooled round.
    internal void MakeHarmless() => collision.enabled = false;
    public bool RunningWall => wallSide >= 0;

    void ResetProgram()
    {
        explicitLifetime = programBudgeted = false;
        leaving = false;
        lastVelocity = exitVelocity = Vector2.zero;
        rampAt = rampOver = 0;
        rampTo = 1;
        pauseAt = fadeAt = splitAt = -1;
        pauseHold = resumeAngle = 0;
        resumeSpeed = 1;
        resumeTurn = Turn.Keep;
        resumed = false;
        showTurn = scattered = false;
        bounces = 0;
        bounceCeiling = true;
        gravity = drift = travel = well = Vector2.zero;
        pull = 0;
        sink = null;
        sinkRadius = 0;
        swayAmplitude = swayRate = 0;
        zigEvery = zigAngle = nextZig = 0;
        zigSign = 1;
        splitCount = 0;
        splitAimed = false;
        splitSpread = splitSpeed = splitRadius = 0;
        wallRunFor = wallRunStart = 0;
        wallDirection = 0;
        wallSide = -1;
    }

    protected float SpeedFactor()
    {
        float factor = rampOver > 0 ? Mathf.Lerp(1, rampTo, Mathf.Clamp01((age - rampAt) / rampOver)) : 1;
        if (pauseAt < 0)
            return factor;
        float resume = pauseAt + pauseHold;
        if (age < pauseAt)
            return factor * Mathf.Clamp01((pauseAt - age) / Mathf.Max(.05f, Mathf.Min(.35f, pauseAt)));
        if (age < resume)
            return 0;
        if (!resumed)
        {
            resumed = true;
            Redirect();
        }
        return factor * resumeSpeed * Mathf.SmoothStep(0, 1, Mathf.Clamp01((age - resume) / .3f));
    }

    void Redirect()
    {
        heading = ResumeHeading();
        needsOrientation = true;
    }

    // The heading a held round will resume on. An aimed round keeps tracking the pilot.
    Vector2 ResumeHeading()
    {
        switch (resumeTurn)
        {
            case Turn.Aim:
                return CombatVisuals.Rotate(CombatVisuals.Aim(transform.position), resumeAngle);
            case Turn.Rotate:
                return CombatVisuals.Rotate(heading, resumeAngle);
            case Turn.Scatter:
                if (!scattered)
                {
                    scatterAngle = UnityEngine.Random.Range(-resumeAngle, resumeAngle);
                    scattered = true;
                }
                return CombatVisuals.Rotate(heading, scatterAngle);
            case Turn.Down:
                return CombatVisuals.Rotate(Vector2.down, resumeAngle);
            default:
                return heading;
        }
    }

    // Signal control: brake to a stop now and hold until Proceed(), then resume on the
    // given turn. The round faces its next heading while it waits.
    public void Halt(float brake, Turn turn, float angle, float speed)
    {
        if (ended)
            return;
        pauseAt = age + Mathf.Max(.02f, brake);
        pauseHold = float.PositiveInfinity;
        resumeTurn = turn;
        resumeAngle = angle;
        resumeSpeed = speed;
        resumed = scattered = false;
        showTurn = true;
        programBudgeted = false;
    }

    // Releases a Halt(): the round sets off on its turn from where it stands.
    public void Proceed()
    {
        if (ended || resumed || pauseAt < 0)
            return;
        if (age < pauseAt)
            pauseAt = age;
        pauseHold = age - pauseAt;
        programBudgeted = false;
    }

    public bool Halted => pauseAt >= 0 && !resumed;

    // Turns the sprite toward the resume heading during a hold, finishing as the hold ends.
    void FaceResume()
    {
        Vector2 current = orientedHeading.sqrMagnitude > .001f ? orientedHeading : heading;
        float angle = Vector2.SignedAngle(current, ResumeHeading());
        float left = pauseAt + pauseHold - age, dt = Time.deltaTime;
        float step = left > dt ? Mathf.Max(540 * dt, Mathf.Abs(angle) * dt / left) : Mathf.Abs(angle);
        var facing = CombatVisuals.Rotate(current, Mathf.Sign(angle) * Mathf.Min(Mathf.Abs(angle), step));
        transform.rotation = Quaternion.FromToRotation(Vector3.up, facing);
        orientedHeading = facing;
    }

    protected void Advance(float dt)
    {
        if (wallSide >= 0)
        {
            RunWall(dt);
            return;
        }
        float factor = SpeedFactor();
        Vector2 step = heading * speed * factor;
        if (pull != 0)
        {
            // A point well: inverse-square-like pull, softened so nothing reaches infinite speed.
            // A negative strength repels; a sink well follows its moving body.
            if (sink != null)
                well = sink.position;
            Vector2 toward = well - (Vector2)transform.position;
            drift += toward.normalized * pull / (toward.sqrMagnitude * .2f + 1) * dt;
            if (sink != null && toward.sqrMagnitude > .0001f)
            {
                // A sink spirals its rounds in instead of letting them settle into orbit:
                // the heading turns inward and sideways drift bleeds away.
                var inward = toward.normalized;
                heading = Vector3.RotateTowards(heading, inward, 1.1f * dt, 0);
                drift -= (drift - inward * Vector2.Dot(drift, inward)) * Mathf.Min(1, 1.4f * dt);
                needsOrientation = true;
            }
        }
        if (gravity != Vector2.zero)
            drift += gravity * dt;
        step += drift;
        if (swayAmplitude != 0 && factor > 0)
        {
            float w = swayRate * Mathf.PI * 2;
            step += new Vector2(-heading.y, heading.x) * swayAmplitude * w * Mathf.Cos(w * age);
        }
        transform.position += (Vector3)step * dt;
        if (step.sqrMagnitude > .0001f)
            travel = step.normalized;
        if (zigEvery > 0 && age >= nextZig && factor > 0)
        {
            nextZig = age + zigEvery;
            heading = CombatVisuals.Rotate(heading, zigSign * zigAngle * 2);
            zigSign = -zigSign;
            needsOrientation = true;
        }
        Walls();
    }

    void Walls()
    {
        var p = transform.position;
        if (wallDirection != 0)
        {
            const float inset = .15f;
            var edge = Border;
            if (p.x <= edge.xMin + inset || p.x >= edge.xMax - inset || p.y >= edge.yMax - inset || p.y <= edge.yMin + inset)
            {
                // 0 left, 1 top, 2 right, 3 bottom: clockwise order around the arena.
                float left = p.x - edge.xMin, right = edge.xMax - p.x, top = edge.yMax - p.y, bottom = p.y - edge.yMin;
                float least = Mathf.Min(Mathf.Min(left, right), Mathf.Min(top, bottom));
                wallSide = least == left ? 0 : least == top ? 1 : least == right ? 2 : 3;
                wallRunStart = age;
                PSXImpactEffects.Vanish(p, Accent, .2f);
            }
            return;
        }
        if (bounces <= 0)
            return;
        // Turn as the round's edge meets the visible border.
        var bounds = Border;
        float reach = BorderReach;
        bool side = p.x < bounds.xMin + reach && heading.x < 0 || p.x > bounds.xMax - reach && heading.x > 0;
        bool ceiling = bounceCeiling && p.y > bounds.yMax - reach && heading.y > 0;
        if (!side && !ceiling)
            return;
        bounces--;
        SoundManager.PlayEnemyEvent("ricochetHit");
        if (side)
            heading.x = -heading.x;
        if (ceiling)
            heading.y = -heading.y;
        p.x = Mathf.Clamp(p.x, bounds.xMin + reach, bounds.xMax - reach);
        transform.position = p;
        needsOrientation = true;
    }

    // Half the round's visible size: how far its centre sits from a wall it touches.
    public float BorderReach => visual != null ? Mathf.Min(visual.bounds.extents.x, visual.bounds.extents.y) : .1f;

    void RunWall(float dt)
    {
        // Runners hug the visible border, their edge on the screen edge.
        var edge = Border;
        float inset = BorderReach;
        float xMin = edge.xMin + inset, xMax = edge.xMax - inset, yMin = edge.yMin + inset, yMax = edge.yMax - inset;
        var p = (Vector2)transform.position;
        float travel = speed * dt;
        for (int guard = 0; guard < 3 && travel > 0; guard++)
        {
            // Clockwise: up the left wall, right along the top, down the right, left along the bottom.
            int side = wallSide;
            Vector2 direction = side == 0 ? Vector2.up : side == 1 ? Vector2.right : side == 2 ? Vector2.down : Vector2.left;
            if (wallDirection < 0)
                direction = -direction;
            if (side == 0 || side == 2)
                p.x = side == 0 ? xMin : xMax;
            else
                p.y = side == 1 ? yMax : yMin;
            float limit = direction.y > 0 ? yMax - p.y : direction.y < 0 ? p.y - yMin : direction.x > 0 ? xMax - p.x : p.x - xMin;
            float move = Mathf.Min(travel, Mathf.Max(0, limit));
            p += direction * move;
            travel -= move;
            if (travel > 0)
                wallSide = (side + (wallDirection > 0 ? 1 : 3)) % 4;
            heading = direction;
        }
        transform.position = new Vector3(p.x, p.y, transform.position.z);
        needsOrientation = true;
        if (age - wallRunStart > wallRunFor)
            LeaveScene();
    }

    void Program()
    {
        // Swallowed by the body that absorbs it.
        if (sink != null && sinkRadius > 0 && ((Vector2)(sink.position - transform.position)).sqrMagnitude < sinkRadius * sinkRadius)
        {
            Vanish();
            return;
        }
        if (Paused)
        {
            float glint = 1.12f + .1f * Mathf.Sin(age * 22);
            visual.color = new Color(glint, glint, glint, 1);
        }
        else if (resumed && pauseHold > 0 && age < pauseAt + pauseHold + .1f)
            visual.color = Color.white;
        if (splitAt >= 0 && age >= splitAt && Live.Count + splitCount <= Limit)
        {
            if (splitAimed)
                heading = CombatVisuals.Aim(transform.position);
            for (int i = 0; i < splitCount; i++)
            {
                float lane = splitCount <= 1 ? 0 : i - (splitCount - 1) * .5f;
                var shard = Launch<SimpleRound>(transform.position, CombatVisuals.Rotate(heading, lane * splitSpread), splitSpeed, splitRadius);
                if (shard != null)
                    shard.generation = generation + 1;
            }
            Release();
            return;
        }
        if (fadeAt >= 0 && age >= fadeAt)
            LeaveScene();
    }

    // Symmetric silhouettes either keep still (hearts, gems) or turn at their own rate
    // (stars, flakes, sweets, moons) instead of pointing along their heading.
    protected virtual bool Upright => false;
    protected virtual float Spin => 0;

    protected void Orient()
    {
        if (Spin != 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, age * Spin * (heading.x < 0 ? -1 : 1));
            return;
        }
        if (Upright)
        {
            if (needsOrientation)
                transform.rotation = Quaternion.identity;
            needsOrientation = false;
            return;
        }
        if (showTurn && Paused)
        {
            FaceResume();
            return;
        }
        // Arrows under gravity or sway point along their actual path, not their launch heading.
        Vector2 facing = gravity != Vector2.zero || pull != 0 || swayAmplitude != 0 ? travel : heading;
        if (!needsOrientation && facing == orientedHeading)
            return;
        if (facing.sqrMagnitude > .001f)
        {
            transform.rotation = Quaternion.FromToRotation(this is LaserSideRound ? Vector3.right : Vector3.up, facing);
            orientedHeading = facing;
            needsOrientation = false;
        }
    }

    protected void Release()
    {
        if (ended)
            return;
        ended = true;
        Live.Remove(this);
        body.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);
        if (!pools.TryGetValue(GetType(), out var pool))
            pools[GetType()] = pool = new Stack<CombatRound>();
        int standard = pools.TryGetValue(typeof(SimpleRound), out var simple) ? simple.Count : 0;
        // Protect the actual warmed standard reserve. Using the larger live cap
        // here discarded prepared needles on clears and forced later reallocation.
        bool room = pooled < PoolBudget &&
            (this is SimpleRound || pooled - standard < PoolBudget - StandardReserve);
        if (pool.Count < Limit && room)
        {
            pool.Push(this);
            pooled++;
        }
        else
            Destroy(gameObject);
    }

    public override void Vanish()
    {
        if (!ended)
        {
            var size = visual.bounds.size;
            PSXImpactEffects.Vanish(transform.position, tint.Tint, Mathf.Max(size.x, size.y));
        }
        Release();
    }

    protected override void OnBecameInvisible()
    {
    } // Lifetime and arena bounds also cover offscreen spawns.
    void OnDisable()
    {
        Live.Remove(this);
    }

    protected bool Ring(int count, float velocity, float radius = .13f, float phase = 0)
    {
        if (generation > 0 || count < 1 || Live.Count + count > Limit)
            return false;
        // Commit the entire ring together. A charged shell waits for capacity
        // instead of losing whichever angles happened to be emitted last.
        for (int i = 0; i < count; i++)
        {
            var s = Launch<SimpleRound>(transform.position, CombatVisuals.Rotate(Vector2.down, phase + i * 360f / count), velocity, radius);
            if (s != null)
                s.generation = 1;
        }
        return true;
    }
}
