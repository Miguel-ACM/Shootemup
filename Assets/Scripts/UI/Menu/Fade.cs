using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Fade : MonoBehaviour
{
    private RectTransform rect;
    private Graphic[] graphics;
    [SerializeField] private bool onStart = true;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        graphics = GetComponentsInChildren<Graphic>();
    }

    private void Start()
    {
        if (onStart)
        {
            FadeIn();
        }
    }

    public void FadeIn(float duration = 0.5f)
    {
        // Make sure element is visible
        gameObject.SetActive(true);

        // Stop all running animations
        //StopAllCoroutines();

        // Starting position slightly up
        Vector2 startPos = rect.anchoredPosition + new Vector2(0, 30);
        Vector2 endPos = rect.anchoredPosition;

        StartCoroutine(FadeRoutine(0f, 1f, startPos, endPos, duration));
    }

    public void FadeOut(float duration = 0.5f)
    {
        // Stop all running animations
        //StopAllCoroutines();

        // End position slightly down
        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = rect.anchoredPosition + new Vector2(0, -30);

        //StartCoroutine(FadeRoutine(1f, 0f, startPos, endPos, duration));
    }

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, Vector2 startPos, Vector2 endPos, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;

            // Smooth animation curve
            float smoothProgress = Mathf.SmoothStep(0, 1, normalizedTime);

            // Update alpha for all graphic elements
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, smoothProgress);
            foreach (Graphic graphic in graphics)
            {
                Color color = graphic.color;
                color.a = currentAlpha;
                graphic.color = color;
            }

            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, smoothProgress);

            yield return null;
        }

        // Ensure we end up exactly at target values
        foreach (Graphic graphic in graphics)
        {
            Color color = graphic.color;
            color.a = endAlpha;
            graphic.color = color;
        }
        rect.anchoredPosition = endPos;

        if (endAlpha == 0f)
        {
            gameObject.SetActive(false);
        }
    }
}