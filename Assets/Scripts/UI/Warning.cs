using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Three-second warning clipped to the portrait playfield: a dozen large rows slide in from
/// random sides at random speeds and delays (re-rolled on every warning) and keep travelling, so
/// the text never jumps back, over a dim wash that fades in and out.</summary>
public class Warning : MonoBehaviour
{
    public TMP_FontAsset font;
    GameObject overlay;
    Canvas canvas;
    CanvasGroup group;
    RectTransform field;
    // Rows sit one line apart (nearly touching) and fill the 720-unit playfield.
    const int RowCount=12;
    const float RowPitch=60,FadeIn=.25f,FadeOut=.35f,Duration=3;
    readonly RectTransform[] rows=new RectTransform[RowCount];
    readonly float[] widths=new float[RowCount],speeds=new float[RowCount],delays=new float[RowCount],lead=new float[RowCount];
    readonly bool[] leftward=new bool[RowCount];
    float elapsed;
    bool showing;
    public bool IsVisible=>showing;
    public RectTransform ScreenRect=>field;
    public void Start(){Build();}
    void Build()
    {
        if(overlay!=null)return;
        var old=GetComponentsInChildren<TextMeshProUGUI>(true);if(font==null && old.Length>0)font=old[0].font;
        foreach(var graphic in GetComponentsInChildren<Graphic>(true))graphic.enabled=false;
        overlay=new GameObject("Playfield warning",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));
        canvas=overlay.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
        var scaler=overlay.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
        group=overlay.GetComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;
        var go=new GameObject("Portrait clip",typeof(RectTransform),typeof(RectMask2D));go.transform.SetParent(overlay.transform,false);
        field=go.GetComponent<RectTransform>();field.anchorMin=field.anchorMax=new Vector2(.5f,.5f);field.sizeDelta=new Vector2(405,720);
        var wash=new GameObject("Warning wash",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));wash.transform.SetParent(field,false);
        var rect=wash.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        wash.GetComponent<Image>().color=new Color(.05f,.03f,.03f,.42f);wash.GetComponent<Image>().raycastTarget=false;
        for(int i=0;i<rows.Length;i++)
        {
            var row=new GameObject("Crossing warning "+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));row.transform.SetParent(field,false);
            var text=row.GetComponent<TextMeshProUGUI>();text.font=font;text.text=string.Concat(System.Linq.Enumerable.Repeat("WARNING   ",12));
            text.fontSize=42;text.fontStyle=FontStyles.Normal;text.textWrappingMode=TextWrappingModes.NoWrap;
            text.alignment=TextAlignmentOptions.MidlineLeft;text.color=i%2==0 ? new Color(.86f,.56f,.48f) : new Color(.56f,.34f,.30f);text.raycastTarget=false;
            rows[i]=row.GetComponent<RectTransform>();rows[i].anchorMin=rows[i].anchorMax=new Vector2(.5f,.5f);rows[i].pivot=new Vector2(0,.5f);rows[i].sizeDelta=new Vector2(3000,RowPitch);
            widths[i]=text.GetPreferredValues(text.text).x;
        }
        canvas.enabled=false;
    }
    public void blink()
    {
        Build();elapsed=0;showing=true;group.alpha=0;canvas.enabled=true;
        for(int i=0;i<rows.Length;i++)
        {
            leftward[i]=Random.value<.5f;speeds[i]=Random.Range(330f,560f);
            delays[i]=Random.Range(0f,.4f);lead[i]=Random.Range(0f,160f);
        }
        AnimateRows();
    }
    void AnimateRows()
    {
        for(int i=0;i<rows.Length;i++)
        {
            // Left-moving rows enter at the right edge, right-moving rows at the left edge.
            float travel=Mathf.Max(0,elapsed-delays[i])*speeds[i]+lead[i];
            float x=leftward[i] ? 203-travel : -203-widths[i]+travel;
            rows[i].anchoredPosition=new Vector2(x,(rows.Length-1)*RowPitch*.5f-i*RowPitch);
        }
    }
    void Update()
    {
        if(!showing)return;
        elapsed+=Time.deltaTime;AnimateRows();
        group.alpha=Mathf.SmoothStep(0,1,elapsed/FadeIn)*Mathf.SmoothStep(0,1,(Duration-elapsed)/FadeOut);
        if(elapsed>=Duration){showing=false;group.alpha=0;canvas.enabled=false;}
    }
    void OnDestroy(){if(overlay!=null)Destroy(overlay);}
}
