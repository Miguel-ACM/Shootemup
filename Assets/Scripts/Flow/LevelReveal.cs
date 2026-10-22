using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LevelReveal : MonoBehaviour
{
    public static void Show()
    {
        new GameObject("Level reveal title").AddComponent<LevelReveal>();
    }

    IEnumerator Start()
    {
        var canvas=gameObject.AddComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32000;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var group=gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
        var heading=Label("STAGE "+LevelRun.Stage.ToString("00"),24,new Vector2(0,96),new Color(.65f,.86f,.95f));
        var title=Label(LevelRun.CurrentName.ToUpperInvariant(),84,new Vector2(0,22),Color.white);
        title.characterSpacing=16;
        Label(LevelRun.Description(LevelRun.Current),19,new Vector2(0,-58),new Color(.85f,.91f,.96f));
        var root=GetComponent<RectTransform>();
        for(float t=0;t<3.7f;t+=Time.unscaledDeltaTime)
        {
            float enter=Mathf.SmoothStep(0,1,t/.7f);
            float leave=Mathf.SmoothStep(0,1,(t-2.6f)/1.1f);
            group.alpha=enter*(1-leave);
            root.anchoredPosition=new Vector2(0,Mathf.Lerp(-22,0,enter)+leave*24);
            title.characterSpacing=Mathf.Lerp(10,19,Mathf.Clamp01(t/3.7f));
            yield return null;
        }
        Destroy(gameObject);
    }

    TextMeshProUGUI Label(string text,float size,Vector2 position,Color color)
    {
        var go=new GameObject(text,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(transform,false);
        var r=go.GetComponent<RectTransform>();r.sizeDelta=new Vector2(1200,130);r.anchoredPosition=position;
        var label=go.GetComponent<TextMeshProUGUI>();label.text=text;label.fontSize=size;label.alignment=TextAlignmentOptions.Center;
        label.color=color;label.raycastTarget=false;
        label.outlineWidth=.16f;label.outlineColor=new Color32(8,20,28,255);
        return label;
    }
}
