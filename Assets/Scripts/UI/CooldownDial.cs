using UnityEngine;
using UnityEngine.UI;

// Blue octagon filling an active item's face. The remaining cooldown sweeps away clockwise from the top,
// and the whole face flashes once when the item is ready again.
public sealed class CooldownDial : MaskableGraphic
{
    const int Segments = 48;
    const float FlashSeconds = .3f;
    // The same corner cut as the instrument plates.
    public float cut = .23f;
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
        float half = Mathf.Min(r.width, r.height) * .5f;
        if (remaining > 0)
        {
            Sector(vh, r.center, half, 1, Track);
            Sector(vh, r.center, half, remaining, Charge);
        }
        else if (flash > 0)
        {
            var c = Ready;
            c.a *= flash / FlashSeconds;
            Sector(vh, r.center, half, 1, c);
        }
    }

    // Distance from the centre to the octagon outline along an angle.
    float Reach(float half, float angle)
    {
        float x = Mathf.Abs(Mathf.Cos(angle)), y = Mathf.Abs(Mathf.Sin(angle));
        float diagonal = half * (2 - 2 * cut);
        float reach = diagonal / Mathf.Max(.0001f, x + y);
        if (x > .0001f)
            reach = Mathf.Min(reach, half / x);
        if (y > .0001f)
            reach = Mathf.Min(reach, half / y);
        return reach;
    }

    void Sector(VertexHelper vh, Vector2 center, float half, float fraction, Color color)
    {
        // Sample the sweep evenly and add the plate's corners, so the edges stay straight.
        float start = Mathf.PI * .5f, sweep = fraction * Mathf.PI * 2;
        var angles = new System.Collections.Generic.List<float>();
        int steps = Mathf.Max(1, Mathf.CeilToInt(Segments * fraction));
        for (int i = 0; i <= steps; i++)
            angles.Add(sweep * i / steps);
        float corner = Mathf.Atan2(1, 1 - 2 * cut);
        foreach (float c in new[] { corner, Mathf.PI * .5f - corner })
            for (int q = 0; q < 4; q++)
            {
                float a = Mathf.Repeat(c + q * Mathf.PI * .5f - start, Mathf.PI * 2);
                if (a > 0 && a < sweep)
                    angles.Add(a);
            }
        angles.Sort();
        int k = vh.currentVertCount;
        vh.AddVert(center, color, Vector2.zero);
        // The unswept part ends at twelve o'clock, so the face empties clockwise.
        for (int i = 0; i < angles.Count; i++)
        {
            float angle = start + angles[i];
            vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Reach(half, angle), color, Vector2.zero);
            if (i > 0)
                vh.AddTriangle(k, k + i + 1, k + i);
        }
    }
}
