using UnityEngine;
using System.Collections.Generic;

// Small local emissions cling to the hull, leaving the collision lanes readable.
public sealed class HullAfflictionVfx
{
    static ParticleSystem flames, bubbles;
    static readonly Stack<HullAfflictionVfx> pool = new Stack<HullAfflictionVfx>();
    AEnemy enemy;
    bool poison;
    float next;
    HullAfflictionVfx()
    {
    }

    public HullAfflictionVfx(AEnemy target, bool toxic)
    {
        Initialize(target, toxic);
    }

    public static HullAfflictionVfx Acquire(AEnemy target, bool toxic)
    {
        var effect = pool.Count > 0 ? pool.Pop() : new HullAfflictionVfx();
        effect.Initialize(target, toxic);
        return effect;
    }

    void Initialize(AEnemy target, bool toxic)
    {
        enemy = target;
        poison = toxic;
        next = 0;
        if (toxic && bubbles == null)
        {
            bubbles = PSXImpactEffects.Create(null, "Shared poison bubbles", 384, true);
            var renderer = bubbles.GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerName = "Bullet";
            renderer.sortingOrder = 29;
        }
        if (!toxic && flames == null)
            flames = PSXImpactEffects.Create(null, "Shared burning hull flames", 384);
    }

    public void Tick()
    {
        var particles = poison ? bubbles : flames;
        if (particles == null || enemy == null || Time.deltaTime <= 0 || Time.time < next)
            return;
        next = Time.time + .08f;
        float radius = Mathf.Clamp(enemy.size * .65f, .2f, .85f);
        for (int i = 0; i < 3; i++)
        {
            var offset = Random.insideUnitCircle * radius;
            var e = new ParticleSystem.EmitParams();
            e.position = enemy.transform.position + new Vector3(offset.x, offset.y, -2);
            var toxic = PlayerShotPalette.Poison;
            toxic.a = .8f;
            e.startColor = poison ? toxic : Color.Lerp(new Color(.85f, .08f, .02f), new Color(1, .55f, .08f), Random.value);
            e.startSize3D = poison ? Vector3.one * Random.Range(.08f, .15f) : new Vector3(.08f, .23f, .08f);
            e.velocity = new Vector3(Random.Range(-.15f, .15f), poison ? .35f : .9f, 0);
            e.startLifetime = poison ? .65f : .38f;
            particles.Emit(e, 1);
        }
    }

    public void Dispose()
    {
        enemy = null;
        pool.Push(this);
    }
}
