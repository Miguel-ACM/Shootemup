using UnityEngine;
using System.Collections.Generic;

// The hull finish is shared across afflicted enemies. The small local emissions use shared pools too.
public sealed class HullAfflictionVfx
{
    static readonly int HullStatus = Shader.PropertyToID("_HullStatus");
    public const int Frost = 4;
    // Each base paint and its variants share a table indexed by burn, poison and frost.
    // Swapping a shared material leaves the renderer's flash, opacity and tint overrides intact.
    static readonly Dictionary<Material, Material[]> finishes = new Dictionary<Material, Material[]>();
    static ParticleSystem flames, bubbles;
    static readonly Stack<HullAfflictionVfx> pool = new Stack<HullAfflictionVfx>();
    readonly List<MeshRenderer> hulls = new List<MeshRenderer>();
    readonly List<Material> materials = new List<Material>();
    AEnemy enemy;
    int coating;
    bool poison => coating == 2;
    float next;
    HullAfflictionVfx()
    {
    }

    public HullAfflictionVfx(AEnemy target, bool toxic)
    {
        Initialize(target, toxic ? 2 : 1);
    }

    public static HullAfflictionVfx Acquire(AEnemy target, bool toxic) => Acquire(target, toxic ? 2 : 1);

    public static HullAfflictionVfx Acquire(AEnemy target, int coating)
    {
        var effect = pool.Count > 0 ? pool.Pop() : new HullAfflictionVfx();
        effect.Initialize(target, coating);
        return effect;
    }

    void Initialize(AEnemy target, int coating)
    {
        enemy = target;
        this.coating = coating;
        next = 0;
        target.GetComponentsInChildren(true, hulls);
        for (int i = hulls.Count - 1; i >= 0; i--)
            if (hulls[i].GetComponentInParent<AEnemy>(true) != target)
                hulls.RemoveAt(i);
        SetFinish(true);
        if (poison && bubbles == null)
        {
            bubbles = PSXImpactEffects.Create(null, "Shared poison bubbles", 384, true);
            var renderer = bubbles.GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerName = "Bullet";
            renderer.sortingOrder = 29;
        }
        if (coating == 1 && flames == null)
            flames = PSXImpactEffects.Create(null, "Shared burning hull flames", 384);
    }

    void SetFinish(bool active)
    {
        foreach (var hull in hulls)
        {
            if (hull == null) continue;
            hull.GetSharedMaterials(materials);
            bool changed = false;
            for (int i = 0; i < materials.Count; i++)
            {
                var finish = WithCoating(materials[i], coating, active);
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
            family = new Material[8];
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

    // Burn flickers: quick, tall licks of flame that grow with the heat. Poison seeps:
    // slow jade bubbles rising and drops falling away, with a swell on every dose.
    public void Tick(float intensity)
    {
        if (enemy == null || enemy.health <= 0)
        {
            if (hulls.Count > 0) SetFinish(false);
            return;
        }
        var particles = poison ? bubbles : flames;
        if (particles == null || Time.deltaTime <= 0 || Time.time < next)
            return;
        float radius = Mathf.Clamp(enemy.size * .65f, .2f, .85f);
        if (poison)
        {
            next = Time.time + .16f;
            for (int i = 0; i < 2; i++)
            {
                bool drip = i == 1;
                var offset = Random.insideUnitCircle * radius;
                var e = new ParticleSystem.EmitParams();
                e.position = enemy.transform.position + new Vector3(offset.x, drip ? -Mathf.Abs(offset.y) : offset.y, -2);
                var toxic = PlayerShotPalette.Poison;
                toxic.a = .8f;
                e.startColor = toxic;
                e.startSize3D = drip ? new Vector3(.07f, .13f, .07f) : Vector3.one * Random.Range(.08f, .15f);
                e.velocity = new Vector3(Random.Range(-.08f, .08f), drip ? -.55f : .25f, 0);
                e.startLifetime = drip ? .55f : .8f;
                particles.Emit(e, 1);
            }
            return;
        }
        next = Time.time + .05f;
        int licks = 1 + Mathf.RoundToInt(Mathf.Clamp01(intensity) * 3);
        float tall = Mathf.Lerp(.7f, 1.35f, Mathf.Clamp01(intensity));
        for (int i = 0; i < licks; i++)
        {
            var offset = Random.insideUnitCircle * radius;
            var e = new ParticleSystem.EmitParams();
            e.position = enemy.transform.position + new Vector3(offset.x, offset.y, -2);
            e.startColor = Color.Lerp(new Color(.85f, .08f, .02f), new Color(1, .62f, .1f), Random.value);
            e.startSize3D = new Vector3(.07f, .22f * tall, .07f);
            e.velocity = new Vector3(Random.Range(-.2f, .2f), 1.3f * tall, 0);
            e.startLifetime = .24f;
            particles.Emit(e, 1);
        }
    }

    // A poison dose: a ring of bubbles swelling out of the hull, larger as the dose grows.
    public void Pulse(float strength)
    {
        if (!poison || bubbles == null || enemy == null)
            return;
        float radius = Mathf.Clamp(enemy.size * .5f, .2f, .7f);
        int count = 6 + Mathf.RoundToInt(Mathf.Clamp01(strength) * 6);
        var toxic = PlayerShotPalette.Poison;
        for (int i = 0; i < count; i++)
        {
            float a = i * Mathf.PI * 2 / count;
            var radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
            var e = new ParticleSystem.EmitParams();
            e.position = enemy.transform.position + radial * radius + Vector3.back * 2;
            e.startColor = Color.Lerp(toxic, Color.white, .15f);
            e.startSize3D = Vector3.one * Mathf.Lerp(.1f, .2f, strength);
            e.velocity = radial * Mathf.Lerp(.6f, 1.2f, strength);
            e.startLifetime = .45f;
            bubbles.Emit(e, 1);
        }
    }

    public void Dispose()
    {
        SetFinish(false);
        enemy = null;
        pool.Push(this);
    }
}
