using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// Decision text is composited after the pixel filter.
public sealed class DecisionOverlay : MonoBehaviour
{
    public const float SkipWidth=96,SkipHeight=48;
    RectTransform skip;
    Image skipFill;
    TMP_FontAsset font;
    readonly List<(AItem item,RectTransform plate,TextMeshProUGUI name,TextMeshProUGUI price)> offers=new List<(AItem,RectTransform,TextMeshProUGUI,TextMeshProUGUI)>();
    public void Add(AItem item)
    {
        var plate=Plate("Offer label",new Vector2(88,60));
        var name=Text(plate,"",10);name.enableAutoSizing=true;name.fontSizeMin=7;name.fontSizeMax=10;name.rectTransform.sizeDelta=new Vector2(82,32);name.rectTransform.anchoredPosition=new Vector2(0,10);
        var price=Text(plate,"",11);price.enableAutoSizing=true;price.fontSizeMin=8;price.fontSizeMax=11;price.textWrappingMode=TextWrappingModes.NoWrap;price.rectTransform.sizeDelta=new Vector2(82,20);price.rectTransform.anchoredPosition=new Vector2(0,-17);
        var stripe=Plate("Rarity edge",new Vector2(88,2),plate);stripe.anchoredPosition=new Vector2(0,29);stripe.GetComponent<Image>().color=AItem.GetRarityColor(item.rarity);
        offers.Add((item,plate,name,price));
    }
    public void SetOffer(int index,string price,bool affordable)
    {
        var offer=offers[index];if(offer.item==null)return;
        offer.name.text=System.Text.RegularExpressions.Regex.Replace(offer.item.GetData()[0],@"\S+","<nobr>$0</nobr>");offer.price.text=price;
        offer.price.color=affordable ? new Color(1,.78f,.4f) : new Color(1,.36f,.3f);
    }
    void LateUpdate()
    {
        var camera=UnityEngine.Camera.main;if(camera==null)return;
        foreach(var offer in offers)
        {
            bool show=offer.item!=null && !offer.item.Collected && !offer.item.IsDismissing;
            offer.plate.gameObject.SetActive(show);if(!show)continue;
            var viewport=camera.WorldToViewportPoint(offer.item.transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,new Vector2(viewport.x*Screen.width,viewport.y*Screen.height),null,out var point);
            offer.plate.anchoredPosition=new Vector2(Mathf.Clamp(point.x,-156,156),Mathf.Clamp(point.y+54,-300,320));
        }
    }
    public void Build(TMP_FontAsset face)
    {
        font=face;
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=65;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
        skip=Plate("Bottom skip",new Vector2(SkipWidth,SkipHeight));skip.anchorMin=skip.anchorMax=new Vector2(.5f,0);skip.anchoredPosition=new Vector2(0,SkipHeight*.5f);
        var fill=Plate("Bottom-up skip charge",new Vector2(SkipWidth,0),skip);fill.anchorMin=fill.anchorMax=new Vector2(.5f,0);fill.pivot=new Vector2(.5f,0);
        skipFill=fill.GetComponent<Image>();skipFill.color=new Color(.85f,.52f,.17f,.55f);
        var edge=Plate("Leading edge",new Vector2(SkipWidth,1.5f),fill);edge.anchorMin=edge.anchorMax=new Vector2(.5f,1);edge.GetComponent<Image>().color=new Color(1,.72f,.32f);
        Text(skip,"SKIP",12);
    }
    public void SetSkip(float progress)
    {
        skipFill.rectTransform.sizeDelta=new Vector2(SkipWidth,SkipHeight*Mathf.SmoothStep(0,1,Mathf.Clamp01(progress)));
        skip.GetComponent<Image>().color=progress>0 ? new Color(.04f,.06f,.075f,1) : Color.black;
    }
    RectTransform Plate(string title,Vector2 size,Transform parent=null)
    {
        var go=new GameObject(title,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent!=null ? parent : transform,false);
        var image=go.GetComponent<Image>();image.color=Color.black;image.raycastTarget=false;
        var r=go.GetComponent<RectTransform>();r.sizeDelta=size;return r;
    }
    TextMeshProUGUI Text(Transform parent,string value,float size)
    {
        var go=new GameObject(value,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
        var text=go.GetComponent<TextMeshProUGUI>();if(font!=null)text.font=font;text.text=value;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;text.color=new Color(.96f,.94f,.86f);text.raycastTarget=false;
        text.rectTransform.sizeDelta=((RectTransform)parent).sizeDelta-new Vector2(8,0);return text;
    }
}
