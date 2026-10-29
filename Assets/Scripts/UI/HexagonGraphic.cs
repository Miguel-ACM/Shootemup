using UnityEngine;
using UnityEngine.UI;

// A pointy-topped hexagon plate. A ring can be drawn only partly, clockwise from the top corner.
public sealed class HexagonGraphic : MaskableGraphic
{
    public float thickness;
    // Share of the ring drawn, 0..1; ignored by a solid face.
    public float progress = 1;
    // Darkens the lower edge of the face, so plates read as lit from above.
    public float shade;

    public static Vector2 Corner(int index, float radius)
    {
        float angle = (90 - 60 * index) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    // Whether a point relative to the centre lies inside a hexagon of this corner radius.
    public static bool Contains(Vector2 p, float radius)
    {
        float x = Mathf.Abs(p.x), y = Mathf.Abs(p.y);
        return x <= radius * .8660254f && y + x * .57735027f <= radius;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        float radius = Mathf.Min(r.width, r.height) * .5f;
        Color Tint(Vector2 p)
        {
            float light = Mathf.Lerp(1 - shade, 1, Mathf.InverseLerp(r.yMin, r.yMax, p.y));
            return new Color(color.r * light, color.g * light, color.b * light, color.a);
        }
        if (thickness <= 0)
        {
            vh.AddVert(r.center, Tint(r.center), Vector2.zero);
            for (int i = 0; i < 6; i++)
            {
                var p = r.center + Corner(i, radius);
                vh.AddVert(p, Tint(p), Vector2.zero);
            }
            for (int i = 0; i < 6; i++)
                vh.AddTriangle(0, i + 1, (i + 1) % 6 + 1);
            return;
        }
        float sides = Mathf.Clamp01(progress) * 6;
        if (sides <= 0)
            return;
        // The inner corner sits on the same bisector, so the ring keeps an even width.
        float inner = radius - thickness / .8660254f;
        int count = 0;
        void Add(float along)
        {
            int side = Mathf.Min(5, Mathf.FloorToInt(along));
            float t = along - side;
            var outer = r.center + Vector2.Lerp(Corner(side, radius), Corner(side + 1, radius), t);
            var inside = r.center + Vector2.Lerp(Corner(side, inner), Corner(side + 1, inner), t);
            vh.AddVert(outer, Tint(outer), Vector2.zero);
            vh.AddVert(inside, Tint(inside), Vector2.zero);
            if (count > 0)
            {
                int a = (count - 1) * 2;
                vh.AddTriangle(a, a + 2, a + 1);
                vh.AddTriangle(a + 1, a + 2, a + 3);
            }
            count++;
        }
        for (int i = 0; i < sides; i++)
            Add(i);
        Add(sides);
    }
}
