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

    public void SetOffer(int index, string price, bool affordable)
    {
        var offer = offers[index];
        if (offer.item == null)
            return;
        offer.name.text = System.Text.RegularExpressions.Regex.Replace(offer.item.GetData()[0], @"\S+", "<nobr>$0</nobr>");
        offer.price.text = price;
        offer.price.color = affordable ? new Color(1, .78f, .4f) : new Color(1, .36f, .3f);
        offer.kind.text = offer.item is APactItem ? "pact" : offer.item.Kind == AItem.ItemType.active ? "active" : offer.item.Kind == AItem.ItemType.weapon || offer.item.Kind == AItem.ItemType.passiveWeapon ? "weapon" : "passive";
    }

    void LateUpdate()
    {
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
            offer.plate.gameObject.SetActive(show);
            if (!show)
                continue;
            offer.fade.alpha = Mathf.MoveTowards(offer.fade.alpha, 1, Time.deltaTime * 6);
            var viewport = camera.WorldToViewportPoint(offer.item.transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, new Vector2(viewport.x * Screen.width, viewport.y * Screen.height), null, out var point);
            offer.plate.anchoredPosition = new Vector2(Mathf.Clamp(point.x, -160, 160), Mathf.Clamp(point.y + 46, -260, 320));
        }
    }

    public void Build(TMP_FontAsset face, bool allowSkip = true)
    {
        font = face;
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 65;
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
