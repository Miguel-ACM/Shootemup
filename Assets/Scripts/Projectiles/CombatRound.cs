using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

public abstract class CombatRound : AEnemyBullet
{
    static readonly Unity.Profiling.ProfilerMarker launchMarker = new Unity.Profiling.ProfilerMarker("CombatRound.Launch");
    static readonly Unity.Profiling.ProfilerMarker createMarker = new Unity.Profiling.ProfilerMarker("CombatRound.Create");
    public const int Limit = 280;
    public static readonly HashSet<CombatRound> Live = new HashSet<CombatRound>();
    static readonly Dictionary<Type, Stack<CombatRound>> pools = new Dictionary<Type, Stack<CombatRound>>();
    static int pooled;
    const int PoolBudget = 768;
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

    public bool PixelOverlaps(Collider2D target) => !UsesPixelContact || ProjectilePixelContact.Overlap(transform, pixelBoxes, target);
    protected float lifetime = 9;
    protected bool ended;
    public int generation;
    public Vector2 Heading => heading;
    public Color Accent => HostileProjectileArt.Accent(GetType());
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPools()
    {
        pools.Clear();
        Live.Clear();
        pooled = CreatedCount = 0;
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
        shot.tint.SetHostile();
        shot.needsOrientation = true;
        shot.transform.SetPositionAndRotation(new Vector3(p.x, p.y, -.5f), Quaternion.identity);
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
        // Large slow geometry must still reach the bottom of the arena on Easy.
        // Offscreen culling and each delivery's own lifetime keep residency bounded.
        shot.lifetime = Mathf.Clamp((Camera.UpLimit - Camera.DownLimit + 2) / Mathf.Max(.75f, shot.speed), 9, 18);
        shot.ended = false;
        shot.enabled = true;
        shot.gameObject.SetActive(true);
        Live.Add(shot);
        shot.ResetProgram();
        shot.Initialize();
        shot.baseScale = shot.transform.localScale;
        shot.collisionReach = shot.localCollisionRadius * Mathf.Max(Mathf.Abs(shot.transform.localScale.x), Mathf.Abs(shot.transform.localScale.y));
        if (shot.UsesPixelContact && shot.collision is CircleCollider2D proxy)
            proxy.radius = shot.localCollisionRadius + ProjectilePixelContact.PolygonSkin / Mathf.Max(.00001f, Mathf.Abs(shot.transform.localScale.x));
        shot.UpdateSimulation();
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
            shot.UsesPixelContact = true;
        }
        ProjectileSpriteTint.Apply(go, shot.Accent);
        shot.tint = go.GetComponent<ProjectileSpriteTint>();
        CreatedCount++;
        return shot;
    }

    public static IEnumerator PrepareBursts()
    {
        // Reserve exactly the existing 768-object budget, across every delivery.
        // Previously only SimpleRound was ready: a first needle/ricochet volley
        // constructed its GameObjects, pixel colliders and special effects in combat.
        yield return Warm<SimpleRound>(280);
        yield return Warm<NeedleRound>(128);
        yield return Warm<RicochetBolt>(96);
        yield return Warm<BurstShell>(40);
        yield return Warm<SuspendedRound>(48);
        yield return Warm<GuidedMissile>(24);
        yield return Warm<ScatterCarrier>(24);
        yield return Warm<HelixRound>(32);
        yield return Warm<PlasmaOrb>(24);
        yield return Warm<ProximityMine>(16);
        yield return Warm<ReturningDisc>(24);
        yield return Warm<LaserSideRound>(16);
        yield return Warm<PulseLaser>(16);
    }

    static IEnumerator Warm<T>(int count) where T : CombatRound
    {
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

    public static void ClearAll()
    {
        foreach (var shot in new List<CombatRound>(Live))
            if (shot != null)
                shot.Release();
    }

    protected override void Start()
    {
    }

    protected virtual void Initialize()
    {
    }

    protected virtual float ClockRate => 1;
    protected virtual float ContactSpeed => speed * Mathf.Max(1, rampTo, resumeSpeed) + drift.magnitude + swayAmplitude * swayRate * 6.3f;
    protected override void Update()
    {
        if (ended || WorldTimeStop.Frozen || Time.deltaTime <= 0)
            return;
        float dt = Time.deltaTime * ClockRate * (this is PulseLaser ? 1 : CombatUpgrades.HostileSpeedAt(transform.position));
        age += dt;
        if (age > lifetime || transform.position.y < Camera.DownLimit - 2 || transform.position.y > Camera.UpLimit + 5 ||
            transform.position.x < Camera.LeftLimit - 3 || transform.position.x > Camera.RightLimit + 3)
        {
            Release();
            return;
        }
        Tick(dt);
        if (!ended)
            Program();
        if (!ended && !(this is PulseLaser) && PocketWind.Current != null)
            transform.position += PocketWind.Velocity(transform.position) * Time.deltaTime;
        if (!ended)
            UpdateSimulation();
    }

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
    internal float fadeAt;
    internal int bounces;
    internal Vector2 gravity, drift;
    internal float swayAmplitude, swayRate;
    internal float zigEvery, zigAngle, nextZig;
    internal int zigSign;
    internal float splitAt, splitSpread, splitSpeed, splitRadius;
    internal int splitCount;
    internal bool splitAimed;
    internal float wallRunFor, wallRunStart;
    internal int wallDirection, wallSide;
    Vector3 baseScale;
    public bool Paused => pauseAt >= 0 && age >= pauseAt && age < pauseAt + pauseHold;
    public bool RunningWall => wallSide >= 0;

    void ResetProgram()
    {
        rampAt = rampOver = 0;
        rampTo = 1;
        pauseAt = fadeAt = splitAt = -1;
        pauseHold = resumeAngle = 0;
        resumeSpeed = 1;
        resumeTurn = Turn.Keep;
        resumed = false;
        bounces = 0;
        gravity = drift = Vector2.zero;
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
        switch (resumeTurn)
        {
            case Turn.Aim:
                heading = CombatVisuals.Rotate(CombatVisuals.Aim(transform.position), resumeAngle);
                break;
            case Turn.Rotate:
                heading = CombatVisuals.Rotate(heading, resumeAngle);
                break;
            case Turn.Scatter:
                heading = CombatVisuals.Rotate(heading, UnityEngine.Random.Range(-resumeAngle, resumeAngle));
                break;
            case Turn.Down:
                heading = CombatVisuals.Rotate(Vector2.down, resumeAngle);
                break;
        }
        needsOrientation = true;
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
        if (gravity != Vector2.zero)
        {
            drift += gravity * dt;
            step += drift;
        }
        if (swayAmplitude > 0 && factor > 0)
        {
            float w = swayRate * Mathf.PI * 2;
            step += new Vector2(-heading.y, heading.x) * swayAmplitude * w * Mathf.Cos(w * age);
        }
        transform.position += (Vector3)step * dt;
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
            const float inset = .3f;
            if (p.x <= Camera.LeftLimit + inset || p.x >= Camera.RightLimit - inset || p.y >= Camera.UpLimit - inset || p.y <= Camera.DownLimit + inset)
            {
                // 0 left, 1 top, 2 right, 3 bottom: clockwise order around the arena.
                float left = p.x - Camera.LeftLimit, right = Camera.RightLimit - p.x, top = Camera.UpLimit - p.y, bottom = p.y - Camera.DownLimit;
                float least = Mathf.Min(Mathf.Min(left, right), Mathf.Min(top, bottom));
                wallSide = least == left ? 0 : least == top ? 1 : least == right ? 2 : 3;
                wallRunStart = age;
                PSXImpactEffects.Vanish(p, Accent, .2f);
            }
            return;
        }
        if (bounces <= 0)
            return;
        bool side = p.x < Camera.LeftLimit + .15f && heading.x < 0 || p.x > Camera.RightLimit - .15f && heading.x > 0;
        bool ceiling = p.y > Camera.UpLimit - .15f && heading.y > 0;
        if (!side && !ceiling)
            return;
        bounces--;
        if (side)
            heading.x = -heading.x;
        if (ceiling)
            heading.y = -heading.y;
        p.x = Mathf.Clamp(p.x, Camera.LeftLimit + .15f, Camera.RightLimit - .15f);
        transform.position = p;
        needsOrientation = true;
    }

    void RunWall(float dt)
    {
        const float inset = .3f;
        float xMin = Camera.LeftLimit + inset, xMax = Camera.RightLimit - inset, yMin = Camera.DownLimit + inset, yMax = Camera.UpLimit - inset;
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
        if (age - wallRunStart > wallRunFor && fadeAt < 0)
            fadeAt = age;
    }

    void Program()
    {
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
        {
            float t = (age - fadeAt) / .3f;
            if (t >= 1)
            {
                Release();
                return;
            }
            transform.localScale = baseScale * (1 - t * t);
        }
    }

    protected void Orient()
    {
        if (!needsOrientation && heading == orientedHeading && !(this is ReturningDisc))
            return;
        if (heading.sqrMagnitude > .001f)
        {
            transform.rotation = Quaternion.FromToRotation(this is LaserSideRound ? Vector3.right : Vector3.up, heading);
            orientedHeading = heading;
            needsOrientation = false;
        }
    }

    protected void Release()
    {
        if (ended)
            return;
        ended = true;
        body.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);
        if (!pools.TryGetValue(GetType(), out var pool))
            pools[GetType()] = pool = new Stack<CombatRound>();
        int standard = pools.TryGetValue(typeof(SimpleRound), out var simple) ? simple.Count : 0;
        bool room = this is SimpleRound ? pooled < PoolBudget : pooled - standard < PoolBudget - Limit;
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
            PSXImpactEffects.Vanish(transform.position, Accent, .25f);
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
