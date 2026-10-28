using UnityEngine;
using UnityEngine.UI;

// Blue disc behind an active item. The remaining cooldown sweeps away clockwise from the top,
// and the whole disc flashes once when the item is ready again.
public sealed class CooldownDial : MaskableGraphic
{
    const int Segments = 48;
    const float FlashSeconds = .3f;
    static readonly Color Charge = new Color(.16f, .45f, 1f, .78f), Track = new Color(.1f, .24f, .5f, .32f), Ready = new Color(.55f, .82f, 1f, .9f);
    float remaining, flash;

    public void Show(float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        if (fraction <= 0 && remaining > 0)
            flash = FlashSeconds;
        if (fraction != remaining)
        {
            remaining = fraction;
            SetVerticesDirty();
        }
        if (flash > 0)
        {
            flash -= Time.unscaledDeltaTime;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        float radius = Mathf.Min(r.width, r.height) * .5f;
        if (remaining > 0)
        {
            Sector(vh, r.center, radius, 1, Track);
            Sector(vh, r.center, radius, remaining, Charge);
        }
        else if (flash > 0)
        {
            float t = 1 - flash / FlashSeconds;
            var c = Ready;
            c.a *= 1 - t;
            Sector(vh, r.center, radius * (1 + .18f * t), 1, c);
        }
    }

    static void Sector(VertexHelper vh, Vector2 center, float radius, float fraction, Color color)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(Segments * fraction));
        int k = vh.currentVertCount;
        vh.AddVert(center, color, Vector2.zero);
        // The unswept part ends at twelve o'clock, so the disc empties clockwise.
        for (int i = 0; i <= steps; i++)
        {
            float angle = Mathf.PI * .5f + fraction * Mathf.PI * 2 * i / steps;
            vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
            if (i > 0)
                vh.AddTriangle(k, k + i + 1, k + i);
        }
    }
}
