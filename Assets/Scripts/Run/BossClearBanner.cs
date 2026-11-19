using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BossClearNotice
{
    public readonly bool NoHitLevel, NoHitBoss;
    public readonly IReadOnlyList<ItemUnlockNotice> Unlocks;
    public BossClearNotice(bool noHitLevel, bool noHitBoss, List<ItemUnlockNotice> unlocks)
    {
        NoHitLevel = noHitLevel;
        NoHitBoss = noHitBoss;
        Unlocks = unlocks.AsReadOnly();
    }
}

// A brief, non-blocking celebration over the cleared arena. The mandatory boss item and
// the persistent hangar reveal queue keep their normal ownership and controls.
public sealed class BossClearBanner : MonoBehaviour
{
    public const float Duration = 2.8f;
    static BossClearBanner current;
    readonly List<TextMeshProUGUI> noHitLabels = new List<TextMeshProUGUI>();
    CanvasGroup fade;
    RectTransform content, routeCard;
    ItemPortraitStudio studio;
    float age;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Listen()
    {
        current = null;
        MetaProgress.BossCleared -= Show;
        MetaProgress.BossCleared += Show;
    }

    static void Show(BossClearNotice notice)
    {
        if (GameOverState.Active || HangarHub.Active || notice.Unlocks.Count == 0 && !notice.NoHitLevel && !notice.NoHitBoss)
            return;
        if (current != null) Destroy(current.gameObject);
        current = new GameObject("Boss clear unlocks", typeof(RectTransform)).AddComponent<BossClearBanner>();
        var dialog = FindAnyObjectByType<Dialog>(FindObjectsInactive.Include);
        current.Build(notice, dialog != null ? dialog.font : null);
    }

    void Build(BossClearNotice notice, TMP_FontAsset font)
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 19; // The reward notice is part of the flight beneath the pause menu.
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        fade = gameObject.AddComponent<CanvasGroup>();
        fade.interactable = fade.blocksRaycasts = false;
        fade.alpha = 0;
        var aperture = new GameObject("Flight area", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
        aperture.SetParent(transform, false);
        aperture.sizeDelta = new Vector2(405, 720);
        var items = new List<ItemUnlockNotice>();
        foreach (var unlock in notice.Unlocks)
        {
            if (RouteUnlocks.IsRoute(unlock.key))
                BuildRouteCard(aperture, unlock, font);
            else
                items.Add(unlock);
        }
        int marks = (notice.NoHitLevel ? 1 : 0) + (notice.NoHitBoss ? 1 : 0);
        if (items.Count == 0 && marks == 0) return;
        float height = items.Count == 0 ? 94 : marks == 0 ? 184 : marks == 1 ? 226 : 250;
        content = DockInstrument.Block(aperture, "Boss rewards", Vector2.zero,
            new Vector2(368, height), new Color(.025f, .04f, .05f, .88f));
        float y = items.Count > 0 ? marks == 2 ? 102 : 90 : (marks - 1) * 17;
        if (notice.NoHitLevel)
        {
            noHitLabels.Add(UnlockReceipt.Label(content, "No-hit level", new Vector2(0, y), new Vector2(344, 30), 24, DockInstrument.Brass, font));
            y -= 34;
        }
        if (notice.NoHitBoss)
            noHitLabels.Add(UnlockReceipt.Label(content, "No-hit boss", new Vector2(0, y), new Vector2(344, 30), 24, DockInstrument.Sea, font));
        if (items.Count == 0) return;
        float itemOffset = marks == 0 ? 25 : 0;
        UnlockReceipt.Label(content, "UNLOCKED", new Vector2(0, 29 + itemOffset), new Vector2(330, 20), 13, DockInstrument.Brass, font);
        studio = ItemPortraitStudio.Create(UnlockReceipt.ItemKeys(items), 128);
        studio.gameObject.SetActive(!Menu.GameplayPaused);
        float spacing = Mathf.Min(116, 348f / items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            float x = (i - (items.Count - 1) * .5f) * spacing;
            UnlockReceipt.Portrait(content, studio, items, i, new Vector2(x, -24 + itemOffset), 76);
            UnlockReceipt.Label(content, items[i].title, new Vector2(x, -82 + itemOffset), new Vector2(spacing - 6, 40), 14, DockInstrument.Paper, font);
        }
    }

    void BuildRouteCard(Transform parent, ItemUnlockNotice route, TMP_FontAsset font)
    {
        var accent = LevelRun.Accent(route.area);
        routeCard = DockInstrument.Block(parent, "New route", Vector2.up * 237, new Vector2(344, 96), accent);
        var face = DockInstrument.Block(routeCard, "Route enamel", Vector2.zero, new Vector2(338, 90), DockInstrument.Ink);
        DockInstrument.Block(face, "Stamp divider", new Vector2(-91, 0), new Vector2(2, 58), accent * .65f);
        UnlockReceipt.DistrictStamp(face, route, new Vector2(-128, 0), 69);
        UnlockReceipt.Label(face, "ROUTE OPEN", new Vector2(35, 25), new Vector2(224, 18), 13, DockInstrument.Brass, font);
        UnlockReceipt.Label(face, route.title, new Vector2(35, 0), new Vector2(224, 30), 24, DockInstrument.Paper, font);
        UnlockReceipt.Label(face, "NEXT FLIGHT", new Vector2(35, -26), new Vector2(224, 16), 11, accent, font);
    }

    void Update()
    {
        if (GameOverState.Active || HangarHub.Active || CloudTransition.Busy)
        {
            Destroy(gameObject);
            return;
        }
        bool paused = Menu.GameplayPaused;
        // The pause item browser uses the same off-screen portrait stage.
        if (studio != null && studio.gameObject.activeSelf == paused)
            studio.gameObject.SetActive(!paused);
        if (paused)
        {
            fade.alpha = 0;
            return;
        }
        age += Time.unscaledDeltaTime;
        float enter = Mathf.SmoothStep(0, 1, age / .16f);
        float leave = Mathf.SmoothStep(0, 1, (age - Duration + .35f) / .35f);
        fade.alpha = enter * (1 - leave);
        if (content != null)
            content.anchoredPosition = Vector2.up * (9 * (1 - enter) + 9 * leave);
        if (routeCard != null)
            routeCard.anchoredPosition = Vector2.up * (237 + 22 * (1 - enter) + 12 * leave);
        float blink = Mathf.Repeat(age, .55f) < .34f ? 1 : .2f;
        foreach (var label in noHitLabels) label.alpha = blink;
        if (age >= Duration) Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (studio != null) Destroy(studio.gameObject);
        if (current == this) current = null;
    }
}
