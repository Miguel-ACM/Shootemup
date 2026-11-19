using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The intact tally is photographed once. Voronoi cells share its UVs until
// the advancing fracture reaches them, then carry the actual letters away.
public sealed class VictoryGlass : MaskableGraphic
{
    public const float Duration = 1.48f;
    static readonly Vector2 Strike = new Vector2(-23, 97);
    sealed class Shard
    {
        public Vector2[] points, uv, projected;
        public Vector2 centre, impulse;
        public Vector3 spin;
        public float delay, zoom;
    }
    readonly List<Shard> pieces = new List<Shard>();
    RenderTexture photograph;
    Material glass;
    float elapsed;
    public override Texture mainTexture => photograph;

    public static VictoryGlass Capture(Canvas canvas, RectTransform panel)
    {
        var photo = RenderTexture.GetTemporary(608, 1080, 24, RenderTextureFormat.ARGB32);
        var cameraObject = new GameObject("Tally photograph");
        var lens = cameraObject.AddComponent<UnityEngine.Camera>();
        lens.enabled = false;
        lens.orthographic = true;
        lens.orthographicSize = 360;
        lens.nearClipPlane = .1f;
        lens.farClipPlane = 20;
        lens.clearFlags = CameraClearFlags.SolidColor;
        lens.backgroundColor = new Color(.016f, .025f, .032f, 1);
        lens.transform.position = new Vector3(10000, 10000, -10000);
        lens.targetTexture = photo;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = lens;
        canvas.planeDistance = 5;
        Canvas.ForceUpdateCanvases();
        lens.Render();
        canvas.worldCamera = null;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        lens.targetTexture = null;
        Destroy(cameraObject);
        var g = new GameObject("Fractured tally", typeof(RectTransform), typeof(CanvasRenderer)).AddComponent<VictoryGlass>();
        g.rectTransform.SetParent(canvas.transform, false);
        g.rectTransform.sizeDelta = panel.sizeDelta;
        g.rectTransform.anchoredPosition = panel.anchoredPosition;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
        g.photograph = photo;
        g.glass = new Material(Resources.Load<Shader>("VictoryGlass"));
        g.material = g.glass;
        g.raycastTarget = false;
        g.Cut();
        return g;
    }
    void Cut()
    {
        var rng = new System.Random(27451);
        float Rand(float lo, float hi) => Mathf.Lerp(lo, hi, (float)rng.NextDouble());
        var sites = new List<Vector2> { Strike };
        var rect = rectTransform.rect;
        for (int ring = 0; ring < 5; ring++)
        {
            int n = 7 + ring * 3;
            float radius = new[] { 14f, 43f, 99f, 185f, 330f }[ring];
            for (int j = 0; j < n; j++)
            {
                float a = (j + Rand(-.28f, .28f) + ring * .4f) * Mathf.PI * 2 / n;
                var p = Strike + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 1.22f) * radius * Rand(.79f, 1.19f);
                if (rect.Contains(p))
                    sites.Add(p);
            }
        }
        for (int i = 0; i < sites.Count; i++)
        {
            var site = sites[i];
            var poly = new List<Vector2> { rect.min, new Vector2(rect.xMax, rect.yMin), rect.max, new Vector2(rect.xMin, rect.yMax) };
            for (int j = 0; j < sites.Count && poly.Count > 0; j++)
                if (i != j)
                {
                    var axis = sites[j] - site;
                    var mid = (sites[j] + site) * .5f;
                    var clipped = new List<Vector2>(poly.Count + 1);
                    var prev = poly[poly.Count - 1];
                    float d0 = Vector2.Dot(prev - mid, axis);
                    foreach (var point in poly)
                    {
                        float d1 = Vector2.Dot(point - mid, axis);
                        if ((d1 <= 0) != (d0 <= 0))
                            clipped.Add(Vector2.LerpUnclamped(prev, point, d0 / (d0 - d1)));
                        if (d1 <= 0)
                            clipped.Add(point);
                        prev = point;
                        d0 = d1;
                    }
                    poly = clipped;
                }
            if (poly.Count < 3)
                continue;
            var s = new Shard { points = poly.ToArray(), uv = new Vector2[poly.Count], projected = new Vector2[poly.Count] };
            for (int j = 0; j < poly.Count; j++)
            {
                s.centre += poly[j];
                s.uv[j] = Rect.PointToNormalized(rect, poly[j]);
            }
            s.centre /= poly.Count;
            var radial = s.centre - Strike;
            s.impulse = radial.normalized * Rand(120, 325) + new Vector2(Rand(-22, 22), Rand(5, 65));
            s.spin = new Vector3(Rand(-142, 142), Rand(-170, 170), Rand(-58, 58));
            s.zoom = Rand(.20f, .9f);
            s.delay = radial.magnitude * .00056f;
            pieces.Add(s);
        }
    }
    public void Animate(float t)
    {
        elapsed = t;
        SetVerticesDirty();
    }
    static void Vertex(VertexHelper vh, Vector2 p, Color c, Vector2 uv, bool edge, float reflection = 0)
    {
        var v = UIVertex.simpleVert;
        v.position = p;
        v.color = c;
        v.uv0 = uv;
        v.uv1 = new Vector2(edge ? 1 : 0, reflection);
        vh.AddVert(v);
    }
    static void Glint(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
    {
        Vector2 v = b - a;
        Vector2 n = new Vector2(-v.y, v.x).normalized * width * .5f;
        int k = vh.currentVertCount;
        Vertex(vh, a - n, color, Vector2.zero, true);
        Vertex(vh, a + n, color, Vector2.zero, true);
        Vertex(vh, b + n, color, Vector2.zero, true);
        Vertex(vh, b - n, color, Vector2.zero, true);
        vh.AddTriangle(k, k + 1, k + 2);
        vh.AddTriangle(k, k + 2, k + 3);
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        foreach (var s in pieces)
        {
            float wake = elapsed - s.delay;
            float t = Mathf.Max(0, wake - .12f);
            float push = 1 - Mathf.Exp(-3 * t);
            var turn = Quaternion.Euler(s.spin * (t * .9f + t * t * .48f));
            var normal = turn * Vector3.forward;
            float reflection = Mathf.Clamp01(1 - normal.z) * .72f;
            Vector2 bevel = (Vector2)normal * (2.9f * push);
            var centre = s.centre + s.impulse * push * (.75f + t) + Vector2.down * (280 * t * t);
            float fade = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.65f, 1.08f, t));
            if (fade <= 0)
                continue;
            float flash = Mathf.Max(0, 1 - Mathf.Abs(wake - .045f) / .075f);
            int k = vh.currentVertCount;
            var face = new Color(1 - .22f * push, 1 - .12f * push, 1, fade);
            for (int i = 0; i < s.points.Length; i++)
            {
                Vector3 v = turn * (Vector3)(s.points[i] - s.centre);
                var p = centre + (Vector2)v * ((1 + t * s.zoom) * 440 / Mathf.Max(190, 440 - v.z));
                s.projected[i] = p;
                Vertex(vh, p, face, s.uv[i], false, reflection);
            }
            for (int j = 1; j < s.points.Length - 1; j++)
                vh.AddTriangle(k, k + j, k + j + 1);
            if (wake < 0)
                continue;
            for (int j = 0; j < s.points.Length; j++)
            {
                float sparkle = .5f + .5f * Mathf.Sin(t * 11 + s.spin.x + j * 2);
                Vector2 a = s.projected[j], b = s.projected[(j + 1) % s.points.Length];
                if (Vector2.Dot(bevel, new Vector2(b.y-a.y, a.x-b.x)) > 0)
                {
                    int edge = vh.currentVertCount;
                    var cold = new Color(.29f, .56f, .72f, fade);
                    var hot = new Color(.82f, .94f, 1, fade);
                    Vertex(vh, a, hot, Vector2.zero, true);
                    Vertex(vh, b, hot, Vector2.zero, true);
                    Vertex(vh, b + bevel, cold, Vector2.zero, true);
                    Vertex(vh, a + bevel, cold, Vector2.zero, true);
                    vh.AddTriangle(edge, edge+1, edge+2);
                    vh.AddTriangle(edge, edge+2, edge+3);
                }
                Glint(vh, a, b,
                    .6f + flash * 2 + sparkle * t, new Color(.69f + .30f * flash, .87f, 1, fade * (flash + .56f * sparkle)));
            }
        }
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (photograph != null)
            RenderTexture.ReleaseTemporary(photograph);
        if (glass != null)
            Destroy(glass);
    }
}
