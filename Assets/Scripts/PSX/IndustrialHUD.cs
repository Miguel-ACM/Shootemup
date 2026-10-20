using UnityEngine;
using UnityEngine.UI;

/// <summary>Live industrial instruments shared by gameplay and the preview.</summary>
public sealed class IndustrialHUD : MonoBehaviour
{
    public Font font;
    public bool showcase;
    Shipcontroller player;
    Text score,chain;
    Image[] healthCells,manaCells,chainCells,bossCells;
    RectTransform bossPanel;
    RectTransform sidePanel;
    Boss boss;
    float nextScan,nextUpdate;
    float bottomLimit,topLimit;
    static readonly Color Steel=new Color(.73f,.77f,.72f), Amber=new Color(.83f,.62f,.30f), Cyan=new Color(.48f,.70f,.72f), Dark=new Color(.07f,.09f,.09f,.94f);

    void Start()
    {
        player=GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();
        // The legacy side-cover script accumulates offsets when the Game view resizes.
        foreach(var cover in FindObjectsByType<SideCoverController>(FindObjectsSortMode.None))cover.enabled=false;
        var legacyUI=GameObject.Find("UI");
        if(legacyUI!=null)foreach(var child in legacyUI.GetComponentsInChildren<Transform>(true))
            if(child.name=="CoverLeft" || child.name=="CoverRight")child.gameObject.SetActive(false);
        var camera=UnityEngine.Camera.main;
        float viewBottom=camera.transform.position.y-camera.orthographicSize;
        float viewHeight=camera.orthographicSize*2;
        // Let the cockpit (the hitbox) come right down to the bottom edge: its lowest point may
        // touch the screen border while the rest of the hull dips off screen.
        var hitbox=player.GetComponent<Collider2D>();
        bottomLimit=hitbox!=null ? viewBottom+(player.transform.position.y-hitbox.bounds.min.y) : viewBottom+1.75f;
        topLimit=viewBottom+viewHeight-.65f;
        var root=new GameObject("Industrial instruments",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));root.transform.SetParent(transform,false);
        root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=5;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
        var left=Panel(root.transform,"Left playfield mask",Color.black);left.anchorMin=Vector2.zero;left.anchorMax=new Vector2(.5f,1);left.offsetMin=Vector2.zero;left.offsetMax=new Vector2(-202.5f,0);
        var right=Panel(root.transform,"Right playfield mask",Color.black);right.anchorMin=new Vector2(.5f,0);right.anchorMax=Vector2.one;right.offsetMin=new Vector2(202.5f,0);right.offsetMax=Vector2.zero;
        var frame=new GameObject("405 x 720 playfield",typeof(RectTransform));frame.transform.SetParent(root.transform,false);
        var rect=frame.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(405,720);
        score=TextAt(frame.transform,"000000000",12,12,11,Steel);
        chain=TextAt(frame.transform,"",333,12,10,Amber);
        chainCells=Cells(frame.transform,333,28,6,7,Amber);
        sidePanel=Panel(frame.transform,"Side instruments",Dark);Place(sidePanel,-44,410,40,268);
        Rule(sidePanel,3,3,34,Steel);
        Place(Panel(sidePanel,"Health cross",Steel),7,16,11,3);
        Place(Panel(sidePanel,"Health cross stem",Steel),11,12,3,11);
        Place(Panel(sidePanel,"Reactor indicator",Cyan),25,13,7,8);
        healthCells=VerticalCells(sidePanel,8,34,24,10,7,Steel);
        manaCells=VerticalCells(sidePanel,25,34,24,7,7,Cyan);
        bossPanel=Panel(frame.transform,"Boss hull",Dark);Place(bossPanel,12,43,380,13);
        bossCells=Cells(bossPanel,5,4,32,9,Amber);bossPanel.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if(sidePanel!=null)sidePanel.anchoredPosition=new Vector2((float)Screen.width/Screen.height>.72f ? -44 : 6,-410);
        if(player==null || !player.enabled)return;
        var position=player.transform.position;position.y=Mathf.Clamp(position.y,bottomLimit,topLimit);player.transform.position=position;
    }

    void Update()
    {
        if(player==null || Time.unscaledTime<nextUpdate)return;nextUpdate=Time.unscaledTime+.1f;
        score.text=Score.CurrentScore.ToString("000000000");chain.text=Score.CurrentMultiplier>1 ? "x"+Score.CurrentMultiplier : "";
        Fill(healthCells,player.health/Mathf.Max(1,player.maxHealth),Steel);Fill(manaCells,player.mana/Mathf.Max(1,player.maxMana),Cyan);
        Fill(chainCells,Score.GetPercentageNextMultiplier(),Amber);
        if(Time.unscaledTime>nextScan){nextScan=Time.unscaledTime+1;boss=FindFirstObjectByType<Boss>();}
        bossPanel.gameObject.SetActive(boss!=null);
        if(boss!=null)Fill(bossCells,boss.HealthFraction,Amber);
    }

    static void Fill(Image[] cells,float value,Color color)
    {int count=Mathf.CeilToInt(Mathf.Clamp01(value)*cells.Length);for(int i=0;i<cells.Length;i++)cells[i].color=i<count ? color : new Color(.18f,.22f,.21f);}

    Image[] Cells(Transform parent,float x,float y,int count,float width,Color color)
    {
        var result=new Image[count];for(int i=0;i<count;i++){var cell=Panel(parent,"Segment "+i,color);Place(cell,x+i*(width+2),y,width,5);result[i]=cell.GetComponent<Image>();}return result;
    }
    Image[] VerticalCells(Transform parent,float x,float y,int count,float width,float height,Color color)
    {
        var result=new Image[count];for(int i=0;i<count;i++){var cell=Panel(parent,"Vertical segment "+i,color);Place(cell,x,y+(count-1-i)*(height+2),width,height);result[i]=cell.GetComponent<Image>();}return result;
    }
    void Rule(Transform parent,float x,float y,float width,Color color){Place(Panel(parent,"Panel seam",color),x,y,width,1);}
    RectTransform Panel(Transform parent,string name,Color color)
    {var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return go.GetComponent<RectTransform>();}
    Text TextAt(Transform parent,string value,float x,float y,int size,Color color)
    {
        var go=new GameObject(value,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(parent,false);
        var text=go.GetComponent<Text>();text.font=font;text.fontSize=size;text.text=value;text.color=color;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;
        Place(go.GetComponent<RectTransform>(),x,y,377,20);return text;
    }
    static void Place(RectTransform rect,float x,float y,float w,float h){rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);}
}
