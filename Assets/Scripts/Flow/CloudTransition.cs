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
        scaler.referenceResolution = new Vector2(1920,1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        gameObject.AddComponent<GraphicRaycaster>();
        banks = new RectTransform[3];
        for (int i=0;i<banks.Length;i++)
        {
            var go = new GameObject("Cloud bank " + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(CloudCurtainGraphic));
            go.transform.SetParent(transform,false);
            banks[i] = go.GetComponent<RectTransform>();
            banks[i].sizeDelta = new Vector2(5000,3200);
            var graphic=go.GetComponent<CloudCurtainGraphic>();
            graphic.color = i==0 ? new Color(.78f,.85f,.90f) : i==1 ? new Color(.92f,.96f,.98f) : Color.white;
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
        yield return new WaitForSecondsRealtime(.18f);
        if(SceneManager.GetActiveScene().name=="L1") LevelReveal.Show();
        yield return Move(false);
        canvas.enabled=false;
        busy=false;
        Time.timeScale=SceneManager.GetActiveScene().name=="Title" ? 0 : 1;
    }

    IEnumerator Move(bool cover)
    {
        const float duration=1.15f;
        for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
        {
            float t=elapsed/duration;
            for(int i=0;i<banks.Length;i++)
            {
                float p=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-i*.055f)/.88f));
                banks[i].anchoredPosition=new Vector2(Mathf.Sin(t*3+i)*22,
                    cover ? Mathf.Lerp(2900,0,p) : Mathf.Lerp(0,-2900,p));
            }
            yield return null;
        }
        foreach(var bank in banks)bank.anchoredPosition=new Vector2(0,cover ? 0 : -2900);
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
        Quad(vh,new Vector2(r.xMin,r.yMin+230),new Vector2(r.xMax,r.yMax-230));
        for(int i=0;i<19;i++)
        {
            float x=Mathf.Lerp(r.xMin,r.xMax,i/18f);
            float radius=210+Mathf.Sin(i*2.7f+phase)*65;
            Circle(vh,new Vector2(x,r.yMin+250),radius);
            Circle(vh,new Vector2(x,r.yMax-250),radius);
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
