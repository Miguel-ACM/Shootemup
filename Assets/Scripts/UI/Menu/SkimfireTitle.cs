using UnityEngine;
using UnityEngine.UI;

public sealed class SkimfireTitle : MonoBehaviour
{
    [SerializeField] Texture2D wordmark = null;

    const float SlideSeconds = .28f;
    const float FireStart = .30f;
    public const float JoinSeconds = FireStart + SlideSeconds;
    const float WordmarkWidth = 728f;
    const float TileHeight = .5f;

    static readonly Vector2 RestPosition = new Vector2(0, 430);
    static readonly Vector2 LetterLean = new Vector2(1, 3);
    static readonly Vector2 EntryOffset = LetterLean * 128;
    static readonly Vector2 Seam = new Vector2(23, 6);
    static readonly Vector2 SlashDrift = LetterLean * 24;

    TitleWord skim, fire;
    TitleSlashParticle slash;
    RectTransform canvas;
    bool entranceRequested, landSoundScheduled;
    float age, fit;
    double startDsp;

    public bool EntranceComplete => !isActiveAndEnabled || age >= JoinSeconds && !slash.HasParticle;
    public void Reveal(double joinAt)
    {
        startDsp = joinAt - JoinSeconds;
        var cue = SoundCueCatalog.Find("titleLand");
        landSoundScheduled = !double.IsNaN(joinAt) && SoundManager.TryScheduleSound(cue.Id, cue.Volume, joinAt);
        entranceRequested = true;
    }

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

        // Use the same clock as the music cue. Silent playback still works in real time.
        float previousAge = age;
        if (entranceRequested && age < JoinSeconds && !CloudTransition.Busy)
        {
            float nextAge = double.IsNaN(startDsp) ? age + Time.unscaledDeltaTime : (float)(AudioSettings.dspTime - startDsp);
            age = Mathf.Clamp(nextAge, age, JoinSeconds);
        }
        if (age >= JoinSeconds)
        {
            skim.FloatLetters(Time.unscaledTime);
            fire.FloatLetters(Time.unscaledTime);
        }
        if (resized || age != previousAge)
            Draw();

        if (previousAge < JoinSeconds && age >= JoinSeconds)
        {
            slash.Emit(Seam, SlashDrift);
            if (!landSoundScheduled)
            {
                var cue = SoundCueCatalog.Find("titleLand");
                SoundManager.PlaySound(cue.Id, cue.Volume);
            }
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

    TitleWord CreateArtwork(Transform parent, string label, Rect uv)
    {
        var image = new GameObject(label, typeof(RectTransform)).AddComponent<TitleWord>();
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

// Cut along the italic strokes. Each scanline retains its original texels and bevel.
public sealed class TitleWord : RawImage
{
    static readonly float[] silver = { 0, 39, 65.5f, 76.5f, 196 };
    static readonly float[] gold = { 0, 133.5f, 144.5f, 169.5f, 196 };
    float clock;
    public void FloatLetters(float t) { clock = t; SetVerticesDirty(); }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect = GetPixelAdjustedRect();
        bool first = uvRect.y > 0;
        var cuts = first ? silver : gold;
        for (int glyph = 0; glyph < 4; glyph++)
        {
            float bob = clock == 0 ? 0 : Mathf.Sin(clock * 1.7f + (glyph + (first ? 0 : 4)) * .82f) * rect.height * .014f;
            for (int row = 0; row < 43; row++)
            {
                float shear = Mathf.Floor((row - 3) * .32f);
                float left = glyph == 0 ? 0 : Mathf.Round(cuts[glyph] - shear) / 196;
                float right = glyph == 3 ? 1 : Mathf.Round(cuts[glyph + 1] - shear) / 196;
                float top = 1 - row / 43f, bottom = 1 - (row + 1) / 43f;
                int v = mesh.currentVertCount;
                void Corner(float x, float y) => mesh.AddVert(new Vector3(rect.x + rect.width*x, rect.y + rect.height*y + bob), color,
                    new Vector2(x, uvRect.y + y * uvRect.height));
                Corner(left,bottom); Corner(left,top); Corner(right,top); Corner(right,bottom);
                mesh.AddTriangle(v,v+1,v+2); mesh.AddTriangle(v+2,v+3,v);
            }
        }
    }
}
