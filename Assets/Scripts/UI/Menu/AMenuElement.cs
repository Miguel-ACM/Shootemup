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

    protected virtual void Start()
    {
        menu = GetComponentInParent<Menu>();
        textElement = GetComponentInChildren<TextMeshProUGUI>();
        backgroundImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        SetNormalState();
    }

    protected virtual void LateUpdate()
    {
        if (menu.CurrentElement == this)
        {
            SetSelectedState();
        }
        else
        {
            SetNormalState();
        }
    }

    protected void SetSelectedState()
    {
        backgroundImage.color = new Color(.76f,.63f,.39f);
        textElement.color = new Color(.06f,.08f,.08f);
    }

    protected void SetNormalState()
    {
        backgroundImage.color = new Color(.10f,.13f,.13f,.85f);
        textElement.color = new Color(.76f,.81f,.76f);
    }

    // Abstract method to be implemented by derived classes
    public abstract void OnAccept();

    public void OnPointerEnter(PointerEventData eventData)
    {
        menu.setCurrentElement(this);
        SetSelectedState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button==PointerEventData.InputButton.Left)menu.TryAccept(this);
    }
}
