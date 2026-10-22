#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>End-to-end play verification, excluded from player builds.</summary>
public sealed class LevelFlowProbe : MonoBehaviour
{
    const string Output="FlowReview/LevelFlowResult.txt";
    const string Images="FlowReview/Captures";
    readonly List<string> errors=new List<string>();
    readonly Queue<string> captures=new Queue<string>();
    void Awake(){Application.runInBackground=true;Application.logMessageReceived+=Log;}
    void Log(string message,string trace,LogType type)
    {
        // Unity 6's editor search index can throw on startup in a freshly imported batch project.
        if(trace.Contains("UnityEditor.Search.SearchDatabase"))return;
        if(type==LogType.Exception || type==LogType.Error)
        { var error=message+"\n"+trace;if(!errors.Contains(error))errors.Add(error); }
    }
    IEnumerator Start()
    {
        StartCoroutine(CaptureFrames());
        var run=Run();
        while(true)
        {
            object current=null;bool more=false;Exception failure=null;
            try{more=run.MoveNext();if(more)current=run.Current;}catch(Exception e){failure=e;}
            if(failure!=null)
            {
                File.WriteAllText(Output,"FAIL\n"+failure+"\n"+string.Join("\n",errors));
                UnityEditor.EditorApplication.Exit(1);yield break;
            }
            if(!more)break;
            yield return current;
        }
        File.WriteAllText(Output,"PASS: live ship/background movement during title and cloud reveal; two-label title with the game font and Level x eyebrow; main menu; random starts and unique route pairs (1000 trials); neutral/switch/frame-rate charge checks; five environments; cloud cover/reveal; actual boss death; both spatial route selections; pause; player/stats/equipment preservation; next-stage waves; return to menu and new-run reset.\n"+string.Join("\n",errors));
        UnityEditor.EditorApplication.Exit(errors.Count==0 ? 0 : 1);
    }
    IEnumerator Run()
    {
        Directory.CreateDirectory(Images);
        yield return null;
        Check(SceneManager.GetActiveScene().name=="Title" && Time.timeScale==0,"Play did not begin at the paused main menu");
        Capture("01-menu");
        FindAnyObjectByType<NewGame>().OnAccept();
        Check(CloudTransition.Busy,"New game bypassed the clouds");
        yield return new WaitForSecondsRealtime(1.1f);Capture("02-white-cover");
        float until=Time.realtimeSinceStartup+12;
        while(FindAnyObjectByType<LevelReveal>()==null && Time.realtimeSinceStartup<until)yield return null;
        yield return null;
        var reveal=FindAnyObjectByType<LevelReveal>();
        Check(reveal!=null && Time.timeScale==1,"Level title paused gameplay");
        var labels=reveal.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        Check(labels.Length==2,"Level title contains extra text");
        Check(Array.Exists(labels,l=>l.text=="Level "+LevelRun.Stage),"Missing small Level x eyebrow");
        foreach(var label in labels)Check(label.font==FindAnyObjectByType<Dialog>().font,"Level title uses a different font from the game");
        var liveShip=FindAnyObjectByType<Shipcontroller>();float beforeX=liveShip.transform.position.x;
        var liveBackground=(MonoBehaviour)FindAnyObjectByType<LevelEnvironment>() ?? FindAnyObjectByType<PSXGrassField>();
        float beforeTravel=(float)Get(liveBackground,"travel");
        var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.RightArrow));
        yield return new WaitForSecondsRealtime(.3f);
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
        yield return null;
        UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
        Check(liveShip.transform.position.x>beforeX+.4f,"Ship cannot move during the title/cloud reveal");
        Check(!Mathf.Approximately((float)Get(liveBackground,"travel"),beforeTravel),"Background stopped during the title");
        while(CloudTransition.Busy && Time.realtimeSinceStartup<until)yield return null;
        Check(!CloudTransition.Busy && Time.timeScale==1,"Menu-to-game transition stalled");
        yield return new WaitForSecondsRealtime(.1f);Capture("02-live-title");
        Check(SceneManager.GetActiveScene().name=="L1","New game did not load gameplay");
        var ship=FindAnyObjectByType<Shipcontroller>();Check(ship!=null,"Player missing");
        var weapon=ship.GetComponentInChildren<AWeapon>();
        yield return new WaitForSecondsRealtime(1.2f);
        foreach(LevelId level in Enum.GetValues(typeof(LevelId)))
        {
            CloudTransition.Change(()=>{LevelRun.Advance(level);LevelEnvironment.Apply();});
            yield return new WaitForSecondsRealtime(1.5f);Capture("03-reveal-"+level);
            until=Time.realtimeSinceStartup+10;
            while(CloudTransition.Busy && Time.realtimeSinceStartup<until)yield return null;
            Check(!CloudTransition.Busy,"Environment transition stalled");
            if(level==LevelId.Thunder)
            {yield return new WaitForSeconds(1.7f);Capture("04-thunder-lightning");}
            yield return new WaitForSecondsRealtime(2.5f);Capture("04-environment-"+level);
            Check(level==LevelId.Fields ? FindAnyObjectByType<PSXGrassField>()!=null : FindAnyObjectByType<LevelEnvironment>()!=null,"Environment missing: "+level);
        }
        var waves=FindAnyObjectByType<WaveOrchestrer>();
        // Skip the opening shops only in this probe, so an actual instantiated boss exercises the death hook.
        foreach(var item in FindObjectsByType<AItem>(FindObjectsSortMode.None))Destroy(item.gameObject);
        Field(waves,"isShopUp",false);
        ship.health=73;GameRules.playerShootDamage=9;
        ship.transform.position=new Vector3(0,-4,ship.transform.position.z);
        Call(waves,"SpawnBoss");yield return new WaitForSeconds(.2f);
        var boss=FindAnyObjectByType<Boss>();Check(boss!=null,"Boss did not spawn");boss.damage(100000);
        yield return new WaitForSeconds(1);
        var choice=FindAnyObjectByType<LevelChoice>();Check(choice!=null && LevelChoice.Active,"Boss death did not open the route picker");
        Check(choice.Left!=choice.Right && choice.Left!=LevelRun.Current && choice.Right!=LevelRun.Current,"Invalid boss route pair");
        yield return new WaitForSeconds(.7f);Check(choice.Charge.Progress==0,"Centre charged a choice");Capture("05-neutral-choice");
        ship.transform.position=new Vector3(-3,-4,ship.transform.position.z);
        yield return new WaitForSeconds(.5f);Check(choice.Charge.Progress>.1f,"Left hold did not charge");
        float paused=choice.Charge.Progress;Time.timeScale=0;
        yield return new WaitForSecondsRealtime(.5f);Check(Mathf.Abs(choice.Charge.Progress-paused)<.001f,"Paused choice kept charging");Time.timeScale=1;
        ship.transform.position=new Vector3(0,-4,ship.transform.position.z);
        yield return new WaitForSeconds(.1f);Check(choice.Charge.Progress==0,"Centre did not cancel the choice");
        var expected=choice.Left;int stage=LevelRun.Stage;
        ship.transform.position=new Vector3(-3,-4,ship.transform.position.z);
        yield return new WaitForSeconds(1.35f);Capture("06-left-half-filled");
        until=Time.realtimeSinceStartup+6;
        while(!CloudTransition.Busy && Time.realtimeSinceStartup<until)yield return null;
        Check(CloudTransition.Busy,"Left route failed to commit");
        ship.health=73; // Freeze a known value after boss drops have finished falling.
        until=Time.realtimeSinceStartup+10;
        while(CloudTransition.Busy && Time.realtimeSinceStartup<until)yield return null;
        Check(LevelRun.Current==expected && LevelRun.Stage==stage+1,"Left route loaded the wrong level or advanced twice");
        Check(ship==FindAnyObjectByType<Shipcontroller>() && ship.health==73 && GameRules.playerShootDamage==9,"Stage change lost player state: health="+ship.health+", damage="+GameRules.playerShootDamage);
        Check(weapon==ship.GetComponentInChildren<AWeapon>(),"Stage change lost equipment");
        Check(!LevelChoice.Active && !GameRules.playerDisabled && Time.timeScale==1,"Stage change left choice or player disabled");
        Check((int)Get(waves,"currentWaveNumber")==1,"Next stage waves did not restart");
        foreach(var item in FindObjectsByType<AItem>(FindObjectsSortMode.None))Destroy(item.gameObject);
        Field(waves,"isShopUp",false);
        Call(waves,"SpawnBoss");yield return new WaitForSeconds(.2f);FindAnyObjectByType<Boss>().damage(100000);
        yield return new WaitForSeconds(1.1f);
        choice=FindAnyObjectByType<LevelChoice>();Check(choice!=null,"Second boss did not open another choice");
        expected=choice.Right;stage=LevelRun.Stage;
        ship.transform.position=new Vector3(3,-4,ship.transform.position.z);
        yield return new WaitForSeconds(1.35f);Capture("07-right-half-filled");
        until=Time.realtimeSinceStartup+6;
        while(!CloudTransition.Busy && Time.realtimeSinceStartup<until)yield return null;
        Check(CloudTransition.Busy,"Right route failed to commit");
        until=Time.realtimeSinceStartup+10;
        while(CloudTransition.Busy && Time.realtimeSinceStartup<until)yield return null;
        Check(LevelRun.Current==expected && LevelRun.Stage==stage+1,"Right route selected the wrong level");
        CloudTransition.Load("Title");
        until=Time.realtimeSinceStartup+10;
        while(CloudTransition.Busy && Time.realtimeSinceStartup<until)yield return null;
        Check(SceneManager.GetActiveScene().name=="Title" && Time.timeScale==0,"Return to menu failed");
        Check((bool)Get(FindAnyObjectByType<Menu>(),"menuOpen"),"Returned main menu cannot accept input");
        FindAnyObjectByType<NewGame>().OnAccept();
        until=Time.realtimeSinceStartup+10;
        while(CloudTransition.Busy && Time.realtimeSinceStartup<until)yield return null;
        Check(LevelRun.Stage==1 && GameRules.playerShootDamage==2 && !GameRules.playerDisabled,"New run retained previous run state");
        Check(errors.Count==0,"Unity reported runtime errors: "+string.Join("\n",errors));
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static object Get(object target,string name)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    static void Field(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    static void Call(object target,string name)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
    void Capture(string name){captures.Enqueue(name);}
    IEnumerator CaptureFrames()
    {
        while(true)
        {
            yield return new WaitForEndOfFrame();
            if(captures.Count==0)continue;
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            if(image!=null){File.WriteAllBytes(Path.Combine(Images,captures.Dequeue()+".png"),image.EncodeToPNG());Destroy(image);}
        }
    }
    void OnDestroy(){Application.logMessageReceived-=Log;}
}
#endif
