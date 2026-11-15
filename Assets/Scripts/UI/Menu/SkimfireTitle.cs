using UnityEngine;
using UnityEngine.UI;

public sealed class SkimfireTitle : MonoBehaviour
{
    const float Diagonal = 27f;
    const float OrbitTime = 1.30f;
    const float ImpactTime = .48f;
    const float Pixel = 4.55f;
    const float HalfSpacing = 45.5f * Pixel;
    const int GhostCount = 3;
    const int SparkCount = 12;

    [SerializeField] Texture2D skim = null;
    [SerializeField] Texture2D fire = null;

    readonly RawImage[,] words = new RawImage[2, GhostCount + 1];
    readonly Image[] sparks = new Image[SparkCount];
    RectTransform artwork, canvas;
    float startTime = -1;
    float lastFit;
    bool settled;

    void Start()
    {
        if (skim == null || fire == null)
        {
            enabled = false;
            return;
        }

        canvas = (RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform;
        artwork = NewRect("SKIMFIRE", transform);
        artwork.anchoredPosition = new Vector2(0, 526);
        artwork.SetAsFirstSibling();

        for (int i = 0; i < sparks.Length; i++)
        {
            sparks[i] = NewRect("Impact spark", artwork).gameObject.AddComponent<Image>();
            sparks[i].raycastTarget = false;
        }

        // Order oldest trails behind both word halves. All are built once per title visit.
        for (int ghost = GhostCount; ghost >= 0; ghost--)
        {
            for (int part = 0; part < 2; part++)
            {
                var word = NewRect(part == 0 ? "SKIM" : "FIRE", artwork).gameObject.AddComponent<RawImage>();
                word.texture = part == 0 ? skim : fire;
                word.rectTransform.sizeDelta = new Vector2(word.texture.width, word.texture.height) * Pixel;
                word.raycastTarget = false;
                words[part, ghost] = word;
            }
        }

        Draw(0);
    }

    void LateUpdate()
    {
        Vector2 available = canvas.rect.size;
        float fit = Mathf.Clamp01(Mathf.Min(available.x / 1080f, available.y / 1920f));
        if (fit > 0 && fit != lastFit)
        {
            artwork.localScale = new Vector3(fit, fit, 1);
            lastFit = fit;
        }

        if (settled)
            return;

        if (startTime < 0)
        {
            if (CloudTransition.Busy)
                return;
            startTime = Time.unscaledTime;
        }

        // The title menu pauses scaled time, even while it is on screen.
        float age = Mathf.Min(Time.unscaledTime - startTime, OrbitTime + ImpactTime);
        Draw(age);
        settled = age >= OrbitTime + ImpactTime;
    }

    void Draw(float age)
    {
        float impact = Mathf.Clamp01((age - OrbitTime) / ImpactTime);
        float kick = Mathf.Sin(impact * Mathf.PI * 2) * Mathf.Pow(1 - impact, 2);
        for (int part = 0; part < 2; part++)
        {
            float side = part == 0 ? -1 : 1;
            var word = words[part, 0];
            if (age < OrbitTime)
                DrawOrbit(word, side, age, Color.white);
            else
            {
                float tilt = Diagonal + 22 * kick;
                SetPose(word.rectTransform, Direction(tilt) * (side * (HalfSpacing + 40 * kick)),
                    tilt, new Vector2(1 + .24f * kick, 1 - .48f * kick));
                word.color = Color.white;
            }

            for (int i = 1; i <= GhostCount; i++)
            {
                float sample = age - .046f * i;
                float alpha = (.40f - .09f * i) * (1 - Mathf.SmoothStep(0, 1, (age - OrbitTime) / .16f));
                DrawOrbit(words[part, i], side, sample, new Color(1, .63f, .24f, alpha));
            }
        }

        DrawImpact(impact);
    }

    static void DrawOrbit(RawImage word, float side, float age, Color tint)
    {
        float t = Mathf.Clamp01(age / OrbitTime);
        float sweep = t + .10f * Mathf.Sin(t * Mathf.PI * 2);
        float envelope = Mathf.Sin(t * Mathf.PI);
        float heading = Diagonal - 382 * (1 - sweep);
        Vector2 orbit = Direction(heading) * (HalfSpacing + 65 * envelope);
        orbit.y *= 1 - .47f * envelope;
        float bank = 105 * envelope * Mathf.Sin(t * Mathf.PI * 2 + side * .65f);
        float zoom = Mathf.Lerp(.67f, 1, Mathf.SmoothStep(0, 1, t / .25f));
        SetPose(word.rectTransform, orbit * side, Diagonal + bank,
            new Vector2(zoom * (1 + .06f * envelope), zoom * (1 - .18f * envelope)));
        tint.a *= Mathf.SmoothStep(0, 1, age / .16f);
        word.color = tint;
    }

    void DrawImpact(float progress)
    {
        float travel = Mathf.SmoothStep(0, 1, progress);
        float alpha = Mathf.SmoothStep(0, 1, progress / .05f) * Mathf.Pow(1 - progress, 2);
        for (int i = 0; i < sparks.Length; i++)
        {
            float heading = Diagonal + i * 147.5f;
            float reach = 110 + (i * 51 % 167);
            bool alongWord = i < 2;
            if (alongWord)
            {
                heading = Diagonal + i * 180;
                reach = 420;
            }

            var spark = sparks[i];
            spark.rectTransform.sizeDelta = new Vector2((alongWord ? 210 : 16 + i % 3 * 9) * (1 - progress),
                alongWord ? 5 : 6);
            SetPose(spark.rectTransform, Direction(heading) * (30 + reach * travel), heading, Vector2.one);
            spark.color = new Color(1, alongWord ? .42f : .68f, .15f, alpha);
        }
    }

    static RectTransform NewRect(string label, Transform parent)
    {
        var rect = new GameObject(label, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        return rect;
    }

    static void SetPose(RectTransform rect, Vector2 position, float degrees, Vector2 scale)
    {
        rect.anchoredPosition = position;
        rect.localRotation = Quaternion.Euler(0, 0, degrees);
        rect.localScale = new Vector3(scale.x, scale.y, 1);
    }

    static Vector2 Direction(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }
}
