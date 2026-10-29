using UnityEngine;
using UnityEngine.UI;

// One piece of a bottom strip whose top edge dips toward the middle.
// Neighbouring pieces leave a V between them: open at the top, meeting at the bottom.
public sealed class CurvedStripGraphic : MaskableGraphic
{
    // Horizontal span of this piece across the whole strip, 0..1.
    public float from, to = 1;
    // Half-width of the V gap at the top of each inner edge, in canvas pixels.
    public float leftNotch, rightNotch;
    public float dip = 20;
    // When positive, only a rim of this thickness is drawn along the top and the V edges.
    public float thickness;
    // Brightens the top edge, so the piece reads as lit glass over gameplay.
    public float topGlow;
    const int Columns = 24;

    public float Top(float u)
    {
        var r = rectTransform.rect;
        float centred = u * 2 - 1;
        return r.yMax - dip * (1 - centred * centred);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        float leftTop = r.xMin + from * r.width + leftNotch, rightTop = r.xMin + to * r.width - rightNotch;
        float leftBottom = r.xMin + from * r.width, rightBottom = r.xMin + to * r.width;
        Vector2 TopAt(float x) => new Vector2(x, Top((x - r.xMin) / r.width));
        Color Tint(float y)
        {
            float t = Mathf.InverseLerp(r.yMin, r.yMax, y);
            return new Color(color.r + topGlow * t * t, color.g + topGlow * t * t, color.b + topGlow * t * t, color.a * Mathf.Lerp(1, 1 + topGlow, t));
        }
        if (thickness <= 0)
        {
            for (int k = 0; k <= Columns; k++)
            {
                float t = k / (float)Columns;
                var top = TopAt(Mathf.Lerp(leftTop, rightTop, t));
                var bottom = new Vector2(Mathf.Lerp(leftBottom, rightBottom, t), r.yMin);
                vh.AddVert(top, Tint(top.y), Vector2.zero);
                vh.AddVert(bottom, Tint(bottom.y), Vector2.zero);
                if (k > 0)
                {
                    int a = (k - 1) * 2;
                    vh.AddTriangle(a, a + 2, a + 1);
                    vh.AddTriangle(a + 1, a + 2, a + 3);
                }
            }
            return;
        }
        // The rim follows the outline: up the left edge, along the curve, down the right edge.
        var path = new System.Collections.Generic.List<Vector2>();
        if (leftNotch > 0)
            path.Add(new Vector2(leftBottom, r.yMin));
        for (int k = 0; k <= Columns; k++)
            path.Add(TopAt(Mathf.Lerp(leftTop, rightTop, k / (float)Columns)));
        if (rightNotch > 0)
            path.Add(new Vector2(rightBottom, r.yMin));
        for (int i = 0; i < path.Count; i++)
        {
            var before = path[Mathf.Max(0, i - 1)];
            var after = path[Mathf.Min(path.Count - 1, i + 1)];
            var direction = (after - before).normalized;
            var inward = new Vector2(direction.y, -direction.x) * thickness;
            vh.AddVert(path[i], Tint(path[i].y), Vector2.zero);
            vh.AddVert(path[i] + inward, Tint(path[i].y), Vector2.zero);
            if (i > 0)
            {
                int a = (i - 1) * 2;
                vh.AddTriangle(a, a + 2, a + 1);
                vh.AddTriangle(a + 1, a + 2, a + 3);
            }
        }
    }
}
