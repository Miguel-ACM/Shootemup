using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Collected items shown as spinning 3D models in the margin to the right of the portrait playfield.
/// A small orthographic camera far from the playfield renders a grid of slots into a point-filtered
/// texture with a transparent background; created on the first pickup. Few items are shown large and
/// shrink as the collection grows, wrapping into more rows. While the boss meter is on, the grid
/// slides right to clear it.
/// </summary>
public sealed class PSXInventoryDisplay : MonoBehaviour
{
    // Canvas units (720 high). The area stays above the loadout octagons at the bottom.
    const float AreaWidth = 168, AreaHeight = 540;
    const float MaxCell = 56, MinCell = 18;
    const float Left = 202.5f + 10, Top = 360 - 10, Gap = 8, ScreenGutter = 8;
    const float WorldPerUnit = 1 / 30f;
    static readonly Vector3 Origin = new Vector3(500, 0, 0);
    static PSXInventoryDisplay instance;
    readonly List<Transform> items = new List<Transform>();
    readonly List<float> itemSizes = new List<float>();
    // Item pings (Miguel, 14 November 2026): an item that activates glows in its slot and pops.
    const float PingSeconds = .65f, PingRetrigger = .2f;
    readonly List<float> pingedAt = new List<float>();
    readonly List<Color> pingColors = new List<Color>();
    readonly List<Image> glows = new List<Image>();
    static Sprite glowSprite;
    readonly Vector3[] corners = new Vector3[4];
    Canvas canvas;
    RectTransform panel;
    RenderTexture texture;
    RectTransform bossHull;
    float nextHullScan, shift;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        instance = null;
    }

    public static Transform Add(Mesh mesh, Material material)
    {
        if (mesh == null || material == null)
            return null;
        if (instance == null)
            instance = new GameObject("PSX inventory display").AddComponent<PSXInventoryDisplay>();
        return instance.AddItem(mesh, material);
    }

    public static void Remove(Transform display)
    {
        if (instance == null || display == null)
            return;
        int index = instance.items.IndexOf(display);
        if (index < 0)
            return;
        instance.items.RemoveAt(index);
        instance.itemSizes.RemoveAt(index);
        instance.pingedAt.RemoveAt(index);
        instance.pingColors.RemoveAt(index);
        if (instance.glows[index] != null)
            Destroy(instance.glows[index].gameObject);
        instance.glows.RemoveAt(index);
        Destroy(display.gameObject);
    }

    /// <summary>Lights the slot of an item that just activated: a halo behind it and a short pop.</summary>
    public static void Ping(Transform display, Color color)
    {
        if (instance == null || display == null)
            return;
        int index = instance.items.IndexOf(display);
        if (index < 0)
            return;
        float now = Time.unscaledTime;
        // Rapid procs hold the glow lit instead of restarting it every hit.
        if (now - instance.pingedAt[index] < PingRetrigger)
            return;
        instance.pingedAt[index] = now;
        instance.pingColors[index] = color;
    }

    float Pulse(int index) => Mathf.Clamp01(1 - (Time.unscaledTime - pingedAt[index]) / PingSeconds);

    void Awake()
    {
        transform.position = Origin;
        // One texel per canvas unit keeps the chunky PSX look at every icon size.
        texture = new RenderTexture((int)AreaWidth, (int)AreaHeight, 16, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point, name = "PSX inventory" };
        var cameraObject = new GameObject("Inventory camera");
        cameraObject.transform.SetParent(transform, false);
        cameraObject.transform.localPosition = new Vector3(0, 0, -10);
        var camera = cameraObject.AddComponent<UnityEngine.Camera>();
        camera.orthographic = true;
        camera.orthographicSize = AreaHeight * WorldPerUnit * .5f;
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 20;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.clear;   // no panel background
        camera.targetTexture = texture;
        camera.depth = -50;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;

        var ui = new GameObject("Inventory panel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        ui.transform.SetParent(transform, false);
        canvas = ui.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6;
        var scaler = ui.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        FluorescentStartup.Attach(ui, .28f);
        var image = new GameObject("Collected items", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        image.transform.SetParent(ui.transform, false);
        var raw = image.GetComponent<RawImage>();
        raw.texture = texture;
        raw.raycastTarget = false;
        panel = image.GetComponent<RectTransform>();
        panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
        // Just right of the 405-wide playfield, top-aligned with it.
        panel.pivot = new Vector2(0, 1);
        panel.anchoredPosition = new Vector2(Left, Top);
        panel.sizeDelta = new Vector2(AreaWidth, AreaHeight);
    }

    Transform AddItem(Mesh mesh, Material material)
    {
        // The slot pivots on the model's centre so off-centre meshes spin in place.
        var slot = new GameObject("Collected " + mesh.name).transform;
        slot.SetParent(transform, false);
        var go = new GameObject("Model");
        go.transform.SetParent(slot, false);
        go.transform.localPosition = -mesh.bounds.center;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        itemSizes.Add(Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z, .01f));
        items.Add(slot);
        pingedAt.Add(float.NegativeInfinity);
        pingColors.Add(Color.white);
        glows.Add(null);
        // New arrivals appear at their slot and grow in from nothing.
        float width = AvailableWidth(BossShift()), cell = CellSize(items.Count, width);
        Arrange(items.Count - 1, cell, Columns(width, cell), 1);
        slot.localScale = Vector3.zero;
        return slot;
    }

    /// <summary>The largest cell, between MinCell and MaxCell, whose grid holds every item in the area.</summary>
    static float CellSize(int count, float width)
    {
        for (float cell = MaxCell; cell > MinCell; cell -= 1)
            if (Mathf.CeilToInt(count / (float)Columns(width, cell)) * cell <= AreaHeight)
                return cell;
        return MinCell;
    }

    static int Columns(float width, float cell)
    {
        return Mathf.Max(1, Mathf.FloorToInt(width / cell));
    }

    /// <summary>Room between the panel's left edge and the screen edge, up to the texture's width.</summary>
    float AvailableWidth(float offset)
    {
        float halfCanvas = ((RectTransform)canvas.transform).rect.width * .5f;
        return Mathf.Clamp(halfCanvas - ScreenGutter - Left - offset, MinCell, AreaWidth);
    }

    /// <summary>How far right the grid must sit to clear the boss meter, or 0 while it is off.</summary>
    float BossShift()
    {
        // The meter belongs to the industrial instruments; follow its hull rather than duplicating its rules.
        if (bossHull == null && Time.unscaledTime > nextHullScan)
        {
            nextHullScan = Time.unscaledTime + .5f;
            var hud = FindAnyObjectByType<IndustrialHUD>();
            if (hud != null)
                foreach (var rect in hud.GetComponentsInChildren<RectTransform>(true))
                    if (rect.name == "Boss hull")
                    {
                        bossHull = rect;
                        break;
                    }
        }
        if (bossHull == null || !bossHull.gameObject.activeInHierarchy)
            return 0;
        // Both canvases are screen-space overlays, so world corners are screen pixels.
        bossHull.GetWorldCorners(corners);
        float right = (corners[2].x - Screen.width * .5f) / Mathf.Max(.01f, canvas.scaleFactor);
        return Mathf.Max(0, right + Gap - Left);
    }

    void Arrange(int index, float cell, int columns, float blend)
    {
        float u = (index % columns + .5f) * cell, v = (index / columns + .5f) * cell;
        var target = new Vector3((u - AreaWidth * .5f) * WorldPerUnit, (AreaHeight * .5f - v) * WorldPerUnit, 0);
        var slot = items[index];
        float pop = 1 + .28f * Pulse(index);
        slot.localPosition = Vector3.Lerp(slot.localPosition, target, blend);
        slot.localScale = Vector3.Lerp(slot.localScale, Vector3.one * (.78f * cell * pop * WorldPerUnit / itemSizes[index]), blend);
    }

    // The halo sits behind the transparent render texture, centred on the item's cell.
    void Glow(int index, float cell, int columns)
    {
        float pulse = Pulse(index);
        var glow = glows[index];
        if (pulse <= 0)
        {
            if (glow != null && glow.enabled)
                glow.enabled = false;
            return;
        }
        if (glow == null)
        {
            var go = new GameObject("Item ping", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            go.transform.SetSiblingIndex(0);
            glows[index] = glow = go.GetComponent<Image>();
            glow.sprite = GlowSprite();
            glow.raycastTarget = false;
            var rect = glow.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        }
        glow.enabled = true;
        float u = (index % columns + .5f) * cell, v = (index / columns + .5f) * cell;
        glow.rectTransform.anchoredPosition = new Vector2(Left + shift + u, Top - v);
        glow.rectTransform.sizeDelta = Vector2.one * cell * (1.05f + .3f * (1 - pulse));
        var color = Color.Lerp(pingColors[index], Color.white, .35f);
        color.a = .9f * pulse * pulse;
        glow.color = color;
    }

    // A chunky soft disc with a brighter rim, point-filtered like the rest of the instruments.
    static Sprite GlowSprite()
    {
        if (glowSprite != null)
            return glowSprite;
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Item ping glow" };
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float r = Vector2.Distance(new Vector2(x + .5f, y + .5f), Vector2.one * size * .5f) / (size * .5f);
                float body = Mathf.Clamp01(1 - r) * .55f;
                float rim = Mathf.Clamp01(1 - Mathf.Abs(r - .78f) / .14f);
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(body + rim * .8f)));
            }
        texture.Apply();
        glowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
        return glowSprite;
    }

    void LateUpdate()
    {
        float blend = 1 - Mathf.Exp(-10 * Time.unscaledDeltaTime);
        float target = BossShift();
        shift = Mathf.Lerp(shift, target, blend);
        panel.anchoredPosition = new Vector2(Left + shift, Top);
        // Size the grid for where the panel is heading so icons settle once.
        float width = AvailableWidth(target), cell = CellSize(items.Count, width);
        int columns = Columns(width, cell);
        // Stepped spin at 15 Hz, each item a little out of phase.
        float t = Mathf.Floor(Time.unscaledTime * 15) / 15;
        for (int i = 0; i < items.Count; i++)
        {
            Arrange(i, cell, columns, blend);
            Glow(i, cell, columns);
            items[i].localRotation = Quaternion.Euler(-18, t * 60 + i * 40, 8);
        }
    }

    void OnDestroy()
    {
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
    }
}
