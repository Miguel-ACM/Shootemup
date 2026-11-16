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
    public const int Limit = 360;
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
    float age, flight, weave, weaveSign, travelled;
    Vector3 restScale;
    bool shaped;
    Vector3 origin;
    Vector2 heading;
    bool detonated, returning, rebounded, rubberReturned;
    int chargeLevel;
    float blastDamage;
    AEnemy guidedTarget;
    float guidanceDelay;
    bool headingLocked;
    bool bankShot, armedBank, vendingCoin, plateDeflected;
    public void GuideTo(AEnemy target, float delay) { guidedTarget = target; guidanceDelay = Mathf.Max(0, delay); }
    public void LockHeading() { headingLocked = true; }
    public void RequireBank()
    {
        bankShot = headingLocked = true;
        returns = false;
    }
    public void AwardVendingCoin() => vendingCoin = true;
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
        flight = weave = travelled = 0;
        shaped = false;
        weaveSign = ShotTraits.NextWeaveSign();
        lifetimeAge = 0;
        lifetime = float.PositiveInfinity;
        chargeLevel = 0;
        blastDamage = 0;
        guidedTarget = null;
        guidanceDelay = 0;
        headingLocked = false;
        bankShot = armedBank = vendingCoin = plateDeflected = false;
        detonated = returning = rebounded = rubberReturned = false;
        companion = companionShot = false;
        this.size = 1;
        followness = .01f;
        direction = new Vector3(0, 1, -10);
        hit.Clear();
        b.grindTimer = 0;
        b.damage = damage;
        b.speed = speed;
        b.delivery = mode;
        b.generation = generation;
        b.origin = p;
        b.heading = d.normalized;
        b.pierces = mode == PlayerDelivery.Needle ? 1 : 0;
        // Rockets read as real ordnance: about 1.45 times a pulse round, keeping the dart's proportions.
        Vector2 span = mode == PlayerDelivery.Needle ? new Vector2(.2f, .88f) : mode == PlayerDelivery.Shell ? Vector2.one * .44f :
            mode == PlayerDelivery.Disc ? Vector2.one * .4f : mode == PlayerDelivery.Flame ? new Vector2(.26f, .5f) :
            mode == PlayerDelivery.Missile ? new Vector2(.33f, .62f) : new Vector2(.23f, .43f);
        var bounds = sr.sprite.bounds.size;
        float size = Mathf.Sqrt(GameRules.playerShootSize) * (generation > 0 ? .85f : 1) * ShotTraits.SizeScale;
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
        flight += Time.deltaTime;
        lifetimeAge += Time.deltaTime;
        if (chargeLevel > 0 && age >= .6f)
        {
            ExplodeCharge();
            return;
        }
        if (!bankShot && !rubberReturned && hit.Count == 0 && !returns && delivery != PlayerDelivery.Disc && heading.y > 0 && RubberReturn.AtTop(transform.position))
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
        // Discs hold and grind instead of returning (see Grind).
        if (delivery == PlayerDelivery.Disc && age >= DiscOut + DiscHold)
        {
            Despawn();
            return;
        }
        if (returns && delivery != PlayerDelivery.Disc && age > .75f && !returning)
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
        else if (!headingLocked && !rubberReturned && age >= guidanceDelay && (delivery == PlayerDelivery.Missile || GameRules.playerShootFollowness > 0))
        {
            var marked = CombatTargets.MarkedInRange(transform.position, 18);
            var target = marked != null && !hit.Contains(marked) ? marked :
                guidedTarget != null && guidedTarget.CanBeTargeted && guidedTarget.CanReceiveDamage && !hit.Contains(guidedTarget)
                    ? guidedTarget : CombatUpgrades.Nearest(transform.position, hit, 7);
            if (target != null)
                heading = Vector3.RotateTowards(heading, target == marked ? CombatTargets.MarkedDirection(transform.position, marked) :
                    ((Vector2)(target.transform.position - transform.position)).normalized,
    Time.deltaTime * (delivery == PlayerDelivery.Missile ? 4.5f : Mathf.Min(3, GameRules.playerShootFollowness)), 0);
        }
        float brake = delivery == PlayerDelivery.Shell ? Mathf.Clamp01(1 - age / .7f) :
            delivery == PlayerDelivery.Missile ? Ignition(flight) : delivery == PlayerDelivery.Disc ? DiscBrake() : 1;
        Vector3 wallFrom = transform.position;
        Vector2 step = heading * speed * brake * ShotTraits.SpeedScale(flight) * Time.deltaTime;
        travelled += step.magnitude;
        float lateral = ShotTraits.Weave(flight, weaveSign);
        step += new Vector2(heading.y, -heading.x) * (lateral - weave);
        weave = lateral;
        var end = wallFrom + (Vector3)step;
        bool solidWall = CombatWalls.Sweep(wallFrom, end, .04f, out var wallPoint, out var wallNormal);
        if (bankShot && !armedBank)
        {
            if (TryBank(wallFrom, step, solidWall, wallPoint, wallNormal, out var point))
            {
                end = new Vector3(point.x, point.y, wallFrom.z);
                step = heading;
            }
        }
        else if (solidWall)
        {
            Despawn();
            return;
        }
        var upgrades = CombatUpgrades.Current;
        if (!plateDeflected && !bankShot && !returning && !companion && heading.y < .985f &&
            upgrades != null && upgrades.Count(UpgradeKind.DeflectorPlate) > 0 &&
            ProjectileCollisionWindow.TryDeflector(wallFrom, end, .075f * Mathf.Max(.2f, size), out var plate))
        {
            plateDeflected = headingLocked = true;
            heading = step = Vector2.up;
            end = new Vector3(plate.x, plate.y + Vector2.Distance(plate, end), end.z);
            if (CombatWalls.Sweep(plate, end, .04f, out _, out _))
            {
                Despawn();
                return;
            }
            upgrades.Deflect(plate);
        }
        transform.position = end;
        transform.rotation = Quaternion.FromToRotation(Vector3.up, step.sqrMagnitude > .0000001f ? step : heading);
        Reshape();
        if (delivery == PlayerDelivery.Disc)
        {
            bool holding = age >= DiscOut;
            transform.Rotate(0, 0, age * (holding ? 1080 : 540));
            visual.color = new Color(1, 1, 1, Mathf.Clamp01((DiscOut + DiscHold - age) / .2f));
            if (holding)
                Grind();
        }
        if (delivery == PlayerDelivery.Missile)
            visual.sprite = PlayerProjectileArt.MissileFrame(Mathf.FloorToInt(age * PlayerProjectileArt.MissileFramesPerSecond));
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
        if ((!bankShot || armedBank) && generation == 0 && CombatUpgrades.Current != null && CombatUpgrades.Current.ProximityReady && age > .1f)
        {
            var e = CombatUpgrades.Nearest(transform.position, null, .8f);
            int bursts = e != null ? CombatUpgrades.Current.SpendProximity(transform.position) : 0;
            if (bursts > 0)
            {
                RunInventory.Ping(UpgradeKind.Proximity);
                Detonate(true, bursts);
            }
        }
        if (transform.position.y > Camera.UpLimit + 3 && !returns && delivery != PlayerDelivery.Disc)
            Despawn();
    }

    bool TryBank(Vector2 from, Vector2 step, bool hitWall, Vector2 wallPoint, Vector2 normal, out Vector2 point)
    {
        point = wallPoint;
        float fraction = hitWall ? (wallPoint - from).sqrMagnitude / Mathf.Max(.000001f, step.sqrMagnitude) : float.PositiveInfinity;
        float first = Mathf.Sqrt(fraction);
        Vector2 end = from + step;
        if (step.x != 0)
        {
            float edge = step.x < 0 ? Camera.LeftLimit : Camera.RightLimit;
            float t = (edge - from.x) / step.x;
            if ((step.x < 0 ? end.x <= edge : end.x >= edge) && t >= 0 && t < first)
            {
                first = t;
                normal = step.x < 0 ? Vector2.right : Vector2.left;
                point = from + step * t;
            }
        }
        if (step.y != 0)
        {
            float edge = step.y < 0 ? Camera.DownLimit : Camera.UpLimit;
            float t = (edge - from.y) / step.y;
            if ((step.y < 0 ? end.y <= edge : end.y >= edge) && t >= 0 && t < first)
            {
                first = t;
                normal = step.y < 0 ? Vector2.up : Vector2.down;
                point = from + step * t;
            }
        }
        if (float.IsPositiveInfinity(first)) return false;
        armedBank = true;
        heading = Vector2.Reflect(heading, normal);
        point += normal * .065f;
        TurnAccent();
        return true;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (detonated || bankShot && !armedBank)
            return;
        var enemy = collision.GetComponent<AEnemy>();
        if (enemy == null || !enemy.CanReceiveDamage || !hit.Add(enemy))
            return;
        Vector2 impact = collision.ClosestPoint(transform.position);
        CombatHit.Apply(enemy, damage * ShotTraits.ReachDamage(travelled), impact, heading,
            generation, companion, delivery == PlayerDelivery.Flame);
        if (delivery == PlayerDelivery.Missile)
            RocketBurst(enemy, impact);
        if (vendingCoin && enemy.health <= 0)
            enemy.DropVendingCoin();
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
                RunInventory.Ping(UpgradeKind.BouncingBall);
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
        int rebounds = !rebounded && CombatUpgrades.Current != null ? CombatUpgrades.Current.TryRebound(transform.position) : 0;
        if (rebounds > 0)
        {
            rebounded = true;
            var next = CombatUpgrades.Nearest(transform.position, hit, 5);
            if (next != null)
            {
                RunInventory.Ping(UpgradeKind.Rebound);
                if (rebounds > 1)
                    SplitRebound(enemy, next);
                heading = ((Vector2)(next.transform.position - transform.position)).normalized;
                damage *= .75f;
                generation = 1;
                TurnAccent();
                return;
            }
        }
        Despawn();
    }

    // Rockets leave the rack slowly, then their motor catches and they streak to cruise speed.
    float Ignition(float time)
    {
        float cruise = Mathf.Min(1.5f, Mathf.Max(1, RocketTopSpeed / Mathf.Max(.01f, speed)));
        return Mathf.Lerp(.6f, cruise, Mathf.SmoothStep(0, 1, time / .3f));
    }

    const float RocketTopSpeed = 24, RocketSplashRadius = .9f, RocketSplashShare = .4f;
    static readonly List<AEnemy> splashTargets = new List<AEnemy>();

    // Disc Thrower (Miguel, 15 November 2026): a disc brakes to a stop a set distance ahead,
    // whatever its shot speed, holds there spinning and keeps cutting every hull it touches,
    // then fades. Its first pass still strikes like any round. It flies 5 units out (was 3.2).
    const float DiscReach = 5f, DiscOut = .5f, DiscHold = 1.5f, DiscTick = .12f, DiscTickShare = .25f;
    float grindTimer;

    // Linear braking that covers exactly DiscReach before the hold.
    float DiscBrake()
    {
        if (age >= DiscOut)
            return 0;
        float scale = Mathf.Clamp(2 * DiscReach / Mathf.Max(.01f, speed * DiscOut), .3f, 4f);
        return (1 - age / DiscOut) * scale;
    }

    void Grind()
    {
        grindTimer -= Time.deltaTime;
        if (grindTimer > 0)
            return;
        grindTimer = DiscTick;
        float radius = .3f * Mathf.Sqrt(GameRules.playerShootSize) * ShotTraits.SizeScale * (generation > 0 ? .85f : 1);
        Vector2 at = transform.position;
        splashTargets.Clear();
        splashTargets.AddRange(AEnemy.Live);
        foreach (var enemy in splashTargets)
        {
            if (enemy == null || !enemy.CanReceiveDamage)
                continue;
            if (!enemy.ClosestDamagePoint(at, out var near) || (near - at).sqrMagnitude > radius * radius)
                continue;
            CombatHit.Apply(enemy, damage * DiscTickShare, near, heading, generation, companion);
        }
    }

    // Every rocket bursts where it lands: a small fireball and splash damage to the hulls around it.
    void RocketBurst(AEnemy struck, Vector2 point)
    {
        // One quiet burst per impact; the fireballs themselves stay silent so a salvo never stacks pops.
        PSXFireballs.Spawn(point, generation > 0 ? .26f : .36f, 2, .12f, .05f, new Color(1f, .8f, .5f), silent: true);
        ItemSound.Cue("itemDepthBurst", volume: .028f, pitch: 1.3f + Random.value * .2f, cooldown: .09f);
        float splash = damage * RocketSplashShare;
        splashTargets.Clear();
        splashTargets.AddRange(AEnemy.Live);
        foreach (var other in splashTargets)
        {
            if (other == null || other == struck || !other.CanReceiveDamage || hit.Contains(other))
                continue;
            if (!other.ClosestDamagePoint(point, out var near) || (near - point).sqrMagnitude > RocketSplashRadius * RocketSplashRadius)
                continue;
            CombatHit.Apply(other, splash, near, heading, 1, companion);
        }
    }

    // Spawners may rescale a round after Spawn, so its resting scale is read on its first move.
    void Reshape()
    {
        if (!shaped)
        {
            restScale = transform.localScale;
            shaped = true;
        }
        if (!ShotTraits.Slung)
            return;
        float stretch = ShotTraits.Stretch(travelled);
        transform.localScale = new Vector3(restScale.x, restScale.y * stretch, restScale.z);
    }

    // A doubled ricochet sends a second round to another nearby enemy, or angled off the first.
    void SplitRebound(AEnemy struck, AEnemy first)
    {
        hit.Add(first);
        var second = CombatUpgrades.Nearest(transform.position, hit, 5);
        hit.Remove(first);
        Vector2 toFirst = ((Vector2)(first.transform.position - transform.position)).normalized;
        Vector2 direction = second != null ? ((Vector2)(second.transform.position - transform.position)).normalized : CombatVisuals.Rotate(toFirst, 35);
        var twin = Spawn(transform.position, direction, damage * .75f, speed, PlayerDelivery.Pulse, 1);
        if (twin == null)
            return;
        twin.companion = companion;
        twin.ExcludePreviousHit(struck);
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

    void Detonate(bool proximity = false, int rings = 1)
    {
        if (detonated)
            return;
        detonated = true;
        ItemProcVfx.Emit(proximity ? ItemProcShape.Lock : ItemProcShape.Detonate, transform.position,
            proximity ? new Color(.75f, .63f, .35f) : new Color(.3f, .65f, .75f), proximity ? .6f : .7f, heading);
        ItemSound.Cue(proximity ? "itemProximity" : "itemDepthBurst", volume: .11f, cooldown: .16f);
        for (int ring = 0; ring < rings; ring++)
            for (int i = 0; i < 7; i++)
                Spawn(transform.position, CombatVisuals.Rotate(heading, (i - 3) * 24 + ring * 12), damage * .45f, 10, PlayerDelivery.Pulse, 1);
        Despawn();
    }
}
