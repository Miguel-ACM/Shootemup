using UnityEngine;
using System.Collections.Generic;

public enum PlayerDelivery
{
    Pulse, Needle, Shell, Disc, Missile, Flame
}
public sealed class PlayerRound : ABullet
{
    static readonly Unity.Profiling.ProfilerMarker spawnMarker = new Unity.Profiling.ProfilerMarker("PlayerRound.Spawn");
    static readonly Unity.Profiling.ProfilerMarker createMarker = new Unity.Profiling.ProfilerMarker("PlayerRound.Create");
    public static int CreatedCount { get; private set; }
    public const int Limit = 180;
    static readonly Queue<PlayerRound> pool = new Queue<PlayerRound>(Limit);
    static Transform poolRoot;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPool()
    {
        pool.Clear();
        poolRoot = null;
        LiveCount = CreatedCount = 0;
    }

    static void EnsurePool()
    {
        if (poolRoot != null) return;
        // Scene-owned: a district unload releases the native bodies, sprites and materials.
        pool.Clear();
        poolRoot = new GameObject("Friendly projectile pool").transform;
    }

    public static System.Collections.IEnumerator Prepare()
    {
        EnsurePool();
        int batch = 0;
        while (pool.Count + LiveCount < Limit)
        {
            var shot = Create();
            shot.Initialize(Vector3.zero, Vector2.up, 1, 1, PlayerDelivery.Pulse, 0);
            pool.Enqueue(shot);
            if (++batch % 12 == 0) yield return null;
        }
    }
    public static int LiveCount
    {
        get; private set;
    }

    public PlayerDelivery delivery;
    public int generation;
    public bool companion;
    public int pierces;
    public bool returns;
    float age;
    Vector3 origin;
    Vector2 heading;
    bool detonated, returning, rebounded, rubberReturned;
    int chargeLevel;
    float blastDamage;
    readonly HashSet<AEnemy> hit = new HashSet<AEnemy>();
    SpriteRenderer visual;
    public void ArmChargeBlast(int level)
    {
        chargeLevel = level;
        blastDamage = damage * .8f;
        damage *= .35f;
        pierces = 0;
        returns = false;
    }

    public void ExcludePreviousHit(AEnemy enemy)
    {
        hit.Add(enemy);
        rebounded = true;
    }

    public static PlayerRound Spawn(Vector3 p, Vector2 d, float damage, float speed, PlayerDelivery mode = PlayerDelivery.Pulse, int generation = 0)
    {
        using var sample = spawnMarker.Auto();
        if (LiveCount >= Limit)
            return null;
        EnsurePool();
        PlayerRound b = null;
        while (pool.Count > 0 && pool.Peek() == null) pool.Dequeue();
        // Native trigger callbacks from this frame must not hit a recycled incarnation.
        if (pool.Count > 0 && pool.Peek().retiredFrame == Time.frameCount) return null;
        if (pool.Count > 0) b = pool.Dequeue();
        if (b == null) b = Create();
        b.Initialize(p, d, damage, speed, mode, generation);
        b.gameObject.SetActive(true);
        return b;
    }

    static PlayerRound Create()
    {
        using var sample = createMarker.Auto();
        CreatedCount++;
        var go = new GameObject("Friendly round");
        go.SetActive(false);
        go.transform.SetParent(poolRoot, false);
        go.tag = "PlayerBullet";
        go.layer = 3;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0;
        var collider = go.AddComponent<CapsuleCollider2D>();
        collider.isTrigger = true;
        collider.direction = CapsuleDirection2D.Vertical;
        var b = go.AddComponent<PlayerRound>();
        b.visual = go.AddComponent<SpriteRenderer>();
        b.contact = collider;
        return b;
    }

    CapsuleCollider2D contact;
    int retiredFrame = -1;
    float lifetime, lifetimeAge;
    public void LimitLifetime(float seconds) { lifetime = Mathf.Max(0, seconds); }

    void Initialize(Vector3 p, Vector2 d, float damage, float speed, PlayerDelivery mode, int generation)
    {
        var b = this;
        var go = gameObject;
        var sr = visual;
        var collider = contact;
        go.transform.SetPositionAndRotation(new Vector3(p.x, p.y, -1), Quaternion.FromToRotation(Vector3.up, d));
        sr.sprite = PlayerProjectileArt.For(mode);
        sr.color = Color.white;
        sr.enabled = true;
        collider.enabled = true;
        age = 0;
        lifetimeAge = 0;
        lifetime = float.PositiveInfinity;
        chargeLevel = 0;
        blastDamage = 0;
        detonated = returning = rebounded = rubberReturned = false;
        companion = companionShot = false;
        this.size = 1;
        followness = .01f;
        direction = new Vector3(0, 1, -10);
        hit.Clear();
        b.damage = damage;
        b.speed = speed;
        b.delivery = mode;
        b.generation = generation;
        b.origin = p;
        b.heading = d.normalized;
        b.pierces = mode == PlayerDelivery.Needle ? 1 : 0;
        Vector2 span = mode == PlayerDelivery.Needle ? new Vector2(.2f, .88f) : mode == PlayerDelivery.Shell ? Vector2.one * .44f :
            mode == PlayerDelivery.Disc ? Vector2.one * .4f : mode == PlayerDelivery.Flame ? new Vector2(.26f, .5f) : new Vector2(.23f, .43f);
        var bounds = sr.sprite.bounds.size;
        float size = Mathf.Sqrt(GameRules.playerShootSize) * (generation > 0 ? .85f : 1);
        go.transform.localScale = new Vector3(span.x / Mathf.Max(.01f, bounds.x), span.y / Mathf.Max(.01f, bounds.y), 1) * size;
        collider.size = new Vector2(bounds.x * .65f, bounds.y * .78f);
        ProjectileSpriteTint.Friendly(go);
        b.returns = generation == 0 && CombatUpgrades.Current != null && CombatUpgrades.Current.ReturnThisVolley;
    }

    public override void Despawn()
    {
        if (!gameObject.activeSelf) return;
        detonated = true;
        retiredFrame = Time.frameCount;
        gameObject.SetActive(false);
        if (pool.Count < Limit) pool.Enqueue(this);
        else Destroy(gameObject);
    }

    void OnEnable()
    {
        LiveCount++;
    }

    void OnDisable()
    {
        LiveCount--;
    }

    void Update()
    {
        if (WorldTimeStop.Frozen || Time.deltaTime <= 0)
            return;
        age += Time.deltaTime;
        lifetimeAge += Time.deltaTime;
        if (chargeLevel > 0 && age >= .6f)
        {
            ExplodeCharge();
            return;
        }
        if (!rubberReturned && hit.Count == 0 && !returns && delivery != PlayerDelivery.Disc && heading.y > 0 && RubberReturn.AtTop(transform.position))
        {
            rubberReturned = true;
            heading = new Vector2(heading.x, -Mathf.Abs(heading.y));
            age = 0;
        }
        if (age > 5 || lifetimeAge > lifetime || transform.position.y < Camera.DownLimit - 3 || Mathf.Abs(transform.position.x) > Camera.RightLimit + 4)
        {
            Despawn();
            return;
        }
        if ((delivery == PlayerDelivery.Disc || returns) && age > .75f && !returning)
        {
            returning = true;
            hit.Clear();
            ItemProcVfx.Emit(ItemProcShape.Return, transform.position, new Color(.4f, .7f, .8f), .3f);
            ItemSound.Cue("itemReturn", volume: .07f, cooldown: .18f);
        }
        if (returning)
        {
            heading = Vector3.RotateTowards(heading, ((Vector2)(origin - transform.position)).normalized, Time.deltaTime * 5, 0);
            if (age > 1.3f && Vector2.Distance(transform.position, origin) < .5f)
            {
                Despawn();
                return;
            }
        }
        else if (!rubberReturned && (delivery == PlayerDelivery.Missile || GameRules.playerShootFollowness > 0))
        {
            var target = CombatUpgrades.Nearest(transform.position, hit, 7);
            if (target != null)
                heading = Vector3.RotateTowards(heading, ((Vector2)(target.transform.position - transform.position)).normalized,
    Time.deltaTime * (delivery == PlayerDelivery.Missile ? 3 : Mathf.Min(3, GameRules.playerShootFollowness)), 0);
        }
        float brake = delivery == PlayerDelivery.Shell ? Mathf.Clamp01(1 - age / .7f) : 1;
        transform.position += (Vector3)heading * speed * brake * Time.deltaTime;
        transform.rotation = Quaternion.FromToRotation(Vector3.up, heading);
        if (delivery == PlayerDelivery.Disc)
            transform.Rotate(0, 0, age * 540);
        if (delivery == PlayerDelivery.Flame)
        {
            visual.color = new Color(1, 1, 1, 1 - Mathf.Clamp01(age / .55f));
            if (age > .55f)
            {
                Despawn();
                return;
            }
        }
        if (delivery == PlayerDelivery.Shell && age > 1.05f)
            Detonate();
        if (generation == 0 && CombatUpgrades.Current != null && CombatUpgrades.Current.ProximityReady && age > .1f)
        {
            var e = CombatUpgrades.Nearest(transform.position, null, .8f);
            if (e != null && CombatUpgrades.Current.SpendProximity())
                Detonate(true);
        }
        if (transform.position.y > Camera.UpLimit + 3 && !returns && delivery != PlayerDelivery.Disc)
            Despawn();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (detonated)
            return;
        var enemy = collision.GetComponent<AEnemy>();
        if (enemy == null || !enemy.CanReceiveDamage || !hit.Add(enemy))
            return;
        CombatHit.Apply(enemy, damage, collision.ClosestPoint(transform.position), heading,
            generation, companion, delivery == PlayerDelivery.Flame);
        if (chargeLevel > 0)
        {
            ExplodeCharge();
            return;
        }
        if (!rebounded && generation == 0 && CombatUpgrades.Current != null && CombatUpgrades.Current.Count(UpgradeKind.BouncingBall) > 0)
        {
            var next = CombatUpgrades.Nearest(transform.position, hit, 12);
            if (next != null)
            {
                rebounded = true;
                heading = ((Vector2)(next.transform.position - transform.position)).normalized;
                generation = 1;
                age = 0;
                origin = transform.position;
                returning = false;
                returns = false;
                TurnAccent();
                return;
            }
        }
        if (delivery == PlayerDelivery.Shell)
        {
            Detonate();
            return;
        }
        if (pierces-- > 0 || delivery == PlayerDelivery.Disc)
            return;
        if (!rebounded && CombatUpgrades.Current != null && CombatUpgrades.Current.TryRebound())
        {
            rebounded = true;
            var next = CombatUpgrades.Nearest(transform.position, hit, 5);
            if (next != null)
            {
                heading = ((Vector2)(next.transform.position - transform.position)).normalized;
                damage *= .75f;
                generation = 1;
                TurnAccent();
                return;
            }
        }
        Despawn();
    }

    void TurnAccent()
    {
        ItemProcVfx.Emit(ItemProcShape.Rebound, transform.position, new Color(.65f, .7f, .58f), .4f, heading);
        ItemSound.Cue("itemRicochet", volume: .07f, cooldown: .14f);
    }

    void ExplodeCharge()
    {
        if (detonated)
            return;
        detonated = true;
        ChargeBlast.Spawn(transform.position, blastDamage, chargeLevel);
        Despawn();
    }

    void Detonate(bool proximity = false)
    {
        if (detonated)
            return;
        detonated = true;
        ItemProcVfx.Emit(proximity ? ItemProcShape.Lock : ItemProcShape.Detonate, transform.position,
            proximity ? new Color(.75f, .63f, .35f) : new Color(.3f, .65f, .75f), proximity ? .6f : .7f, heading);
        ItemSound.Cue(proximity ? "itemProximity" : "itemDepthBurst", volume: .11f, cooldown: .16f);
        for (int i = 0; i < 7; i++)
            Spawn(transform.position, CombatVisuals.Rotate(heading, (i - 3) * 24), damage * .45f, 10, PlayerDelivery.Pulse, 1);
        Despawn();
    }
}
