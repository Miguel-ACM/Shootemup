#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class VerifyWaveWatcher
{
    const string Key="WaveWatcherReview";
    static double deadline;
    static VerifyWaveWatcher()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            {new GameObject("Wave watcher review").AddComponent<WaveWatcherReview>();deadline=EditorApplication.timeSinceStartup+420;}
        };
        EditorApplication.update+=()=>
        {
            if(deadline==0 || !SessionState.GetBool(Key,false))return;
            const string result="ArtSource/WaveWatcherReview/Verification.txt";
            if(File.Exists(result)){SessionState.SetBool(Key,false);EditorApplication.Exit(File.ReadAllText(result).StartsWith("PASS") ? 0 : 1);}
            else if(EditorApplication.timeSinceStartup>deadline){File.WriteAllText(result,"FAIL: timed out");EditorApplication.Exit(1);}
        };
    }
    // Command-line entry point for a disposable project copy; exits that editor when finished.
    public static void RunFinal(){SessionState.SetBool("WaveWatcherQuickReview",true);Run();}
    public static void Run()
    {
        Directory.CreateDirectory("ArtSource/WaveWatcherReview");
        if(File.Exists("ArtSource/WaveWatcherReview/Verification.txt"))File.Delete("ArtSource/WaveWatcherReview/Verification.txt");
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
}

public sealed class WaveWatcherReview : MonoBehaviour
{
    const string Folder="ArtSource/WaveWatcherReview/";
    readonly List<string> checks=new List<string>(),errors=new List<string>();
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Application.logMessageReceived+=Log;}
    void OnDestroy(){Application.logMessageReceived-=Log;}
    void Log(string message,string trace,LogType type){if((type==LogType.Error || type==LogType.Exception) && !trace.Contains("UnityEditor.Search"))errors.Add(message);}
    void Check(bool condition,string message){if(!condition)throw new Exception(message);checks.Add("PASS "+message);}
    IEnumerator Start()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            bool more=false;object step=null;Exception error=null;
            try{more=stack.Peek().MoveNext();if(more)step=stack.Peek().Current;}catch(Exception ex){error=ex;}
            if(error!=null){errors.Add(error.ToString());break;}
            if(!more){stack.Pop();continue;}
            if(step is IEnumerator nested){stack.Push(nested);continue;}yield return step;
        }
        Time.timeScale=1;
        File.WriteAllText(Folder+"Verification.txt",(errors.Count==0 ? "PASS" : "FAIL")+"\n"+string.Join("\n",checks)+"\n"+string.Join("\n",errors));
    }
    static Button Button(string name)=>UnityEngine.Object.FindObjectsByType<Button>().First(x=>x.name==name);
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder+name+".jpg",image.EncodeToJPG(88));Destroy(image);
    }
    IEnumerator Clip(string name,float seconds)
    {
        Directory.CreateDirectory(Folder+name);float start=Time.time,next=0;int frames=0,peak=0;
        while(Time.time-start<seconds)
        {
            peak=Mathf.Max(peak,CombatRound.Live.Count);
            if(Time.time-start>=next){yield return Capture(name+"/"+(frames++).ToString("D4"));next+=.1f;}
            yield return null;
        }
        Check(peak>0,name+" shows actual live enemy shooting (peak "+peak+" bullets)");
    }
    IEnumerator Run()
    {
        if(SceneManager.GetActiveScene().name!="Title")yield return SceneManager.LoadSceneAsync("Title");
        yield return null;yield return new WaitForSecondsRealtime(.3f);
        var entry=FindAnyObjectByType<WaveWatcherMenu>();Check(entry!=null,"title menu exposes Wave Watcher");
        yield return Capture("TitleEntry");entry.OnAccept();
        float deadline=Time.realtimeSinceStartup+55;
        while((WaveWatcher.Current==null || !WaveWatcher.Current.Ready) && Time.realtimeSinceStartup<deadline)yield return null;
        var watcher=WaveWatcher.Current;Check(watcher!=null && watcher.Ready,"title entry loads a ready live viewer after the cloud curtain");
        Canvas.ForceUpdateCanvases();
        foreach(var label in watcher.GetComponentsInChildren<TextMeshProUGUI>())
        {
            label.ForceMeshUpdate();
            Check(!label.isTextOverflowing && (label.text.Contains("\n") || label.textInfo.lineCount<=1),"watcher label fits: "+label.text);
        }
        var ship=FindAnyObjectByType<Shipcontroller>();
        Check(!FindAnyObjectByType<WaveOrchestrer>().enabled,"ordinary wave progression is stopped in the viewer");
        Check(ship.ObserverMode && !ship.enabled && ship.GetComponentsInChildren<Renderer>().All(x=>x.forceRenderingOff),"player hull, equipment and thrusters are invisible");
        Check(ship.GetComponentsInChildren<Collider2D>().All(x=>!x.enabled),"invisible player cannot erase pattern bullets");
        float health=ship.health;ship.ReceiveHazardDamage(1000000);
        Check(ship.health==health && !GameOverState.Active,"invisible aim target is invulnerable even to beams");
        Check(!GameRules.playerDisabled,"enemy shooting stays enabled in observer mode");
        var round=CombatRound.Launch<SimpleRound>(ship.transform.position,Vector2.down,0);yield return new WaitForSeconds(.15f);
        Check(round!=null && round.gameObject.activeSelf,"hostile bullets pass through the hidden player");
        watcher.SetAim(-1);Check(ship.transform.position.x<0,"left aim changes the real target");
        watcher.SetAim(1);Check(ship.transform.position.x>0,"right aim changes the real target");watcher.SetAim(0);
        Button("PAUSE").onClick.Invoke();yield return null;yield return null;
        float stopped=watcher.Elapsed;yield return new WaitForSecondsRealtime(.25f);
        Check(watcher.Paused && Time.timeScale==0 && watcher.Elapsed==stopped,"pause freezes real movement and bullets while UI remains usable");
        Button("PAUSE").onClick.Invoke();watcher.CycleSpeed();yield return null;yield return null;
        Check(Time.timeScale==.5f,"half-speed playback controls the actual simulation");watcher.CycleSpeed();yield return null;
        Check(watcher.PlaybackSpeed==2,"double-speed playback is available");watcher.CycleSpeed();
        foreach(var level in SessionState.GetBool("WaveWatcherQuickReview",false) ? new[]{LevelId.Fields} : new[]{LevelId.Fields,LevelId.Snow,LevelId.City,LevelId.Thunder,LevelId.Tunnel})
        {
            watcher.SelectLevel(level);
            Check(watcher.GetComponentsInChildren<Button>().Count(x=>int.TryParse(x.name,out _))==19,level+" exposes every authored wave from 2 through 20");
            for(int wave=WaveWatcher.FirstWave;wave<=WaveWatcher.LastWave;wave++)
            {Button(wave.ToString()).onClick.Invoke();yield return null;Check(watcher.Encounter==wave,level+" wave "+wave+" selectable");}
            watcher.SelectEncounter(2);yield return Clip(level+"Wave",8);
            Check(watcher.Spawned==DistrictWaves.Create(2).units.Count,level+" uses the full real wave recipe and spawn timing");
            watcher.SelectEncounter(WaveWatcher.Miniboss);yield return Clip(level+"Mini",9);
            watcher.SelectEncounter(WaveWatcher.BossTwo);yield return Clip(level+"BossTwo",12);
            if(level==LevelId.City)Check(FindObjectsByType<CityDuoBoss>().Length==1,"City phase two shows the actual enraged survivor");
            if(level==LevelId.Tunnel)Check(FindObjectsByType<TunnelSawBoss>().Length==3,"Tunnel phase two deploys the two real smaller saws");
        }
        watcher.SelectLevel(LevelId.Fields);watcher.SelectEncounter(2);yield return new WaitForSeconds(.2f);
        float elapsed=watcher.Elapsed;Button("REPLAY").onClick.Invoke();yield return null;yield return null;
        Check(watcher.Elapsed<elapsed,"replay resets the real wave timeline");
        watcher.ToggleLoop();Check(!watcher.Looping,"looping can be disabled");watcher.ToggleLoop();
        watcher.CycleSpeed();watcher.CycleSpeed(); // Two-times simulation makes the full hazard-tail check faster.
        bool repeated=false;float previous=watcher.Elapsed;deadline=Time.realtimeSinceStartup+25;
        while(!repeated && Time.realtimeSinceStartup<deadline)
        {
            yield return null;
            repeated=previous>1 && watcher.Elapsed<previous;previous=watcher.Elapsed;
        }
        Check(repeated,"loop replays only after ships and their real projectile tails have cleared");watcher.CycleSpeed();
        yield return Capture("Controls");Button("BACK").onClick.Invoke();
        deadline=Time.realtimeSinceStartup+30;
        while((SceneManager.GetActiveScene().name!="Title" || CloudTransition.Busy) && Time.realtimeSinceStartup<deadline)yield return null;
        Check(SceneManager.GetActiveScene().name=="Title" && WaveWatcher.Current==null,"Back returns to title and disposes the viewer");
        FindAnyObjectByType<NewGame>().OnAccept();deadline=Time.realtimeSinceStartup+30;
        while((SceneManager.GetActiveScene().name!="L1" || CloudTransition.Busy) && Time.realtimeSinceStartup<deadline)yield return null;
        yield return null;
        ship=FindAnyObjectByType<Shipcontroller>();
        Check(WaveWatcher.Current==null && ship.enabled && !ship.ObserverMode,"normal START has a visible controllable vulnerable player");
        Check(FindAnyObjectByType<WaveOrchestrer>().enabled && Time.timeScale==1,"normal START restores ordinary combat and time");
    }
}
#endif
