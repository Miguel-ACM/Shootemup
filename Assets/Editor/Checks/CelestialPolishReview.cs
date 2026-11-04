#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.Profiling;

public sealed class CelestialPolishReview
{
    public const string Folder="C:/Users/Miguel/Shootemup/ArtSource/CelestialPolishReview";
    readonly List<string> notes=new List<string>(),errors=new List<string>();
    readonly HashSet<Type> deliveries=new HashSet<Type>();
    readonly Dictionary<Type,int> counts=new Dictionary<Type,int>(),peaks=new Dictionary<Type,int>();
    int peak;
    object EndFrame=>Application.isBatchMode?null:new WaitForEndOfFrame();
    WaveWatcher watcher;
    void Check(bool ok,string message){notes.Add((ok?"PASS ":"FAIL ")+message);if(!ok)errors.Add(message);}
    void Log(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error)errors.Add(message+"\n"+stack);}
    IEnumerator Ready(){yield return null;while(CloudTransition.Busy||!WavePreparation.Ready)yield return null;yield return null;}
    void Capture(string clip,int frame)
    {
        string path=Folder+"/Frames/"+clip;Directory.CreateDirectory(path);
        Texture2D picture;
        if(Application.isBatchMode)
        {
            var camera=UnityEngine.Camera.main;
            var target=RenderTexture.GetTemporary(1280,720,24);var previous=RenderTexture.active;
            var presentation=camera.GetComponent<PSXPresentation>();
            camera.Render();Graphics.Blit(presentation.SceneTexture,target,presentation.CompositeMaterial);RenderTexture.active=target;
            picture=new Texture2D(1280,720,TextureFormat.RGB24,false);picture.ReadPixels(new Rect(0,0,1280,720),0,0);picture.Apply();
            RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
        }
        else picture=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(path+"/"+frame.ToString("D4")+".jpg",picture.EncodeToJPG(88));UnityEngine.Object.Destroy(picture);
    }
    IEnumerator Clip(string name,float seconds)
    {
        float until=Time.time+seconds,next=0;int frame=0;
        while(Time.time<until){yield return EndFrame;if(Time.time>=next){next=Time.time+.2f;Capture(name,frame++);}}
    }
    void Observe()
    {
        peak=Mathf.Max(peak,CombatRound.Live.Count);counts.Clear();
        foreach(var shot in CombatRound.Live)
        {
            var type=shot.GetType();deliveries.Add(type);counts.TryGetValue(type,out int n);counts[type]=n+1;
        }
        foreach(var count in counts){peaks.TryGetValue(count.Key,out int old);peaks[count.Key]=Mathf.Max(old,count.Value);}
    }
    public IEnumerator Review(WaveWatcher source)
    {
        watcher=source;Application.logMessageReceived+=Log;Directory.CreateDirectory(Folder);
        if(SessionState.GetBool("CelestialPolishFinalArt",false))
        {
            watcher.SelectDifficulty(RunDifficulty.Hard);watcher.SelectView(WatcherView.Bosses);watcher.SelectLevel(LevelId.Void);yield return Ready();watcher.SelectEncounter(WaveWatcher.BossFour);yield return Ready();
            VoidSovereignBoss final=null;while(final==null){final=UnityEngine.Object.FindAnyObjectByType<VoidSovereignBoss>();yield return null;}
            while(final.Form!=4||final.Invulnerable||final.Hidden)yield return null;
            yield return Clip("Void-Final-Art",16);
            File.WriteAllText(Folder+"/Final-Art.txt",(errors.Count==0?"PASS":"FAIL")+"\nFinal-form eye socket, visible core and revised obsidian surface.\n"+string.Join("\n",errors));
            Application.logMessageReceived-=Log;SessionState.SetBool("CelestialPolishFinalArt",false);SessionState.SetBool("CelestialPolish",false);SessionState.SetBool("HeavenVoidReview",false);EditorApplication.Exit(errors.Count==0?0:1);yield break;
        }
        bool fixes=SessionState.GetBool("CelestialPolishFixes",false);
        if(!fixes)
        {
        watcher.SelectDifficulty(RunDifficulty.Normal);watcher.SelectLevel(LevelId.Heaven);yield return Ready();
        watcher.SelectView(WatcherView.Enemies);
        for(int enemy=0;enemy<5;enemy++){watcher.SelectEncounter(enemy);yield return Ready();yield return Clip("Heaven-Enemy-"+enemy,8);}
        watcher.SelectView(WatcherView.Waves);watcher.SelectEncounter(8);yield return Ready();yield return Clip("Heaven-Ruins-Wave",18);
        watcher.SelectView(WatcherView.Bosses);watcher.SelectEncounter(WaveWatcher.Miniboss);yield return Ready();yield return Clip("Heaven-Gatekeeper",14);
        watcher.SelectEncounter(WaveWatcher.BossOne);yield return Ready();yield return Clip("Heaven-Angel",18);
        }
        watcher.SelectView(WatcherView.Bosses);watcher.SelectLevel(LevelId.Void);yield return Ready();
        foreach(var difficulty in fixes?new[]{RunDifficulty.Hard}:new[]{RunDifficulty.Normal,RunDifficulty.Easy,RunDifficulty.Hard})
        {
            watcher.SelectDifficulty(difficulty);yield return Ready();watcher.SelectEncounter(WaveWatcher.BossOne);yield return null;
            VoidSovereignBoss boss=null;while(boss==null){boss=UnityEngine.Object.FindAnyObjectByType<VoidSovereignBoss>();yield return null;}
            while(boss.Invulnerable)yield return null;
            int created=CombatRound.CreatedCount;peak=0;
            Check(boss.PhaseCount==(difficulty==RunDifficulty.Easy?3:4),difficulty+" retains phase count");
            for(int phase=1;phase<=boss.PhaseCount;phase++)
            {
                while(boss.Invulnerable)yield return null;
                bool second=false,complete=false;float started=Time.time,next=0;int frame=0;var top=new int[2];
                while(Time.time-started<65)
                {
                    yield return EndFrame;Observe();
                    if(boss.CardIndex==1)second=true;
                    if(second&&boss.CardIndex==0){complete=true;break;}
                    foreach(var shot in CombatRound.Live)
                        if(shot.transform.position.y>CombatRound.Border.yMax-2.4f&&shot.transform.position.y<CombatRound.Border.yMax&&Mathf.Abs(shot.transform.position.x)<Camera.RightLimit)
                            top[boss.CardIndex]++;
                    if(Time.time>=next)
                    {
                        next=Time.time+(difficulty==RunDifficulty.Normal||fixes?.25f:3f);
                        Capture("Void-"+difficulty+"-"+boss.Form,frame++);
                    }
                }
                Check(complete,difficulty+" form "+boss.Form+" completes both distinct cards");
                Check(top[0]>10&&top[1]>10,difficulty+" form "+boss.Form+" attacks the top on both cards: "+top[0]+" / "+top[1]);
                if(phase==boss.PhaseCount)
                {
                    if(difficulty==RunDifficulty.Hard)
                    {
                        Time.captureDeltaTime=0;
                        using(var recorder=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1024))
                        {
                            float end=Time.time+12;while(Time.time<end){Observe();yield return null;}
                            var samples=recorder.ToArray().Select(s=>s.Value/1000000.0).OrderBy(v=>v).ToArray();
                            if(samples.Length>0)notes.Add("Hard full-frame main thread: median="+samples[samples.Length/2].ToString("F2")+" ms; p99="+samples[(int)((samples.Length-1)*.99)].ToString("F2")+" ms; max="+samples.Last().ToString("F2")+" ms");
                        }
                        Time.captureDeltaTime=1f/30;
                    }
                    break;
                }
                boss.damage(10000000);while(!boss.Hidden)yield return null;
                Check(!boss.CanBeTargeted&&!boss.CanReceiveDamage,"Form break is invulnerable and untargetable");
                float hiddenAt=Time.time;next=0;frame=0;
                while(boss.Hidden)
                {
                    yield return EndFrame;Observe();
                    if(difficulty==RunDifficulty.Normal&&Time.time>=next){next=Time.time+.25f;Capture("Void-Interlude-"+phase,frame++);}
                }
                Check(Mathf.Abs(boss.InterludeElapsed-15)<.05f&&Time.time-hiddenAt>=14.9f,"Fifteen-second barrage / "+difficulty+" / "+phase);
            }
            notes.Add(difficulty+" observed live peak: "+peak);
            Check(peak<CombatRound.Limit,difficulty+" retains live budget headroom");
            Check(CombatRound.CreatedCount==created,difficulty+" performs no projectile construction during the fight ("+(CombatRound.CreatedCount-created)+")");
            if(difficulty==RunDifficulty.Hard){boss.damage(10000000);yield return null;Check(BossDestruction.Current!=null,"Final form enters the shared finale");yield return Clip("Void-Finale",4);}
        }
        foreach(var type in new[]{typeof(SimpleRound),typeof(NeedleRound),typeof(BurstShell),typeof(SuspendedRound),typeof(GiantOrb),typeof(ReturningDisc),typeof(RicochetBolt),typeof(HelixRound),typeof(PulseLaser),typeof(PlasmaOrb),typeof(GuidedMissile),typeof(ScatterCarrier),typeof(ProximityMine)})
            Check(deliveries.Contains(type),"Score uses "+type.Name);
        foreach(var pair in peaks)notes.Add(pair.Key.Name+" observed peak: "+pair.Value);
        File.WriteAllText(Folder+(fixes?"/Runtime-Hard.txt":"/Runtime-Batch.txt"),(errors.Count==0?"PASS":"FAIL")+"\n"+string.Join("\n",notes)+"\n"+string.Join("\n",errors));
        Application.logMessageReceived-=Log;
        SessionState.SetBool("CelestialPolishFixes",false);SessionState.SetBool("CelestialPolish",false);SessionState.SetBool("HeavenVoidReview",false);EditorApplication.Exit(errors.Count==0?0:1);
    }
}
#endif
