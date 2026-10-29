using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>One persistent, opaque cloud curtain. All animation uses real time, including paused menus.</summary>
public sealed class CloudTransition : MonoBehaviour
{
    static CloudTransition instance;
    public static bool Busy => instance != null && instance.busy;
    public const float RevealSeconds = 2.1f;
    const float OffscreenY = 960;
    const float LayerSpacing = 36;
    const float TravelSpeed = (OffscreenY + 3 * LayerSpacing) / RevealSeconds;
    const float BankHalfHeight = CloudCurtainGraphic.EdgeOuterY + 5;
    bool busy;
    RectTransform[] banks;
    CloudCurtainGraphic[] billows;
    // Cloud travel advances by capped frame steps, so a loading hitch pauses the flow
    // for one frame instead of making every bank jump ahead.
    float flowElapsed;
    const float MaxFlowStep = 1 / 30f;
    // Keep the curtain closed until frames have settled after the new scene starts.
    const int SettledFrames = 12;
    const float SettledFrameSeconds = 1 / 40f, MaxSettleSeconds = 4;
    bool revealing;
    readonly float[] trailingExtension = new float[4];
    Canvas canvas;
    Coroutine scenePreparation, routePreparation;
    string preparedScene;
    AsyncOperation preparedLoad;
    readonly AudioClip[] routeMusic = new AudioClip[3];
    bool scenePreparing, routesPreparing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        instance = new GameObject("Cloud scene transitions").AddComponent<CloudTransition>();
        DontDestroyOnLoad(instance.gameObject);
        SceneManager.sceneLoaded += instance.SceneLoaded;
    }

    void Awake()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        gameObject.AddComponent<GraphicRaycaster>();
        // Match the HUD's 405-wide viewport, leaving the side instruments visible.
        var viewport = new GameObject("Cloud playfield mask", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(transform, false);
        var clip = viewport.GetComponent<RectTransform>();
        clip.anchorMin = new Vector2(.5f, 0);
        clip.anchorMax = new Vector2(.5f, 1);
        clip.sizeDelta = new Vector2(405, 0);
        banks = new RectTransform[4];
        billows = new CloudCurtainGraphic[4];
        var artwork = Resources.Load<Texture2D>("CloudBillow");
        for (int i = 0; i < banks.Length; i++)
        {
            var go = new GameObject("Cloud bank " + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(CloudCurtainGraphic));
            go.transform.SetParent(clip, false);
            banks[i] = go.GetComponent<RectTransform>();
            banks[i].sizeDelta = new Vector2(650, 2 * BankHalfHeight);
            var graphic = go.GetComponent<CloudCurtainGraphic>();
            billows[i] = graphic;
            graphic.color = Color.Lerp(new Color(.88f, .92f, .95f), Color.white, i / 3f);
            graphic.Configure(artwork, i);
        }
        cloudLayer = viewport;
        // Some trips (the title into the hangar) dip briefly through black instead of the clouds.
        var dip = new GameObject("Black dip", typeof(RectTransform), typeof(Image));
        dip.transform.SetParent(transform, false);
        var dipRect = dip.GetComponent<RectTransform>();
        dipRect.anchorMin = Vector2.zero;
        dipRect.anchorMax = Vector2.one;
        dipRect.offsetMin = dipRect.offsetMax = Vector2.zero;
        blackDip = dip.GetComponent<Image>();
        blackDip.color = new Color(0, 0, 0, 0);
        blackDip.raycastTarget = false;
        canvas.enabled = false;
    }

    GameObject cloudLayer;
    Image blackDip;
    bool clouds = true;
    const float DipSeconds = .3f;

    IEnumerator Dip(bool cover)
    {
        for (float t = 0; t < DipSeconds; t += Mathf.Min(Time.unscaledDeltaTime, MaxFlowStep))
        {
            float a = Mathf.SmoothStep(0, 1, t / DipSeconds);
            blackDip.color = new Color(0, 0, 0, cover ? a : 1 - a);
            yield return null;
        }
        blackDip.color = new Color(0, 0, 0, cover ? 1 : 0);
    }

    void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "L1" && HangarHub.Active)
            HangarHub.Build();
        else if (scene.name == "L1")
            LevelEnvironment.Apply();
        // Supersede the old world-space clouds, which were behind the PSX output canvas.
        foreach (var spawner in FindObjectsByType<CloudSpawner>())
            spawner.enabled = false;
        // The menu is already usable: spend idle time preparing gameplay, even while paused.
        if (scene.name == "Title" && !busy)
            PrepareGameplay();
        else if (scene.name == "L1" && !busy)
            StartCoroutine(LevelEnvironment.Prepare());
    }

    void PrepareGameplay()
    {
        if (scenePreparation == null)
            scenePreparation = StartCoroutine(PreloadScene("L1"));
    }

    public static void PrepareRoutes(params LevelId[] options)
    {
        if (instance.routePreparation != null)
            instance.StopCoroutine(instance.routePreparation);
        instance.routePreparation = instance.StartCoroutine(instance.WarmRoutes(options));
    }

    IEnumerator WarmRoutes(LevelId[] options)
    {
        routesPreparing = true;
        try
        {
            yield return LevelEnvironment.Prepare();
            // Scene objects stay on the main thread, but each build yields in small batches.
            for (int i = 0; i < options.Length; i++)
            {
                yield return LevelEnvironment.PrepareRoute(options[i]);
                yield return WarmRouteMusic(options[i], i);
                yield return null;
            }
        }
        finally { routesPreparing = false; }
    }

    IEnumerator WarmRouteMusic(LevelId level, int index)
    {
        var request = Resources.LoadAsync<AudioClip>("Music/" + BGMManager.LevelTrack(level));
        yield return request;
        var clip = request.asset as AudioClip;
        routeMusic[index] = clip; // Keep the loaded clip alive until the route's music source owns it.
        if (clip == null)
            yield break;
        if (clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();
        while (clip.loadState == AudioDataLoadState.Loading)
            yield return null;
    }

    public static void Load(string scene, Action covered = null, bool clouds = true)
    {
        if (Busy)
            return;
        instance.clouds = clouds;
        LevelEnvironment.FadeOutStorm();
        // Leaving gameplay supersedes route preparation; do not leave resource requests behind
        // a newly parked scene load, or build scenery into the menu after its level unloads.
        if (instance.routePreparation != null)
            instance.StopCoroutine(instance.routePreparation);
        instance.routePreparation = null;
        instance.routesPreparing = false;
        instance.StartCoroutine(instance.Travel(scene, covered));
    }

    public static void Change(Action covered)
    {
        if (Busy)
            return;
        LevelEnvironment.FadeOutStorm();
        instance.StartCoroutine(instance.Travel(null, covered));
    }

    public static void FadeOutAudio(GameObject root, AudioClip[] clips, float seconds)
    {
        instance.StartCoroutine(instance.FadeAudio(root, clips, seconds));
    }

    IEnumerator FadeAudio(GameObject root, AudioClip[] clips, float seconds)
    {
        var sources = root.GetComponentsInChildren<AudioSource>();
        var volumes = new float[sources.Length];
        for (int i = 0; i < sources.Length; i++)
            volumes[i] = sources[i].volume;
        try
        {
            for (float elapsed = 0; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                float gain = 1 - Mathf.SmoothStep(0, 1, elapsed / seconds);
                for (int i = 0; i < sources.Length; i++)
                    if (sources[i] != null)
                        sources[i].volume = volumes[i] * gain;
                yield return null;
            }
        }
        finally
        {
            if (root != null)
                Destroy(root);
            foreach (var clip in clips)
                if (clip != null)
                    Destroy(clip);
        }
    }

    IEnumerator Travel(string scene, Action covered)
    {
        busy = true;
        revealing = false;
        flowElapsed = 0;
        // Clouds are an overlay: simulation and controls keep running throughout both legs.
        Time.timeScale = 1;
        PositionClouds();
        cloudLayer.SetActive(clouds);
        canvas.enabled = true;
        var previousPriority = Application.backgroundLoadingPriority;
        Application.backgroundLoadingPriority = ThreadPriority.Low;
        try
        {
            var preparing = StartCoroutine(LevelEnvironment.Prepare());
            if (scene != null && scenePreparation == null)
                scenePreparation = StartCoroutine(PreloadScene(scene));
            yield return clouds ? Move(true) : Dip(true);
            yield return preparing;
            if (scene != null)
            {
                while (scenePreparing)
                    yield return null;
                // A speculative load cannot be cancelled. Drain it under cover if the target changed.
                if (preparedScene != scene)
                {
                    preparedLoad.allowSceneActivation = true;
                    while (!preparedLoad.isDone)
                        yield return null;
                    yield return PreloadScene(scene);
                }
            }
            else
                while (routesPreparing)
                    yield return null;
            covered?.Invoke();
            if (scene != null && preparedLoad != null)
            {
                var load = preparedLoad;
                preparedLoad = null;
                preparedScene = null;
                scenePreparation = null;
                load.allowSceneActivation = true;
                while (!load.isDone)
                    yield return null;
            }
            // Start new scene components behind the moving cloud bank, then let wave geometry,
            // pools and shaders finish warming while the clouds keep flowing.
            yield return null;
            if (SceneManager.GetActiveScene().name == "L1")
            {
                yield return Settle();
                Time.timeScale = 1;
                if (!HangarHub.Active)
                    LevelReveal.Show();
            }
            yield return clouds ? Move(false) : Dip(false);
        }
        finally
        {
            blackDip.color = new Color(0, 0, 0, 0);
            cloudLayer.SetActive(true);
            clouds = true;
            Application.backgroundLoadingPriority = previousPriority;
            canvas.enabled = false;
            busy = false;
            Time.timeScale = SceneManager.GetActiveScene().name == "Title" ? 0 : 1;
            // The title and the hangar are idle screens: park the next gameplay scene behind them.
            if (SceneManager.GetActiveScene().name == "Title" || HangarHub.Active)
                PrepareGameplay();
        }
    }

    IEnumerator Settle()
    {
        float until = Time.unscaledTime + MaxSettleSeconds;
        int smooth = 0;
        while (Time.unscaledTime < until && ((!WavePreparation.Ready && !HangarHub.Active) || smooth < SettledFrames))
        {
            yield return null;
            smooth = Time.unscaledDeltaTime < SettledFrameSeconds ? smooth + 1 : 0;
        }
    }

    IEnumerator PreloadScene(string scene)
    {
        scenePreparing = true;
        try
        {
            // A parked scene blocks Unity's async queue. Finish shared asset preparation first.
            yield return LevelEnvironment.Prepare();
            var load = SceneManager.LoadSceneAsync(scene);
            load.allowSceneActivation = false;
            load.priority = -1;
            preparedScene = scene;
            preparedLoad = load;
            while (load.progress < .9f)
                yield return null;
        }
        finally { scenePreparing = false; }
    }

    void Update()
    {
        if (!busy)
            return;
        flowElapsed += Mathf.Min(Time.unscaledDeltaTime, MaxFlowStep);
        PositionClouds();
    }

    void PositionClouds()
    {
        float elapsed = flowElapsed;
        for (int i = 0; i < banks.Length; i++)
        {
            // One uninterrupted downward pass, including the loading handoff. Feed more cloud
            // in from above while loading; release that trailing edge once the scene is ready.
            float y = OffscreenY + i * LayerSpacing - elapsed * TravelSpeed;
            if (!revealing)
                trailingExtension[i] = Mathf.Max(0, -y + (banks.Length - 1 - i) * LayerSpacing);
            float extension = trailingExtension[i];
            float height = 2 * BankHalfHeight + extension;
            banks[i].sizeDelta = new Vector2(650, height);
            banks[i].pivot = new Vector2(.5f, BankHalfHeight / height);
            banks[i].anchoredPosition = new Vector2(0, y);
            billows[i].SetTravel(y, extension, elapsed * (TravelSpeed + 52));
        }
    }

    IEnumerator Move(bool cover)
    {
        if (cover)
        {
            while (banks[banks.Length - 1].anchoredPosition.y > 0)
                yield return null;
            yield break;
        }
        PositionClouds();
        revealing = true;
        // Every trailing sprite is below the viewport before the canvas switches off.
        float revealEnd = flowElapsed + RevealSeconds;
        while (flowElapsed < revealEnd)
            yield return null;
        PositionClouds();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        if (instance == this)
            instance = null;
    }
}

/// <summary>Overlapping PSX cloud sprites over an opaque backing; textured edges hide the handoff.</summary>
public sealed class CloudCurtainGraphic : MaskableGraphic
{
    Texture2D texture;
    Vector2 white;
    int variant;
    float carrierY, extension;
    const int BackingColumns = 325;
    const int EdgeColumns = 5, FlowRows = 16;
    const float EdgeHalfHeight = 125;
    public const float EdgeOuterY = 450 + EdgeHalfHeight;
    readonly float[] backingTop = new float[BackingColumns];
    readonly float[] backingBottom = new float[BackingColumns];
    struct EdgeSprite
    {
        public Vector2 lo, hi, uv0, uv1;
    }

    readonly EdgeSprite[] edges = new EdgeSprite[2 * EdgeColumns];
    public float FlowDistance
    {
        get; private set;
    }

    public override Texture mainTexture => texture != null ? texture : base.mainTexture;

    public void Configure(Texture2D artwork, int index)
    {
        texture = artwork;
        variant = index;
        var pixels = texture.GetPixels32();
        int best = -1;
        for (int i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            int brightness = p.r + p.g + p.b;
            if (p.a == 255 && brightness > best)
            {
                best = brightness;
                white = new Vector2((i % texture.width + .5f) / texture.width, (i / texture.width + .5f) / texture.height);
            }
        }
        for (int edge = 0; edge < 2; edge++)
            for (int col = 0; col < EdgeColumns; col++)
            {
                int row = edge * 8;
                float seed = row * 17.13f + col * 7.71f + variant * 11.37f;
                float x = -360 + col * 180 + Mathf.Sin(seed) * 60;
                float width = 390 + Mathf.Sin(seed * 1.9f) * 65;
                float y = edge == 0 ? -450 : 450;
                bool mirror = (row + col + variant) % 2 == 0;
                edges[edge * EdgeColumns + col] = new EdgeSprite
                {
                    lo = new Vector2(x - width / 2, y - EdgeHalfHeight),
                    hi = new Vector2(x + width / 2, y + EdgeHalfHeight),
                    uv0 = new Vector2(mirror ? 1 : 0, edge == 0 ? 1 : 0),
                    uv1 = new Vector2(mirror ? 0 : 1, edge == 0 ? 0 : 1)
                };
            }
        // Join the backing INSIDE opaque sprite pixels. A rectangular backing exposed its
        // straight edge through the transparent valleys between the rounded cloud tops.
        for (int column = 0; column < BackingColumns; column++)
        {
            float x = -325 + column * 2;
            backingTop[column] = FindBackingSeam(pixels, x, 1);
            backingBottom[column] = FindBackingSeam(pixels, x, -1);
        }
        SetMaterialDirty();
        SetVerticesDirty();
    }

    float FindBackingSeam(Color32[] pixels, float x, int side)
    {
        int solidRun = 0;
        for (float distance = EdgeOuterY - 1; distance >= 450 - EdgeHalfHeight + 1; distance -= 2)
        {
            bool solid = true;
            // Include the full strip width, so its corners cannot protrude into a valley.
            for (int sample = 0; sample <= 4 && solid; sample++)
            {
                bool covered = false;
                float sx = x + sample * .5f, y = distance * side;
                for (int i = side > 0 ? EdgeColumns : 0; i < (side > 0 ? edges.Length : EdgeColumns); i++)
                {
                    var sprite = edges[i];
                    if (sx < sprite.lo.x || sx > sprite.hi.x || y < sprite.lo.y || y > sprite.hi.y)
                        continue;
                    float u = Mathf.Lerp(sprite.uv0.x, sprite.uv1.x, Mathf.InverseLerp(sprite.lo.x, sprite.hi.x, sx));
                    float v = Mathf.Lerp(sprite.uv0.y, sprite.uv1.y, Mathf.InverseLerp(sprite.lo.y, sprite.hi.y, y));
                    int px = Mathf.Clamp((int)(u * texture.width), 0, texture.width - 1);
                    int py = Mathf.Clamp((int)(v * texture.height), 0, texture.height - 1);
                    // The downsampled sprite's solid interior is mostly alpha 253.
                    if (pixels[py * texture.width + px].a >= 250)
                    {
                        covered = true;
                        break;
                    }
                }
                solid = covered;
            }
            solidRun = solid ? solidRun + 1 : 0;
            if (solidRun == 6)
                return (distance + 4) * side;
        }
        // Outside the playfield, a strip may miss the sprites entirely; omit its backing.
        return 0;
    }

    public void SetTravel(float y, float trailingExtension, float distance)
    {
        float snapped = Mathf.Floor(distance / 2) * 2;
        carrierY = y;
        extension = trailingExtension;
        FlowDistance = snapped;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (texture == null)
            return;
        // One connected surface: independent narrow rectangles expose vertical edges
        // through the cloud texture's near-opaque pixels, especially at fractional scales.
        for (int column = 0; column <= BackingColumns; column++)
        {
            float x = -325 + column * 2;
            int left = Mathf.Max(0, column - 1), right = Mathf.Min(BackingColumns - 1, column);
            float bottom = Mathf.Max(backingBottom[left], backingBottom[right]);
            float top = Mathf.Min(backingTop[left], backingTop[right]) + extension;
            int k = vh.currentVertCount;
            vh.AddVert(new Vector3(x, bottom), color, white);
            vh.AddVert(new Vector3(x, top), color, white);
            if (column == 0)
                continue;
            vh.AddTriangle(k - 2, k - 1, k + 1);
            vh.AddTriangle(k - 2, k + 1, k);
        }
        // Fixed edge sprites preserve the scalloped silhouette of the incoming/outgoing carrier.
        for (int i = 0; i < edges.Length; i++)
        {
            var sprite = edges[i];
            var offset = i < EdgeColumns ? Vector2.zero : new Vector2(0, extension);
            Quad(vh, sprite.lo + offset, sprite.hi + offset, sprite.uv0, sprite.uv1);
        }
        // Recycle well outside the visible area, so the interior reads as a continuous fly-through.
        for (int row = 0; row < FlowRows; row++)
        {
            float y = Mathf.Repeat(row * (1240f / FlowRows) - FlowDistance, 1240) - 620 - carrierY;
            for (int col = 0; col < 5; col++)
            {
                float seed = row * 17.13f + col * 7.71f + variant * 11.37f;
                float x = -360 + col * 180 + Mathf.Sin(seed) * 60;
                float width = 380 + Mathf.Sin(seed * 1.9f) * 65;
                float height = 245 + Mathf.Cos(seed * .7f) * 35;
                bool mirror = (row + col + variant) % 2 == 0;
                Quad(vh, new Vector2(x - width / 2, y - height / 2), new Vector2(x + width / 2, y + height / 2),
                    new Vector2(mirror ? 1 : 0, 0), new Vector2(mirror ? 0 : 1, 1), true);
            }
        }
    }

    void Quad(VertexHelper vh, Vector2 lo, Vector2 hi, Vector2 uv0, Vector2 uv1, bool stream = false)
    {
        Color bottom = color, top = color;
        if (stream)
        {
            if (hi.y <= -450 || lo.y >= 450 + extension)
                return;
            float from = lo.y, to = hi.y, v0 = uv0.y, v1 = uv1.y;
            lo.y = Mathf.Max(-450, lo.y);
            hi.y = Mathf.Min(450 + extension, hi.y);
            uv0.y = Mathf.Lerp(v0, v1, Mathf.InverseLerp(from, to, lo.y));
            uv1.y = Mathf.Lerp(v0, v1, Mathf.InverseLerp(from, to, hi.y));
            bottom.a *= Mathf.InverseLerp(-450, -290, lo.y) * Mathf.InverseLerp(450 + extension, 290 + extension, lo.y);
            top.a *= Mathf.InverseLerp(-450, -290, hi.y) * Mathf.InverseLerp(450 + extension, 290 + extension, hi.y);
        }
        int k = vh.currentVertCount;
        vh.AddVert(new Vector3(lo.x, lo.y), bottom, uv0);
        vh.AddVert(new Vector3(lo.x, hi.y), top, new Vector2(uv0.x, uv1.y));
        vh.AddVert(new Vector3(hi.x, hi.y), top, uv1);
        vh.AddVert(new Vector3(hi.x, lo.y), bottom, new Vector2(uv1.x, uv0.y));
        vh.AddTriangle(k, k + 1, k + 2);
        vh.AddTriangle(k, k + 2, k + 3);
    }
}
