using UnityEngine;
using UnityEngine.UI;

// Cut-corner instrument plate. A ring uses the same outline as its inset face.
public sealed class OctagonGraphic : MaskableGraphic
{
    public float thickness;
    public float cut = .23f;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        Vector2[] Outline(float inset)
        {
            float l = r.xMin + inset, b = r.yMin + inset, h = r.height - inset * 2, w = r.width - inset * 2, c = Mathf.Min(w, h) * cut;
            return new[] { new Vector2(l + c, b), new Vector2(l + w - c, b), new Vector2(l + w, b + c), new Vector2(l + w, b + h - c), new Vector2(l + w - c, b + h), new Vector2(l + c, b + h), new Vector2(l, b + h - c), new Vector2(l, b + c) };
        }
        var outer = Outline(0);
        if (thickness <= 0)
        {
            vh.AddVert(r.center, color, Vector2.zero);
            foreach (var p in outer)
                vh.AddVert(p, color, Vector2.zero);
            for (int i = 0; i < 8; i++)
                vh.AddTriangle(0, i + 1, (i + 1) % 8 + 1);
        }
        else
        {
            var inner = Outline(thickness);
            for (int i = 0; i < 8; i++)
            {
                vh.AddVert(outer[i], color, Vector2.zero);
                vh.AddVert(inner[i], color, Vector2.zero);
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
