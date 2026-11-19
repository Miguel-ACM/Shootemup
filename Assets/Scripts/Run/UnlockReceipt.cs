using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One release stays on the turntable until the pilot turns the page.
public sealed class UnlockReceipt : MonoBehaviour
{
    readonly List<GameObject> pages = new List<GameObject>();
    ItemPortraitStudio studio;
    TMP_FontAsset font;
    TextMeshProUGUI pagination;
    PromptDevice promptDevice;
    int page, heldDirection;

    public static void Show(Transform parent, FlightLog flight, TMP_FontAsset font)
    {
        var go = new GameObject("Flight receipt", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var receipt = go.AddComponent<UnlockReceipt>();
        receipt.font = font;
        receipt.Build(flight);
    }

    void Build(FlightLog flight)
    {
        var notices = flight.unlocks?.OrderByDescending(n => RouteUnlocks.IsRoute(n.key)).ToArray() ?? System.Array.Empty<ItemUnlockNotice>();
        var keys = ItemKeys(notices);
        if (keys.Count > 0) studio = ItemPortraitStudio.Create(keys, 144);
        for (int i = 0; i < notices.Length; i++)
        {
            var notice = notices[i];
            bool route = RouteUnlocks.IsRoute(notice.key);
            var card = NewPage(route ? "NEW ROUTE" : "NEW ITEM", notice.title, notice.Requirement);
            Portrait(card, studio, notices, i, Vector2.up * 16, 132);
        }

        if (flight.awards != null)
            foreach (var award in flight.awards)
                AddAward(award, notices);

        var details = Page("Flight log");
        Text(details, "FLIGHT LOG", 96, 24, 14, DockInstrument.Brass);
        Text(details, (flight.contract ?? flight.frame.ToString()) + "   /   " + flight.difficulty, 45, 36, 19, DockInstrument.Paper);
        Text(details, flight.route, -5, 55, 14, DockInstrument.Muted);
        Text(details, flight.bosses + (flight.bosses == 1 ? " boss" : " bosses") + "   /   " + flight.clean + " no-hit", -67, 24, 15, DockInstrument.Paper);
        Text(details, flight.hits + (flight.hits == 1 ? " hit" : " hits") + "   /   " + flight.peak + "x peak", -98, 24, 15, DockInstrument.Paper);
        Text(details, flight.Duration + " / " + flight.earned + " coins earned", -150, 32, 13, DockInstrument.Muted);

        if (pages.Count > 1)
            pagination = Label(transform, "", new Vector2(0, -248), new Vector2(240, 38), 16, DockInstrument.Paper, font);
        heldDirection = Direction(GameInput.Actions.PlayerControls.Move.ReadValue<Vector2>().x);
        SetPage(0);
    }

    RectTransform Page(string name)
    {
        var card = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        card.SetParent(transform, false);
        card.anchoredPosition = new Vector2(0, -23);
        card.sizeDelta = new Vector2(355, 356);
        card.gameObject.SetActive(false);
        pages.Add(card.gameObject);
        return card;
    }

    TextMeshProUGUI Text(RectTransform parent, string value, float y, float height, float size, Color color)
        => Label(parent, value, Vector2.up * y, new Vector2(342, height), size, color, font);

    RectTransform NewPage(string category, string title, string reason = null)
    {
        var card = Page(title);
        Text(card, category, 112, 24, 14, DockInstrument.Brass);
        Text(card, title, -79, 52, 24, DockInstrument.Paper);
        if (!string.IsNullOrWhiteSpace(reason))
            Text(card, reason, -150, 68, 14, DockInstrument.Muted).name = "Unlock reason";
        return card;
    }

    void AddAward(string award, ItemUnlockNotice[] notices)
    {
        if (string.IsNullOrWhiteSpace(award)) return;
        int split = award.IndexOf('/');
        string first = (split < 0 ? award : award.Substring(0, split)).Trim();
        string second = split < 0 ? "" : award.Substring(split + 1).Trim();
        if (first == "Blueprint" && notices.Any(n => n.title == second)) return;

        bool hull = first == "Hull released";
        bool district = second == "boss stamp" || second == "no-hit star";
        string category = hull ? "NEW HULL" : district ? second.ToUpperInvariant() : split < 0 ? "AWARD" : first.ToUpperInvariant();
        var card = NewPage(category, district || split < 0 ? first : second);
        string art = hull ? "Portrait" + second : district ? "Stamp" + (first == "Plains" ? "Fields" : first) : "Simulator";
        var sprite = Resources.Load<Sprite>("DockArt/" + art) ?? Resources.Load<Sprite>("DockArt/Wings");
        var image = DockInstrument.Block(card, "Award emblem", Vector2.up * 16, Vector2.one * (district ? 99 : 124), Color.white).GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
    }

    void SetPage(int value)
    {
        pages[page].SetActive(false);
        page = (value + pages.Count) % pages.Count;
        pages[page].SetActive(true);
        RefreshPrompt();
    }

    void RefreshPrompt()
    {
        if (pagination == null) return;
        promptDevice = InputPrompts.Device;
        var move = GameInput.Actions.PlayerControls.Move;
        string Icon(string direction) => InputPrompts.InlineKey(promptDevice == PromptDevice.Keyboard
            ? InputPrompts.Key(move, promptDevice, direction) : InputPrompts.KeyForControl("dpad/" + direction, promptDevice));
        pagination.text = Icon("left") + "   " + (page + 1) + " / " + pages.Count + "   " + Icon("right");
    }

    static int Direction(float x) => x > .55f ? 1 : x < -.55f ? -1 : 0;

    void Update()
    {
        if (pagination == null) return;
        if (promptDevice != InputPrompts.Device) RefreshPrompt();
        float x = GameInput.Actions.PlayerControls.Move.ReadValue<Vector2>().x;
        if (Mathf.Abs(x) < .3f) heldDirection = 0;
        int direction = Direction(x);
        if (direction == 0 || direction == heldDirection) return;
        heldDirection = direction;
        if (CloudTransition.Busy || Menu.GameplayPaused) return;
        SetPage(page + direction);
        SoundManager.PlaySound("uiConfirm", .08f);
    }

    void OnDestroy()
    {
        if (studio != null) Destroy(studio.gameObject);
    }

    public static TextMeshProUGUI Label(Transform parent, string text, Vector2 at, Vector2 size, float points, Color color, TMP_FontAsset font)
    {
        var label = new GameObject("Unlock lettering", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(parent, false);
        label.rectTransform.anchoredPosition = at;
        label.rectTransform.sizeDelta = size;
        if (font != null) label.font = font;
        label.spriteAsset = InputPrompts.InlineAsset;
        label.text = text;
        label.fontSizeMax = points;
        label.fontSizeMin = points * .8f;
        label.enableAutoSizing = true;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }
    public static List<string> ItemKeys(IEnumerable<ItemUnlockNotice> notices)
    {
        var keys = new List<string>();
        foreach (var notice in notices)
            if (!RouteUnlocks.IsRoute(notice.key)) keys.Add(notice.key);
        return keys;
    }
    // A new district wears its stamp in its own colour where an item would turn on its stand.
    public static Graphic Portrait(Transform parent, ItemPortraitStudio portraits, IReadOnlyList<ItemUnlockNotice> notices, int index, Vector2 at, float size)
    {
        var notice = notices[index];
        if (RouteUnlocks.IsRoute(notice.key)) return DistrictStamp(parent, notice, at, size);
        int cell = 0;
        for (int i = 0; i < index; i++)
            if (!RouteUnlocks.IsRoute(notices[i].key)) cell++;
        return Picture(parent, portraits, cell, at, size);
    }
    public static Image DistrictStamp(Transform parent, ItemUnlockNotice notice, Vector2 at, float size)
    {
        // Whole-pixel scale keeps the nine-pixel stamp crisp.
        var image = Stamp(parent, notice, at, Mathf.Max(9, Mathf.Floor(size * .85f / 9) * 9));
        image.color = LevelRun.Accent(notice.area);
        return image;
    }
    public static RawImage Picture(Transform parent, ItemPortraitStudio portraits, int index, Vector2 at, float size)
    {
        var image = new GameObject("Spinning item", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        image.transform.SetParent(parent, false);
        image.texture = portraits.Atlas;
        image.uvRect = portraits.Cell(index);
        image.rectTransform.anchoredPosition = at;
        image.rectTransform.sizeDelta = Vector2.one * size;
        image.raycastTarget = false;
        return image;
    }
    public static Image Stamp(Transform parent, ItemUnlockNotice notice, Vector2 at, float size)
    {
        var image = new GameObject("Area stamp", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.sprite = Resources.Load<Sprite>("DockArt/Stamp" + notice.area);
        image.enabled = image.sprite != null;
        image.rectTransform.anchoredPosition = at;
        image.rectTransform.sizeDelta = Vector2.one * size;
        image.raycastTarget = false;
        return image;
    }
}
