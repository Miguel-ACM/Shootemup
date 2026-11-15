using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// Decision text is composited after the pixel filter.
public sealed class DecisionOverlay : MonoBehaviour
{
    public const float SkipWidth = 405, SkipHeight = 145;
    const float SkipDip = 26;
    RectTransform skip, skipFill;
    AngledStripGraphic skipGlass;
    CanvasGroup fade;
    bool dismissing;
    float age, dismissAge;
    const float EnterSeconds = .3f, DismissSeconds = .3f, Slide = 14;
    TMP_FontAsset font;
    RectTransform[] pagePads;
    HexagonGraphic[] pageCharge;
    TextMeshProUGUI[] pageArrows;
    readonly List<(AItem item, RectTransform plate, TextMeshProUGUI name, TextMeshProUGUI price, TextMeshProUGUI kind, CanvasGroup fade)> offers = new List<(AItem, RectTransform, TextMeshProUGUI, TextMeshProUGUI, TextMeshProUGUI, CanvasGroup)>();
    public void Add(AItem item)
    {
        var plate = Plate("Offer label", new Vector2(76, 43));
        var fade = plate.gameObject.AddComponent<CanvasGroup>();
        fade.alpha = 0;
        var name = Text(plate, "", 9);
        name.enableAutoSizing = true;
        name.fontSizeMin = 7;
        name.fontSizeMax = 9;
        name.rectTransform.sizeDelta = new Vector2(72, 24);
        name.rectTransform.anchoredPosition = new Vector2(0, 7);
        var price = Text(plate, "", 10);
        price.rectTransform.sizeDelta = new Vector2(72, 16);
        price.rectTransform.anchoredPosition = new Vector2(0, -13);
        var kind = Text(plate, "", 8);
        kind.rectTransform.sizeDelta = new Vector2(76, 15);
        kind.rectTransform.anchoredPosition = new Vector2(0, -86);
        kind.outlineWidth = .15f;
        kind.outlineColor = Color.black;
        var stripe = Plate("Rarity edge", new Vector2(76, 1.5f), plate);
        stripe.anchoredPosition = new Vector2(0, 21);
        stripe.GetComponent<Image>().color = AItem.GetRarityColor(item.rarity);
        offers.Add((item, plate, name, price, kind, fade));
    }

    public void SetOffer(int index, string price, bool affordable, string category = null)
    {
        var offer = offers[index];
        if (offer.item == null)
            return;
        offer.name.text = System.Text.RegularExpressions.Regex.Replace(offer.item.GetData()[0], @"\S+", "<nobr>$0</nobr>");
        offer.price.text = price;
        offer.price.color = affordable ? new Color(1, .78f, .4f) : new Color(1, .36f, .3f);
        offer.kind.text = category ?? Category(offer.item);
    }

    public static string Category(AItem.ItemType kind) => kind == AItem.ItemType.passiveWeapon ? "secondary" : kind.ToString();
    public static string Category(AItem item) => item is APactItem ? "pact" : item.Kind == AItem.ItemType.partner ? "passive" : Category(item.Kind);

    public void SetPages(int page, int count, float progress, int direction)
    {
        if (count <= 1)
            return;
        if (pagePads == null)
        {
            pagePads = new RectTransform[2];
            pageCharge = new HexagonGraphic[2];
            pageArrows = new TextMeshProUGUI[2];
            for (int i = 0; i < 2; i++)
            {
                pagePads[i] = Plate("Inventory page", new Vector2(34, 34));
                pagePads[i].GetComponent<Image>().enabled = false;
                var ring = new GameObject("Hold", typeof(RectTransform), typeof(CanvasRenderer), typeof(HexagonGraphic));
                ring.transform.SetParent(pagePads[i], false);
                var rect = (RectTransform)ring.transform;
                rect.sizeDelta = new Vector2(34, 34);
                pageCharge[i] = ring.GetComponent<HexagonGraphic>();
                pageCharge[i].thickness = 2;
                pageCharge[i].color = new Color(.65f, .85f, .9f);
                pageArrows[i] = Text(pagePads[i], i == 0 ? "<" : ">", 22);
            }
        }
        var camera = UnityEngine.Camera.main;
        for (int i = 0; i < 2; i++)
        {
            bool available = i == 0 ? page > 0 : page + 1 < count;
            pageArrows[i].color = available ? Color.white : new Color(.3f, .35f, .4f);
            pageCharge[i].progress = direction == (i == 0 ? -1 : 1) ? progress : 0;
            pageCharge[i].SetVerticesDirty();
            var world = new Vector3(i == 0 ? Camera.LeftLimit + 1 : Camera.RightLimit - 1, Mathf.Lerp(Camera.DownLimit, Camera.UpLimit, .35f), 0);
            var viewport = camera.WorldToViewportPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, new Vector2(viewport.x * Screen.width, viewport.y * Screen.height), null, out var point);
            pagePads[i].anchoredPosition = point;
        }
    }

    void LateUpdate()
    {
        if (reasonPlate != null)
            PlaceReason();
        if (dismissing)
        {
            dismissAge += Mathf.Min(Time.unscaledDeltaTime, .05f);
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(dismissAge / DismissSeconds));
            fade.alpha = Mathf.Min(fade.alpha, 1 - t);
            skip.anchoredPosition = new Vector2(0, SkipHeight * .5f - Slide * t);
            if (dismissAge >= DismissSeconds)
                Destroy(gameObject);
            return;
        }
        // The skip rises out of the bottom edge as it fades in.
        age += Mathf.Min(Time.unscaledDeltaTime, .05f);
        float enter = Mathf.SmoothStep(0, 1, Mathf.Clamp01(age / EnterSeconds));
        fade.alpha = enter;
        skip.anchoredPosition = new Vector2(0, SkipHeight * .5f - Slide * (1 - enter));
        var camera = UnityEngine.Camera.main;
        if (camera == null)
            return;
        foreach (var offer in offers)
        {
            bool show = offer.item != null && offer.item.Settled && !offer.item.Collected && !offer.item.IsDismissing;
            if (!show)
            {
                offer.plate.gameObject.SetActive(false);
                continue;
            }
            offer.fade.alpha = Mathf.MoveTowards(offer.fade.alpha, 1, Time.deltaTime * 6);
            // The camera renders into the low-resolution scene texture, so go through the viewport.
            var viewport = camera.WorldToViewportPoint(offer.item.transform.position);
            bool visible = !HangarHub.Active || (viewport.y >= 0 && viewport.y <= 1);
            offer.plate.gameObject.SetActive(visible);
            if (!visible)
                continue;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, new Vector2(viewport.x * Screen.width, viewport.y * Screen.height), null, out var point);
            var labelPoint = point + Vector2.up * 46;
            if (HangarHub.Active)
            {
                // Dockside prices travel with their plinths along the scrolling lane.
                offer.plate.anchoredPosition = labelPoint;
            }
            else
                offer.plate.anchoredPosition = new Vector2(Mathf.Clamp(point.x, -160, 160), Mathf.Clamp(labelPoint.y, -260, 320));
        }
    }

    /// <summary>
    /// Labels stay crisp above the pixel filter; while they are up, the player's ship is drawn over them again.
    /// </summary>
    public static void PlaceUnderShips(Canvas target, int order)
    {
        target.renderMode = RenderMode.ScreenSpaceOverlay;
        target.sortingOrder = order;
        target.gameObject.AddComponent<ShipsOverLabels>();
    }

    public void Build(TMP_FontAsset face, bool allowSkip = true)
    {
        font = face;
        var canvas = gameObject.AddComponent<Canvas>();
        PlaceUnderShips(canvas, 17);
        fade = gameObject.AddComponent<CanvasGroup>();
        fade.alpha = 0;
        fade.interactable = false;
        fade.blocksRaycasts = false;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        // The top edge steps down at thirty degrees from each corner.
        skip = Plate("Bottom skip", new Vector2(SkipWidth, SkipHeight));
        skip.anchorMin = skip.anchorMax = new Vector2(.5f, 0);
        skip.anchoredPosition = new Vector2(0, SkipHeight * .5f - Slide);
        skip.GetComponent<Image>().enabled = false;
        skipGlass = Strip(skip, "Glass", new Color(.025f, .045f, .07f, .45f), 0);
        var fill = Plate("Bottom-up skip charge", new Vector2(SkipWidth, 0), skip);
        fill.anchorMin = fill.anchorMax = new Vector2(.5f, 0);
        fill.pivot = new Vector2(.5f, 0);
        fill.GetComponent<Image>().enabled = false;
        fill.gameObject.AddComponent<RectMask2D>();
        skipFill = fill;
        var charge = Strip(fill, "Charge", new Color(.85f, .52f, .17f, .37f), 0);
        charge.rectTransform.anchorMin = charge.rectTransform.anchorMax = new Vector2(.5f, 0);
        charge.rectTransform.pivot = new Vector2(.5f, 0);
        charge.rectTransform.sizeDelta = new Vector2(SkipWidth, SkipHeight);
        Strip(skip, "Rim", new Color(1, .72f, .32f, .55f), 1.5f);
        var label = Text(skip, "SKIP", 12);
        label.rectTransform.anchoredPosition = new Vector2(0, -SkipDip * .5f);
        skip.gameObject.SetActive(allowSkip);
    }

    // Why this item is offered (BOSS REWARD, CARGO DELIVERY, ELITE HUNT COMPLETE), in the empty
    // left margin beside the flight area rather than over the offers (Miguel, 15 November 2026).
    // It is a compact row like the margin's other rows (132 x 30, 12-point type, the same dark
    // plate), in their column, marked by a bar in the reward's rarity colour.
    RectTransform reasonPlate;

    public void SetReason(string reason, Color colour)
    {
        var playfield = new GameObject("405 x 720 playfield", typeof(RectTransform)).GetComponent<RectTransform>();
        playfield.SetParent(transform, false);
        playfield.anchorMin = playfield.anchorMax = new Vector2(.5f, .5f);
        playfield.sizeDelta = new Vector2(405, 720);
        reasonPlate = Plate("Reward reason", new Vector2(132, 30), playfield);
        reasonPlate.anchorMin = reasonPlate.anchorMax = reasonPlate.pivot = new Vector2(0, .5f);
        reasonPlate.GetComponent<Image>().color = new Color(.07f, .09f, .09f, .94f);
        var bar = Plate("Reward rarity", Vector2.zero, reasonPlate);
        bar.anchorMin = Vector2.zero;
        bar.anchorMax = new Vector2(0, 1);
        bar.pivot = new Vector2(0, .5f);
        bar.sizeDelta = new Vector2(3, 0);
        bar.GetComponent<Image>().color = colour;
        var label = Text(reasonPlate, reason, 12);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(9, 2);
        label.rectTransform.offsetMax = new Vector2(-4, -2);
        label.alignment = TextAlignmentOptions.Left;
        label.enableAutoSizing = true;
        label.fontSizeMin = 8;
        label.fontSizeMax = 12;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.color = Color.Lerp(colour, Color.white, .35f);
        PlaceReason();
    }

    // Shrinks into narrow margins exactly like the instrument column beside it.
    void PlaceReason()
    {
        float margin = Mathf.Max(0, (Screen.width * 720f / Mathf.Max(1, Screen.height) - 405) * .5f);
        float scale = Mathf.Min(1, Mathf.Max(0, margin - 12) / 132);
        reasonPlate.localScale = Vector3.one * scale;
        reasonPlate.anchoredPosition = new Vector2(-12 - 132 * scale, 60);
    }

    public void SetSkip(float progress)
    {
        skipFill.sizeDelta = new Vector2(SkipWidth, SkipHeight * Mathf.SmoothStep(0, 1, Mathf.Clamp01(progress)));
        skipGlass.color = new Color(.025f, .045f, .07f, .45f);
        if (progress >= 1 && skipShown < 1)
            SoundManager.PlaySound("uiSkip", .1f);
        skipShown = progress;
    }

    float skipShown;

    AngledStripGraphic Strip(Transform parent, string title, Color color, float thickness)
    {
        var go = new GameObject(title, typeof(RectTransform), typeof(CanvasRenderer), typeof(AngledStripGraphic));
        go.transform.SetParent(parent, false);
        var graphic = go.GetComponent<AngledStripGraphic>();
        graphic.color = color;
        graphic.raycastTarget = false;
        graphic.dip = SkipDip;
        graphic.thickness = thickness;
        graphic.topGlow = thickness > 0 ? 0 : .06f;
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return graphic;
    }

    public void Dismiss()
    {
        dismissing = true;
    }

    RectTransform Plate(string title, Vector2 size, Transform parent = null)
    {
        var go = new GameObject(title, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent != null ? parent : transform, false);
        var image = go.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta = size;
        return r;
    }

    TextMeshProUGUI Text(Transform parent, string value, float size)
    {
        var go = new GameObject(value, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(.96f, .94f, .86f);
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = ((RectTransform)parent).sizeDelta - new Vector2(8, 0);
        return text;
    }
}

/// <summary>Keeps the ship-over-labels pass running while this label canvas is shown.</summary>
public sealed class ShipsOverLabels : MonoBehaviour
{
    void OnEnable() => PSXPresentation.LabelsShown++;
    void OnDisable() => PSXPresentation.LabelsShown--;
}
