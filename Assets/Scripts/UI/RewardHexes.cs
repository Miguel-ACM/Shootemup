using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A mid-wave encounter offer: one hexagon per option somewhere in the arena. Hold the ship inside one to take it.</summary>
public sealed class RewardHexes : MonoBehaviour
{
    public static bool Active => current != null;
    static RewardHexes current;
    public const float FillSeconds = 3, LifeSeconds = 20;
    // World radius of each hexagon, corner to centre.
    public const float Radius = 1.15f;
    const float EnterSeconds = .45f, ExitSeconds = .4f, Stagger = .12f, WarningSeconds = 4, HoverAlpha = .1f;
    static readonly Color Steel = new Color(.48f, .59f, .61f), Glass = new Color(.025f, .045f, .07f, .14f);

    sealed class Zone
    {
        public EncounterKind kind;
        public Vector2 centre;
        public RectTransform root;
        public CanvasGroup group;
        public HexagonGraphic hover, track, charge;
        public Image icon;
        public float progress;
    }

    Zone[] zones;
    Transform player;
    Action<int> picked;
    CanvasGroup prompt;
    float age, exitAge;
    int chosen = -1;
    bool leaving;

    public static RewardHexes Open(EncounterKind[] options, Transform player, int seed, Action<int> picked)
    {
        if (current != null)
            current.Dismiss();
        var hexes = new GameObject("Mid-wave reward hexagons", typeof(RectTransform)).AddComponent<RewardHexes>();
        hexes.player = player;
        hexes.picked = picked;
        hexes.Build(options, seed);
        current = hexes;
        return hexes;
    }

    /// <summary>Withdraws the offer without a choice, as when a boss or event begins.</summary>
    public void Dismiss()
    {
        if (leaving)
            return;
        leaving = true;
        chosen = -1;
        picked = null;
        if (current == this)
            current = null;
    }

    // Better rewards lean toward the riskier upper arena; anything can still land near the top.
    static float Importance(EncounterKind kind)
    {
        switch (kind)
        {
            case EncounterKind.Item:
                return 1;
            case EncounterKind.Challenge:
                return .75f;
            case EncounterKind.Health:
                return .35f;
            default:
                return .55f;
        }
    }

    static Vector2[] Place(EncounterKind[] options, int seed, Vector2 avoid)
    {
        var rng = new System.Random(seed);
        float margin = Radius + .35f;
        float left = Camera.LeftLimit + margin, right = Camera.RightLimit - margin;
        // Kept in the lower half of the arena, within easy reach of the ship.
        float low = Mathf.Lerp(Camera.DownLimit, Camera.UpLimit, .14f), high = Mathf.Lerp(Camera.DownLimit, Camera.UpLimit, .55f);
        var spots = new Vector2[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            float exponent = Mathf.Lerp(1.6f, .3f, Importance(options[i]));
            Vector2 best = Vector2.zero;
            float bestScore = float.MinValue;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                var spot = new Vector2(Mathf.Lerp(left, right, (float)rng.NextDouble()), Mathf.Lerp(low, high, Mathf.Pow((float)rng.NextDouble(), exponent)));
                // Keep clear of the ship, so nothing starts filling by accident, and apart from the other hexagons.
                float score = Mathf.Min(Vector2.Distance(spot, avoid) - Radius * 2.2f, 0);
                for (int j = 0; j < i; j++)
                    score += Mathf.Min(Vector2.Distance(spot, spots[j]) - Radius * 3, 0);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = spot;
                }
                if (score >= 0)
                    break;
            }
            spots[i] = best;
        }
        return spots;
    }

    void Build(EncounterKind[] options, int seed)
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        var spots = Place(options, seed, player != null ? (Vector2)player.position : new Vector2(0, Camera.DownLimit));
        zones = new Zone[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            var accent = EncounterChoice.Accent(options[i]);
            var zone = zones[i] = new Zone { kind = options[i], centre = spots[i] };
            zone.root = Rect(transform, EncounterChoice.Label(options[i]), Vector2.one * 100);
            zone.root.anchorMin = zone.root.anchorMax = new Vector2(.5f, .5f);
            zone.group = zone.root.gameObject.AddComponent<CanvasGroup>();
            zone.group.alpha = 0;
            // Faint smoked glass keeps hostile rounds readable through the zone.
            Hex(zone.root, "Glass", Glass, 0, 0);
            zone.hover = Hex(zone.root, "Hover tint", new Color(accent.r, accent.g, accent.b, 0), 0, 0);
            Hex(zone.root, "Machined rim", new Color(Steel.r, Steel.g, Steel.b, .8f), 0, 2).shade = .5f;
            zone.track = Hex(zone.root, "Charge track", new Color(accent.r, accent.g, accent.b, .3f), .07f, 3);
            zone.charge = Hex(zone.root, "Charge", accent, .07f, 3);
            zone.charge.progress = 0;
            var icon = Rect(zone.root, "Encounter sprite", Vector2.zero);
            icon.anchorMin = new Vector2(.3f, .3f);
            icon.anchorMax = new Vector2(.7f, .7f);
            icon.offsetMin = icon.offsetMax = Vector2.zero;
            zone.icon = icon.gameObject.AddComponent<Image>();
            zone.icon.sprite = EncounterChoice.Icon(options[i]);
            zone.icon.preserveAspect = true;
            zone.icon.raycastTarget = false;
        }
        BuildPrompt(Array.IndexOf(options, EncounterKind.Item) >= 0 ? "Choose an item" : "Choose a reward");
        LateUpdate();
    }

    void BuildPrompt(string message)
    {
        // Composited after the pixel filter, like the shop labels, so it stays crisp.
        var root = Rect(transform, "Reward prompt", new Vector2(176, 24));
        root.anchorMin = root.anchorMax = new Vector2(.5f, 1);
        root.anchoredPosition = new Vector2(0, -30);
        var canvas = root.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 65;
        prompt = root.gameObject.AddComponent<CanvasGroup>();
        prompt.alpha = 0;
        var plate = root.gameObject.AddComponent<Image>();
        plate.color = new Color(0, 0, 0, .8f);
        plate.raycastTarget = false;
        var text = Rect(root, message, Vector2.zero);
        text.anchorMin = Vector2.zero;
        text.anchorMax = Vector2.one;
        text.offsetMin = text.offsetMax = Vector2.zero;
        var label = text.gameObject.AddComponent<TextMeshProUGUI>();
        var dialog = GameObject.FindGameObjectWithTag("Dialog");
        var font = dialog != null ? dialog.GetComponent<Dialog>().font : null;
        if (font != null)
            label.font = font;
        label.text = message;
        label.fontSize = 12;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(.96f, .94f, .86f);
        label.raycastTarget = false;
    }

    void Update()
    {
        if (leaving || Time.timeScale == 0)
            return;
        age += Time.deltaTime;
        if (age >= LifeSeconds)
        {
            // Nobody held a zone in time: the offer lapses like a skip.
            var callback = picked;
            Dismiss();
            callback?.Invoke(-1);
            return;
        }
        bool counting = age > EnterSeconds && player != null && !GameOverState.Active && !CloudTransition.Busy;
        for (int i = 0; i < zones.Length; i++)
        {
            var zone = zones[i];
            bool inside = counting && HexagonGraphic.Contains((Vector2)player.position - zone.centre, Radius);
            // Leaving drains the charge, so the reward goes to whoever holds the area.
            zone.progress = Mathf.Clamp01(zone.progress + Time.deltaTime / FillSeconds * (inside ? 1 : -1.5f));
            var accent = EncounterChoice.Accent(zone.kind);
            zone.hover.color = new Color(accent.r, accent.g, accent.b, Mathf.MoveTowards(zone.hover.color.a, inside ? HoverAlpha : 0, Time.deltaTime * .6f));
            zone.charge.progress = zone.progress;
            zone.charge.SetVerticesDirty();
            if (zone.kind == EncounterKind.Challenge)
            {
                // The prowess offer shimmers so it stands apart from ordinary rewards.
                float shimmer = .5f + .5f * Mathf.Sin(Time.time * 5);
                zone.track.color = new Color(accent.r, accent.g, accent.b, .3f + .25f * shimmer);
            }
            if (zone.progress >= 1)
            {
                Choose(i);
                return;
            }
        }
    }

    void Choose(int index)
    {
        var callback = picked;
        Dismiss();
        chosen = index;
        if (SoundManager.HasSynth("uiConfirm"))
            SoundManager.PlaySound("uiConfirm", .1f);
        else
            ItemSound.Play("ready", .2f);
        zones[index].charge.color = Color.white;
        callback?.Invoke(index);
    }

    void LateUpdate()
    {
        var camera = UnityEngine.Camera.main;
        if (camera == null)
            return;
        var canvas = (RectTransform)transform;
        float size = Radius * 2 * canvas.rect.height / (camera.orthographicSize * 2);
        float exit = leaving ? Mathf.SmoothStep(0, 1, Mathf.Clamp01((exitAge += Time.deltaTime) / ExitSeconds)) : 0;
        for (int i = 0; i < zones.Length; i++)
        {
            var zone = zones[i];
            var viewport = camera.WorldToViewportPoint(new Vector3(zone.centre.x, zone.centre.y, 0));
            zone.root.anchoredPosition = new Vector2((viewport.x - .5f) * canvas.rect.width, (viewport.y - .5f) * canvas.rect.height);
            float enter = Mathf.Clamp01((age - i * Stagger) / EnterSeconds);
            // A slight overshoot, so the hexagon lands rather than fades.
            float grow = enter < 1 ? 1 - Mathf.Pow(1 - enter, 3) * Mathf.Cos(enter * 5) : 1;
            float scale = Mathf.Lerp(.6f, 1, grow), alpha = Mathf.SmoothStep(0, 1, enter);
            float remaining = LifeSeconds - age;
            if (!leaving && remaining < WarningSeconds)
                alpha *= .7f + .3f * Mathf.Cos((WarningSeconds - remaining) * Mathf.PI * 2 * Mathf.Lerp(1, 3, 1 - remaining / WarningSeconds));
            if (leaving)
            {
                bool selected = i == chosen;
                scale *= selected ? 1 + .25f * Mathf.Sin(exit * Mathf.PI * .5f) : 1 - .3f * exit;
                alpha *= 1 - (selected ? exit * exit : exit);
                if (selected)
                    zone.charge.color = Color.Lerp(Color.white, EncounterChoice.Accent(zone.kind), exit);
            }
            zone.root.sizeDelta = Vector2.one * size;
            zone.root.localScale = Vector3.one * scale;
            zone.group.alpha = alpha;
        }
        prompt.alpha = leaving ? 1 - exit : Mathf.SmoothStep(0, 1, age / EnterSeconds);
        if (leaving && exitAge >= ExitSeconds)
            Destroy(gameObject);
    }

    HexagonGraphic Hex(Transform parent, string name, Color color, float inset, float thickness)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(HexagonGraphic));
        go.transform.SetParent(parent, false);
        var hex = go.GetComponent<HexagonGraphic>();
        hex.color = color;
        hex.thickness = thickness;
        hex.raycastTarget = false;
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.one * inset;
        rect.anchorMax = Vector2.one * (1 - inset);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return hex;
    }

    static RectTransform Rect(Transform parent, string name, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        return rect;
    }

    void OnDestroy()
    {
        if (current == this)
            current = null;
    }
}
