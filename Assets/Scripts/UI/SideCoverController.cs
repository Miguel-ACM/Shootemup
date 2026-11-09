using UnityEngine;

/// <summary>
/// Black side bars around the 9:16 playfield on wider screens, with the side item
/// columns moved in beside them. Every layout is computed from the original anchored
/// positions and the canvas's actual size, so switching between windowed, borderless
/// and fullscreen (or resizing) never accumulates offsets or uses a stale size.
/// </summary>
public class SideCoverController : MonoBehaviour
{
    [SerializeField] private GameObject blackRectLeft;
    [SerializeField] private GameObject blackRectRight;
    [SerializeField] private GameObject itemsLeft;
    [SerializeField] private GameObject itemsRight;
    RectTransform canvasRect;
    Vector2 originalLeft, originalRight;
    Vector2 appliedSize = new Vector2(-1, -1);

    void Awake()
    {
        var canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
        originalLeft = ((RectTransform)itemsLeft.transform).anchoredPosition;
        originalRight = ((RectTransform)itemsRight.transform).anchoredPosition;
    }

    // The canvas scaler resizes the canvas after a resolution change; follow the canvas,
    // not Screen, so the bars are laid out against the size that will actually be drawn.
    void LateUpdate()
    {
        if (canvasRect == null)
            return;
        Vector2 size = canvasRect.rect.size;
        if (size.x < 1 || size.y < 1 || size == appliedSize)
            return;
        appliedSize = size;
        var left = (RectTransform)itemsLeft.transform;
        var right = (RectTransform)itemsRight.transform;
        bool wide = size.x / size.y > 9f / 16f;
        float padding = wide ? (size.x - size.y * 9 / 16) / 2 : 0;
        left.anchoredPosition = originalLeft + new Vector2(padding, 0);
        right.anchoredPosition = originalRight - new Vector2(padding, 0);
        blackRectLeft.SetActive(wide);
        blackRectRight.SetActive(wide);
        if (wide)
        {
            ((RectTransform)blackRectLeft.transform).sizeDelta = new Vector2(padding, size.y);
            ((RectTransform)blackRectRight.transform).sizeDelta = new Vector2(padding, size.y);
        }
    }
}
