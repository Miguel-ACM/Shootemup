using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Collected items shown as spinning 3D models in a panel to the right of the portrait playfield.
/// A small orthographic camera far from the playfield renders a single column of slots into a
/// point-filtered texture with a transparent background, shown next to the playfield; created on
/// the first pickup.
/// </summary>
public sealed class PSXInventoryDisplay : MonoBehaviour
{
    const int Columns = 1, Rows = 12;
    const int SlotPixels = 30;
    const float Slot = 1;   // world units per slot
    static readonly Vector3 Origin = new Vector3(500, 0, 0);
    static PSXInventoryDisplay instance;
    readonly List<Transform> items = new List<Transform>();
    RawImage panel;
    RenderTexture texture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        instance = null;
    }

    public static void Add(Mesh mesh, Material material)
    {
        if (mesh == null || material == null)
            return;
        if (instance == null)
            instance = new GameObject("PSX inventory display").AddComponent<PSXInventoryDisplay>();
        instance.AddItem(mesh, material);
    }

    void Awake()
    {
        transform.position = Origin;
        texture = new RenderTexture(Columns * SlotPixels, Rows * SlotPixels, 16, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point, name = "PSX inventory" };
        var cameraObject = new GameObject("Inventory camera");
        cameraObject.transform.SetParent(transform, false);
        cameraObject.transform.localPosition = new Vector3(0, 0, -10);
        var camera = cameraObject.AddComponent<UnityEngine.Camera>();
        camera.orthographic = true;
        camera.orthographicSize = Rows * Slot * .5f;
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 20;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.clear;   // no panel background
        camera.targetTexture = texture;
        camera.depth = -50;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;

        var ui = new GameObject("Inventory panel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        ui.transform.SetParent(transform, false);
        var canvas = ui.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6;
        var scaler = ui.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        FluorescentStartup.Attach(ui, .28f);
        var image = new GameObject("Collected items", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        image.transform.SetParent(ui.transform, false);
        panel = image.GetComponent<RawImage>();
        panel.texture = texture;
        panel.raycastTarget = false;
        var rect = image.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        // Just right of the 405-wide playfield, top-aligned with it.
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(202.5f + 10, 360 - 10);
        rect.sizeDelta = new Vector2(Columns * SlotPixels, Rows * SlotPixels);
    }

    void AddItem(Mesh mesh, Material material)
    {
        int index = items.Count;
        if (index >= Columns * Rows)
            return;
        var go = new GameObject("Collected " + mesh.name);
        go.transform.SetParent(transform, false);
        float x = (index % Columns - (Columns - 1) * .5f) * Slot, y = (Rows - 1) * Slot * .5f - index / Columns * Slot;
        go.transform.localPosition = new Vector3(x, y, 0);
        float size = Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z);
        go.transform.localScale = Vector3.one * (.78f * Slot / Mathf.Max(size, .01f));
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        items.Add(go.transform);
    }

    void LateUpdate()
    {
        // Stepped spin at 15 Hz, each item a little out of phase.
        float t = Mathf.Floor(Time.unscaledTime * 15) / 15;
        for (int i = 0; i < items.Count; i++)
            items[i].localRotation = Quaternion.Euler(-18, t * 60 + i * 40, 8);
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
