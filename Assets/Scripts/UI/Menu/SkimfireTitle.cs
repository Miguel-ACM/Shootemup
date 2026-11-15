using UnityEngine;
using UnityEngine.UI;

public sealed class SkimfireTitle : MonoBehaviour
{
    [SerializeField] Texture2D wordmark = null;

    const float DropSeconds = .24f;
    const float ReboundSeconds = .14f;
    const float DropHeight = 78f;
    const float ReboundHeight = 9f;
    const float WordmarkWidth = 728f;
    static readonly Vector2 RestPosition = new Vector2(0, 430);

    RawImage artwork;
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
        artwork = new GameObject("SKIMFIRE", typeof(RectTransform)).AddComponent<RawImage>();
        artwork.texture = wordmark;
        artwork.raycastTarget = false;
        artwork.color = Color.clear;

        var rect = artwork.rectTransform;
        rect.SetParent(transform, false);
        rect.SetAsFirstSibling();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = RestPosition + Vector2.up * DropHeight;
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

        const float Duration = DropSeconds + ReboundSeconds;
        if (age >= Duration || CloudTransition.Busy || menu.Suspended)
            return;

        // The title pauses scaled time. Wait for the curtain and for a visible menu.
        float previousAge = age;
        age = Mathf.Min(age + Time.unscaledDeltaTime, Duration);
        float drop = Mathf.Clamp01(age / DropSeconds);
        float rebound = Mathf.Clamp01((age - DropSeconds) / ReboundSeconds);
        float height = DropHeight * (1 - drop * drop) + ReboundHeight * 4 * rebound * (1 - rebound);
        artwork.rectTransform.anchoredPosition = RestPosition + Vector2.up * height;
        artwork.color = new Color(1, 1, 1, Mathf.SmoothStep(0, 1, drop * 2));

        if (previousAge < DropSeconds && age >= DropSeconds)
        {
            var cue = SoundCueCatalog.Find("titleLand");
            SoundManager.PlaySound(cue.Id, cue.Volume);
        }
    }
}
