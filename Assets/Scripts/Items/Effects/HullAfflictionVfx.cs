using UnityEngine;
using System.Collections.Generic;

// The hull finish is shared across afflicted enemies. The small local emissions use shared pools too.
public sealed class HullAfflictionVfx
{
    static readonly int HullStatus = Shader.PropertyToID("_HullStatus");
    // Each base paint and its variants share a table indexed by burn (1) and poison (2).
    // Swapping a shared material leaves the renderer's flash, opacity and tint overrides intact.
    static readonly Dictionary<Material, Material[]> finishes = new Dictionary<Material, Material[]>();
    static ParticleSystem flames, bubbles;
    static readonly Stack<HullAfflictionVfx> pool = new Stack<HullAfflictionVfx>();
    readonly List<MeshRenderer> hulls = new List<MeshRenderer>();
    readonly List<Material> materials = new List<Material>();
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
        target.GetComponentsInChildren(true, hulls);
        for (int i = hulls.Count - 1; i >= 0; i--)
            if (hulls[i].GetComponentInParent<AEnemy>(true) != target)
                hulls.RemoveAt(i);
        SetFinish(true);
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

    void SetFinish(bool active)
    {
        int bit = poison ? 2 : 1;
        foreach (var hull in hulls)
        {
            if (hull == null) continue;
            hull.GetSharedMaterials(materials);
            bool changed = false;
            for (int i = 0; i < materials.Count; i++)
            {
                var finish = WithCoating(materials[i], bit, active);
                changed |= materials[i] != finish;
                materials[i] = finish;
            }
            if (changed) hull.SetSharedMaterials(materials);
        }
        materials.Clear();
        if (!active) hulls.Clear();
    }

    static Material WithCoating(Material current, int bit, bool active)
    {
        if (current == null || !current.HasProperty(HullStatus)) return current;
        int state = (int)current.GetFloat(HullStatus);
        int nextState = active ? state | bit : state & ~bit;
        if (nextState == state) return current;

        if (!finishes.TryGetValue(current, out var family))
        {
            family = new Material[4];
            family[0] = current;
            finishes.Add(current, family);
        }
        if (family[nextState] == null)
        {
            var finish = new Material(family[0]) { name = family[0].name + " (hull status " + nextState + ")" };
            finish.SetFloat(HullStatus, nextState);
            finish.EnableKeyword("HULL_AFFLICTED");
            finishes.Add(finish, family);
            family[nextState] = finish;
        }
        return family[nextState];
    }

    public void Tick()
    {
        if (enemy == null || enemy.health <= 0)
        {
            if (hulls.Count > 0) SetFinish(false);
            return;
        }
        var particles = poison ? bubbles : flames;
        if (particles == null || Time.deltaTime <= 0 || Time.time < next)
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
        SetFinish(false);
        enemy = null;
        pool.Push(this);
    }
}
