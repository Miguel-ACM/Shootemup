using System.Collections.Generic;
using UnityEngine;

// One continuous damage outline for the visible body. Cache mesh outlines once;
// articulated parts only transform those few boundary vertices during flight.
[DefaultExecutionOrder(200)]
public sealed class BossHullCollider : MonoBehaviour
{
    sealed class Part
    {
        public MeshFilter filter;
        public MeshRenderer renderer;
        public Mesh mesh;
        public Vector3[] outline;
    }
    readonly List<Part> parts = new();
    readonly List<Vector2> projected = new(512), boundary = new(256), previous = new(256);
    readonly Dictionary<Mesh, Vector3[]> outlines = new();
    static readonly System.Comparison<Vector2> pointOrder = Compare;
    DistrictBoss boss;
    PolygonCollider2D contact;

    public static PolygonCollider2D Attach(DistrictBoss owner)
    {
        var old = owner.GetComponent<CircleCollider2D>();
        old.enabled = false;
        Destroy(old);
        var shape = owner.gameObject.AddComponent<PolygonCollider2D>();
        shape.isTrigger = true;
        var fitter = owner.gameObject.AddComponent<BossHullCollider>();
        fitter.boss = owner;
        fitter.contact = shape;
        return shape;
    }

    void Start()
    {
        var thrusters = boss.GetComponent<EnemyThrusters>();
        foreach (var filter in boss.Hull.GetComponentsInChildren<MeshFilter>(true))
            if ((!(boss is VoidSovereignBoss sovereign) || sovereign.EchoTarget(filter)) &&
                (thrusters == null || !thrusters.IsExhaust(filter.transform)) &&
                (!(boss is DreamPainter) || filter.transform == boss.Hull) &&
                (!(boss is CoreGantry gantry) || filter.transform == gantry.Reactor))
                parts.Add(new Part { filter = filter, renderer = filter.GetComponent<MeshRenderer>() });
        if (boss is VoidSovereignBoss)
            for (int form = 0; form < 4; form++)
            {
                Outline(IndustrialMeshFactory.VoidCore(form));
                for (int limb = 0; limb < (form == 3 ? 4 : 2); limb++)
                    Outline(IndustrialMeshFactory.VoidLimb(form, limb));
            }
        PartHitFeedback.Bind(boss is CoreGantry core ? core.Reactor : transform, false, boss is VoidSovereignBoss);
        Fit();
    }

    void LateUpdate() => Fit();

    void Fit()
    {
        if (boss == null || !boss.enabled || boss.health <= 0 || contact == null) return;
        projected.Clear();
        var inverse = transform.worldToLocalMatrix;
        foreach (var part in parts)
        {
            if (part.filter == null || part.renderer == null || !part.renderer.enabled ||
                !part.filter.gameObject.activeInHierarchy || part.filter.sharedMesh == null) continue;
            var mesh = part.filter.sharedMesh;
            if (part.mesh != mesh)
            {
                part.mesh = mesh;
                part.outline = Outline(mesh);
            }
            var matrix = inverse * part.filter.transform.localToWorldMatrix;
            foreach (var point in part.outline) projected.Add(matrix.MultiplyPoint3x4(point));
        }
        ConvexOutline(projected, boundary);
        if (boundary.Count < 3) return;
        bool changed = previous.Count != boundary.Count;
        for (int i = 0; !changed && i < boundary.Count; i++)
            changed = (boundary[i]-previous[i]).sqrMagnitude > .00000001f;
        if (!changed) return;
        contact.SetPath(0, boundary);
        previous.Clear();
        previous.AddRange(boundary);
    }

    Vector3[] Outline(Mesh mesh)
    {
        if (outlines.TryGetValue(mesh, out var result)) return result;
        var vertices = mesh.vertices;
        var flat = new List<Vector2>(vertices.Length);
        foreach (var vertex in vertices) flat.Add(vertex);
        var hull = new List<Vector2>();
        ConvexOutline(flat, hull);
        // Preserve the mesh's depth when its wheels or limbs tilt in 3D.
        result = new Vector3[hull.Count * 2];
        for (int i = 0; i < hull.Count; i++)
        {
            result[i*2] = new Vector3(hull[i].x, hull[i].y, mesh.bounds.min.z);
            result[i*2+1] = new Vector3(hull[i].x, hull[i].y, mesh.bounds.max.z);
        }
        outlines.Add(mesh, result);
        return result;
    }

    static float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
    static int Compare(Vector2 a, Vector2 b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x);
    static void ConvexOutline(List<Vector2> points, List<Vector2> hull)
    {
        hull.Clear();
        if (points.Count < 3) return;
        points.Sort(pointOrder);
        foreach (var point in points)
        {
            while (hull.Count >= 2 && Cross(hull[^2], hull[^1], point) <= .000001f) hull.RemoveAt(hull.Count-1);
            hull.Add(point);
        }
        int lower = hull.Count;
        for (int i = points.Count-2; i >= 0; i--)
        {
            while (hull.Count > lower && Cross(hull[^2], hull[^1], points[i]) <= .000001f) hull.RemoveAt(hull.Count-1);
            hull.Add(points[i]);
        }
        hull.RemoveAt(hull.Count-1);
    }
}
