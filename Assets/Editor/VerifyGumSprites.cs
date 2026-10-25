#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// Run in a separate project copy: never takes over an existing player session.
[InitializeOnLoad]
public static class VerifyGumSprites
{
    const string Key="GumSpriteReview";
    static double deadline;
    static VerifyGumSprites()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            {new GameObject("Requested item review").AddComponent<GumSpriteReview>();deadline=EditorApplication.timeSinceStartup+480;}
        };
        EditorApplication.update+=()=>
        {
            if(deadline==0)return;
            const string report="ArtSource/GumSpriteReview/Verification.txt";
            if(File.Exists(report)){deadline=0;SessionState.SetBool(Key,false);EditorApplication.Exit(File.ReadAllText(report).StartsWith("PASS") ? 0 : 1);}
            else if(EditorApplication.timeSinceStartup>deadline){File.WriteAllText(report,"FAIL timed out");EditorApplication.Exit(1);}
        };
    }
    public static void Run()
    {
        Directory.CreateDirectory("ArtSource/GumSpriteReview");File.Delete("ArtSource/GumSpriteReview/Verification.txt");
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }

}
public sealed class GumSpriteReview : MonoBehaviour
{
    const string Folder="ArtSource/GumSpriteReview/";
    static readonly Type[] Items={typeof(ChewingGum),typeof(HotSauce),typeof(RubberBand),typeof(MagnifyingGlass),typeof(StaticSweater),typeof(PocketFan),typeof(DecoyProjector)};
    readonly List<string> checks=new List<string>(),errors=new List<string>();
    Shipcontroller ship;Keyboard keyboard;AItem granted;
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Application.logMessageReceived+=Log;}
    void OnDestroy(){Application.logMessageReceived-=Log;}
    void Log(string message,string trace,LogType type){if((type==LogType.Exception || type==LogType.Error) && !trace.Contains("UnityEditor.Search"))errors.Add(message+"\n"+trace);}
    void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add("PASS "+message);}
    void Press(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();}
    IEnumerator Grant(Type type,int slot=0)
    {
        var go=Instantiate(Resources.Load<GameObject>("Items/Item"),new Vector3(100,100,1),Quaternion.identity);
        granted=(AItem)go.AddComponent(type);yield return null;granted.GrantTo(ship,slot);yield return null;
    }
    static void Clear()
    {
        foreach(var b in UnityEngine.Object.FindObjectsByType<AEnemyBullet>(FindObjectsSortMode.None))b.Vanish();
        foreach(var b in UnityEngine.Object.FindObjectsByType<ABullet>(FindObjectsSortMode.None))Destroy(b.gameObject);
        foreach(var e in AEnemy.Live.ToArray())if(e!=null)Destroy(e.gameObject);
        foreach(var p in UnityEngine.Object.FindObjectsByType<APickable>(FindObjectsSortMode.None))Destroy(p.gameObject);
    }
    DistrictEnemy Target(Vector3 p)
    {
        var e=DistrictEnemy.Spawn(typeof(ScoutInterceptor),p);e.health=1000;
        typeof(DistrictEnemy).GetField("nextAttack",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(e,10000f);
        foreach(var mover in e.GetComponents<PatternMover>())mover.enabled=false;
        return e;
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder+name+".png",image.EncodeToPNG());Destroy(image);
    }
    IEnumerator Start()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            bool more=false;object step=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)step=stack.Peek().Current;}catch(Exception ex){failure=ex;}
            if(failure!=null){errors.Add(failure.ToString());break;}
            if(!more){stack.Pop();continue;}
            if(step is IEnumerator nested){stack.Push(nested);continue;}yield return step;
        }
        if(keyboard!=null){Press();InputSystem.RemoveDevice(keyboard);}
        if(WorldTimeStop.Current!=null)Destroy(WorldTimeStop.Current);Time.timeScale=1;
        File.WriteAllText(Folder+"Verification.txt",(errors.Count==0 ? "PASS" : "FAIL")+"\n"+string.Join("\n",checks)+"\n"+string.Join("\n",errors));
    }
    IEnumerator Run()
    {
        GameRules.StartNewRun();LevelRun.Reset(LevelId.Fields,7921);Time.timeScale=1;
        CloudTransition.Load("L1");while(CloudTransition.Busy || SceneManager.GetActiveScene().name!="L1")yield return null;
        yield return null;CombatDecision.End();Time.timeScale=1;
        ship=FindAnyObjectByType<Shipcontroller>();ship.ObserverMode=true;ship.transform.position=new Vector3(0,-4,-1);
        var flow=FindAnyObjectByType<WaveOrchestrer>();flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();
        Clear();yield return new WaitForSeconds(2);
        yield return Grant(typeof(ChewingGum));yield return Grant(typeof(BouncingBall));
        var first=Target(new Vector3(0,2));var second=Target(new Vector3(2,2));
        var shot=PlayerRound.Spawn(Vector3.zero,Vector2.up,10,6);
        var hostile=CombatRound.Launch<SimpleRound>(new Vector3(-2,3),Vector2.down,0);
        Check(shot.GetComponent<SpriteRenderer>().enabled && shot.GetComponent<MeshRenderer>()==null,"player bullet uses a sprite without a mesh renderer");
        Check(hostile.GetComponent<SpriteRenderer>().enabled && hostile.GetComponent<MeshRenderer>()==null,"enemy bullet uses a sprite without a mesh renderer");
        float deadline=Time.time+1;while(first.GetComponent<StickyGum>()==null && Time.time<deadline)yield return null;
        var gum=first.GetComponent<StickyGum>();Check(gum!=null && first.health==1000,"actual hit attaches gum and retains delayed damage");
        var bubble=first.transform.Find("Swelling gum");
        Check(bubble!=null && bubble.GetComponent<SpriteRenderer>()!=null && bubble.GetComponent<MeshFilter>()==null,"attached gum is a flat sprite, not a 3D mesh");
        Check(bubble.GetComponent<SpriteRenderer>().sprite.texture==ProjectileSprites.Load("standard").texture,"gum reuses approved neutral sprite pixels");
        yield return new WaitForSeconds(.2f);yield return Capture("AttachedGum");
        var bubbleScale=bubble.localScale;
        var freeze=ship.gameObject.AddComponent<WorldTimeStop>();freeze.Begin(1);
        yield return new WaitForSecondsRealtime(.15f);
        Check(bubble.localScale==bubbleScale && first.health==1000,"Bullet Time suspends sprite swelling and delayed damage");
        Destroy(freeze);yield return null;Time.timeScale=1;
        deadline=Time.time+1;while(FindAnyObjectByType<GumSpritePop>()==null && Time.time<deadline)yield return null;
        var pop=FindAnyObjectByType<GumSpritePop>();Check(pop!=null,"gum impact creates the sprite pop animation");
        Check(pop.GetComponentsInChildren<SpriteRenderer>().Length==5 && pop.GetComponentsInChildren<MeshRenderer>().Length==0,"every pop particle is a sprite");
        yield return Capture("GumPop00");yield return new WaitForSeconds(.08f);yield return Capture("GumPop01");
        yield return new WaitForSeconds(.6f);
        Check(first.health==990 && second.health==990,"both bounced gum hits retain their original damage");
        Check(FindObjectsByType<GumSpritePop>(FindObjectsSortMode.None).Length==0,"sprite pops clean up after their short animation");
        Clear();yield return null;
    }
}
#endif
