using UnityEngine;

// The boss keeps two rifts for the whole encounter. A pointed tear, luminous lips,
// inward streaks and a dark interior remain distinct from the round hostile sprites.
// Each rift also owns a shock ring, a jagged travel bolt and a shard burst, all built
// once with the boss so a teleport beat never creates objects.
public sealed class VoidRiftVisual : MonoBehaviour
{
    static Mesh mesh;
    static Material surface;
    static readonly Color Violet = new Color(.72f, .34f, 1f), Pale = new Color(.93f, .78f, 1f);
    const int RingPoints = 49, BoltPoints = 14;
    MeshRenderer visual;
    MaterialPropertyBlock ink;
    LineRenderer ring, halo, bolt;
    ParticleSystem shards;

    public static VoidRiftVisual Create(Transform parent)
    {
        if (mesh == null)
        {
            mesh = new Mesh { name = "Void dimensional slit" };
            mesh.vertices = new[] { new Vector3(-1,-1), new Vector3(1,-1), new Vector3(1,1), new Vector3(-1,1) };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.triangles = new[] { 0,2,1,0,3,2 };
            mesh.RecalculateBounds();
        }
        if (surface == null) surface = new Material(Resources.Load<Shader>("VoidRift"));
        var go = new GameObject("Purple dimensional tear");
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var rift = go.AddComponent<VoidRiftVisual>();
        rift.ink = new MaterialPropertyBlock();
        rift.visual = go.AddComponent<MeshRenderer>();
        rift.visual.sharedMaterial = surface;
        rift.visual.sortingLayerName = "Enemy";
        rift.visual.sortingOrder = 20;
        rift.visual.enabled = false;
        rift.ring = Line(parent, RingPoints, .085f);
        rift.halo = Line(parent, RingPoints, .22f);
        rift.bolt = Line(parent, BoltPoints, .16f);
        rift.shards = PSXImpactEffects.Create(null, "Void rift shards", 160);
        return rift;
    }

    static LineRenderer Line(Transform parent, int points, float width)
    {
        var line = CombatVisuals.Line(parent, Violet, width);
        line.useWorldSpace = true;
        line.positionCount = points;
        line.sortingLayerName = "Enemy";
        line.sortingOrder = 21;
        line.enabled = false;
        return line;
    }

    // A circle of the given radius; the soft halo underneath doubles as its glow.
    public void Shock(Vector3 center, float radius, float strength)
    {
        bool on = strength > .01f && radius > .02f;
        ring.enabled = halo.enabled = on;
        if (!on)
            return;
        center += Vector3.back * 2.1f;
        for (int i = 0; i < RingPoints; i++)
        {
            float a = i * Mathf.PI * 2 / (RingPoints - 1);
            // A slightly torn circle: the rim wavers like the slit's lips.
            float r = radius * (1 + .04f * Mathf.Sin(a * 7 + radius * 5));
            var p = center + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0);
            ring.SetPosition(i, p);
            halo.SetPosition(i, p);
        }
        ring.startColor = ring.endColor = new Color(Pale.r, Pale.g, Pale.b, strength);
        halo.startColor = halo.endColor = new Color(Violet.r, Violet.g, Violet.b, strength * .35f);
    }

    // A jagged bolt racing from the old perch to the new one, so the eye follows the boss.
    public void Bolt(Vector3 from, Vector3 to, float progress, float clock)
    {
        bool on = progress > 0 && progress < 1;
        bolt.enabled = on;
        if (!on)
            return;
        float head = Mathf.SmoothStep(0, 1, Mathf.Clamp01(progress * 1.35f));
        float tail = Mathf.SmoothStep(0, 1, Mathf.Clamp01(progress * 1.35f - .35f));
        Vector3 across = Vector3.Cross(to - from, Vector3.forward).normalized;
        for (int i = 0; i < BoltPoints; i++)
        {
            float u = i / (BoltPoints - 1f);
            float along = Mathf.Lerp(tail, head, u);
            float jag = i == 0 || i == BoltPoints - 1 ? 0 : Mathf.Sin(i * 2.3f + clock * 38) * .32f * Mathf.Sin(u * Mathf.PI);
            bolt.SetPosition(i, Vector3.Lerp(from, to, along) + across * jag + Vector3.back * 2.1f);
        }
        float fade = Mathf.Sin(progress * Mathf.PI);
        bolt.startColor = new Color(Violet.r, Violet.g, Violet.b, 0);
        bolt.endColor = new Color(Pale.r, Pale.g, Pale.b, fade);
        bolt.widthMultiplier = .6f + .6f * fade;
    }

    // Shards drawn into the departing tear, or flung out of the arriving one.
    public void Burst(Vector3 center, bool inward)
    {
        const int Count = 34;
        for (int i = 0; i < Count; i++)
        {
            float a = (i + Random.value * .6f) * Mathf.PI * 2 / Count;
            var radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
            float life = Random.Range(.32f, .5f);
            float reach = Random.Range(1.8f, 3.1f);
            var e = new ParticleSystem.EmitParams
            {
                position = center + Vector3.back * 2.2f + radial * (inward ? reach : .25f),
                velocity = radial * (inward ? -reach / life : reach / life),
                startColor = i % 3 == 0 ? Pale : Violet,
                startLifetime = life
            };
            float size = Random.Range(.05f, .12f);
            e.startSize3D = new Vector3(size, size * 2.6f, size);
            e.rotation3D = new Vector3(0, 0, 90 - a * Mathf.Rad2Deg);
            shards.Emit(e, 1);
        }
    }

    public void Pose(Vector3 position, float opening, float clock, bool arrival)
    {
        transform.position = position + Vector3.back * 2;
        transform.localScale = new Vector3(2.8f, 3.6f, 1);
        ink.SetFloat("_Open", Mathf.Clamp01(opening));
        ink.SetFloat("_Clock", clock);
        ink.SetFloat("_Arrival", arrival ? 1 : 0);
        visual.SetPropertyBlock(ink);
        visual.enabled = opening > .001f;
    }
    public void Hide()
    {
        visual.enabled = false;
        ring.enabled = halo.enabled = bolt.enabled = false;
    }
}
