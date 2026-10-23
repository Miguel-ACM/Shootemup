using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Centred screen-space item card; fading never depends on world position.</summary>
public class Dialog : MonoBehaviour
{
    public TMP_FontAsset font;
    Canvas canvas;
    CanvasGroup group;
    RectTransform panel;
    TextMeshProUGUI title,flavour,description;
    Image accent;
    GameObject overlay;
    float target;
    public bool IsVisible => group!=null && group.alpha>.001f;
    public RectTransform Panel => panel;

    public void Start(){Build();}
    void Build()
    {
        if(overlay!=null)return;
        var original=GetComponentsInChildren<TextMeshProUGUI>(true);
        if(font==null && original.Length>0)font=original[0].font;
        foreach(var graphic in GetComponentsInChildren<Graphic>(true))graphic.enabled=false;
        overlay=new GameObject("Item description overlay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));
        canvas=overlay.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=60;
        var scaler=overlay.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
        group=overlay.GetComponent<CanvasGroup>();group.alpha=0;group.interactable=false;group.blocksRaycasts=false;
        panel=Block(overlay.transform,"Item card",new Color(.045f,.06f,.065f,.97f));
        panel.anchorMin=panel.anchorMax=new Vector2(.5f,0);panel.pivot=new Vector2(.5f,0);panel.anchoredPosition=new Vector2(0,22);panel.sizeDelta=new Vector2(340,100);
        var edge=Block(panel,"Top accent",new Color(.73f,.62f,.39f));edge.anchorMin=new Vector2(0,1);edge.anchorMax=Vector2.one;edge.pivot=new Vector2(.5f,1);edge.sizeDelta=new Vector2(-2,2);edge.anchoredPosition=Vector2.zero;accent=edge.GetComponent<Image>();
        title=Text("Name",12,12,15,TextAlignmentOptions.Center,new Color(.88f,.9f,.84f));
        flavour=Text("Flavour",12,40,10,TextAlignmentOptions.Center,new Color(.49f,.56f,.55f));
        flavour.enabled=false;
        description=Text("Description",18,40,12,TextAlignmentOptions.TopLeft,new Color(.77f,.81f,.77f));
        var rect=description.rectTransform;rect.sizeDelta=new Vector2(-36,52);
        canvas.enabled=false;
    }
    void Update()
    {
        if(group==null)return;
        group.alpha=Mathf.MoveTowards(group.alpha,target,Time.unscaledDeltaTime/ .14f);
        canvas.enabled=target>0 || group.alpha>0;
        float width=Mathf.Min(340,Screen.width*720f/Mathf.Max(1,Screen.height)-24);
        panel.sizeDelta=new Vector2(Mathf.Max(180,width),100);
    }
    public void ShowItemDesc(string name,string detail,string effect,string rarity)
    {
        Build();title.text=name;flavour.text=detail;description.text=effect;
        accent.color=AItem.GetRarityColor(rarity);target=1;canvas.enabled=true;
    }
    public void Hide(){target=0;}
    void OnDestroy(){if(overlay!=null)Destroy(overlay);}
    RectTransform Block(Transform parent,string name,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return go.GetComponent<RectTransform>();
    }
    TextMeshProUGUI Text(string name,float inset,float y,float size,TextAlignmentOptions alignment,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));go.transform.SetParent(panel,false);
        var text=go.GetComponent<TextMeshProUGUI>();text.font=font;text.fontSize=size;text.color=color;text.alignment=alignment;text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.Normal;
        var rect=go.GetComponent<RectTransform>();rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-y);rect.sizeDelta=new Vector2(-inset*2,25);return text;
    }
}
