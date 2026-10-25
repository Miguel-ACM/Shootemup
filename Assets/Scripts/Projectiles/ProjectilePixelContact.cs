using System.Collections.Generic;
using UnityEngine;

// Exact narrow phase against the same merged opaque-pixel rectangles used by
// ProjectileSprites.Fit. One native circle finds possible contacts; empty ink
// and holes never deal damage. No per-contact arrays or physics-tree fixtures.
public static class ProjectilePixelContact
{
    // Box2D's polygon skin is present even when the authored box radius is zero.
    public const float PolygonSkin = .01f;
    sealed class Shape
    {
        public Vector2[] local, projected;
    }

    static readonly Dictionary<Collider2D, Shape> shapes = new Dictionary<Collider2D, Shape>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        shapes.Clear();
    }

    public static bool Overlap(Transform bullet, Rect[] boxes, Collider2D target)
    {
        if (target == null || !target.enabled || !target.gameObject.activeInHierarchy)
            return false;
        float bulletScale = Mathf.Max(.00001f, Mathf.Abs(bullet.lossyScale.x));
        if (target is CircleCollider2D circle)
        {
            Vector2 centre = bullet.InverseTransformPoint(circle.transform.TransformPoint(circle.offset));
            var scale = circle.transform.lossyScale;
            float radius = (circle.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)) + PolygonSkin) / bulletScale;
            foreach (var box in boxes)
            {
                float x = Mathf.Clamp(centre.x, box.xMin, box.xMax), y = Mathf.Clamp(centre.y, box.yMin, box.yMax);
                if ((centre - new Vector2(x, y)).sqrMagnitude <= radius * radius)
                    return true;
            }
            return false;
        }
        if (!shapes.TryGetValue(target, out var shape))
        {
            shape = new Shape();
            if (target is PolygonCollider2D polygon)
                shape.local = polygon.GetPath(0); // Player's convex hexagon.
            else if (target is BoxCollider2D rectangle)
            {
                var e = rectangle.size * .5f;
                shape.local = new[] { new Vector2(-e.x, -e.y), new Vector2(e.x, -e.y), new Vector2(e.x, e.y), new Vector2(-e.x, e.y) };
            }
            else
                return false;
            shape.projected = new Vector2[shape.local.Length];
            shapes[target] = shape;
        }
        Matrix4x4 matrix = bullet.worldToLocalMatrix * target.transform.localToWorldMatrix;
        float skin = 2 * PolygonSkin / bulletScale;
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i < shape.local.Length; i++)
        {
            Vector2 point = matrix.MultiplyPoint3x4(shape.local[i] + target.offset);
            shape.projected[i] = point;
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minY = Mathf.Min(minY, point.y);
            maxY = Mathf.Max(maxY, point.y);
        }
        foreach (var box in boxes)
        {
            if (minX > box.xMax + skin || maxX < box.xMin - skin || minY > box.yMax + skin || maxY < box.yMin - skin)
                continue;
            bool overlap = true;
            for (int edge = 0; edge < shape.projected.Length; edge++)
            {
                var a = shape.projected[edge];
                var b = shape.projected[(edge + 1) % shape.projected.Length];
                var axis = new Vector2(a.y - b.y, b.x - a.x);
                float lo = float.MaxValue, hi = float.MinValue;
                foreach (var point in shape.projected)
                {
                    float p = Vector2.Dot(axis, point);
                    lo = Mathf.Min(lo, p);
                    hi = Mathf.Max(hi, p);
                }
                float centre = Vector2.Dot(axis, box.center), extent = Mathf.Abs(axis.x) * box.width * .5f + Mathf.Abs(axis.y) * box.height * .5f;
                if (lo > centre + extent || hi < centre - extent)
                {
                    overlap = false;
                    break;
                }
            }
            if (overlap)
                return true;
            // Rounded native polygon skins require Euclidean edge distance,
            // rather than inflating rectangular corners into square hit regions.
            foreach (var point in shape.projected)
            {
                var closest = new Vector2(Mathf.Clamp(point.x, box.xMin, box.xMax), Mathf.Clamp(point.y, box.yMin, box.yMax));
                if ((point - closest).sqrMagnitude <= skin * skin)
                    return true;
            }
            for (int corner = 0; corner < 4; corner++)
            {
                var point = new Vector2((corner & 1) == 0 ? box.xMin : box.xMax, (corner & 2) == 0 ? box.yMin : box.yMax);
                for (int edge = 0; edge < shape.projected.Length; edge++)
                {
                    var a = shape.projected[edge];
                    var segment = shape.projected[(edge + 1) % shape.projected.Length] - a;
                    float t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / Mathf.Max(.0000001f, segment.sqrMagnitude));
                    if ((point - a - segment * t).sqrMagnitude <= skin * skin)
                        return true;
                }
            }
        }
        return false;
    }
}
