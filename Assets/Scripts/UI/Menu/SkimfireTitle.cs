using UnityEngine;
using UnityEngine.UI;

public sealed class SkimfireTitle : MonoBehaviour
{
    [SerializeField] Texture2D wordmark = null;

    const float SlideSeconds = .28f;
    const float FireStart = .30f;
    const float Impact = FireStart + SlideSeconds;
    const float WordmarkWidth = 728f;
    const float TileHeight = .5f;

    static readonly Vector2 RestPosition = new Vector2(0, 430);
    static readonly Vector2 LetterLean = new Vector2(1, 3);
    static readonly Vector2 EntryOffset = LetterLean * 128;
    static readonly Vector2 Seam = new Vector2(23, 6);
    static readonly Vector2 SlashDrift = LetterLean * 24;

    RawImage skim, fire;
    TitleSlashParticle slash;
    RectTransform canvas;
    bool entranceRequested;
    float age, fit;

    public bool EntranceComplete => !isActiveAndEnabled || age >= Impact && !slash.HasParticle;
    public void Reveal() => entranceRequested = true;

    void Start()
    {
        if (wordmark == null)
        {
            enabled = false;
            return;
        }

        canvas = (RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform;

        var root = new GameObject("SKIMFIRE", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(transform, false);
        root.SetAsFirstSibling();
        root.anchorMin = root.anchorMax = new Vector2(.5f, .5f);
        root.anchoredPosition = RestPosition;

        // Both rows use the same wordmark coordinates so the lettering lines up at rest.
        skim = CreateArtwork(root, "SKIM", new Rect(0, TileHeight, 1, TileHeight));
        fire = CreateArtwork(root, "FIRE", new Rect(0, 0, 1, TileHeight));
        slash = new GameObject("White slash particle", typeof(RectTransform)).AddComponent<TitleSlashParticle>();
        slash.rectTransform.SetParent(root, false);
        slash.rectTransform.anchorMin = slash.rectTransform.anchorMax = new Vector2(.5f, .5f);
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
            slash.rectTransform.localScale = new Vector3(fit, fit, 1);
        }

        // The title menu pauses scaled time.
        float previousAge = age;
        if (entranceRequested && age < Impact && !CloudTransition.Busy)
            age = Mathf.Min(age + Time.unscaledDeltaTime, Impact);
        if (resized || age != previousAge)
            Draw();

        if (previousAge < Impact && age >= Impact)
        {
            slash.Emit(Seam, SlashDrift);
            var cue = SoundCueCatalog.Find("titleLand");
            SoundManager.PlaySound(cue.Id, cue.Volume);
        }
    }

    void Draw()
    {
        PlacePart(skim, age / SlideSeconds, -EntryOffset);
        PlacePart(fire, (age - FireStart) / SlideSeconds, EntryOffset);
    }

    void PlacePart(RawImage part, float progress, Vector2 entry)
    {
        progress = Mathf.Clamp01(progress);
        float travel = progress * (.35f + .65f * progress);
        part.rectTransform.anchoredPosition = entry * ((1 - travel) * fit);
        part.color = new Color(1, 1, 1, Mathf.SmoothStep(0, 1, progress));
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
