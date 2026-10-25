#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class VerifyWaveVariety
{
    static double deadline;
    static VerifyWaveVariety()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("WaveVarietyReview",false))
            {new GameObject("Wave variety review").AddComponent<WaveVarietyReview>();deadline=EditorApplication.timeSinceStartup+600;}
        };
        EditorApplication.update+=()=>
        {
            const string path="ArtSource/WaveVarietyReview/Verification.txt";
            if(deadline==0 || !SessionState.GetBool("WaveVarietyReview",false))return;
            if(File.Exists(path)){SessionState.SetBool("WaveVarietyReview",false);EditorApplication.Exit(File.ReadAllText(path).StartsWith("PASS") ? 0 : 1);}
            else if(EditorApplication.timeSinceStartup>deadline){File.WriteAllText(path,"FAIL timed out");EditorApplication.Exit(1);}
        };
    }
    public static void Run()=>Begin(false);
    public static void Integration()=>Begin(true);
    public static void Preparation()=>Begin(true,true);
    static void Begin(bool integration,bool preparation=false)
    {
        SessionState.SetBool("WaveVarietyIntegration",integration);SessionState.SetBool("WaveVarietyPreparation",preparation);
        Directory.CreateDirectory("ArtSource/WaveVarietyReview");
        if(File.Exists("ArtSource/WaveVarietyReview/Verification.txt"))File.Delete("ArtSource/WaveVarietyReview/Verification.txt");
        SessionState.SetBool("WaveVarietyReview",true);EditorApplication.isPlaying=true;
    }
}
public sealed class WaveVarietyReview : MonoBehaviour
{
    const string Folder="ArtSource/WaveVarietyReview/";
    readonly List<string> checks=new List<string>(),errors=new List<string>();
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Application.logMessageReceived+=Log;}
    void OnDestroy(){Application.logMessageReceived-=Log;}
    void Log(string message,string trace,LogType type){if((type==LogType.Error || type==LogType.Exception) && !trace.Contains("UnityEditor.Search"))errors.Add(message);}
    void Check(bool pass,string message){if(!pass)throw new Exception(message);checks.Add("PASS "+message);File.WriteAllText(Folder+"Progress.txt",string.Join("\n",checks));}
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
        Time.timeScale=1;File.WriteAllText(Folder+"Verification.txt",(errors.Count==0 ? "PASS" : "FAIL")+"\n"+string.Join("\n",checks)+"\n"+string.Join("\n",errors));
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder+name+".jpg",image.EncodeToJPG(86));Destroy(image);
    }
    static void LiveSpacing(string context)
    {
        var flights=FindObjectsByType<FormationFlight>();
        for(int i=0;i<flights.Length;i++)for(int j=0;j<i;j++)
        {
            var a=flights[i].GetComponent<DistrictEnemy>();var b=flights[j].GetComponent<DistrictEnemy>();
            float ar=WaveGeometry.Radius(a.GetType().Name),br=WaveGeometry.Radius(b.GetType().Name);
            Vector2 p=a.transform.position,q=b.transform.position;
            if(WaveGeometry.Visible(p,ar) && WaveGeometry.Visible(q,br) && Vector2.Distance(p,q)<ar+br-.015f)
                throw new Exception("Live model overlap: "+context+" / "+a.name+" / "+b.name);
        }
    }
    IEnumerator Director(WaveOrchestrer flow)
    {
        LevelRun.Reset(LevelId.Fields,41971);LevelEnvironment.Apply();
        var ship=FindAnyObjectByType<Shipcontroller>();ship.ObserverMode=true;ship.enabled=false;
        foreach(var weapon in ship.GetComponentsInChildren<AWeapon>()){weapon.StopFiring();weapon.enabled=false;}
        foreach(var collider in ship.GetComponentsInChildren<Collider2D>())collider.enabled=false;
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        typeof(WaveOrchestrer).GetField("currentWaveNumber",flags).SetValue(flow,0);
        typeof(WaveOrchestrer).GetField("isWaveComingOut",flags).SetValue(flow,false);
        typeof(WaveOrchestrer).GetField("stopingWave",flags).SetValue(flow,false);
        flow.waves=new[]{"2","3","4"};flow.enabled=true;flow.BeginWaves();
        Time.timeScale=3;float start=Time.time;int wave=0,peak=0;
        while(Time.time-start<70)
        {
            wave=(int)typeof(WaveOrchestrer).GetField("currentWaveNumber",flags).GetValue(flow);
            LiveSpacing("normal director handoff");peak=Mathf.Max(peak,CombatRound.Live.Count);
            if(wave==3 && !(bool)typeof(WaveOrchestrer).GetField("isWaveComingOut",flags).GetValue(flow))break;
            yield return null;
        }
        flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();Time.timeScale=1;
        Check(wave==3 && AEnemy.Live.Count==0 && peak>0,"normal wave director completes three prepared waves with actual fire and no overlapping hulls across handoffs");
    }
    IEnumerator Run()
    {
        GameRules.StartNewRun();LevelRun.Reset();Time.timeScale=1;
        if(SceneManager.GetActiveScene().name!="L1")yield return SceneManager.LoadSceneAsync("L1");
        yield return new WaitForSeconds(1);
        var flow=FindAnyObjectByType<WaveOrchestrer>();flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();
        foreach(var enemy in FindObjectsByType<AEnemy>())Destroy(enemy.gameObject);
        yield return null;
        if(!SessionState.GetBool("WaveVarietyPreparation",false))
        {checks.AddRange(WaveVarietyAudit.Inspect(SessionState.GetBool("WaveVarietyIntegration",false) ? 1 : 8,text=>File.WriteAllText(Folder+"AuditProgress.txt",text)));Check(true,"seed, spacing and composition stress audit completed");
            yield return Director(flow);
        }
        // Exercise the real viewer, including the new reroll control.
        CloudTransition.Load("Title");while(CloudTransition.Busy)yield return null;
        FindAnyObjectByType<WaveWatcherMenu>().OnAccept();
        while(WaveWatcher.Current==null || !WaveWatcher.Current.Ready)yield return null;
        var watcher=WaveWatcher.Current;
        int oldSeed=LevelRun.Seed;string old=WaveVarietyAudit.Signature(DistrictWaves.Create(2));
        watcher.NewSet();yield return null;yield return null;
        Check(LevelRun.Seed!=oldSeed && WaveVarietyAudit.Signature(DistrictWaves.Create(2))!=old,"New Set changes the actual wave plan");
        string replay=WaveVarietyAudit.Signature(DistrictWaves.Create(2));watcher.Replay();yield return null;
        Check(WaveVarietyAudit.Signature(DistrictWaves.Create(2))==replay,"Replay keeps the exact seeded parts");
        watcher.ToggleLoop();
        foreach(var level in new[]{LevelId.Fields,LevelId.Snow,LevelId.City,LevelId.Thunder,LevelId.Tunnel})
        {
            watcher.SelectLevel(level);
            while(!WavePreparation.Ready)yield return null;
            var cached=System.Diagnostics.Stopwatch.StartNew();
            for(int id=2;id<=20;id++)DistrictWaves.Create(id);cached.Stop();
            Check(cached.Elapsed.TotalMilliseconds<20,level+" completes its full plan before combat; all nineteen cached wave lookups "+cached.Elapsed.TotalMilliseconds.ToString("F2")+"ms");
            if(SessionState.GetBool("WaveVarietyPreparation",false))continue;
            foreach(int budget in new[]{2,8,15})
            {
                watcher.SelectEncounter(budget);var wave=DistrictWaves.Create(budget);
                string clip=level+"Wave"+budget;Directory.CreateDirectory(Folder+clip);foreach(var oldFrame in Directory.GetFiles(Folder+clip,"????.jpg"))File.Delete(oldFrame);
                float duration=wave.duration+.4f,start=Time.time,next=0;int frame=0,peak=0;
                while(Time.time-start<duration)
                {
                    LiveSpacing(clip);
                    peak=Mathf.Max(peak,CombatRound.Live.Count);
                    if(Time.time-start>=next){yield return Capture(clip+"/"+(frame++).ToString("D4"));next+=.2f;}
                    yield return null;
                }
                Check(peak>0 && watcher.Spawned==wave.units.Count,clip+" full live formation, coordinated fire and no hull overlap; "+string.Join(" + ",wave.parts));
            }
        }
        watcher.Exit();while(CloudTransition.Busy)yield return null;
    }
}
#endif
