using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public abstract class AMenuElement : MonoBehaviour, IPointerEnterHandler
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
        backgroundImage.color = Color.white;
        textElement.color = Color.black;
    }

    protected void SetNormalState()
    {
        backgroundImage.color = Color.clear;
        textElement.color = Color.white;
    }

    // Abstract method to be implemented by derived classes
    public abstract void OnAccept();

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("OnPointerEnter");
        menu.setCurrentElement(this);
        SetSelectedState();
    }
}