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
    // Item names, prices and the hangar wallet stay crisp above the filter; while any are up, the player's ship
    // is rendered once more on its own and drawn, filtered the same way, over them.
    public static int LabelsShown;
    UnityEngine.Camera shipCamera;
    RenderTexture shipPixels;
    Canvas shipCanvas;
    RawImage shipImage;
    Material shipComposite;
    // The player's cockpit pane skips the filter: this camera draws only the pane (player hulls clip
    // everything else while _CockpitPass is set) at full screen resolution, and the composite swaps
    // it in wherever the low-resolution scene still shows the pane.
    UnityEngine.Camera cockpitCamera;
    RenderTexture cockpitPixels;
    static readonly int CockpitPass = Shader.PropertyToID("_CockpitPass");
    public RenderTexture SceneTexture => pixels;
    public Material CompositeMaterial => composite;

    public void Configure(Material material)
    {
        if (initialized || material == null)
            return;
        source = GetComponent<UnityEngine.Camera>();
        if (source == null)
            return;
        filterEnabled = GameOptions.PsxFilter;
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
        var shipCameraObject = new GameObject("PSX ships over labels");
        shipCameraObject.transform.SetParent(transform, false);
        shipCamera = shipCameraObject.AddComponent<UnityEngine.Camera>();
        shipCamera.CopyFrom(source);
        shipCamera.cullingMask = LayerMask.GetMask("Player");
        shipCamera.clearFlags = CameraClearFlags.SolidColor;
        shipCamera.backgroundColor = Color.clear;
        shipCamera.depth = source.depth + 1;
        shipCamera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        shipCamera.enabled = false;
        shipComposite = new Material(material) { name = "PSX ships over labels" };
        shipComposite.SetFloat("_Cutout", 1);
        shipComposite.SetFloat("_GlowStrength", 0);
        var shipCanvasObject = new GameObject("PSX ships over labels", typeof(RectTransform), typeof(Canvas));
        shipCanvasObject.transform.SetParent(transform, false);
        shipCanvas = shipCanvasObject.GetComponent<Canvas>();
        shipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Just above the labels (16-17), under the side covers, pause menu and fades (UI canvas, 20) and every curtain.
        shipCanvas.sortingOrder = 18;
        var scaler = shipCanvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        var aperture = new GameObject("Ship portrait aperture", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
        aperture.SetParent(shipCanvasObject.transform, false);
        aperture.anchorMin = new Vector2(.5f, 0);
        aperture.anchorMax = new Vector2(.5f, 1);
        aperture.sizeDelta = new Vector2(405, 0);
        var shipDisplay = new GameObject("Ships", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        shipDisplay.transform.SetParent(aperture, false);
        shipImage = shipDisplay.GetComponent<RawImage>();
        shipImage.raycastTarget = false;
        shipImage.material = shipComposite;
        shipImage.enabled = false;
        shipImage.rectTransform.anchorMin = shipImage.rectTransform.anchorMax = new Vector2(.5f, .5f);
        var cockpitCameraObject = new GameObject("PSX sharp player cockpit");
        cockpitCameraObject.transform.SetParent(transform, false);
        cockpitCamera = cockpitCameraObject.AddComponent<UnityEngine.Camera>();
        cockpitCamera.CopyFrom(source);
        cockpitCamera.cullingMask = LayerMask.GetMask("Player");
        cockpitCamera.clearFlags = CameraClearFlags.SolidColor;
        cockpitCamera.backgroundColor = Color.clear;
        cockpitCamera.depth = source.depth + .5f;
        cockpitCamera.allowHDR = false;
        cockpitCamera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        initialized = true;
        Resize();
        RenderPipelineManager.beginCameraRendering += BeforeCamera;
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
        if (shipPixels != null)
        {
            shipPixels.Release();
            Destroy(shipPixels);
        }
        shipPixels = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { filterMode = FilterMode.Point, name = "PSX ships over labels", antiAliasing = 1 };
        shipPixels.Create();
        shipCamera.targetTexture = shipPixels;
        shipImage.texture = shipPixels;
        shipImage.rectTransform.sizeDelta = new Vector2(width * 720f / height, 720);
        shipComposite.SetFloat("_FilterEnabled", filterEnabled ? 1 : 0);
        ResizeCockpit();
        glow = new ProjectileGlow(w, h);
        composite.SetTexture("_GlowTex", glow.Texture);
        composite.SetFloat("_FilterEnabled", filterEnabled ? 1 : 0);
        image.material = composite;
        // PSXv2Lit snaps vertices to this pixel grid (PS1-style jitter at the internal resolution).
        Shader.SetGlobalVector("_PSXSnapResolution", new Vector4(w, h, 0, 0));
    }

    void ResizeCockpit()
    {
        cockpitCamera.targetTexture = null;
        if (cockpitPixels != null)
        {
            cockpitPixels.Release();
            Destroy(cockpitPixels);
            cockpitPixels = null;
        }
        // Without the filter the whole scene is already sharp.
        cockpitCamera.enabled = filterEnabled;
        if (filterEnabled)
        {
            cockpitPixels = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point, name = "PSX sharp player cockpit", antiAliasing = 1 };
            cockpitPixels.Create();
            cockpitCamera.targetTexture = cockpitPixels;
        }
        foreach (var material in new[] { composite, shipComposite })
        {
            material.SetTexture("_CockpitTex", cockpitPixels);
            material.SetFloat("_CockpitOn", filterEnabled ? 1 : 0);
        }
    }

    void BeforeCamera(ScriptableRenderContext context, UnityEngine.Camera camera)
    {
        Shader.SetGlobalFloat(CockpitPass, camera == cockpitCamera ? 1 : 0);
    }

    void AfterCamera(ScriptableRenderContext context, UnityEngine.Camera camera)
    {
        if (camera == cockpitCamera)
            Shader.SetGlobalFloat(CockpitPass, 0);
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
            GameOptions.SetPsxFilter(!filterEnabled);
        if (width != Screen.width || height != Screen.height)
            Resize();
        if (cockpitCamera.enabled)
        {
            cockpitCamera.orthographic = source.orthographic;
            cockpitCamera.orthographicSize = source.orthographicSize;
            cockpitCamera.fieldOfView = source.fieldOfView;
            cockpitCamera.nearClipPlane = source.nearClipPlane;
            cockpitCamera.farClipPlane = source.farClipPlane;
        }
        bool ships = LabelsShown > 0 || HangarHub.Active;
        shipCanvas.sortingOrder = HangarHub.Active ? 4 : 18;
        shipCamera.enabled = shipImage.enabled = ships;
        if (ships)
        {
            shipCamera.orthographic = source.orthographic;
            shipCamera.orthographicSize = source.orthographicSize;
            shipCamera.fieldOfView = source.fieldOfView;
            shipCamera.nearClipPlane = source.nearClipPlane;
            shipCamera.farClipPlane = source.farClipPlane;
            shipComposite.SetFloat("_Underwater", composite.GetFloat("_Underwater"));
        }
    }

    public void SetFilter(bool enabled)
    {
        if (filterEnabled == enabled)
            return;
        filterEnabled = enabled;
        Resize();
    }

    void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering -= BeforeCamera;
        RenderPipelineManager.endCameraRendering -= AfterCamera;
        Shader.SetGlobalFloat(CockpitPass, 0);
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
        if (shipPixels != null)
        {
            shipPixels.Release();
            Destroy(shipPixels);
        }
        if (cockpitPixels != null)
        {
            cockpitPixels.Release();
            Destroy(cockpitPixels);
        }
        if (composite != null)
            Destroy(composite);
        if (shipComposite != null)
            Destroy(shipComposite);
    }
}
