using UnityEngine;
using UnityEngine.UI;

public sealed class SkimfireTitle : MonoBehaviour
{
    [SerializeField] Texture2D wordmark = null;

    const float FadeSeconds = .24f;
    const float WordmarkWidth = 728f;

    RawImage artwork;
    RectTransform canvas;
    float fadeStarted = -1;
    float fit;

    void Start()
    {
        if (wordmark == null)
        {
            enabled = false;
            return;
        }

        canvas = (RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform;
        artwork = new GameObject("SKIMFIRE", typeof(RectTransform)).AddComponent<RawImage>();
        artwork.texture = wordmark;
        artwork.raycastTarget = false;
        artwork.color = Color.clear;

        var rect = artwork.rectTransform;
        rect.SetParent(transform, false);
        rect.SetAsFirstSibling();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(0, 430);
    }

    void LateUpdate()
    {
        Vector2 size = canvas.rect.size;
        float nextFit = Mathf.Clamp01(Mathf.Min(size.x / 1080f, size.y / 1920f));
        if (nextFit > 0 && nextFit != fit)
        {
            fit = nextFit;
            artwork.rectTransform.sizeDelta = new Vector2(WordmarkWidth, WordmarkWidth * wordmark.height / wordmark.width) * fit;
        }

        if (artwork.color.a >= 1 || CloudTransition.Busy)
            return;

        // The menu pauses scaled time, and the return curtain can cover its first frames.
        if (fadeStarted < 0)
            fadeStarted = Time.unscaledTime;

        float alpha = Mathf.SmoothStep(0, 1, (Time.unscaledTime - fadeStarted) / FadeSeconds);
        artwork.color = new Color(1, 1, 1, alpha);
    }
}
