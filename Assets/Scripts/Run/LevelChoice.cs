using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A spatial choice over the actual playfield. The pilot remains visible through both panels.</summary>
public sealed class LevelChoice : MonoBehaviour
{
    public static bool Active
    {
        get; private set;
    }

    public LevelId Left
    {
        get; private set;
    }

    public LevelId Right
    {
        get; private set;
    }

    public RouteCharge Charge { get; } = new RouteCharge();
    Transform player;
    Action<LevelId> picked;
    Action<LevelId> locked;
    Action<int> encounterPicked;
    string[] captions;
    Color[] accents;
    Sprite[] icons;
    // Routes the save has not opened yet: shown, never chargeable.
    bool[] sealedRoutes;
    float[] rattle;
    int pressedSealed = -1;
    const float RattleSeconds = .35f;
    CanvasGroup group;
    RectTransform frame;
    RectTransform[] fills, contents, panels;
    CanvasGroup[] panelGroups;
    float[] displayed;
    public LevelId[] Options
    {
        get; private set;
    }

    float panelWidth;
    readonly AnimationCurve fillCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    TMP_FontAsset font;
    float age;
    bool committed;
    public const float ExitSeconds = .45f;

    public static LevelChoice Open(Transform player, Action<LevelId> picked, Action<LevelId> locked = null)
    {
        var choice = new GameObject("Choose the next route").AddComponent<LevelChoice>();
        choice.player = player;
        choice.picked = picked;
        choice.locked = locked;
        LevelRun.GetChoices(out var left, out var right);
        choice.Left = left;
        choice.Right = right;
        choice.Options = LevelRun.GetOptions();
        choice.captions = Array.ConvertAll(choice.Options, level => level == LevelId.Fields ? "PLAINS" : level.ToString().ToUpperInvariant());
        choice.accents = Array.ConvertAll(choice.Options, LevelRun.Accent);
        choice.sealedRoutes = Array.ConvertAll(choice.Options, level => !MetaProgress.RouteOpen(level));
        choice.Allocate();
        CloudTransition.PrepareRoutes(Array.FindAll(choice.Options, MetaProgress.RouteOpen));
        choice.Build();
        Active = true;
        return choice;
    }

    public static LevelChoice OpenEncounter(Transform player, string[] captions, Color[] accents, Action<int> picked, Sprite[] icons = null)
    {
        var choice = new GameObject("Choose an encounter").AddComponent<LevelChoice>();
        choice.player = player;
        choice.captions = captions;
        choice.accents = accents;
        choice.encounterPicked = picked;
        choice.icons = icons;
        choice.Allocate();
        choice.Build();
        Active = true;
        return choice;
    }

    void Allocate()
    {
        fills = new RectTransform[captions.Length];
        contents = new RectTransform[captions.Length];
        panels = new RectTransform[captions.Length];
        panelGroups = new CanvasGroup[captions.Length];
        displayed = new float[captions.Length];
        rattle = new float[captions.Length];
    }

    void Build()
    {
        var dialog = FindAnyObjectByType<Dialog>();
        font = dialog != null ? dialog.font : null;
        if (font == null)
            foreach (var label in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
                if (label.font != null)
                {
                    font = label.font;
                    break;
                }
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0;
        frame = Rect(transform, "Route playfield", new Vector2(405, 720), Vector2.zero);
        frame.gameObject.AddComponent<RectMask2D>();
        panelWidth = 405f / captions.Length;
        for (int i = 0; i < captions.Length; i++)
        {
            var accent = accents[i];
            var half = Rect(frame, captions[i], new Vector2(panelWidth, 720), new Vector2(-202.5f + panelWidth * (i + .5f), 0));
            panels[i] = half;
            panelGroups[i] = half.gameObject.AddComponent<CanvasGroup>();
            Paint(half, PanelTint(i));
            fills[i] = Rect(half, "Bottom-up route charge", new Vector2(panelWidth, 0), Vector2.zero);
            fills[i].anchorMin = fills[i].anchorMax = new Vector2(.5f, 0);
            fills[i].pivot = new Vector2(.5f, 0);
            Paint(fills[i], new Color(accent.r, accent.g, accent.b, .37f));
            var edge = Rect(fills[i], "Leading edge", new Vector2(panelWidth, 1.5f), Vector2.zero);
            edge.anchorMin = edge.anchorMax = new Vector2(.5f, 1);
            Paint(edge, new Color(accent.r, accent.g, accent.b, .65f));
            contents[i] = BuildChoiceContent(i);
        }
        var centre = Rect(frame, "Neutral flight corridor", captions.Length == 3 ? new Vector2(405, 180) : new Vector2(405 * RouteCharge.NeutralWidth, 720), captions.Length == 3 ? new Vector2(0, -270) : Vector2.zero);
        Paint(centre, new Color(.04f, .07f, .10f, .2f));
        for (int i = 1; i < captions.Length; i++)
        {
            var rule = Rect(frame, "Route divider", new Vector2(1, 720), new Vector2(-202.5f + i * panelWidth, 0));
            Paint(rule, new Color(.78f, .88f, .95f, .22f));
        }
    }

    Color PanelTint(int index) => Sealed(index) ? new Color(.006f, .01f, .016f, .84f) : new Color(.025f, .045f, .07f, .43f);

    RectTransform BuildChoiceContent(int index)
    {
        var panel = panels[index];
        var accent = accents[index];
        if (icons != null)
        {
            var icon = Rect(panel, "Encounter sprite", new Vector2(96, 96), Vector2.zero);
            var image = icon.gameObject.AddComponent<Image>();
            image.sprite = icons[index];
            image.preserveAspect = true;
            image.raycastTarget = false;
            return icon;
        }
        if (Sealed(index))
        {
            // A sealed district keeps its secret: a padlock instead of its name.
            var padlock = Rect(panel, "Sealed route", new Vector2(40, 40), Vector2.zero);
            var image = padlock.gameObject.AddComponent<Image>();
            image.sprite = Resources.Load<Sprite>("DockArt/RoutePadlock");
            image.color = new Color(.8f, .86f, .9f, .8f);
            image.raycastTarget = false;
            if (MetaProgress.RouteOpensNextFlight(Options[index]))
            {
                padlock.anchoredPosition = Vector2.up * 13;
                image.color = Color.Lerp(accent, Color.white, .35f);
                Text(padlock, "NEXT FLIGHT", 12, Vector2.down * 37, image.color);
            }
            return padlock;
        }

        bool narrow = captions.Length == 3;
        var label = Text(panel, captions[index], narrow ? 17 : 24, Vector2.zero, Color.white);
        label.rectTransform.sizeDelta = new Vector2(panelWidth - 14, 48);
        label.enableAutoSizing = true;
        label.fontSizeMin = narrow ? 12 : 16;
        label.fontSizeMax = narrow ? 17 : 24;
        if (narrow)
            label.color = Color.Lerp(accent, Color.white, .5f);
        if (Options != null)
            EventBadge(label.rectTransform, Options[index], accent, narrow);
        return label.rectTransform;
    }

    internal void RefreshOpenRoutes()
    {
        if (Options == null || committed)
            return;
        bool changed = false;
        for (int i = 0; i < Options.Length; i++)
        {
            if (!Sealed(i) || !MetaProgress.RouteOpen(Options[i]))
                continue;
            changed = true;
            sealedRoutes[i] = false;
            rattle[i] = 0;
            panels[i].GetComponent<Image>().color = PanelTint(i);
            contents[i].gameObject.SetActive(false);
            Destroy(contents[i].gameObject);
            contents[i] = BuildChoiceContent(i);
        }
        if (changed)
            CloudTransition.PrepareRoutes(Array.FindAll(Options, MetaProgress.RouteOpen));
    }

    void Update()
    {
        // Match the game's pause state; pausing cannot finish a selection offscreen.
        if (Time.timeScale == 0 || CloudTransition.Busy || GameOverState.Active || player == null)
            return;
        if (committed)
            return;
        age += Time.deltaTime;
        float enter = Mathf.SmoothStep(0, 1, age / .3f);
        group.alpha = enter;
        if (age >= .3f)
        {
            float x = player.position.x / (player.position.x < 0 ? -Camera.LeftLimit : Camera.RightLimit);
            if (captions.Length == 2)
            {
                int side = RouteCharge.SideOf(x);
                int pressed = side == 0 ? -1 : side < 0 ? 0 : 1;
                bool refused = pressed >= 0 && Sealed(pressed);
                // Pushing into a sealed side rattles its padlock once; it never charges.
                if (refused && pressed != pressedSealed)
                    rattle[pressed] = RattleSeconds;
                pressedSealed = refused ? pressed : -1;
                Charge.StepSelection(refused ? 0 : side, Time.deltaTime);
            }
            else
            {
                float column = (x + 1) * 1.5f;
                bool seam = Mathf.Abs(column - 1) < .09f || Mathf.Abs(column - 2) < .09f;
                bool neutral = player.position.y < Mathf.Lerp(Camera.DownLimit, Camera.UpLimit, .25f) || seam;
                Charge.StepSelection(neutral ? 0 : Mathf.Clamp(Mathf.FloorToInt(column), 0, 2) + 1, Time.deltaTime);
            }
        }
        for (int i = 0; i < captions.Length; i++)
        {
            bool selected = Charge.Side == (captions.Length == 2 ? (i == 0 ? -1 : 1) : i + 1);
            float target = selected ? fillCurve.Evaluate(Charge.Progress) : 0;
            displayed[i] = Mathf.Lerp(displayed[i], target, 1 - Mathf.Exp(-Time.deltaTime * 20));
            if (Charge.Complete && selected)
                displayed[i] = 1;
            fills[i].sizeDelta = new Vector2(panelWidth, 720 * displayed[i]);
            contents[i].localScale = Vector3.one * (1 + (selected ? .035f * Mathf.Sin(Charge.Progress * Mathf.PI) : 0));
            if (rattle[i] > 0)
            {
                rattle[i] = Mathf.Max(0, rattle[i] - Time.deltaTime);
                float shake = rattle[i] / RattleSeconds;
                contents[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(rattle[i] * 55) * 9 * shake);
            }
        }
        if (Charge.Complete)
        {
            committed = true;
            StartCoroutine(Commit());
        }
    }

    bool Sealed(int index) => sealedRoutes != null && sealedRoutes[index];

    IEnumerator Commit()
    {
        int index = captions.Length == 2 ? (Charge.Side < 0 ? 0 : 1) : Charge.Side - 1;
        if (locked != null) locked(Options[index]);
        yield return new WaitForSeconds(.1f);
        for (float elapsed = 0; elapsed < ExitSeconds; elapsed += Time.deltaTime)
        {
            AnimateExit(index, Mathf.Clamp01(elapsed / ExitSeconds));
            yield return null;
        }
        AnimateExit(index, 1);
        Active = false;
        if (encounterPicked != null)
            encounterPicked(index);
        else
            picked?.Invoke(Options[index]);
    }

    void AnimateExit(int selected, float progress)
    {
        float t = Mathf.Clamp01(progress), ease = Mathf.SmoothStep(0, 1, t);
        group.alpha = 1 - Mathf.SmoothStep(0, 1, t * t);
        for (int i = 0; i < panels.Length; i++)
        {
            bool chosen = i == selected;
            float x = -202.5f + panelWidth * (i + .5f);
            panelGroups[i].alpha = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(t * (chosen ? 1 : 1.5f)));
            panels[i].anchoredPosition = new Vector2(x + (chosen ? 0 : Mathf.Sign(x) * 30 * ease), chosen ? 16 * ease : 0);
            contents[i].localScale = Vector3.one * (chosen ? 1 + .14f * Mathf.Sin(t * Mathf.PI) - .12f * t * t : 1 - .1f * ease);
        }
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

    static void Paint(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    TextMeshProUGUI Text(Transform parent, string text, float size, Vector2 pos, Color color)
    {
        var rect = Rect(parent, text, new Vector2(195, 48), pos);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    // The district's event this run (blizzard, solar flare...), shown under its name so the pilot
    // can weigh it while choosing. It rides the caption's selection pulse.
    // Its level modifiers (MORE ELITES, MORE COINS, FAST FIRE) stack beneath the event.
    void EventBadge(RectTransform caption, LevelId level, Color accent, bool narrow)
    {
        float y = narrow ? -32 : -40;
        var ink = Color.Lerp(accent, Color.white, .55f);
        var condition = DistrictConditions.For(level, LevelRun.Seed);
        if (condition != DistrictCondition.Ordinary)
            y = Badge(caption, DistrictConditions.Icon(condition), DistrictConditions.Label(condition), y, ink, narrow);
        var modifiers = LevelModifiers.Next(level);
        foreach (var modifier in LevelModifiers.All)
            if ((modifiers & modifier) != 0)
                y = Badge(caption, LevelModifiers.Icon(modifier), LevelModifiers.Label(modifier), y, ink, narrow);
    }

    // One badge, its icon over its name; returns where the next badge starts.
    float Badge(RectTransform caption, Sprite sprite, string text, float y, Color color, bool narrow)
    {
        float icon = narrow ? 28 : 32;
        var badge = Rect(caption, text + " icon", new Vector2(icon, icon), new Vector2(0, y));
        var image = badge.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        var name = Text(caption, text, narrow ? 9 : 11, new Vector2(0, y - (narrow ? 22 : 26)), color);
        name.rectTransform.sizeDelta = new Vector2(panelWidth - 10, 20);
        name.enableAutoSizing = true;
        name.fontSizeMin = 7;
        name.fontSizeMax = narrow ? 9 : 11;
        return y - (narrow ? 50 : 58);
    }

    void OnDestroy()
    {
        Active = false;
    }
}
