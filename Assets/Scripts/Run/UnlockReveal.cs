using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A persistent queue is acknowledged one tag at a time, only after arrival in the hangar.
// The key that closes one tag never buys an item or launches the ship underneath it.
public sealed class UnlockReveal : MonoBehaviour
{
    const float EnterDuration = .22f, ExitDuration = .14f;
    static UnlockReveal instance;
    static int closedFrame = -1;
    public static bool BlocksInput => instance != null || closedFrame == Time.frameCount;
    ItemPortraitStudio studio;
    RawImage portrait;
    Image stamp, districtStamp, rim;
    // Portrait cell per queued tag; district tags have none.
    readonly List<int> cells = new List<int>();
    TextMeshProUGUI heading, title, category, description, source, counter, footer;
    RectTransform card;
    CanvasGroup fade;
    int index, total, shownFrame;
    float age, visibility, closingFrom;
    bool closing;
    string currentKey;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { instance = null; closedFrame = -1; }

    public static void Show(Transform parent, TMP_FontAsset font)
    {
        if (instance != null || MetaProgress.PendingUnlocks.Count == 0) return;
        var go = new GameObject("Item unlock tags", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        instance = go.AddComponent<UnlockReveal>();
        instance.Build(font);
    }
    void Build(TMP_FontAsset font)
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32010;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        var aperture = DockInstrument.Block(transform, "Hangar aperture", Vector2.zero, new Vector2(405, 720), new Color(.01f, .025f, .03f, .48f));
        aperture.gameObject.AddComponent<RectMask2D>();
        card = DockInstrument.Block(aperture, "Unlock tag", Vector2.zero, new Vector2(364, 592), DockInstrument.Brass);
        rim = card.GetComponent<Image>();
        var face = DockInstrument.Block(card, "Enamel", Vector2.zero, new Vector2(358, 586), DockInstrument.Ink);
        DockInstrument.Block(face, "Tag slot", new Vector2(0, 280), new Vector2(35, 5), new Color(.01f, .02f, .025f));
        fade = aperture.gameObject.AddComponent<CanvasGroup>();
        fade.blocksRaycasts = false;
        heading = UnlockReceipt.Label(face, "", new Vector2(0, 254), new Vector2(240, 30), 17, DockInstrument.Brass, font);
        counter = UnlockReceipt.Label(face, "", new Vector2(135, 254), new Vector2(56, 20), 11, DockInstrument.Muted, font);
        var keys = UnlockReceipt.ItemKeys(MetaProgress.PendingUnlocks);
        total = MetaProgress.PendingUnlocks.Count;
        int cell = 0;
        foreach (var notice in MetaProgress.PendingUnlocks)
            cells.Add(RouteUnlocks.IsRoute(notice.key) ? -1 : cell++);
        studio = ItemPortraitStudio.Create(keys, 192);
        portrait = UnlockReceipt.Picture(face, studio, 0, new Vector2(0, 140), 192);
        districtStamp = UnlockReceipt.DistrictStamp(face, MetaProgress.PendingUnlocks[0], new Vector2(0, 140), 128);
        stamp = UnlockReceipt.Stamp(face, MetaProgress.PendingUnlocks[0], new Vector2(-136, 202), 35);
        title = UnlockReceipt.Label(face, "", new Vector2(0, 13), new Vector2(330, 54), 26, DockInstrument.Paper, font);
        category = UnlockReceipt.Label(face, "", new Vector2(0, -31), new Vector2(324, 20), 13, DockInstrument.Sea, font);
        description = UnlockReceipt.Label(face, "", new Vector2(0, -101), new Vector2(324, 100), 16, DockInstrument.Paper, font);
        description.textWrappingMode = TextWrappingModes.Normal;
        source = UnlockReceipt.Label(face, "", new Vector2(0, -198), new Vector2(324, 56), 14, DockInstrument.Muted, font);
        source.textWrappingMode = TextWrappingModes.Normal;
        footer = UnlockReceipt.Label(face, "", new Vector2(0, -264), new Vector2(80, 32), 24, DockInstrument.Brass, font);
        ShowCurrent();
    }
    void ShowCurrent()
    {
        var notice = MetaProgress.PendingUnlocks[0];
        currentKey = notice.key;
        title.text = notice.title;
        bool district = RouteUnlocks.IsRoute(notice.key);
        var accent = LevelRun.Accent(notice.area);
        heading.text = district ? "ROUTE OPEN" : "UNLOCKED";
        rim.color = district ? accent : DockInstrument.Brass;
        GalleryShelf.Exhibit item = null;
        bool known = !district && GalleryShelf.Resolve(notice.key, out item);
        category.text = district ? "NEXT FLIGHT" : known ? DecisionOverlay.Category(item.category).ToUpperInvariant() : "";
        description.text = district ? LevelRun.Description(notice.area) : known ? item.description : "";
        source.text = notice.Requirement;
        counter.text = (index + 1) + " / " + total;
        footer.text = DockInstrument.MenuKey();
        int at = index < cells.Count ? cells[index] : -1;
        portrait.enabled = at >= 0;
        if (at >= 0) portrait.uvRect = studio.Cell(at);
        stamp.sprite = districtStamp.sprite = Resources.Load<Sprite>("DockArt/Stamp" + notice.area);
        stamp.enabled = !district && stamp.sprite != null;
        districtStamp.enabled = district && districtStamp.sprite != null;
        districtStamp.color = Color.white;
        age = 0;
        closing = false;
        shownFrame = Time.frameCount;
        Present(0);
        ItemSound.Play("ready", .16f);
    }
    void Update()
    {
        footer.text = DockInstrument.MenuKey();
        if (Menu.GameplayPaused || CloudTransition.Busy) return;
        Animate(Time.unscaledDeltaTime);
        var controls = GameInput.Actions.PlayerControls;
        if (closing || Time.frameCount <= shownFrame || Menu.HandledInputThisFrame ||
            !(controls.Accept.WasPressedThisFrame() || controls.Back.WasPressedThisFrame())) return;
        Dismiss();
    }
    void Dismiss()
    {
        if (closing) return;
        // Persist the acknowledgement immediately, but keep the hangar blocked through the exit.
        MetaProgress.DismissUnlock(currentKey);
        closing = true;
        closingFrom = visibility;
        age = 0;
    }
    void Present(float value)
    {
        visibility = value;
        fade.alpha = value;
        card.anchoredPosition = Vector2.down * (28 * (1 - value));
        card.localScale = Vector3.one * Mathf.Lerp(.92f, 1, value);
    }
    void Animate(float delta)
    {
        age += delta;
        if (!closing)
        {
            Present(1 - Mathf.Pow(1 - Mathf.Clamp01(age / EnterDuration), 3));
            return;
        }
        Present(closingFrom * (1 - Mathf.SmoothStep(0, 1, age / ExitDuration)));
        if (age < ExitDuration) return;
        index++;
        if (MetaProgress.PendingUnlocks.Count == 0)
        {
            closedFrame = Time.frameCount;
            Destroy(gameObject);
        }
        else ShowCurrent();
    }
    void OnDestroy()
    {
        if (studio != null) Destroy(studio.gameObject);
        if (instance == this) instance = null;
    }
}
