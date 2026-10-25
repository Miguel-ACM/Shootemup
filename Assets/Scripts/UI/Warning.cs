using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Boss warning clipped to the portrait playfield: seven yellow WARNING ribbons, each edged
/// with thin hazard stripes over a dark band, enter from fully outside the playfield on random sides
/// with a short ease-out burst, then cruise across and leave completely. Spacing, sides, speeds and
/// delays are re-rolled on every warning. Text never fades; only the dim wash eases in and out.</summary>
public class Warning : MonoBehaviour
{
    public TMP_FontAsset font;
    GameObject overlay;
    Canvas canvas;
    CanvasGroup group;
    RectTransform field;
    const int RowCount = 7;
    const float RowHeight = 56, Stripe = 4, FadeIn = .3f, FadeOut = .4f, HalfField = 203;
    const float EntryBoost = 150, EntryEase = 6;   // extra distance covered quickly on entry, and how fast it eases off
    static readonly Color Yellow = new Color(1f, .82f, .16f), YellowTop = new Color(1f, .94f, .5f), YellowBottom = new Color(1f, .7f, .08f);
    float duration;
    Image washImage;
    static Texture2D hazard;
    readonly RectTransform[] rows = new RectTransform[RowCount];
    readonly float[] widths = new float[RowCount], speeds = new float[RowCount], delays = new float[RowCount], heights = new float[RowCount];
    readonly bool[] leftward = new bool[RowCount];
    float elapsed;
    bool showing;
    public bool IsVisible => showing;
    public RectTransform ScreenRect => field;
    public void Start()
    {
        Build();
    }

    /// <summary>4-pixel-tall diagonal yellow/black tile, repeated along each ribbon edge.</summary>
    static Texture2D Hazard()
    {
        if (hazard != null)
            return hazard;
        hazard = new Texture2D(8, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, name = "Warning hazard stripe" };
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 8; x++)
                hazard.SetPixel(x, y, ((x + y) / 4) % 2 == 0 ? Yellow : new Color(.08f, .06f, .03f, 1));
        hazard.Apply();
        return hazard;
    }

    static RectTransform Child(string name, Transform parent, params System.Type[] components)
    {
        var go = new GameObject(name, components);
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        if (rect == null)
            rect = go.AddComponent<RectTransform>();
        return rect;
    }

    static void Stretch(RectTransform rect, float bottom, float top)
    {
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(.5f, 0);
        rect.offsetMin = new Vector2(0, bottom);
        rect.offsetMax = new Vector2(0, top);
    }

    TextMeshProUGUI Label(string name, Transform parent, Vector2 offset)
    {
        var rect = Child(name, parent, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(20, Stripe) + offset;
        rect.offsetMax = new Vector2(-20, -Stripe) + offset;
        var text = rect.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = string.Concat(System.Linq.Enumerable.Repeat("WARNING     ", 3)).TrimEnd();
        text.fontSize = 38;
        text.fontStyle = FontStyles.Normal;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.characterSpacing = 6;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        return text;
    }

    void Build()
    {
        if (overlay != null)
            return;
        var old = GetComponentsInChildren<TextMeshProUGUI>(true);
        if (font == null && old.Length > 0)
            font = old[0].font;
        foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            graphic.enabled = false;
        overlay = new GameObject("Playfield warning", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        group = overlay.GetComponent<CanvasGroup>();
        group.alpha = 0;
        group.blocksRaycasts = false;
        field = Child("Portrait clip", overlay.transform, typeof(RectTransform), typeof(RectMask2D));
        field.anchorMin = field.anchorMax = new Vector2(.5f, .5f);
        field.sizeDelta = new Vector2(405, 720);
        var wash = Child("Warning wash", field, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        wash.anchorMin = Vector2.zero;
        wash.anchorMax = Vector2.one;
        wash.offsetMin = wash.offsetMax = Vector2.zero;
        washImage = wash.GetComponent<Image>();
        washImage.color = new Color(.04f, .03f, .01f, 0);
        washImage.raycastTarget = false;
        for (int i = 0; i < rows.Length; i++)
        {
            var row = Child("Warning ribbon " + i, field, typeof(RectTransform));
            row.anchorMin = row.anchorMax = new Vector2(.5f, .5f);
            row.pivot = new Vector2(0, .5f);
            var band = Child("Band", row, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            band.anchorMin = Vector2.zero;
            band.anchorMax = Vector2.one;
            band.offsetMin = band.offsetMax = Vector2.zero;
            var bandImage = band.GetComponent<Image>();
            bandImage.color = new Color(.07f, .05f, .02f, .6f);
            bandImage.raycastTarget = false;
            var shadow = Label("Shadow", row, new Vector2(3, -3));
            shadow.color = new Color(.22f, .12f, 0, .9f);
            var text = Label("Text", row, Vector2.zero);
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(YellowTop, YellowTop, YellowBottom, YellowBottom);
            widths[i] = text.GetPreferredValues(text.text).x + 40 + 3;
            row.sizeDelta = new Vector2(widths[i], RowHeight);
            foreach (var edge in new[] { (0f, Stripe), (RowHeight - Stripe, RowHeight) })
            {
                var stripe = Child("Hazard edge", row, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                Stretch(stripe, edge.Item1, edge.Item2);
                var image = stripe.GetComponent<RawImage>();
                image.texture = Hazard();
                image.raycastTarget = false;
                image.uvRect = new Rect(0, 0, widths[i] / 8, 1);
            }
        }
        canvas.enabled = false;
    }

    public void blink()
    {
        Build();
        elapsed = 0;
        showing = true;
        group.alpha = 1;
        canvas.enabled = true;
        duration = 0;
        float pitch = 720f / RowCount;
        for (int i = 0; i < rows.Length; i++)
        {
            leftward[i] = Random.value < .5f;
            speeds[i] = Random.Range(340f, 520f);
            delays[i] = Random.Range(0f, .45f);
            heights[i] = 360 - pitch * (i + .5f) + Random.Range(-pitch * .18f, pitch * .18f);
            // Finished once the far end of the ribbon has crossed the whole playfield.
            duration = Mathf.Max(duration, delays[i] + (2 * HalfField + widths[i]) / speeds[i]);
        }
        AnimateRows();
    }

    void AnimateRows()
    {
        for (int i = 0; i < rows.Length; i++)
        {
            float t = Mathf.Max(0, elapsed - delays[i]);
            // Ease-out entry: a short burst of extra speed that settles into the cruising speed.
            float travel = t * speeds[i] + EntryBoost * (1 - Mathf.Exp(-t * EntryEase));
            // Left-moving ribbons start just past the right edge, right-moving ones just past the left.
            float x = leftward[i] ? HalfField - travel : -HalfField - widths[i] + travel;
            rows[i].anchoredPosition = new Vector2(Mathf.Round(x), heights[i]);
        }
    }

    void Update()
    {
        if (!showing)
            return;
        elapsed += Time.deltaTime;
        AnimateRows();
        var wash = washImage.color;
        wash.a = .35f * Mathf.SmoothStep(0, 1, elapsed / FadeIn) * Mathf.SmoothStep(0, 1, (duration - elapsed) / FadeOut);
        washImage.color = wash;
        if (elapsed >= duration)
        {
            showing = false;
            group.alpha = 0;
            canvas.enabled = false;
        }
    }

    void OnDestroy()
    {
        if (overlay != null)
            Destroy(overlay);
    }
}
