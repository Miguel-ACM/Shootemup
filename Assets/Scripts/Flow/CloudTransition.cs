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
    bool busy;
    RectTransform[] banks;
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
        for (int i=0;i<banks.Length;i++)
        {
            var go = new GameObject("Cloud bank " + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(CloudCurtainGraphic));
            go.transform.SetParent(clip,false);
            banks[i] = go.GetComponent<RectTransform>();
            banks[i].sizeDelta = new Vector2(650,1200);
            var graphic=go.GetComponent<CloudCurtainGraphic>();
            graphic.color = Color.Lerp(new Color(.67f,.77f,.84f),Color.white,i/3f);
            graphic.phase=i*1.7f;
        }
        canvas.enabled=false;
    }

    void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name=="L1") LevelEnvironment.Apply();
        // Supersede the old world-space clouds, which were behind the PSX output canvas.
        foreach(var spawner in FindObjectsByType<CloudSpawner>(FindObjectsSortMode.None)) spawner.enabled=false;
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
        Time.timeScale=0;
        canvas.enabled=true;
        yield return Move(true);
        covered?.Invoke();
        if(scene!=null)
        {
            var load=SceneManager.LoadSceneAsync(scene);
            while(!load.isDone) yield return null;
        }
        // Give new scene objects a frame to initialise while the screen is completely white.
        yield return null;
        yield return new WaitForSecondsRealtime(.3f);
        if(SceneManager.GetActiveScene().name=="L1")
        {
            // The title is an overlay on live gameplay, including while the clouds uncover it.
            Time.timeScale=1;
            LevelReveal.Show();
        }
        yield return Move(false);
        canvas.enabled=false;
        busy=false;
        Time.timeScale=SceneManager.GetActiveScene().name=="Title" ? 0 : 1;
    }

    IEnumerator Move(bool cover)
    {
        const float duration=2.4f;
        for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
        {
            float t=elapsed/duration;
            for(int i=0;i<banks.Length;i++)
            {
                float p=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-i*.065f)/.805f));
                banks[i].anchoredPosition=new Vector2(Mathf.Sin(t*Mathf.PI*2+i)*14,
                    cover ? Mathf.Lerp(1120,0,p) : Mathf.Lerp(0,-1120,p));
            }
            yield return null;
        }
        foreach(var bank in banks)bank.anchoredPosition=new Vector2(0,cover ? 0 : -1120);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded-=SceneLoaded;
        if(instance==this)instance=null;
    }
}

/// <summary>Solid body and overlapping circular lobes: no transparent seam can expose a scene load.</summary>
public sealed class CloudCurtainGraphic : MaskableGraphic
{
    public float phase;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r=rectTransform.rect;
        Quad(vh,new Vector2(r.xMin,r.yMin+80),new Vector2(r.xMax,r.yMax-80));
        for(int i=0;i<12;i++)
        {
            float x=Mathf.Lerp(r.xMin,r.xMax,i/11f);
            float radius=66+Mathf.Sin(i*2.7f+phase)*22;
            float billow=Mathf.Sin(i*1.9f+phase)*20;
            Circle(vh,new Vector2(x,r.yMin+92+billow),radius);
            Circle(vh,new Vector2(x+18,r.yMax-92+billow),radius*1.08f);
            // Smaller overlapping tufts break up the large scallops into soft billows.
            Circle(vh,new Vector2(x+26,r.yMin+69+billow),radius*.57f);
            Circle(vh,new Vector2(x-16,r.yMax-69+billow),radius*.57f);
        }
    }
    void Quad(VertexHelper vh,Vector2 lo,Vector2 hi)
    {
        int k=vh.currentVertCount;
        vh.AddVert(new Vector3(lo.x,lo.y),color,Vector2.zero);vh.AddVert(new Vector3(lo.x,hi.y),color,Vector2.zero);
        vh.AddVert(new Vector3(hi.x,hi.y),color,Vector2.zero);vh.AddVert(new Vector3(hi.x,lo.y),color,Vector2.zero);
        vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
    }
    void Circle(VertexHelper vh,Vector2 center,float radius)
    {
        int k=vh.currentVertCount;
        vh.AddVert(center,color,Vector2.zero);
        const int segments=40;
        for(int j=0;j<=segments;j++)
        {
            float a=j*Mathf.PI*2/segments;
            vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,color,Vector2.zero);
            if(j>0)vh.AddTriangle(k,k+j,k+j+1);
        }
    }
}
