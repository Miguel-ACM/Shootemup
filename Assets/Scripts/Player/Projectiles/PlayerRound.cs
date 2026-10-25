using UnityEngine;
using System.Collections.Generic;

public enum PlayerDelivery
{
    Pulse, Needle, Shell, Disc, Missile, Flame
}
public sealed class PlayerRound : ABullet
{
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
    readonly HashSet<AEnemy> hit = new HashSet<AEnemy>();
    SpriteRenderer visual;
    public void ExcludePreviousHit(AEnemy enemy)
    {
        hit.Add(enemy);
        rebounded = true;
    }

    public static PlayerRound Spawn(Vector3 p, Vector2 d, float damage, float speed, PlayerDelivery mode = PlayerDelivery.Pulse, int generation = 0)
    {
        if (LiveCount >= 180)
            return null;
        var go = new GameObject("Friendly " + mode);
        go.tag = "PlayerBullet";
        go.layer = 3;
        go.transform.position = new Vector3(p.x, p.y, -1);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0;
        var collider = go.AddComponent<CapsuleCollider2D>();
        collider.isTrigger = true;
        collider.direction = CapsuleDirection2D.Vertical;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PlayerProjectileArt.For(mode);
        sr.sharedMaterial = CombatVisuals.ProjectileMaterial;
        sr.sortingLayerName = "Bullet";
        sr.sortingOrder = 16;
        sr.color = Color.white;
        var b = go.AddComponent<PlayerRound>();
        b.visual = sr;
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
        return b;
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
        if (!rubberReturned && hit.Count == 0 && !returns && delivery != PlayerDelivery.Disc && heading.y > 0 && RubberReturn.AtTop(transform.position))
        {
            rubberReturned = true;
            heading = new Vector2(heading.x, -Mathf.Abs(heading.y));
            age = 0;
        }
        if (age > 5 || transform.position.y < Camera.DownLimit - 3 || Mathf.Abs(transform.position.x) > Camera.RightLimit + 4)
        {
            Destroy(gameObject);
            return;
        }
        if ((delivery == PlayerDelivery.Disc || returns) && age > .75f && !returning)
        {
            returning = true;
            hit.Clear();
        }
        if (returning)
        {
            heading = Vector3.RotateTowards(heading, ((Vector2)(origin - transform.position)).normalized, Time.deltaTime * 5, 0);
            if (age > 1.3f && Vector2.Distance(transform.position, origin) < .5f)
            {
                Destroy(gameObject);
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
                Destroy(gameObject);
        }
        if (delivery == PlayerDelivery.Shell && age > 1.05f)
            Detonate();
        if (generation == 0 && CombatUpgrades.Current != null && CombatUpgrades.Current.ProximityReady && age > .1f)
        {
            var e = CombatUpgrades.Nearest(transform.position, null, .8f);
            if (e != null && CombatUpgrades.Current.SpendProximity())
                Detonate();
        }
        if (transform.position.y > Camera.UpLimit + 3 && !returns && delivery != PlayerDelivery.Disc)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        var enemy = collision.GetComponent<AEnemy>();
        if (enemy == null || enemy.health <= 0 || !hit.Add(enemy))
            return;
        float dealt = CombatUpgrades.Current != null ? CombatUpgrades.Current.DirectHit(enemy, damage, generation, companion, delivery == PlayerDelivery.Flame) : damage;
        enemy.HitAt(collision.ClosestPoint(transform.position), heading);
        enemy.damage(dealt);
        Score.IncreaseScore(Mathf.RoundToInt(dealt));
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
                PSXImpactEffects.Burst(transform.position, new Color(.3f, .85f, 1));
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
                return;
            }
        }
        Destroy(gameObject);
    }

    void Detonate()
    {
        if (detonated)
            return;
        detonated = true;
        PSXImpactEffects.Vanish(transform.position, new Color(.4f, 1, .8f), .7f);
        for (int i = 0; i < 7; i++)
            Spawn(transform.position, CombatVisuals.Rotate(heading, (i - 3) * 24), damage * .45f, 10, PlayerDelivery.Pulse, 1);
        Destroy(gameObject);
    }
}
