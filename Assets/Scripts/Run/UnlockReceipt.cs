using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Each item gets a portrait, name and reason beneath the flight statistics. Long runs page automatically;
// the pilot can return immediately and inspect each release on the hangar turntable.
public sealed class UnlockReceipt : MonoBehaviour
{
    const int PageSize = 3;
    readonly List<GameObject> tiles = new List<GameObject>();
    ItemPortraitStudio studio;
    TextMeshProUGUI pagination;
    int pages, page = -1;
    float age;

    public static void Show(Transform parent, IList<ItemUnlockNotice> notices, TMP_FontAsset font, string hullRelease = null)
    {
        if (notices == null || notices.Count == 0) return;
        var go = new GameObject("Unlocked items", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<UnlockReceipt>().Build(notices, font, hullRelease);
    }

    void Build(IList<ItemUnlockNotice> notices, TMP_FontAsset font, string hullRelease)
    {
        var keys = new List<string>();
        foreach (var notice in notices) keys.Add(notice.key);
        studio = ItemPortraitStudio.Create(keys);
        Label(transform, "NEW ITEMS" + (hullRelease == null ? "" : "  /  " + hullRelease.Replace("Hull released", "NEW HULL")),
            new Vector2(0, -125), new Vector2(350, 22), 13, DockInstrument.Brass, font);
        pages = Mathf.CeilToInt(notices.Count / (float)PageSize);
        for (int i = 0; i < notices.Count; i++)
        {
            int slot = i % PageSize;
            var tile = DockInstrument.Block(transform, notices[i].title,
                new Vector2(0, -166 - slot * 57), new Vector2(360, 54), DockInstrument.Ink);
            Picture(tile, studio, i, new Vector2(-151, 0), 44);
            Label(tile, notices[i].title, new Vector2(26, 15), new Vector2(292, 18), 13, DockInstrument.Paper, font)
                .alignment = TextAlignmentOptions.MidlineLeft;
            var reason = Label(tile, notices[i].Requirement, new Vector2(26, -9), new Vector2(292, 30), 12, DockInstrument.Muted, font);
            reason.name = "Unlock reason";
            reason.alignment = TextAlignmentOptions.MidlineLeft;
            reason.textWrappingMode = TextWrappingModes.Normal;
            Stamp(tile, notices[i], new Vector2(-170, 19), 12);
            tiles.Add(tile.gameObject);
        }
        pagination = Label(transform, "", new Vector2(0, -317), new Vector2(340, 12), 10, DockInstrument.Muted, font);
        SetPage(0);
    }

    void SetPage(int value)
    {
        page = value;
        for (int i = 0; i < tiles.Count; i++) tiles[i].SetActive(i / PageSize == page);
        pagination.text = pages > 1 ? (page + 1) + " / " + pages : "";
    }
    void Update()
    {
        age += Time.unscaledDeltaTime;
        int next = Mathf.FloorToInt(age / 6) % pages;
        if (next != page) SetPage(next);
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
