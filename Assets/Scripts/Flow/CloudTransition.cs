using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>One persistent, opaque cloud curtain. All animation uses real time, including paused menus.</summary>
public sealed class CloudTransition : MonoBehaviour
{
    static CloudTransition instance;
    public static bool Busy => instance != null && instance.busy;
    public const float RevealSeconds=3.2f;
    bool busy;
    RectTransform[] banks;
    CloudCurtainGraphic[] billows;
    float flowStartedAt;
    Canvas canvas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        instance = new GameObject("Cloud scene transitions").AddComponent<CloudTransition>();
        DontDestroyOnLoad(instance.gameObject);
        SceneManager.sceneLoaded += instance.SceneLoaded;
    }

    void Awake()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280,720);
        scaler.matchWidthOrHeight = 1;
        gameObject.AddComponent<GraphicRaycaster>();
        // Match the HUD's 405-wide viewport, leaving the side instruments visible.
        var viewport=new GameObject("Cloud playfield mask",typeof(RectTransform),typeof(RectMask2D));
        viewport.transform.SetParent(transform,false);
        var clip=viewport.GetComponent<RectTransform>();
        clip.anchorMin=new Vector2(.5f,0);clip.anchorMax=new Vector2(.5f,1);
        clip.sizeDelta=new Vector2(405,0);
        banks = new RectTransform[4];
        billows = new CloudCurtainGraphic[4];
        var artwork=Resources.Load<Texture2D>("CloudBillow");
        for (int i=0;i<banks.Length;i++)
        {
            var go = new GameObject("Cloud bank " + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(CloudCurtainGraphic));
            go.transform.SetParent(clip,false);
            banks[i] = go.GetComponent<RectTransform>();
            banks[i].sizeDelta = new Vector2(650,900);
            var graphic=go.GetComponent<CloudCurtainGraphic>();
            billows[i]=graphic;
            graphic.color = Color.Lerp(new Color(.88f,.92f,.95f),Color.white,i/3f);
            graphic.Configure(artwork,i);
        }
        canvas.enabled=false;
    }

    void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name=="L1") LevelEnvironment.Apply();
        // Supersede the old world-space clouds, which were behind the PSX output canvas.
        foreach(var spawner in FindObjectsByType<CloudSpawner>()) spawner.enabled=false;
    }

    public static void Load(string scene, Action covered = null)
    {
        if (Busy) return;
        instance.StartCoroutine(instance.Travel(scene,covered));
    }

    public static void Change(Action covered)
    {
        if (Busy) return;
        instance.StartCoroutine(instance.Travel(null,covered));
    }

    IEnumerator Travel(string scene, Action covered)
    {
        busy=true;
        flowStartedAt=Time.unscaledTime;
        // Clouds are an overlay: simulation and controls keep running throughout both legs.
        Time.timeScale=1;
        foreach(var bank in banks)bank.anchoredPosition=new Vector2(0,820);
        canvas.enabled=true;
        var previousPriority=Application.backgroundLoadingPriority;
        Application.backgroundLoadingPriority=ThreadPriority.Low;
        try
        {
            var preparing=StartCoroutine(LevelEnvironment.Prepare());
            AsyncOperation load=null;
            var preloading=scene!=null ? StartCoroutine(PreloadScene(scene,operation=>load=operation)) : null;
            yield return Move(true);
            yield return preparing;
            if(preloading!=null)yield return preloading;
            covered?.Invoke();
            if(load!=null)
            {
                load.allowSceneActivation=true;
                while(!load.isDone)yield return null;
            }
            // Start new scene components behind the moving cloud bank; no timed loading hold.
            yield return null;
            if(SceneManager.GetActiveScene().name=="L1")
            {
                Time.timeScale=1;
                LevelReveal.Show();
            }
            yield return Move(false);
        }
        finally
        {
            Application.backgroundLoadingPriority=previousPriority;
            canvas.enabled=false;
            busy=false;
            Time.timeScale=SceneManager.GetActiveScene().name=="Title" ? 0 : 1;
        }
    }

    IEnumerator PreloadScene(string scene,Action<AsyncOperation> started)
    {
        // A parked scene blocks Unity's async queue, so finish the scenery asset request first.
        while(!LevelEnvironment.SourceReady)yield return null;
        var load=SceneManager.LoadSceneAsync(scene);
        load.allowSceneActivation=false;
        started(load);
        while(load.progress<.9f)yield return null;
    }

    void Update()
    {
        if(!busy)return;
        // Texture billows keep travelling even when the opaque carrier must wait for activation.
        float distance=(Time.unscaledTime-flowStartedAt)*52;
        foreach(var graphic in billows)graphic.SetFlow(distance);
    }

    IEnumerator Move(bool cover)
    {
        float duration=cover ? 2.4f : RevealSeconds;
        for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
        {
            float t=elapsed/duration;
            for(int i=0;i<banks.Length;i++)
            {
                // The front bank leaves first, exposing the shaded banks behind it in succession.
                int order=cover ? i : banks.Length-1-i;
                float local=Mathf.Clamp01((t-order*.045f)/.865f);
                float p=cover ? Mathf.SmoothStep(0,1,local) : local*(.72f+.28f*local);
                float x=Mathf.Sin(t*Mathf.PI*2+i)*12*Mathf.Sin(t*Mathf.PI);
                banks[i].anchoredPosition=new Vector2(Mathf.Round(x/2)*2,
                    Mathf.Round((cover ? Mathf.Lerp(820,0,p) : Mathf.Lerp(0,-820,p))/2)*2);
            }
            yield return null;
        }
        // Every textured edge is below the viewport before the canvas switches off.
        foreach(var bank in banks)bank.anchoredPosition=new Vector2(0,cover ? 0 : -820);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded-=SceneLoaded;
        if(instance==this)instance=null;
    }
}

/// <summary>Overlapping PSX cloud sprites over an opaque backing; textured edges hide the handoff.</summary>
public sealed class CloudCurtainGraphic : MaskableGraphic
{
    Texture2D texture;
    Vector2 white;
    int variant;
    public float FlowDistance { get; private set; }
    public override Texture mainTexture => texture!=null ? texture : base.mainTexture;

    public void Configure(Texture2D artwork,int index)
    {
        texture=artwork;variant=index;
        var pixels=texture.GetPixels32();int best=-1;
        for(int i=0;i<pixels.Length;i++)
        {
            var p=pixels[i];int brightness=p.r+p.g+p.b;
            if(p.a==255 && brightness>best)
            {best=brightness;white=new Vector2((i%texture.width+.5f)/texture.width,(i/texture.width+.5f)/texture.height);}
        }
        SetMaterialDirty();SetVerticesDirty();
    }

    public void SetFlow(float distance)
    {
        float snapped=Mathf.Floor(distance/2)*2;
        if(snapped==FlowDistance)return;
        FlowDistance=snapped;SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if(texture==null)return;
        Quad(vh,new Vector2(-325,-375),new Vector2(325,375),white,white);
        // Fixed edge sprites preserve the scalloped silhouette of the incoming/outgoing carrier.
        for(int row=0;row<=8;row+=8)
        {
            float y=Mathf.Lerp(-355,355,row/8f);
            for(int col=0;col<4;col++)
            {
                float seed=row*17.13f+col*7.71f+variant*11.37f;
                float x=-330+col*220+Mathf.Sin(seed)*65;
                bool edge=row==0 || row==8;
                float width=350+Mathf.Sin(seed*1.9f)*65;
                float height=edge ? 190 : 195+Mathf.Cos(seed*.7f)*30;
                float cy=y+(edge ? 0 : Mathf.Sin(seed*1.3f)*18);
                bool mirror=(row+col+variant)%2==0;
                // Both exposed edges carry rounded billows; the lower row faces downwards.
                Vector2 uv0=new Vector2(mirror ? 1 : 0,row==0 ? 1 : 0);
                Vector2 uv1=new Vector2(mirror ? 0 : 1,row==0 ? 0 : 1);
                Quad(vh,new Vector2(x-width/2,cy-height/2),new Vector2(x+width/2,cy+height/2),uv0,uv1);
            }
        }
        // Recycle well outside the visible area, so the interior reads as a continuous fly-through.
        for(int row=0;row<14;row++)
        {
            float y=Mathf.Repeat(row*(1240f/14)-FlowDistance,1240)-620;
            for(int col=0;col<4;col++)
            {
                float seed=row*17.13f+col*7.71f+variant*11.37f;
                float x=-330+col*220+Mathf.Sin(seed)*65;
                float width=350+Mathf.Sin(seed*1.9f)*65;
                float height=195+Mathf.Cos(seed*.7f)*30;
                bool mirror=(row+col+variant)%2==0;
                Quad(vh,new Vector2(x-width/2,y-height/2),new Vector2(x+width/2,y+height/2),
                    new Vector2(mirror ? 1 : 0,0),new Vector2(mirror ? 0 : 1,1),true);
            }
        }
    }
    void Quad(VertexHelper vh,Vector2 lo,Vector2 hi,Vector2 uv0,Vector2 uv1,bool stream=false)
    {
        Color bottom=color,top=color;
        if(stream)
        {
            if(hi.y<=-450 || lo.y>=450)return;
            float from=lo.y,to=hi.y,v0=uv0.y,v1=uv1.y;
            lo.y=Mathf.Max(-450,lo.y);hi.y=Mathf.Min(450,hi.y);
            uv0.y=Mathf.Lerp(v0,v1,Mathf.InverseLerp(from,to,lo.y));
            uv1.y=Mathf.Lerp(v0,v1,Mathf.InverseLerp(from,to,hi.y));
            bottom.a*=Mathf.InverseLerp(450,290,Mathf.Abs(lo.y));
            top.a*=Mathf.InverseLerp(450,290,Mathf.Abs(hi.y));
        }
        int k=vh.currentVertCount;
        vh.AddVert(new Vector3(lo.x,lo.y),bottom,uv0);vh.AddVert(new Vector3(lo.x,hi.y),top,new Vector2(uv0.x,uv1.y));
        vh.AddVert(new Vector3(hi.x,hi.y),top,uv1);vh.AddVert(new Vector3(hi.x,lo.y),bottom,new Vector2(uv1.x,uv0.y));
        vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
    }
}
