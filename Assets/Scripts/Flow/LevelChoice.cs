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
    readonly RectTransform[] fills=new RectTransform[2];
    readonly TextMeshProUGUI[] hints=new TextMeshProUGUI[2];
    readonly RectTransform[] names=new RectTransform[2];
    readonly float[] displayed=new float[2];
    float age;
    bool committed;

    public static LevelChoice Open(Transform player,Action<LevelId> picked)
    {
        var choice=new GameObject("Choose the next route").AddComponent<LevelChoice>();
        choice.player=player;choice.picked=picked;
        LevelRun.GetChoices(out var left,out var right);choice.Left=left;choice.Right=right;
        choice.Build();Active=true;return choice;
    }

    void Build()
    {
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=4;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
        group=gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;group.alpha=0;
        frame=Rect(transform,"Route playfield",new Vector2(405,720),Vector2.zero);
        for(int i=0;i<2;i++)
        {
            bool left=i==0;var level=left ? Left : Right;var accent=LevelRun.Accent(level);
            var half=Rect(frame,level.ToString(),new Vector2(202.5f,720),new Vector2(left ? -101.25f : 101.25f,0));
            Paint(half,new Color(.025f,.045f,.07f,.43f));
            fills[i]=Rect(half,"Outside-in route charge",new Vector2(0,720),new Vector2(left ? -101.25f : 101.25f,0));
            fills[i].pivot=new Vector2(left ? 0 : 1,.5f);Paint(fills[i],new Color(accent.r,accent.g,accent.b,.37f));
            var edge=Rect(fills[i],"Leading edge",new Vector2(1.5f,720),Vector2.zero);
            edge.anchorMin=edge.anchorMax=new Vector2(left ? 1 : 0,.5f);Paint(edge,new Color(accent.r,accent.g,accent.b,.65f));
            Text(half,left ? "01 / LEFT ROUTE" : "02 / RIGHT ROUTE",12,new Vector2(0,124),accent);
            names[i]=Text(half,level.ToString().ToUpperInvariant(),32,new Vector2(0,88),Color.white).rectTransform;
            Text(half,LevelRun.Description(level),9,new Vector2(0,54),accent);
            hints[i]=Text(half,"HOLD THIS SIDE",11,new Vector2(0,-92),Color.white);
            Text(half,left ? "←" : "→",32,new Vector2(0,-132),accent);
        }
        var centre=Rect(frame,"Neutral flight corridor",new Vector2(405*RouteCharge.NeutralWidth,720),Vector2.zero);
        Paint(centre,new Color(.04f,.07f,.10f,.2f));
        var rule=Rect(frame,"Centre line",new Vector2(1,720),Vector2.zero);Paint(rule,new Color(.78f,.88f,.95f,.22f));
        Text(frame,"BOSS DEFEATED",12,new Vector2(0,286),new Color(.84f,.75f,.48f));
        Text(frame,"CHOOSE YOUR NEXT ROUTE",18,new Vector2(0,255),Color.white);
        Text(frame,"STEER TO A SIDE · HOLD TO COMMIT",10,new Vector2(0,-256),new Color(.79f,.86f,.91f));
        Text(frame,"CENTRE IS NEUTRAL",9,new Vector2(0,-276),new Color(.59f,.69f,.76f));
    }

    void Update()
    {
        // Match the game's pause state; pausing cannot finish a selection offscreen.
        if(Time.timeScale==0 || CloudTransition.Busy || player==null)return;
        age+=Time.deltaTime;
        float enter=Mathf.SmoothStep(0,1,age/.3f);group.alpha=enter;
        frame.localScale=Vector3.one*Mathf.Lerp(.97f,1,enter);
        if(committed)return;
        if(age>=.3f)Charge.Step(player.position.x/(player.position.x<0 ? -Camera.LeftLimit : Camera.RightLimit),Time.deltaTime);
        for(int i=0;i<2;i++)
        {
            bool selected=Charge.Side==(i==0 ? -1 : 1);
            float target=selected ? Charge.Progress : 0;
            displayed[i]=Mathf.Lerp(displayed[i],target,1-Mathf.Exp(-Time.deltaTime*20));
            if(Charge.Complete && selected)displayed[i]=1;
            fills[i].sizeDelta=new Vector2(202.5f*displayed[i],720);
            names[i].localScale=Vector3.one*(1+(selected ? .035f*Mathf.Sin(Charge.Progress*Mathf.PI) : 0));
            hints[i].text=selected ? (Charge.Complete ? "ROUTE LOCKED" : Mathf.CeilToInt(Charge.Progress*100)+"% · STAY ON COURSE") : "HOLD THIS SIDE";
        }
        if(Charge.Complete){committed=true;StartCoroutine(Commit());}
    }

    IEnumerator Commit()
    {
        yield return new WaitForSeconds(.1f);
        picked?.Invoke(Charge.Side<0 ? Left : Right);
    }

    static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 position)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
        var rect=go.GetComponent<RectTransform>();rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
    }
    static void Paint(RectTransform rect,Color color){var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;}
    static TextMeshProUGUI Text(Transform parent,string text,float size,Vector2 pos,Color color)
    {
        var rect=Rect(parent,text,new Vector2(parent==null ? 200 : 390,48),pos);
        // Each half's labels stay inside its own width, including the long subtitles.
        if(parent.name=="Fields" || parent.name=="Tunnel" || parent.name=="Snow" || parent.name=="Thunder" || parent.name=="City") rect.sizeDelta=new Vector2(195,48);
        var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.text=text;label.fontSize=size;
        label.alignment=TextAlignmentOptions.Center;label.color=color;label.raycastTarget=false;
        label.outlineWidth=.12f;label.outlineColor=new Color32(4,12,22,255);return label;
    }
    void OnDestroy(){Active=false;}
}
