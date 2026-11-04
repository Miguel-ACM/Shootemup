using UnityEngine;
using UnityEngine.UI;

// One small UI mesh per broken offer: no particles, lights, textures or per-hit objects.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RewardHexShatter : MaskableGraphic
{
    public const float Duration = .72f;
    public float elapsed;
    public Vector2 impact;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        float t = Mathf.Clamp01(elapsed / Duration);
        if (t >= 1) return;
        var rect = rectTransform.rect;
        float radius = Mathf.Min(rect.width, rect.height) * .5f;
        // Crack briefly in place, then kick the pieces outward and let them tumble away.
        float travel = Mathf.Clamp01((t - .055f) / .945f);
        float fade = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.2f, 1, t));
        float flash = Mathf.Clamp01(1 - t / .16f);
        Color rim = Color.Lerp(color, new Color(.85f, .95f, 1), flash);
        rim.a = fade * .9f;
        var glass = new Color(color.r, color.g, color.b, fade * (.1f + flash * .1f));
        Vector2 origin = impact * radius;
        for (int i = 0; i < 12; i++)
        {
            int edge = i / 2;
            Vector2 corner = HexagonGraphic.Corner(edge, radius);
            Vector2 next = HexagonGraphic.Corner(edge + 1, radius);
            Vector2 a = Vector2.Lerp(corner, next, (i % 2) * .5f);
            Vector2 b = Vector2.Lerp(corner, next, (i % 2 + 1) * .5f);
            Vector2 pivot = (origin + a + b) / 3;
            Vector2 direction = (pivot - origin).normalized;
            float speed = .48f + (i * 7 % 5) * .11f;
            Vector2 offset = direction * radius * speed * (1 - Mathf.Pow(1 - travel, 2))
                + Vector2.down * radius * travel * travel * .32f;
            float angle = travel * (i % 2 == 0 ? 1 : -1) * (25 + (i * 11 % 40)) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
            float shrink = 1 - travel * .35f;
            Vector2 Move(Vector2 point)
            {
                point = (point - pivot) * shrink;
                return rect.center + pivot + offset + new Vector2(point.x * cos - point.y * sin, point.x * sin + point.y * cos);
            }
            Vector2 p = Move(origin), q = Move(a), r = Move(b);
            Triangle(vh, p, q, r, glass);
            Line(vh, q, r, 1.8f * (1 - t * .5f), rim);
            var crack = rim;
            crack.a *= .6f * (1 - travel);
            Line(vh, p, q, .7f, crack);
            // Brief slivers continue the outward motion without obscuring the ship.
            if (travel > .08f)
            {
                Vector2 tip = (q + r) * .5f + direction * radius * travel * .3f;
                var glint = rim;
                glint.a *= .6f * (1 - travel);
                Line(vh, tip, tip - direction * radius * .12f * (1 - travel), 1, glint);
            }
        }
    }

    static void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color tint)
    {
        int start = vh.currentVertCount;
        vh.AddVert(a, tint, Vector2.zero);
        vh.AddVert(b, tint, Vector2.zero);
        vh.AddVert(c, tint, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
    }

    static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
    {
        Vector2 along = (b - a).normalized;
        Vector2 normal = new Vector2(-along.y, along.x) * (width * .5f);
        int start = vh.currentVertCount;
        vh.AddVert(a - normal, tint, Vector2.zero);
        vh.AddVert(a + normal, tint, Vector2.zero);
        vh.AddVert(b + normal, tint, Vector2.zero);
        vh.AddVert(b - normal, tint, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}
