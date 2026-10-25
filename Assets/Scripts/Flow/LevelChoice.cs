using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A spatial choice over the actual playfield. The pilot remains visible through both panels.</summary>
public sealed class LevelChoice : MonoBehaviour
{
    public static bool Active { get; private set; }
    public LevelId Left { get; private set; }
    public LevelId Right { get; private set; }
    public RouteCharge Charge { get; } = new RouteCharge();
    Transform player;
    Action<LevelId> picked;
    CanvasGroup group;
    RectTransform frame;
    RectTransform[] fills,names;
    float[] displayed;
    public LevelId[] Options {get;private set;}
    float panelWidth;
    readonly AnimationCurve fillCurve=AnimationCurve.EaseInOut(0,0,1,1);
    TMP_FontAsset font;
    float age;
    bool committed;

    public static LevelChoice Open(Transform player,Action<LevelId> picked)
    {
        var choice=new GameObject("Choose the next route").AddComponent<LevelChoice>();
        choice.player=player;choice.picked=picked;
        LevelRun.GetChoices(out var left,out var right);choice.Left=left;choice.Right=right;
        choice.Options=LevelRun.GetOptions();choice.fills=new RectTransform[choice.Options.Length];choice.names=new RectTransform[choice.Options.Length];choice.displayed=new float[choice.Options.Length];
        CloudTransition.PrepareRoutes(choice.Options);
        choice.Build();Active=true;return choice;
    }

    void Build()
    {
        var dialog=FindAnyObjectByType<Dialog>();
        font=dialog!=null ? dialog.font : null;
        if(font==null)
            foreach(var label in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
                if(label.font!=null){font=label.font;break;}
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=4;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
        group=gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;group.alpha=0;
        frame=Rect(transform,"Route playfield",new Vector2(405,720),Vector2.zero);
        frame.gameObject.AddComponent<RectMask2D>();
        panelWidth=405f/Options.Length;
        for(int i=0;i<Options.Length;i++)
        {
            var level=Options[i];var accent=LevelRun.Accent(level);
            var half=Rect(frame,level.ToString(),new Vector2(panelWidth,720),new Vector2(-202.5f+panelWidth*(i+.5f),0));
            Paint(half,new Color(.025f,.045f,.07f,.43f));
            fills[i]=Rect(half,"Bottom-up route charge",new Vector2(panelWidth,0),Vector2.zero);
            fills[i].anchorMin=fills[i].anchorMax=new Vector2(.5f,0);
            fills[i].pivot=new Vector2(.5f,0);Paint(fills[i],new Color(accent.r,accent.g,accent.b,.37f));
            var edge=Rect(fills[i],"Leading edge",new Vector2(panelWidth,1.5f),Vector2.zero);
            edge.anchorMin=edge.anchorMax=new Vector2(.5f,1);Paint(edge,new Color(accent.r,accent.g,accent.b,.65f));
            names[i]=Text(half,level==LevelId.Fields ? "PLAINS" : level.ToString().ToUpperInvariant(),Options.Length==3 ? 17 : 24,Vector2.zero,Color.white).rectTransform;
            names[i].sizeDelta=new Vector2(panelWidth-14,48);
            if(Options.Length==3)
            {
                var label=names[i].GetComponent<TextMeshProUGUI>();label.enableAutoSizing=true;label.fontSizeMin=12;label.fontSizeMax=17;
                label.color=Color.Lerp(accent,Color.white,.5f);
            }
        }
        var centre=Rect(frame,"Neutral flight corridor",Options.Length==3 ? new Vector2(405,180) : new Vector2(405*RouteCharge.NeutralWidth,720),Options.Length==3 ? new Vector2(0,-270) : Vector2.zero);
        Paint(centre,new Color(.04f,.07f,.10f,.2f));
        for(int i=1;i<Options.Length;i++)
        {
            var rule=Rect(frame,"Route divider",new Vector2(1,720),new Vector2(-202.5f+i*panelWidth,0));Paint(rule,new Color(.78f,.88f,.95f,.22f));
        }
    }

    void Update()
    {
        // Match the game's pause state; pausing cannot finish a selection offscreen.
        if(Time.timeScale==0 || CloudTransition.Busy || player==null)return;
        age+=Time.deltaTime;
        float enter=Mathf.SmoothStep(0,1,age/.3f);group.alpha=enter;
        if(committed)return;
        if(age>=.3f)
        {
            float x=player.position.x/(player.position.x<0 ? -Camera.LeftLimit : Camera.RightLimit);
            if(Options.Length==2)Charge.Step(x,Time.deltaTime);
            else
            {
                float column=(x+1)*1.5f;
                bool seam=Mathf.Abs(column-1)<.09f || Mathf.Abs(column-2)<.09f;
                bool neutral=player.position.y<Mathf.Lerp(Camera.DownLimit,Camera.UpLimit,.25f) || seam;
                Charge.StepSelection(neutral ? 0 : Mathf.Clamp(Mathf.FloorToInt(column),0,2)+1,Time.deltaTime);
            }
        }
        for(int i=0;i<Options.Length;i++)
        {
            bool selected=Charge.Side==(Options.Length==2 ? (i==0 ? -1 : 1) : i+1);
            float target=selected ? fillCurve.Evaluate(Charge.Progress) : 0;
            displayed[i]=Mathf.Lerp(displayed[i],target,1-Mathf.Exp(-Time.deltaTime*20));
            if(Charge.Complete && selected)displayed[i]=1;
            fills[i].sizeDelta=new Vector2(panelWidth,720*displayed[i]);
            names[i].localScale=Vector3.one*(1+(selected ? .035f*Mathf.Sin(Charge.Progress*Mathf.PI) : 0));
        }
        if(Charge.Complete){committed=true;StartCoroutine(Commit());}
    }

    IEnumerator Commit()
    {
        yield return new WaitForSeconds(.1f);
        picked?.Invoke(Options[Options.Length==2 ? (Charge.Side<0 ? 0 : 1) : Charge.Side-1]);
    }

    static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 position)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
        var rect=go.GetComponent<RectTransform>();rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
    }
    static void Paint(RectTransform rect,Color color){var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;}
    TextMeshProUGUI Text(Transform parent,string text,float size,Vector2 pos,Color color)
    {
        var rect=Rect(parent,text,new Vector2(195,48),pos);
        var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.text=text;label.fontSize=size;
        label.alignment=TextAlignmentOptions.Center;label.color=color;label.raycastTarget=false;
        label.textWrappingMode=TextWrappingModes.NoWrap;return label;
    }
    void OnDestroy(){Active=false;}
}
