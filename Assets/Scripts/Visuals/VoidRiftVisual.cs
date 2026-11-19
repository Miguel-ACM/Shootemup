using UnityEngine;

// The boss keeps two folding apertures for the whole encounter.
// Each rift also owns a shock ring, a jagged travel bolt and a shard burst, all built
// once with the boss so a teleport beat never creates objects.
public sealed class VoidRiftVisual : MonoBehaviour
{
    static Mesh mesh;
    static Material surface;
    static Color RouteColor => LevelProjectilePalette.Accent(LevelId.Void);
    static Color Pale => Color.Lerp(RouteColor,Color.white,.65f);
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
        var go = new GameObject("Dimensional tear");
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
        rift.halo = Line(parent, RingPoints, .054f);
        rift.bolt = Line(parent, BoltPoints, .16f);
        rift.shards = PSXImpactEffects.Create(parent, "Void rift shards", 160);
        return rift;
    }

    static LineRenderer Line(Transform parent, int points, float width)
    {
        var line = CombatVisuals.Line(parent, RouteColor, width);
        line.useWorldSpace = true;
        line.positionCount = points;
        line.sortingLayerName = "Enemy";
        line.sortingOrder = 21;
        line.enabled = false;
        return line;
    }

    // A pair of offset angular echoes marks the space folding shut.
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
            float x = Mathf.Cos(a), y = Mathf.Sin(a);
            float r = radius / (Mathf.Abs(x) + Mathf.Abs(y));
            var vertex = new Vector3(x*r*.80f + y*r*.23f,y*r,0);
            ring.SetPosition(i, center + vertex);
            halo.SetPosition(i, center + vertex*.81f + Vector3.right*radius*.12f);
        }
        ring.startColor = ring.endColor = new Color(Pale.r, Pale.g, Pale.b, strength);
        halo.startColor = halo.endColor = new Color(RouteColor.r, RouteColor.g, RouteColor.b, strength * .35f);
    }

    internal static Vector3 Crossing(Vector3 from,Vector3 to,Vector3 bend,float u)
        => Vector3.Lerp(from,to,u) + bend*(4*u*(1-u));

    // Draw only the part of the arc already traversed. The bright end stays on the
    // moving body, with a thin wake that catches up as it lands.
    public void Bolt(Vector3 from,Vector3 to,Vector3 bend,float head)
    {
        bolt.enabled = head>.001f && head<.999f;
        if (!bolt.enabled) return;
        float tail = head*head*head;
        for(int i=0;i<BoltPoints;i++)
        {
            float u = Mathf.Lerp(tail,head,i/(BoltPoints-1f));
            bolt.SetPosition(i,Crossing(from,to,bend,u)+Vector3.back*1.15f);
        }
        Color start=RouteColor, end=Pale;
        start.a=0; end.a=.82f*Mathf.Sin(head*Mathf.PI);
        bolt.startColor=start;
        bolt.endColor=end;
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
                startColor = i % 3 == 0 ? Pale : RouteColor,
                startLifetime = life
            };
            float size = Random.Range(.05f, .12f);
            e.startSize3D = new Vector3(size, size * 2.6f, size);
            e.rotation3D = new Vector3(0, 0, 90 - a * Mathf.Rad2Deg);
            shards.Emit(e, 1);
        }
    }

    public void Pose(Vector3 position, float opening, float clock, bool arrival, float lean = 0)
    {
        transform.rotation = Quaternion.Euler(0,0,lean);
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
