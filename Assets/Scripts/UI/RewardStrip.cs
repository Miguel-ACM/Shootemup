using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>A mid-wave encounter offer along the bottom edge. Combat keeps running while it is open.</summary>
public sealed class RewardStrip : MonoBehaviour
{
    public static bool Active => current != null;
    static RewardStrip current;
    // Hold off on input for a moment so a key already held for something else cannot pick.
    const float Arming = .25f, EnterSeconds = .3f, ExitSeconds = .35f;
    const float OptionsWidth = 335, Height = 86, SkipWidth = 62, SkipHeight = 50, Gap = 8;
    static readonly string[] KeyLabels2 = { "Q", "E" }, KeyLabels3 = { "Q", "W", "E" };
    static readonly string[] PadLabels2 = { "←", "→" }, PadLabels3 = { "←", "↑", "→" };
    static Sprite keycap;
    EncounterKind[] options;
    Action<int> picked;
    CanvasGroup group;
    RectTransform body;
    CurvedStripGraphic[] fills, rims;
    CurvedStripGraphic skipRim;
    RectTransform[] icons;
    CanvasGroup[] pieces;
    Text[] keys;
    Text skipKey;
    Font font;
    float age, exitAge;
    int chosen = -2;
    bool leaving, stickHeld;

    public static RewardStrip Open(EncounterKind[] options, Action<int> picked)
    {
        if (current != null)
            current.Dismiss();
        var strip = new GameObject("Mid-wave reward strip", typeof(RectTransform)).AddComponent<RewardStrip>();
        strip.options = options;
        strip.picked = picked;
        strip.Build();
        current = strip;
        return strip;
    }

    /// <summary>Withdraws the offer without a choice, as when a boss or event begins.</summary>
    public void Dismiss()
    {
        if (leaving)
            return;
        leaving = true;
        chosen = -2;
        picked = null;
        if (current == this)
            current = null;
    }

    void Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0;
        body = Rect(transform, "Strip body", new Vector2(405, Height), Vector2.zero);
        body.anchorMin = body.anchorMax = new Vector2(.5f, 0);
        body.pivot = new Vector2(.5f, 0);
        int n = options.Length;
        var optionsRect = Rect(body, "Options", new Vector2(OptionsWidth, Height), new Vector2(-202.5f + OptionsWidth * .5f, 0));
        optionsRect.anchorMin = optionsRect.anchorMax = new Vector2(.5f, 0);
        optionsRect.pivot = new Vector2(.5f, 0);
        fills = new CurvedStripGraphic[n];
        rims = new CurvedStripGraphic[n];
        icons = new RectTransform[n];
        pieces = new CanvasGroup[n];
        keys = new Text[n];
        for (int i = 0; i < n; i++)
        {
            var accent = EncounterChoice.Accent(options[i]);
            bool painted = options[i] == EncounterKind.Challenge;
            var piece = Rect(optionsRect, EncounterChoice.Label(options[i]), new Vector2(OptionsWidth, Height), Vector2.zero);
            pieces[i] = piece.gameObject.AddComponent<CanvasGroup>();
            float from = i / (float)n, to = (i + 1) / (float)n;
            // Challenge (the prowess offer) is painted in its own colour rather than smoked glass.
            fills[i] = Strip(piece, "Glass", painted ? new Color(accent.r * .55f, accent.g * .35f, accent.b * .6f, .5f) : new Color(.025f, .045f, .07f, .42f), from, to, i > 0 ? 9 : 0, i < n - 1 ? 9 : 0, 22, 0);
            fills[i].topGlow = painted ? .25f : .06f;
            rims[i] = Strip(piece, "Rim", new Color(accent.r, accent.g, accent.b, .75f), from, to, i > 0 ? 9 : 0, i < n - 1 ? 9 : 0, 22, painted ? 2.5f : 1.5f);
            float x = OptionsWidth * ((from + to) * .5f - .5f);
            icons[i] = Rect(piece, "Encounter sprite", new Vector2(40, 40), new Vector2(x - 12, -2));
            var image = icons[i].gameObject.AddComponent<Image>();
            image.sprite = EncounterChoice.Icon(options[i]);
            image.preserveAspect = true;
            image.raycastTarget = false;
            keys[i] = Key(piece, new Vector2(x + 26, -8));
        }
        var skip = Rect(body, "Skip", new Vector2(SkipWidth, SkipHeight), new Vector2(202.5f - SkipWidth * .5f, 0));
        skip.anchorMin = skip.anchorMax = new Vector2(.5f, 0);
        skip.pivot = new Vector2(.5f, 0);
        Strip(skip, "Glass", new Color(.025f, .045f, .07f, .42f), 0, 1, 0, 0, 7, 0).topGlow = .06f;
        skipRim = Strip(skip, "Rim", new Color(1, .72f, .32f, .6f), 0, 1, 0, 0, 7, 1.5f);
        var label = Label(skip, "SKIP", 10, new Vector2(0, -12), new Color(.96f, .94f, .86f, .85f));
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, .5f);
        skipKey = Key(skip, new Vector2(0, 7));
        Refresh();
    }

    void Update()
    {
        if (leaving)
        {
            exitAge += Time.deltaTime;
            float t = Mathf.Clamp01(exitAge / ExitSeconds), ease = Mathf.SmoothStep(0, 1, t);
            body.anchoredPosition = new Vector2(0, -18 * ease);
            for (int i = 0; i < pieces.Length; i++)
            {
                bool selected = i == chosen;
                pieces[i].alpha = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(t * (selected ? 1 : 2.2f)));
                icons[i].localScale = Vector3.one * (selected ? 1 + .3f * Mathf.Sin(t * Mathf.PI) : 1 - .15f * ease);
                if (selected)
                    rims[i].color = Color.Lerp(Color.white, EncounterChoice.Accent(options[i]), t);
            }
            group.alpha = chosen >= 0 ? 1 - ease * ease : 1 - ease;
            if (exitAge >= ExitSeconds)
                Destroy(gameObject);
            return;
        }
        if (Time.timeScale == 0)
            return;
        age += Time.deltaTime;
        float enter = Mathf.SmoothStep(0, 1, age / EnterSeconds);
        group.alpha = enter;
        body.anchoredPosition = new Vector2(0, -18 * (1 - enter));
        Refresh();
        for (int i = 0; i < options.Length; i++)
            if (options[i] == EncounterKind.Challenge)
            {
                // The painted prowess offer shimmers so it stands apart from ordinary rewards.
                float shimmer = .5f + .5f * Mathf.Sin(Time.time * 5);
                var accent = EncounterChoice.Accent(options[i]);
                rims[i].color = Color.Lerp(accent, Color.white, .35f * shimmer);
                fills[i].topGlow = .18f + .14f * shimmer;
                fills[i].SetVerticesDirty();
            }
        if (age < Arming || GameOverState.Active || CloudTransition.Busy)
            return;
        int choice = ReadChoice();
        if (choice == -2)
            return;
        Choose(choice);
    }

    // -1 skips; -2 means nothing was pressed this frame.
    int ReadChoice()
    {
        int n = options.Length;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.qKey.wasPressedThisFrame)
                return 0;
            if (keyboard.eKey.wasPressedThisFrame)
                return n - 1;
            if (n == 3 && keyboard.wKey.wasPressedThisFrame)
                return 1;
            if (keyboard.rKey.wasPressedThisFrame)
                return -1;
        }
        var pad = Gamepad.current;
        if (pad == null)
            return -2;
        // A deliberate flick of the otherwise unused right stick; the left stick keeps flying.
        var stick = pad.rightStick.ReadValue();
        if (stick.magnitude < .35f)
        {
            stickHeld = false;
            return -2;
        }
        if (stickHeld || stick.magnitude < .75f)
            return -2;
        stickHeld = true;
        if (Mathf.Abs(stick.x) > Mathf.Abs(stick.y))
            return stick.x < 0 ? 0 : n - 1;
        if (stick.y < 0)
            return -1;
        return n == 3 ? 1 : -2;
    }

    void Choose(int index)
    {
        var callback = picked;
        Dismiss();
        chosen = index;
        if (index >= 0)
        {
            ItemSound.Play("ready", .2f);
            rims[index].color = Color.white;
        }
        else
            skipRim.color = Color.white;
        callback?.Invoke(index);
    }

    void Refresh()
    {
        bool pad = Gamepad.current != null && (Keyboard.current == null || Gamepad.current.lastUpdateTime > Keyboard.current.lastUpdateTime);
        var labels = pad ? options.Length == 3 ? PadLabels3 : PadLabels2 : options.Length == 3 ? KeyLabels3 : KeyLabels2;
        for (int i = 0; i < keys.Length; i++)
            keys[i].text = labels[i];
        skipKey.text = pad ? "↓" : "R";
    }

    CurvedStripGraphic Strip(Transform parent, string name, Color color, float from, float to, float leftNotch, float rightNotch, float dip, float thickness)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(CurvedStripGraphic));
        go.transform.SetParent(parent, false);
        var graphic = go.GetComponent<CurvedStripGraphic>();
        graphic.color = color;
        graphic.raycastTarget = false;
        graphic.from = from;
        graphic.to = to;
        graphic.leftNotch = leftNotch;
        graphic.rightNotch = rightNotch;
        graphic.dip = dip;
        graphic.thickness = thickness;
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return graphic;
    }

    Text Key(Transform parent, Vector2 at)
    {
        if (keycap == null)
        {
            var texture = new Texture2D(32, 24, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color[32 * 24];
            for (int y = 0; y < 24; y++)
                for (int x = 0; x < 32; x++)
                {
                    bool corner = (x < 3 || x > 28) && (y < 3 || y > 20);
                    pixels[y * 32 + x] = corner ? Color.clear : x < 2 || x > 29 || y < 2 ? new Color(.12f, .19f, .2f) : y > 21 ? new Color(.83f, .9f, .86f) : y < 5 ? new Color(.29f, .39f, .39f) : new Color(.65f, .74f, .71f);
                }
            texture.SetPixels(pixels);
            texture.Apply();
            keycap = Sprite.Create(texture, new Rect(0, 0, 32, 24), new Vector2(.5f, .5f), 32);
        }
        var rect = Rect(parent, "Control keycap sprite", new Vector2(24, 18), at);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = keycap;
        image.raycastTarget = false;
        var text = Label(rect, "", 11, new Vector2(0, 1), new Color(.04f, .09f, .1f));
        text.rectTransform.sizeDelta = new Vector2(24, 18);
        return text;
    }

    Text Label(Transform parent, string value, int size, Vector2 at, Color color)
    {
        var rect = Rect(parent, value == "" ? "Label" : value, new Vector2(60, 16), at);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    void OnDestroy()
    {
        if (current == this)
            current = null;
    }
}
