using System.Collections.Generic;
using UnityEngine;

/// <summary>One shared, bounded particle buffer for hits and pickup bursts; sphere explosions on death.</summary>
public static class PSXImpactEffects
{
    static readonly Unity.Profiling.ProfilerMarker vanishMarker = new Unity.Profiling.ProfilerMarker("PSXImpact.Vanish");
    static ParticleSystem buffer;
    static ParticleSystem dissolves;
    static Material material;
    static Material auraMaterial;
    static readonly List<MeshRenderer> hullRenderers = new List<MeshRenderer>();
    static MaterialPropertyBlock hullProperties;
    public static int BurstCount
    {
        get; private set;
    }

    public static int ExplosionCount
    {
        get; private set;
    }

    public static void Warm()
    {
        if (buffer == null)
            buffer = Create(null, "PSX shared impacts", 512);
        if (dissolves == null)
            dissolves = Create(null, "PSX bullet dissolves", 3200);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        buffer = null;
        dissolves = null;
        material = null;
        auraMaterial = null;
        BurstCount = 0;
        ExplosionCount = 0;
    }

    public static ParticleSystem Create(Transform parent, string name, int budget, bool aura = false)
    {
        var go = new GameObject(name);
        if (parent != null)
            go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.maxParticles = budget;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = .35f;
        main.startSize3D = true;
        main.startSize = .055f;
        main.startSpeed = 0;
        main.gravityModifier = 0;
        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = false;
        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(.9f, 0), new GradientAlphaKey(0, 1) });
        fade.color = gradient;
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (material == null)
            material = Resources.Load<Material>("Effects/PSXParticles");
        if (auraMaterial == null)
            auraMaterial = Resources.Load<Material>("Effects/PSXAura");
        renderer.sharedMaterial = aura ? auraMaterial : material;
        renderer.sortingLayerName = aura ? "Objects" : "Bullet";
        renderer.sortingOrder = aura ? -1 : 30;
        ps.Play();
        return ps;
    }

    public static void Burst(Vector3 position, Color color, bool pickup = false, Vector2 incoming = default)
    {
        if (buffer == null)
            buffer = Create(null, "PSX shared impacts", 512);
        BurstCount++;
        int count = pickup ? 22 : 9;
        Vector2 direction = incoming.sqrMagnitude > .001f ? incoming.normalized : Vector2.up;
        for (int i = 0; i < count; i++)
        {
            float angle = pickup ? i * Mathf.PI * 2 / count + Random.Range(-.18f, .18f) : Mathf.Atan2(direction.y, direction.x) + Random.Range(-.48f, .48f);
            var e = new ParticleSystem.EmitParams();
            e.position = position;
            e.velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * Random.Range(pickup ? 1f : 1.6f, pickup ? 3.5f : 4.8f);
            e.startColor = i % 4 == 0 ? Color.Lerp(color, Color.white, .65f) : color;
            float size = Random.Range(.035f, pickup ? .10f : .055f);
            if (!pickup && i == 0)
                size *= 1.7f;
            e.startSize3D = pickup ? Vector3.one * size : new Vector3(size, size * 2.8f, size);
            e.rotation3D = new Vector3(0, 0, 90 - angle * Mathf.Rad2Deg);
            e.startLifetime = Random.Range(.16f, pickup ? .55f : .34f);
            buffer.Emit(e, 1);
        }
    }

    /// <summary>Growing fire spheres scaled to the destroyed object: sphere size, sphere count, spread
    /// and chain length all grow with the hull, so a gunboat or the boss goes up in a long, wide blast.</summary>
    public static void Explosion(Vector3 position, float size)
    {
        Explosion(position, size, Color.white);
    }

    static void Explosion(Vector3 position, float size, Color tint)
    {
        ExplosionCount++;
        size = Mathf.Max(.6f, size);
        if (LevelRun.Current == LevelId.Underwater)
        {
            UnderwaterExplosion(position, size, tint);
            return;
        }
        float sphere = .6f + size * .75f;
        int count = Mathf.Clamp(3 + Mathf.RoundToInt(size * 2), 4, 18);
        PSXFireballs.Spawn(position, sphere, count, size * .45f, Mathf.Clamp(size * .15f, .1f, 1.1f), tint);
        int sparks = Mathf.Clamp(Mathf.RoundToInt(size), 1, 6);
        for (int i = 0; i < sparks; i++)
            Burst(position + (Vector3)(Random.insideUnitCircle * size * .4f), new Color(1, .68f, .30f));
    }

    static void UnderwaterExplosion(Vector3 position, float size, Color tint)
    {
        if (buffer == null)
            buffer = Create(null, "PSX shared impacts", 512);
        int count = Mathf.Clamp(Mathf.RoundToInt(7 + size * 3), 8, 22);
        for (int i = 0; i < count; i++)
        {
            var e = new ParticleSystem.EmitParams();
            Vector2 offset = Random.insideUnitCircle * size * .25f;
            e.position = position + new Vector3(offset.x, offset.y, -.3f);
            e.velocity = new Vector3(offset.x * .5f, Random.Range(.3f, 1.1f), 0);
            e.startColor = i == 0 ? new Color(.65f, .84f, .87f, .55f) : new Color(.32f, .65f, .72f, .45f);
            Color tinted = e.startColor * tint;
            tinted.a = e.startColor.a;
            e.startColor = Color.Lerp(e.startColor, tinted, .25f);
            float diameter = i == 0 ? size * .42f : Random.Range(.04f, .12f) * Mathf.Sqrt(size);
            e.startSize3D = Vector3.one * diameter;
            e.startLifetime = i == 0 ? .18f : Random.Range(.55f, 1.1f);
            buffer.Emit(e, 1);
        }
    }

    public static void Explosion(GameObject source)
    {
        PSXPolygonDebris.Spawn(source);
        var collider = source.GetComponent<Collider2D>();
        float size = collider != null ? Mathf.Max(collider.bounds.size.x, collider.bounds.size.y) : 1;
        Explosion(collider != null ? collider.bounds.center : source.transform.position, size, HullColor(source));
    }

    public static Color HullColor(GameObject source)
    {
        hullRenderers.Clear();
        source.GetComponentsInChildren(false, hullRenderers);
        if (hullProperties == null)
            hullProperties = new MaterialPropertyBlock();
        Color sum = Color.black;
        int count = 0;
        foreach (var renderer in hullRenderers)
        {
            if (!renderer.enabled || renderer.sharedMaterial == null)
                continue;
            hullProperties.Clear();
            renderer.GetPropertyBlock(hullProperties);
            Color color = hullProperties.HasColor("_Tint") ? hullProperties.GetColor("_Tint") :
                renderer.sharedMaterial.HasProperty("_Color") ? renderer.sharedMaterial.color : Color.white;
            if (Mathf.Max(color.r, Mathf.Max(color.g, color.b)) < .08f)
                continue;
            sum += color;
            count++;
        }
        hullRenderers.Clear();
        if (count == 0)
            return Color.white;
        Color tint = sum / count;
        tint.a = 1;
        return tint;
    }

    /// <summary>Small forward cone of hot sparks from a gun muzzle (shares the impact buffer).</summary>
    public static void Muzzle(Vector3 position, Vector2 direction)
    {
        if (buffer == null)
            buffer = Create(null, "PSX shared impacts", 512);
        float baseAngle = Mathf.Atan2(direction.y, direction.x);
        for (int i = 0; i < 4; i++)
        {
            float angle = baseAngle + Random.Range(-.35f, .35f);
            var e = new ParticleSystem.EmitParams();
            e.position = position + Vector3.back * .3f;
            e.velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * Random.Range(2.5f, 5f);
            e.startColor = i == 0 ? new Color(1, .97f, .85f) : new Color(1, .8f, .45f);
            float size = Random.Range(.04f, .07f);
            e.startSize3D = new Vector3(size, size * 2.6f, size);
            e.rotation3D = new Vector3(0, 0, 90 - angle * Mathf.Rad2Deg);
            e.startLifetime = Random.Range(.08f, .16f);
            buffer.Emit(e, 1);
        }
    }

    /// <summary>Collecting an item: the usual pickup burst plus a fountain of sparkles in its rarity
    /// colour that float up and fade, with a few white glints among them.</summary>
    public static void ItemGet(Vector3 position, Color color)
    {
        Burst(position, color, true);
        for (int i = 0; i < 26; i++)
        {
            var e = new ParticleSystem.EmitParams();
            e.position = position + new Vector3(Random.Range(-.3f, .3f), Random.Range(-.2f, .2f), -.3f);
            e.velocity = new Vector3(Random.Range(-.9f, .9f), Random.Range(1.4f, 3.4f), 0);
            e.startColor = i % 5 == 0 ? Color.white : Color.Lerp(color, Color.white, Random.Range(0f, .4f));
            e.startSize3D = Vector3.one * Random.Range(.05f, .1f);
            e.rotation3D = new Vector3(0, 0, 45);
            e.startLifetime = Random.Range(.45f, .85f);
            buffer.Emit(e, 1);
        }
    }

    /// <summary>A bullet dissolving: its core shrinks to a sparkle while a ring of sparks expands from
    /// its rim. Every part scales with the bullet's own world size, so a giant orb pops widely and a
    /// needle barely flickers. Mass clears share one bounded buffer apart from the hit sparks.</summary>
    public static void Vanish(Vector3 position, Color color, float size)
    {
        using var sample = vanishMarker.Auto();
        if (dissolves == null)
            dissolves = Create(null, "PSX bullet dissolves", 3200);
        float d = Mathf.Clamp(size, .08f, 3f);
        Vector3 at = position + Vector3.back * .3f;
        var core = new ParticleSystem.EmitParams();
        core.position = at;
        core.velocity = Vector3.zero;
        core.startColor = Color.Lerp(color, Color.white, .55f);
        core.startSize3D = Vector3.one * d * .9f;
        core.rotation3D = new Vector3(0, 0, 45);
        core.startLifetime = .14f + d * .06f;
        dissolves.Emit(core, 1);
        int sparks = d > .6f ? 10 : 6;
        float turn = Random.Range(0, Mathf.PI * 2);
        for (int i = 0; i < sparks; i++)
        {
            float angle = turn + i * Mathf.PI * 2 / sparks;
            var outward = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            var e = new ParticleSystem.EmitParams();
            e.position = at + outward * d * .45f;
            e.velocity = outward * (1.1f + d * 2.4f);
            e.startColor = i % 2 == 0 ? Color.Lerp(color, Color.white, .3f) : color;
            e.startSize3D = Vector3.one * (.025f + d * .07f);
            e.rotation3D = new Vector3(0, 0, 45);
            e.startLifetime = .12f + d * .07f;
            dissolves.Emit(e, 1);
        }
    }

    public static Color PickupColor(Component pickup)
    {
        string kind = pickup.GetType().Name.ToLowerInvariant();
        return kind.Contains("life") ? new Color(.85f, .25f, .20f) : kind.Contains("mana") ? new Color(.20f, .55f, .95f) : new Color(.85f, .68f, .32f);
    }
}
