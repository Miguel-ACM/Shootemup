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
    const float OffscreenY=960;
    bool busy;
    RectTransform[] banks;
    CloudCurtainGraphic[] billows;
    float flowStartedAt;
    Canvas canvas;
    Coroutine scenePreparation, routePreparation;
    string preparedScene;
    AsyncOperation preparedLoad;
    readonly AudioClip[] routeMusic=new AudioClip[2];
    bool scenePreparing, routesPreparing;

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
            banks[i].sizeDelta = new Vector2(650,1100);
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
        // The menu is already usable: spend idle time preparing gameplay, even while paused.
        if(scene.name=="Title" && !busy)PrepareGameplay();
        else if(scene.name=="L1" && !busy)StartCoroutine(LevelEnvironment.Prepare());
    }

    void PrepareGameplay()
    {
        if(scenePreparation==null)scenePreparation=StartCoroutine(PreloadScene("L1"));
    }

    public static void PrepareRoutes(LevelId left,LevelId right)
    {
        if(instance.routePreparation!=null)instance.StopCoroutine(instance.routePreparation);
        instance.routePreparation=instance.StartCoroutine(instance.WarmRoutes(left,right));
    }

    IEnumerator WarmRoutes(LevelId left,LevelId right)
    {
        routesPreparing=true;
        try
        {
            yield return LevelEnvironment.Prepare();
            // Scene objects stay on the main thread, but each build yields in small batches.
            yield return LevelEnvironment.PrepareRoute(left);
            yield return WarmRouteMusic(left,0);
            yield return null;
            yield return LevelEnvironment.PrepareRoute(right);
            yield return WarmRouteMusic(right,1);
        }
        finally { routesPreparing=false; }
    }

    IEnumerator WarmRouteMusic(LevelId level,int index)
    {
        var request=Resources.LoadAsync<AudioClip>("Music/"+level.ToString().ToLowerInvariant());
        yield return request;
        var clip=request.asset as AudioClip;
        routeMusic[index]=clip; // Keep the loaded clip alive until the route's music source owns it.
        if(clip==null)yield break;
        if(clip.loadState==AudioDataLoadState.Unloaded)clip.LoadAudioData();
        while(clip.loadState==AudioDataLoadState.Loading)yield return null;
    }

    public static void Load(string scene, Action covered = null)
    {
        if (Busy) return;
        LevelEnvironment.FadeOutStorm();
        // Leaving gameplay supersedes route preparation; do not leave resource requests behind
        // a newly parked scene load, or build scenery into the menu after its level unloads.
        if(instance.routePreparation!=null)instance.StopCoroutine(instance.routePreparation);
        instance.routePreparation=null;instance.routesPreparing=false;
        instance.StartCoroutine(instance.Travel(scene,covered));
    }

    public static void Change(Action covered)
    {
        if (Busy) return;
        LevelEnvironment.FadeOutStorm();
        instance.StartCoroutine(instance.Travel(null,covered));
    }

    public static void FadeOutAudio(GameObject root,AudioClip[] clips,float seconds)
    {
        instance.StartCoroutine(instance.FadeAudio(root,clips,seconds));
    }

    IEnumerator FadeAudio(GameObject root,AudioClip[] clips,float seconds)
    {
        var sources=root.GetComponentsInChildren<AudioSource>();
        var volumes=new float[sources.Length];
        for(int i=0;i<sources.Length;i++)volumes[i]=sources[i].volume;
        try
        {
            for(float elapsed=0;elapsed<seconds;elapsed+=Time.unscaledDeltaTime)
            {
                float gain=1-Mathf.SmoothStep(0,1,elapsed/seconds);
                for(int i=0;i<sources.Length;i++)
                    if(sources[i]!=null)sources[i].volume=volumes[i]*gain;
                yield return null;
            }
        }
        finally
        {
            if(root!=null)Destroy(root);
            foreach(var clip in clips)if(clip!=null)Destroy(clip);
        }
    }

    IEnumerator Travel(string scene, Action covered)
    {
        busy=true;
        flowStartedAt=Time.unscaledTime;
        // Clouds are an overlay: simulation and controls keep running throughout both legs.
        Time.timeScale=1;
        foreach(var bank in banks)bank.anchoredPosition=new Vector2(0,OffscreenY);
        canvas.enabled=true;
        var previousPriority=Application.backgroundLoadingPriority;
        Application.backgroundLoadingPriority=ThreadPriority.Low;
        try
        {
            var preparing=StartCoroutine(LevelEnvironment.Prepare());
            if(scene!=null && scenePreparation==null)
                scenePreparation=StartCoroutine(PreloadScene(scene));
            yield return Move(true);
            yield return preparing;
            if(scene!=null)
            {
                while(scenePreparing)yield return null;
                // A speculative load cannot be cancelled. Drain it under cover if the target changed.
                if(preparedScene!=scene)
                {
                    preparedLoad.allowSceneActivation=true;
                    while(!preparedLoad.isDone)yield return null;
                    yield return PreloadScene(scene);
                }
            }
            else while(routesPreparing)yield return null;
            covered?.Invoke();
            if(scene!=null && preparedLoad!=null)
            {
                var load=preparedLoad;
                preparedLoad=null;preparedScene=null;scenePreparation=null;
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
            if(SceneManager.GetActiveScene().name=="Title")PrepareGameplay();
        }
    }

    IEnumerator PreloadScene(string scene)
    {
        scenePreparing=true;
        try
        {
            // A parked scene blocks Unity's async queue. Finish shared asset preparation first.
            yield return LevelEnvironment.Prepare();
            var load=SceneManager.LoadSceneAsync(scene);
            load.allowSceneActivation=false;
            load.priority=-1;
            preparedScene=scene;preparedLoad=load;
            while(load.progress<.9f)yield return null;
        }
        finally { scenePreparing=false; }
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
                    Mathf.Round((cover ? Mathf.Lerp(OffscreenY,0,p) : Mathf.Lerp(0,-OffscreenY,p))/2)*2);
            }
            yield return null;
        }
        // Every textured edge is below the viewport before the canvas switches off.
        foreach(var bank in banks)bank.anchoredPosition=new Vector2(0,cover ? 0 : -OffscreenY);
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
    const int BackingColumns=325;
    readonly float[] backingTop=new float[BackingColumns];
    readonly float[] backingBottom=new float[BackingColumns];
    struct EdgeSprite
    {
        public Vector2 lo,hi,uv0,uv1;
    }
    readonly EdgeSprite[] edges=new EdgeSprite[8];
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
        for(int edge=0;edge<2;edge++)
        for(int col=0;col<4;col++)
        {
            int row=edge*8;
            float seed=row*17.13f+col*7.71f+variant*11.37f;
            float x=-330+col*220+Mathf.Sin(seed)*65;
            float width=350+Mathf.Sin(seed*1.9f)*65;
            float y=edge==0 ? -450 : 450;
            bool mirror=(row+col+variant)%2==0;
            edges[edge*4+col]=new EdgeSprite {
                lo=new Vector2(x-width/2,y-95),hi=new Vector2(x+width/2,y+95),
                uv0=new Vector2(mirror ? 1 : 0,edge==0 ? 1 : 0),
                uv1=new Vector2(mirror ? 0 : 1,edge==0 ? 0 : 1)
            };
        }
        // Join the backing INSIDE opaque sprite pixels. A rectangular backing exposed its
        // straight edge through the transparent valleys between the rounded cloud tops.
        for(int column=0;column<BackingColumns;column++)
        {
            float x=-325+column*2;
            backingTop[column]=FindBackingSeam(pixels,x,1);
            backingBottom[column]=FindBackingSeam(pixels,x,-1);
        }
        SetMaterialDirty();SetVerticesDirty();
    }

    float FindBackingSeam(Color32[] pixels,float x,int side)
    {
        int solidRun=0;
        for(float distance=544;distance>=356;distance-=2)
        {
            bool solid=true;
            // Include the full strip width, so its corners cannot protrude into a valley.
            for(int sample=0;sample<=4 && solid;sample++)
            {
                bool covered=false;
                float sx=x+sample*.5f,y=distance*side;
                for(int i=side>0 ? 4 : 0;i<(side>0 ? 8 : 4);i++)
                {
                    var sprite=edges[i];
                    if(sx<sprite.lo.x || sx>sprite.hi.x || y<sprite.lo.y || y>sprite.hi.y)continue;
                    float u=Mathf.Lerp(sprite.uv0.x,sprite.uv1.x,Mathf.InverseLerp(sprite.lo.x,sprite.hi.x,sx));
                    float v=Mathf.Lerp(sprite.uv0.y,sprite.uv1.y,Mathf.InverseLerp(sprite.lo.y,sprite.hi.y,y));
                    int px=Mathf.Clamp((int)(u*texture.width),0,texture.width-1);
                    int py=Mathf.Clamp((int)(v*texture.height),0,texture.height-1);
                    // The downsampled sprite's solid interior is mostly alpha 253.
                    if(pixels[py*texture.width+px].a>=250){covered=true;break;}
                }
                solid=covered;
            }
            solidRun=solid ? solidRun+1 : 0;
            if(solidRun==6)return (distance+4)*side;
        }
        // Outside the playfield, a strip may miss the sprites entirely; omit its backing.
        return 0;
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
        for(int column=0;column<BackingColumns;column++)
        {
            float x=-325+column*2;
            Quad(vh,new Vector2(x,backingBottom[column]),new Vector2(x+2,backingTop[column]),white,white);
        }
        // Fixed edge sprites preserve the scalloped silhouette of the incoming/outgoing carrier.
        foreach(var sprite in edges)
        {
            Quad(vh,sprite.lo,sprite.hi,sprite.uv0,sprite.uv1);
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
