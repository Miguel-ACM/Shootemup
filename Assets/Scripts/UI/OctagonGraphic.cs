using UnityEngine;
using UnityEngine.UI;

// Cut-corner instrument plate. A ring uses the same outline as its inset face.
public sealed class OctagonGraphic : MaskableGraphic
{
    public float thickness;
    public float cut = .23f;
    // Darkens the lower edge of the face, so plates read as lit from above.
    public float shade;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        Vector2[] Outline(float inset)
        {
            float l = r.xMin + inset, b = r.yMin + inset, h = r.height - inset * 2, w = r.width - inset * 2, c = Mathf.Min(w, h) * cut;
            return new[] { new Vector2(l + c, b), new Vector2(l + w - c, b), new Vector2(l + w, b + c), new Vector2(l + w, b + h - c), new Vector2(l + w - c, b + h), new Vector2(l + c, b + h), new Vector2(l, b + h - c), new Vector2(l, b + c) };
        }
        Color Tint(Vector2 p)
        {
            float light = Mathf.Lerp(1 - shade, 1, Mathf.InverseLerp(r.yMin, r.yMax, p.y));
            return new Color(color.r * light, color.g * light, color.b * light, color.a);
        }
        var outer = Outline(0);
        if (thickness <= 0)
        {
            vh.AddVert(r.center, Tint(r.center), Vector2.zero);
            foreach (var p in outer)
                vh.AddVert(p, Tint(p), Vector2.zero);
            for (int i = 0; i < 8; i++)
                vh.AddTriangle(0, i + 1, (i + 1) % 8 + 1);
        }
        else
        {
            var inner = Outline(thickness);
            for (int i = 0; i < 8; i++)
            {
                vh.AddVert(outer[i], Tint(outer[i]), Vector2.zero);
                vh.AddVert(inner[i], Tint(inner[i]), Vector2.zero);
            }
            for (int i = 0; i < 8; i++)
            {
                int a = i * 2, b = (i + 1) % 8 * 2;
                vh.AddTriangle(a, b, a + 1);
                vh.AddTriangle(a + 1, b, b + 1);
            }
        }
    }
}
