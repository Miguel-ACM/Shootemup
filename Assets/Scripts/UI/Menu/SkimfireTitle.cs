using UnityEngine;
using UnityEngine.UI;

public sealed class SkimfireTitle : MonoBehaviour
{
    [SerializeField] Texture2D wordmark = null;

    const float SlideSeconds = .28f;
    const float FireStart = .30f;
    const float Impact = FireStart + SlideSeconds;
    const float SlashSeconds = .19f;
    const float Duration = Impact + SlashSeconds;
    const float WordmarkWidth = 728f;
    const float TileHeight = 1f / 3f;

    static readonly Vector2 RestPosition = new Vector2(0, 430);
    static readonly Vector2 Diagonal = new Vector2(330, 232);
    static readonly Vector2 Seam = new Vector2(23, 6);
    static readonly Vector2 SlashDrift = new Vector2(24, 17);

    RawImage skim, fire, slash;
    RectTransform canvas;
    Menu menu;
    float age, fit;

    void Start()
    {
        if (wordmark == null)
        {
            enabled = false;
            return;
        }

        menu = GetComponent<Menu>();
        canvas = (RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform;

        var root = new GameObject("SKIMFIRE", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(transform, false);
        root.SetAsFirstSibling();
        root.anchorMin = root.anchorMax = new Vector2(.5f, .5f);
        root.anchoredPosition = RestPosition;

        // The first two rows use the same wordmark coordinates so the lettering lines up at rest.
        skim = CreateArtwork(root, "SKIM", new Rect(0, TileHeight * 2, 1, TileHeight));
        fire = CreateArtwork(root, "FIRE", new Rect(0, TileHeight, 1, TileHeight));
        float squareWidth = (wordmark.height / 3f) / wordmark.width;
        slash = CreateArtwork(root, "White slash", new Rect(0, 0, squareWidth, TileHeight));
    }

    void LateUpdate()
    {
        Vector2 size = canvas.rect.size;
        float nextFit = Mathf.Clamp01(Mathf.Min(size.x / 1080f, size.y / 1920f));
        bool resized = nextFit > 0 && nextFit != fit;
        if (resized)
        {
            fit = nextFit;
            var partSize = new Vector2(WordmarkWidth, WordmarkWidth * wordmark.height * TileHeight / wordmark.width) * fit;
            skim.rectTransform.sizeDelta = fire.rectTransform.sizeDelta = partSize;
        }

        // The title pauses scaled time. Wait for the curtain and for a visible menu.
        float previousAge = age;
        if (age < Duration && !CloudTransition.Busy && !menu.Suspended)
            age = Mathf.Min(age + Time.unscaledDeltaTime, Duration);
        if (resized || age != previousAge)
            Draw();

        if (previousAge < Impact && age >= Impact)
        {
            var cue = SoundCueCatalog.Find("titleLand");
            SoundManager.PlaySound(cue.Id, cue.Volume);
        }
    }

    void Draw()
    {
        PlacePart(skim, age / SlideSeconds, -Diagonal);
        PlacePart(fire, (age - FireStart) / SlideSeconds, Diagonal);

        float flash = Mathf.Clamp01((age - Impact) / SlashSeconds);
        float light = 1 - flash;
        slash.rectTransform.anchoredPosition = (Seam + SlashDrift * flash) * fit;
        slash.rectTransform.sizeDelta = Vector2.one * (fit * Mathf.Lerp(152, 208, flash));
        slash.color = new Color(1, 1, 1, age >= Impact ? light * light : 0);
    }

    void PlacePart(RawImage part, float progress, Vector2 entry)
    {
        progress = Mathf.Clamp01(progress);
        float travel = progress * (.35f + .65f * progress);
        part.rectTransform.anchoredPosition = entry * ((1 - travel) * fit);
        part.color = new Color(1, 1, 1, Mathf.SmoothStep(0, 1, progress * 3));
    }

    RawImage CreateArtwork(Transform parent, string label, Rect uv)
    {
        var image = new GameObject(label, typeof(RectTransform)).AddComponent<RawImage>();
        var rect = image.rectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        image.texture = wordmark;
        image.uvRect = uv;
        image.raycastTarget = false;
        image.color = Color.clear;
        return image;
    }
}
