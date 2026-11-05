using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A persistent queue is acknowledged one tag at a time, only after arrival in the hangar.
// The key that closes one tag never buys an item or launches the ship underneath it.
public sealed class UnlockReveal : MonoBehaviour
{
    static UnlockReveal instance;
    static int closedFrame = -1;
    public static bool BlocksInput => instance != null || closedFrame == Time.frameCount;
    ItemPortraitStudio studio;
    RawImage portrait;
    Image stamp;
    TextMeshProUGUI title, source, footer;
    RectTransform card;
    CanvasGroup fade;
    int index, total, shownFrame;
    float age;

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
        card = DockInstrument.Block(aperture, "Unlock tag", Vector2.zero, new Vector2(364, 432), DockInstrument.Brass);
        var face = DockInstrument.Block(card, "Enamel", Vector2.zero, new Vector2(358, 426), DockInstrument.Ink);
        DockInstrument.Block(face, "Tag slot", new Vector2(0, 200), new Vector2(35, 5), new Color(.01f, .02f, .025f));
        fade = card.gameObject.AddComponent<CanvasGroup>();
        fade.blocksRaycasts = false;
        UnlockReceipt.Label(face, "UNLOCKED", new Vector2(0, 170), new Vector2(300, 30), 17, DockInstrument.Brass, font);
        var keys = new List<string>();
        foreach (var notice in MetaProgress.PendingUnlocks) keys.Add(notice.key);
        total = keys.Count;
        studio = ItemPortraitStudio.Create(keys, 192);
        portrait = UnlockReceipt.Picture(face, studio, 0, new Vector2(0, 26), 192);
        stamp = UnlockReceipt.Stamp(face, MetaProgress.PendingUnlocks[0], new Vector2(-136, 120), 35);
        title = UnlockReceipt.Label(face, "", new Vector2(0, -102), new Vector2(330, 44), 26, DockInstrument.Paper, font);
        source = UnlockReceipt.Label(face, "", new Vector2(0, -151), new Vector2(324, 38), 14, DockInstrument.Muted, font);
        footer = UnlockReceipt.Label(face, "", new Vector2(0, -194), new Vector2(320, 24), 13, DockInstrument.Brass, font);
        ShowCurrent();
    }
    void ShowCurrent()
    {
        var notice = MetaProgress.PendingUnlocks[0];
        title.text = notice.title;
        source.text = notice.Requirement;
        portrait.uvRect = studio.Cell(index);
        stamp.sprite = Resources.Load<Sprite>("DockArt/Stamp" + notice.area);
        stamp.enabled = stamp.sprite != null;
        age = 0;
        shownFrame = Time.frameCount;
        // Immediate, readable identity; only the tag's small settling motion is animated.
        fade.alpha = 1;
        ItemSound.Play("ready", .16f);
    }
    void Update()
    {
        age += Time.unscaledDeltaTime;
        card.anchoredPosition = Vector2.up * (12 * (1 - Mathf.SmoothStep(0, 1, age / .16f)));
        footer.text = (index + 1) + " / " + total + "     " + DockInstrument.MenuKey() + "  /  " + DockInstrument.MenuKey(true);
        var controls = GameInput.Actions.PlayerControls;
        if (Time.frameCount <= shownFrame || Menu.GameplayPaused || Menu.HandledInputThisFrame || CloudTransition.Busy ||
            !(controls.Accept.WasPressedThisFrame() || controls.Back.WasPressedThisFrame())) return;
        MetaProgress.DismissUnlock(MetaProgress.PendingUnlocks[0].key);
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
