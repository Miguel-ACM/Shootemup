using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Display glass remains bolted to a world position. Its lettering is drawn at display
// resolution after the district compositor and clipped by the same portrait aperture.
[DefaultExecutionOrder(110)]
public sealed class DockReadouts : MonoBehaviour
{
    sealed class Mount
    {
        public RectTransform panel;
        public Vector3 point;
        public float scale;
        public bool bob;
        public int region;
        public CanvasGroup regionVisibility;
    }
    readonly List<Mount> mounts = new List<Mount>();
    public int BuildingRoom { get; set; } = -1;
    RectTransform aperture;
    UnityEngine.Camera view;

    public void Initialize(UnityEngine.Camera camera)
    {
        view = camera;
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        aperture = new GameObject("Portrait glass", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
        aperture.SetParent(transform, false);
        aperture.sizeDelta = new Vector2(405, 720);
    }
    public void MountAt(RectTransform panel, Vector3 anchor, float worldScale, bool bob = false)
    {
        panel.SetParent(aperture, false);
        mounts.Add(new Mount { panel = panel, point = anchor, scale = worldScale, bob = bob, region = BuildingRoom, regionVisibility = BuildingRoom >= 0 ? panel.gameObject.AddComponent<CanvasGroup>() : null });
    }
    public void ShowRoom(int room)
    {
        foreach (var mount in mounts)
            if (mount.regionVisibility != null)
                mount.regionVisibility.alpha = room < 0 || mount.region == room ? 1 : 0;
    }
    public void Aim(RectTransform panel, Vector3 point)
    {
        foreach (var mount in mounts)
            if (mount.panel == panel)
            {
                mount.point = point;
                return;
            }
    }
    void LateUpdate()
    {
        float pixels = 360 / view.orthographicSize;
        for (int i = mounts.Count - 1; i >= 0; i--)
        {
            var m = mounts[i];
            if (m.panel == null)
            {
                mounts.RemoveAt(i);
                continue;
            }
            if (!m.panel.gameObject.activeSelf)
                continue;
            var offset = m.point - view.transform.position;
            if (m.bob)
                offset.y += Mathf.Sin(Time.time * 1.8f + m.point.y) * .09f;
            m.panel.anchoredPosition = new Vector2(offset.x, offset.y) * pixels;
            m.panel.localScale = Vector3.one * (pixels * m.scale);
        }
    }
}

// The curved phosphor field has a bowed outline, convex highlights and fine scan lines.
// Text sits on a separate layer of the same tube and stays sharp.
public sealed class DockTubeGlass : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var size = rectTransform.rect.size;
        const int cols = 20, rows = 160;
        for (int y = 0; y <= rows; y++)
        {
            float v = y * 2f / rows - 1;
            for (int x = 0; x <= cols; x++)
            {
                float u = x * 2f / cols - 1;
                float curvature = u * u * .045f + v * v * .035f;
                float glow = Mathf.Max(0, 1 - u * u - v * v * .7f);
                var tint = Color.Lerp(new Color(.014f, .033f, .042f), new Color(.07f, .16f, .17f), glow * .72f + (y % 2) * .22f);
                vh.AddVert(new Vector3(u * size.x * .5f * (1 - .035f * v * v), v * size.y * .5f * (1 - .045f * u * u), curvature * 7), tint, Vector2.zero);
                if (x > 0 && y > 0)
                {
                    int n = y * (cols + 1) + x;
                    vh.AddTriangle(n - cols - 2, n - 1, n);
                    vh.AddTriangle(n - cols - 2, n, n - cols - 1);
                }
            }
        }
    }
}
