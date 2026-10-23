using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Decision text is composited after the pixel filter.
public sealed class DecisionOverlay : MonoBehaviour
{
    RectTransform skip;
    Image skipFill;
    TMP_FontAsset font;
    public void Build(TMP_FontAsset face)
    {
        font=face;
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=65;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
        skip=Plate("Bottom skip",new Vector2(132,30));skip.anchorMin=skip.anchorMax=new Vector2(.5f,0);skip.anchoredPosition=new Vector2(0,25);
        Text(skip,"SKIP",14).rectTransform.anchoredPosition=new Vector2(0,2);
        var fill=Plate("Hold progress",new Vector2(0,3),skip);fill.anchorMin=fill.anchorMax=new Vector2(0,0);fill.pivot=Vector2.zero;fill.anchoredPosition=new Vector2(5,3);
        skipFill=fill.GetComponent<Image>();skipFill.color=new Color(1,.72f,.32f);
    }
    public void SetSkip(float progress)
    {
        skipFill.rectTransform.sizeDelta=new Vector2(122*Mathf.Clamp01(progress),3);
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
