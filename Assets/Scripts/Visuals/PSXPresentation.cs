using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

/// <summary>Integer-scaled low-resolution scene with PS1-style dithering and 15-bit colour.</summary>
public sealed class PSXPresentation : MonoBehaviour
{
    /// <summary>Minimum internal lines. The real height is the screen divided by the largest whole
    /// factor that keeps at least this many lines, so the upscale uses even, whole pixels.</summary>
    [Range(180, 720)] public int renderHeight = 480;
    public bool filterEnabled = true;
    UnityEngine.Camera source, output;
    RenderTexture pixels, previousTarget;
    Canvas canvas;
    RawImage image;
    Material composite;
    ProjectileGlow glow;
    bool previousHDR;
    int width, height;
    bool initialized;
    float timeStopTint;
    public RenderTexture SceneTexture => pixels;
    public Material CompositeMaterial => composite;

    public void Configure(Material material)
    {
        if (initialized || material == null)
            return;
        source = GetComponent<UnityEngine.Camera>();
        if (source == null)
            return;
        // Gameplay roots occupy Z=0..20; raised mesh surfaces extend in front of them.
        // An orthographic camera can move back without changing the playfield framing.
        var cameraPosition = source.transform.localPosition;
        cameraPosition.z = -40;
        source.transform.localPosition = cameraPosition;
        source.nearClipPlane = .1f;
        source.farClipPlane = 1200;
        var shaker = source.GetComponent<EZCameraShake.CameraShaker>();
        if (shaker != null)
        {
            var rest = shaker.RestPositionOffset;
            rest.z = -40;
            shaker.RestPositionOffset = rest;
        }
        source.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        previousTarget = source.targetTexture;
        previousHDR = source.allowHDR;
        source.allowHDR = true;
        composite = new Material(material) { name = "PSX runtime composite" };
        var cameraObject = new GameObject("PSX Screen Output");
        cameraObject.transform.SetParent(transform, false);
        output = cameraObject.AddComponent<UnityEngine.Camera>();
        output.cullingMask = 0;
        output.clearFlags = CameraClearFlags.SolidColor;
        output.backgroundColor = Color.black;
        output.depth = 100;
        output.allowHDR = false;
        output.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        var ui = new GameObject("PSX pixel display", typeof(RectTransform), typeof(Canvas));
        ui.transform.SetParent(transform, false);
        canvas = ui.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -100;
        var display = new GameObject("320-line dithered scene", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        display.transform.SetParent(ui.transform, false);
        image = display.GetComponent<RawImage>();
        image.raycastTarget = false;
        var rect = display.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        initialized = true;
        Resize();
        RenderPipelineManager.endCameraRendering += AfterCamera;
    }

    void Resize()
    {
        if (!initialized || Screen.height < 1 || Screen.width < 1)
            return;
        width = Screen.width;
        height = Screen.height;
        source.targetTexture = null;
        if (pixels != null)
        {
            pixels.Release();
            Destroy(pixels);
        }
        glow?.Dispose();
        int factor = filterEnabled ? Mathf.Max(1, height / renderHeight) : 1;
        int h = Mathf.Max(1, height / factor), w = Mathf.Max(1, width / factor);
        pixels = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { filterMode = FilterMode.Point, name = "PSX world pixels", antiAliasing = 1 };
        pixels.Create();
        source.targetTexture = pixels;
        image.texture = pixels;
        glow = new ProjectileGlow(w, h);
        composite.SetTexture("_GlowTex", glow.Texture);
        composite.SetFloat("_FilterEnabled", filterEnabled ? 1 : 0);
        image.material = composite;
        // PSXv2Lit snaps vertices to this pixel grid (PS1-style jitter at the internal resolution).
        Shader.SetGlobalVector("_PSXSnapResolution", new Vector4(w, h, 0, 0));
    }

    void AfterCamera(ScriptableRenderContext context, UnityEngine.Camera camera)
    {
        if (camera == source && pixels != null)
            glow.Render(pixels);
    }

    void Update()
    {
        if (!initialized)
            return;
        composite.SetFloat("_Underwater", LevelRun.Current == LevelId.Underwater ? 1 : 0);
        // Real time, since the world itself is frozen while the filter is up.
        timeStopTint = Mathf.MoveTowards(timeStopTint, WorldTimeStop.Frozen ? 1 : 0, Time.unscaledDeltaTime * 3.5f);
        composite.SetFloat("_TimeStop", Mathf.SmoothStep(0, 1, timeStopTint));
        composite.SetFloat("_ClearFlash", ClearEffects.Flash);
        composite.SetFloat("_ClearSweep", ClearEffects.Sweep);
        composite.SetColor("_ClearTint", ClearEffects.Tint);
        if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
        {
            filterEnabled = !filterEnabled;
            Resize();
        }
        if (width != Screen.width || height != Screen.height)
            Resize();
    }

    void OnDestroy()
    {
        RenderPipelineManager.endCameraRendering -= AfterCamera;
        if (source != null)
        {
            source.targetTexture = previousTarget;
            source.allowHDR = previousHDR;
        }
        glow?.Dispose();
        if (pixels != null)
        {
            pixels.Release();
            Destroy(pixels);
        }
        if (composite != null)
            Destroy(composite);
    }
}
