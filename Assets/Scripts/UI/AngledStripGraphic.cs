using UnityEngine;
using UnityEngine.UI;

// Bottom strip whose top edge steps down at thirty degrees from each upper corner to a flat middle.
public sealed class AngledStripGraphic : MaskableGraphic
{
    public float dip = 20;
    // When positive, only a rim of this thickness is drawn along the top edge.
    public float thickness;
    // Brightens the top edge, so the strip reads as lit glass over gameplay.
    public float topGlow;
    static readonly float Run = 1 / Mathf.Tan(30 * Mathf.Deg2Rad);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        float run = Mathf.Min(dip * Run, r.width * .5f);
        Vector2[] top = { new Vector2(r.xMin, r.yMax), new Vector2(r.xMin + run, r.yMax - dip), new Vector2(r.xMax - run, r.yMax - dip), new Vector2(r.xMax, r.yMax) };
        Color Tint(float y)
        {
            float t = Mathf.InverseLerp(r.yMin, r.yMax, y);
            return new Color(color.r + topGlow * t * t, color.g + topGlow * t * t, color.b + topGlow * t * t, color.a * Mathf.Lerp(1, 1 + topGlow, t));
        }
        if (thickness <= 0)
        {
            for (int k = 0; k < top.Length; k++)
            {
                vh.AddVert(top[k], Tint(top[k].y), Vector2.zero);
                vh.AddVert(new Vector2(top[k].x, r.yMin), Tint(r.yMin), Vector2.zero);
                if (k > 0)
                {
                    int a = (k - 1) * 2;
                    vh.AddTriangle(a, a + 2, a + 1);
                    vh.AddTriangle(a + 1, a + 2, a + 3);
                }
            }
            return;
        }
        // Mitred joins keep the rim an even width through both bends.
        for (int i = 0; i < top.Length; i++)
        {
            var before = i > 0 ? Normal(top[i - 1], top[i]) : Normal(top[i], top[i + 1]);
            var after = i < top.Length - 1 ? Normal(top[i], top[i + 1]) : before;
            var mitre = (before + after).normalized;
            var inward = mitre * thickness / Mathf.Max(.2f, Vector2.Dot(mitre, before));
            vh.AddVert(top[i], Tint(top[i].y), Vector2.zero);
            vh.AddVert(top[i] + inward, Tint(top[i].y), Vector2.zero);
            if (i > 0)
            {
                int a = (i - 1) * 2;
                vh.AddTriangle(a, a + 2, a + 1);
                vh.AddTriangle(a + 1, a + 2, a + 3);
            }
        }
    }

    static Vector2 Normal(Vector2 from, Vector2 to)
    {
        var direction = (to - from).normalized;
        return new Vector2(direction.y, -direction.x);
    }
}
