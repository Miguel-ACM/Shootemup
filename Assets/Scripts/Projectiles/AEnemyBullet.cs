using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class AEnemyBullet : MonoBehaviour
{
    Vector3 direction;
    public float damage;
    public float speed;
    Rigidbody2D rigidBody;
    protected bool moveOnUpdate = false;
    protected bool dieOnTrigger = true;

    protected virtual void Start()
    {
        ProjectileSpriteTint.LegacyEnemy(gameObject);
        PolarityEngine.Assign(GetComponent<ProjectileSpriteTint>());
        rigidBody = GetComponent<Rigidbody2D>();
    }

    protected virtual void Update()
    {
        // Read the limits directly: getLimits() allocates a new array, once per bullet per frame here.
        if (rigidBody.position.x > Camera.RightLimit + 1f || rigidBody.position.x < Camera.LeftLimit - 1f)
            Destroy(gameObject);
        Move();
        transform.position += PocketWind.Velocity(transform.position) * Time.deltaTime;
    }

    protected virtual void Move()
    {
        rigidBody.linearVelocity = direction * speed * CombatUpgrades.HostileSpeedAt(transform.position);
    }

    public void setProperties(Dictionary<string, float> stats)
    {
        damage = Utils.GetValueOrDefault(stats, "damage", 1f);
        if (stats.ContainsKey("angle"))
        {
            float angle = Utils.GetValueOrDefault(stats, "angle", 0f);
            direction = Utils.GetVelocityAngle(angle);
        }
        else
        {
            float xdir = Utils.GetValueOrDefault(stats, "dirx", 0f);
            float ydir = Utils.GetValueOrDefault(stats, "diry", -1f);
            Vector3 player_pos = new Vector3(xdir, ydir);
            direction = Utils.GetVelocityBetweenVectors(transform.position, player_pos);
        }
        speed = Utils.GetValueOrDefault(stats, "speed", 1f);
        // Let the model part representing this shooter turn/kick and throw muzzle sparks, and start
        // the round at that part's depth so raised hull layers don't hide it as it leaves the gun.
        float? muzzleZ = PSXShooterVisual.NotifyShot(transform.position, direction);
        if (muzzleZ.HasValue)
        {
            Vector3 p = transform.position;
            transform.position = new Vector3(p.x, p.y, muzzleZ.Value);
        }
    }

    public float getDamage()
    {
        return damage;
    }

    public void OnTrigger()
    {
        if (dieOnTrigger)
        {
            Vanish();
        }
    }

    /// <summary>Removes the round with a short pop (a small ring and sparks in its colour) instead of
    /// letting it blink out; used on hits and when a blank bullet erases it.</summary>
    public virtual void Vanish()
    {
        var sprite = GetComponent<SpriteRenderer>();
        var coded = GetComponent<ProjectileSpriteTint>();
        Color tint = coded != null ? coded.Tint : sprite != null && sprite.sprite != null ? Average(sprite) : new Color(1f, .6f, .25f);
        float size = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : transform.lossyScale.x;
        PSXImpactEffects.Vanish(transform.position, tint, size);
        Destroy(gameObject);
    }

    static readonly Dictionary<Sprite, Color> averages = new Dictionary<Sprite, Color>();
    static Color Average(SpriteRenderer renderer)
    {
        if (averages.TryGetValue(renderer.sprite, out var cached))
            return cached;
        Color sum = Color.black;
        int count = 0;
        var texture = renderer.sprite.texture;
        if (texture.isReadable)
            foreach (var c in texture.GetPixels())
                if (c.a > .5f)
                {
                    sum += c;
                    count++;
                }
        var result = count > 0 ? sum / count : new Color(1f, .6f, .25f);
        result.a = 1;
        averages[renderer.sprite] = result;
        return result;
    }

    protected virtual void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}
