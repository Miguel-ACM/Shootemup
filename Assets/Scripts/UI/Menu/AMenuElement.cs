
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public abstract class AMenuElement : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    protected Menu menu;
    protected TextMeshProUGUI textElement;
    protected Image backgroundImage;
    protected RectTransform rectTransform;
    // Focus eases in and out in real time, so the paused menu animates too.
    float focus;
    Image marker;
    float textShift;
    static readonly Color Normal = new Color(.10f, .13f, .13f, .85f), Lit = new Color(.76f, .63f, .39f);
    static readonly Color NormalText = new Color(.76f, .81f, .76f), LitText = new Color(.06f, .08f, .08f);

    protected virtual void Start()
    {
        menu = GetComponentInParent<Menu>();
        textElement = GetComponentInChildren<TextMeshProUGUI>();
        backgroundImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        var go = new GameObject("Focus marker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);
        marker = go.GetComponent<Image>();
        marker.raycastTarget = false;
        var rect = marker.rectTransform;
        rect.anchorMin = new Vector2(0, .5f);
        rect.anchorMax = new Vector2(0, .5f);
        rect.pivot = new Vector2(0, .5f);
        rect.anchoredPosition = Vector2.zero;
        SetNormalState();
    }

    protected virtual void LateUpdate()
    {
        bool current = menu.CurrentElement == this;
        focus = Mathf.MoveTowards(focus, current ? 1 : 0, Time.unscaledDeltaTime * 9);
        Apply(Mathf.SmoothStep(0, 1, focus));
    }

    void Apply(float amount)
    {
        backgroundImage.color = Color.Lerp(Normal, Lit, amount);
        textElement.color = Color.Lerp(NormalText, LitText, amount);
        // Relative to the last shift, so a layout applied after Start keeps its own position.
        textElement.rectTransform.anchoredPosition += new Vector2(24 * amount - textShift, 0);
        textShift = 24 * amount;
        if (marker == null)
            return;
        // A light bar grows out of the row's left edge.
        marker.color = new Color(1, .9f, .66f, amount);
        marker.rectTransform.sizeDelta = new Vector2(14, rectTransform.rect.height * amount);
    }

    protected void SetSelectedState()
    {
        focus = 1;
        Apply(1);
    }

    protected void SetNormalState()
    {
        focus = 0;
        Apply(0);
    }

    // Abstract method to be implemented by derived classes
    public abstract void OnAccept();

    public void OnPointerEnter(PointerEventData eventData)
    {
        menu.setCurrentElement(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            menu.TryAccept(this);
    }
}
