using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LevelReveal : MonoBehaviour
{
    TMP_FontAsset font;
    RectTransform content;

    public static void Show()
    {
        new GameObject("Level reveal title").AddComponent<LevelReveal>();
    }

    IEnumerator Start()
    {
        // Reuse the font already assigned to the game's item cards and menus.
        var dialog=FindAnyObjectByType<Dialog>();
        font=dialog!=null ? dialog.font : null;
        if(font==null)
            foreach(var label in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
                if(label.font!=null){font=label.font;break;}

        var canvas=gameObject.AddComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32000;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
        var group=gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
        var body=new GameObject("Title and eyebrow",typeof(RectTransform));body.transform.SetParent(transform,false);
        content=body.GetComponent<RectTransform>();content.sizeDelta=new Vector2(405,120);
        Label("Level "+LevelRun.Stage,11,new Vector2(0,40),new Color(.73f,.77f,.72f));
        Label(LevelRun.CurrentName.ToUpperInvariant(),40,new Vector2(0,-3),new Color(.9f,.92f,.87f));
        float duration=CloudTransition.RevealSeconds+1.3f;
        for(float t=0;t<duration;t+=Time.deltaTime)
        {
            float enter=Mathf.SmoothStep(0,1,t/.7f);
            float leave=Mathf.SmoothStep(0,1,(t-(duration-1.1f))/1.1f);
            group.alpha=enter*(1-leave);
            content.anchoredPosition=new Vector2(0,Mathf.Round(Mathf.Lerp(-12,0,enter)+leave*12));
            yield return null;
        }
        Destroy(gameObject);
    }

    void Label(string text,float size,Vector2 position,Color color)
    {
        var go=new GameObject(text,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(content,false);
        var r=go.GetComponent<RectTransform>();r.sizeDelta=new Vector2(390,60);r.anchoredPosition=position;
        var label=go.GetComponent<TextMeshProUGUI>();label.font=font;label.text=text;label.fontSize=size;
        label.alignment=TextAlignmentOptions.Center;label.color=color;label.raycastTarget=false;
        label.textWrappingMode=TextWrappingModes.NoWrap;
    }
}
