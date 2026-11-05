#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Profiling;

// Command-line simulation only. Native positions are exported for offline geometry review.
[InitializeOnLoad]
public static class VerifyCelestialPatterns
{
    public const string Folder="C:/Users/Miguel/Shootemup/ArtSource/CelestialPatternCorrection";
    static double deadline;
    static VerifyCelestialPatterns()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("CelestialPatternCorrection",false))return;
            deadline=EditorApplication.timeSinceStartup+1800;
            new GameObject("Celestial pattern regression").AddComponent<CelestialPatternRuntime>();
        };
        EditorApplication.update+=()=>
        {
            if(deadline>0&&EditorApplication.timeSinceStartup>deadline)
            {File.WriteAllText(Folder+"/Runtime.txt","FAIL timeout");EditorApplication.Exit(1);}
        };
    }
    static void Guard()
    {
        if(!Application.isBatchMode||!Application.dataPath.Replace('\\','/').Contains("/Temp/DecisionHoverReview/"))
            throw new Exception("Run only in the isolated command-line checkout");
    }
    public static void Run()
    {
        Guard();Directory.CreateDirectory(Folder+"/Patterns");
        PlayerSettings.companyName="ShootemupReview";PlayerSettings.productName="CelestialPatternCorrection";
        QualitySettings.renderPipeline=ScriptableObject.CreateInstance<CelestialNoRenderAsset>();
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("CelestialPatternCorrection",true);EditorApplication.isPlaying=true;
    }
    public static void RunFixes(){SessionState.SetBool("CelestialPatternFixes",true);Run();}
    public static void RunResume(){SessionState.SetBool("CelestialPatternResume",true);Run();}
    public static void RunProfile(){SessionState.SetBool("CelestialPatternProfile",true);Run();}
    public static void Build()
    {
        Guard();if(!File.ReadAllText(Folder+"/Runtime.txt").StartsWith("PASS"))throw new Exception("Review must pass");
        PlayerSettings.companyName="DefaultCompany";PlayerSettings.productName="seup";
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
            target=BuildTarget.StandaloneWindows64,
            locationPathName="C:/Users/Miguel/Shootemup/Build/CelestialPatterns/Shootemup.exe"
        });
        bool ok=report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded;
        File.WriteAllText(Folder+"/Build.txt",(ok?"PASS":"FAIL")+"\nErrors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings);
        EditorApplication.Exit(ok?0:1);
    }
}

public sealed class CelestialNoRenderAsset : UnityEngine.Rendering.RenderPipelineAsset<CelestialNoRenderPipeline>
{
    public override string renderPipelineShaderTag=>string.Empty;
    protected override UnityEngine.Rendering.RenderPipeline CreatePipeline()=>new CelestialNoRenderPipeline();
}
public sealed class CelestialNoRenderPipeline : UnityEngine.Rendering.RenderPipeline
{
    protected override void Render(UnityEngine.Rendering.ScriptableRenderContext context,List<UnityEngine.Camera> cameras) { }
}

public sealed class CelestialPatternRuntime : MonoBehaviour
{
    readonly List<string> notes=new(),errors=new();
    readonly HashSet<CombatRound> previous=new();
    readonly List<double> frames=new();
    static readonly FieldInfo sink=typeof(CombatRound).GetField("sink",BindingFlags.Instance|BindingFlags.NonPublic);
    WaveWatcher watcher;
    ProfilerRecorder mainThread;
    void Check(bool okay,string message)
    {
        notes.Add((okay?"PASS ":"FAIL ")+message);if(!okay)errors.Add(message);
        File.WriteAllLines(VerifyCelestialPatterns.Folder+"/Progress.txt",notes);
    }
    void Log(string text,string stack,LogType kind){if(kind==LogType.Error||kind==LogType.Exception)errors.Add(text+"\n"+stack);}
    IEnumerator Ready(){yield return null;while(CloudTransition.Busy||!WavePreparation.Ready)yield return null;yield return null;}
    IEnumerator Start()
    {
        QualitySettings.renderPipeline=ScriptableObject.CreateInstance<CelestialNoRenderAsset>();
        DontDestroyOnLoad(gameObject);Application.logMessageReceived+=Log;
        if(SessionState.GetBool("CelestialPatternResume",false))
        {
            notes.AddRange(File.ReadAllLines(VerifyCelestialPatterns.Folder+"/Progress-Initial.txt"));
            errors.AddRange(notes.Where(n=>n.StartsWith("FAIL")));
        }
        if(SessionState.GetBool("CelestialPatternFixes",false))
        {
            var retained=File.ReadAllLines(VerifyCelestialPatterns.Folder+"/Progress-Batch.txt")
                .Where(n=>!n.Contains("-Space-")&&!n.Contains("Hard-Heaven-")&&!n.Contains("Hard-Void-4")).ToArray();
            notes.AddRange(retained);errors.AddRange(retained.Where(n=>n.StartsWith("FAIL")));
        }
        while(CloudTransition.Busy||Time.frameCount<5)yield return null;
        WaveWatcher.Launch();
        while(WaveWatcher.Current==null||!WaveWatcher.Current.Ready||CloudTransition.Busy||!WavePreparation.Ready)yield return null;
        watcher=WaveWatcher.Current;if(watcher.Looping)watcher.ToggleLoop();
        QualitySettings.renderPipeline=ScriptableObject.CreateInstance<CelestialNoRenderAsset>();
        Time.captureDeltaTime=1f/60;
        mainThread=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);
        if(SessionState.GetBool("CelestialPatternProfile",false))
        {
            yield return Profile();yield break;
        }
        foreach(var mode in new[]{RunDifficulty.Normal,RunDifficulty.Hard,RunDifficulty.Easy})
        {
            watcher.SelectDifficulty(mode);watcher.SelectView(WatcherView.Bosses);yield return Ready();
            foreach(var level in new[]{LevelId.Heaven,LevelId.Void,LevelId.Space})
            {
                if(SessionState.GetBool("CelestialPatternResume",false)&&mode==RunDifficulty.Normal&&level!=LevelId.Space)continue;
                bool fixes=SessionState.GetBool("CelestialPatternFixes",false);
                if(fixes&&!(level==LevelId.Space||mode==RunDifficulty.Hard))continue;
                watcher.SelectLevel(level);yield return Ready();
                var entries=WatcherCatalog.For(WatcherView.Bosses,level);
                bool finalVoid=fixes&&level==LevelId.Void;
                int entry=entries.FindIndex(e=>e.Encounter==(finalVoid?WaveWatcher.BossFour:WaveWatcher.BossOne));
                watcher.SelectEntry(entry);yield return Ready();
                DistrictBoss boss=null;
                while(boss==null){boss=FindObjectsByType<DistrictBoss>().FirstOrDefault();yield return null;}
                if(finalVoid)while(boss.CurrentPhase<4)yield return null;
                yield return Fight(boss,level,mode);
            }
        }
        mainThread.Dispose();SessionState.SetBool("CelestialPatternCorrection",false);SessionState.SetBool("CelestialPatternFixes",false);SessionState.SetBool("CelestialPatternResume",false);
        File.WriteAllText(VerifyCelestialPatterns.Folder+"/Runtime.txt",(errors.Count==0?"PASS":"FAIL")+
            "\nHeadless native simulation; CPU frames include subsequent physics, exclude rendering.\n"+string.Join("\n",notes)+"\n"+string.Join("\n",errors));
        Application.logMessageReceived-=Log;EditorApplication.Exit(errors.Count==0?0:1);
    }
    IEnumerator Profile()
    {
        var report=new StringBuilder("Complete native CPU frames, no trajectory capture, no rendering.\n");
        watcher.SelectDifficulty(RunDifficulty.Hard);watcher.SelectView(WatcherView.Bosses);yield return Ready();
        foreach(var level in new[]{LevelId.Heaven,LevelId.Space})
        {
            watcher.SelectLevel(level);yield return Ready();
            var entries=WatcherCatalog.For(WatcherView.Bosses,level);
            watcher.SelectEntry(entries.FindIndex(e=>e.Encounter==WaveWatcher.BossThree));yield return Ready();
            DistrictBoss boss=null;
            while(boss==null){boss=FindObjectsByType<DistrictBoss>().FirstOrDefault();yield return null;}
            while(boss.CurrentPhase<3||boss.Invulnerable)yield return null;
            float warm=Time.time+3;while(Time.time<warm)yield return null;
            frames.Clear();frames.Capacity=4096;float end=Time.time+25;int peak=0,created=CombatRound.CreatedCount;
            while(Time.time<end)
            {
                yield return null;
                if(mainThread.Valid&&mainThread.LastValue>0)frames.Add(mainThread.LastValue/1000000.0);
                peak=Mathf.Max(peak,CombatRound.Live.Count);
            }
            frames.Sort();
            report.AppendLine(level+" phase 3: peak="+peak+" created="+(CombatRound.CreatedCount-created)+
                " CPU p50="+frames[frames.Count/2].ToString("F2")+"ms p95="+frames[(int)(frames.Count*.95)].ToString("F2")+"ms max="+frames[^1].ToString("F2")+"ms");
        }
        mainThread.Dispose();SessionState.SetBool("CelestialPatternProfile",false);SessionState.SetBool("CelestialPatternCorrection",false);
        File.WriteAllText(VerifyCelestialPatterns.Folder+"/Profile.txt",report.ToString());
        Application.logMessageReceived-=Log;EditorApplication.Exit(errors.Count==0?0:1);
    }
    IEnumerator Fight(DistrictBoss boss,LevelId level,RunDifficulty mode)
    {
        var angel=boss as SeraphJudicatorBoss;var sovereign=boss as VoidSovereignBoss;var space=boss as SpaceCitadelBoss;
        for(int phase=boss.CurrentPhase;phase<=boss.PhaseCount;phase++)
        {
            while(boss.Invulnerable||(sovereign!=null&&(sovereign.Hidden||sovereign.Teleporting)))yield return null;
            bool review=space==null||phase==1||phase==3;
            if(review)
            {
                string label=mode+"-"+level+"-"+phase;
                int peak=0,created=CombatRound.CreatedCount,up=0,down=0,far=0,remote=0,spawnBands=0,coverage=0;
                bool first=false,second=false;float until=Time.time+(space!=null?16:36),capture=0;int frame=0;
                Quaternion wheel=angel!=null?angel.Angel.Wheel(0).rotation:Quaternion.identity;float rotation=0;
                bool sampledPreviousFrame=false;
                var csv=new StringBuilder("frame,time,bossX,bossY,card,x,y,width,height,angle,type\n");
                previous.Clear();frames.Clear();
                while(Time.time<until)
                {
                    yield return null;
                    int card=angel!=null?angel.CardIndex:sovereign!=null?sovereign.CardIndex:0;
                    first|=card==0;second|=card==1;peak=Mathf.Max(peak,CombatRound.Live.Count);
                    // Omit frames that serialized captures. Retain all other complete
                    // frames, including the physics steps immediately after dense volleys.
                    if(!sampledPreviousFrame&&mainThread.Valid&&mainThread.LastValue>0)frames.Add(mainThread.LastValue/1000000.0);
                    sampledPreviousFrame=false;
                    if(angel!=null)
                    {
                        var next=angel.Angel.Wheel(0).rotation;rotation+=Quaternion.Angle(wheel,next);wheel=next;
                    }
                    Rect r=CombatRound.Border;
                    foreach(var shot in CombatRound.Live)
                    {
                        var p=shot.transform.position;
                        if(r.Contains(p))coverage|=1<<Mathf.Clamp((int)((p.y-r.yMin)/r.height*4),0,3);
                        if(previous.Contains(shot)||shot is PulseLaser)continue;
                        if(shot.Heading.y>.25f)up++;if(shot.Heading.y<-.25f)down++;
                        if(angel!=null&&Vector2.Distance(p,boss.transform.position)>2.8f)remote++;
                        if(space!=null&&sink.GetValue(shot)!=null)
                        {
                            if(Vector2.Distance(p,space.Reactor.transform.position)>6)far++;
                            if(r.Contains(p))spawnBands|=1<<Mathf.Clamp((int)((p.y-r.yMin)/r.height*4),0,3);
                        }
                    }
                    previous.Clear();foreach(var shot in CombatRound.Live)previous.Add(shot);
                    if(Time.time<capture)continue;capture=Time.time+.25f;frame++;
                    sampledPreviousFrame=true;
                    foreach(var shot in CombatRound.Live)
                    {
                        var p=shot.transform.position;var sprite=shot.GetComponent<SpriteRenderer>();var size=sprite.bounds.size;
                        csv.AppendLine(FormattableString.Invariant($"{frame},{Time.time},{boss.transform.position.x},{boss.transform.position.y},{card},{p.x},{p.y},{size.x},{size.y},{shot.transform.eulerAngles.z},{shot.GetType().Name}"));
                    }
                }
                File.WriteAllText(VerifyCelestialPatterns.Folder+"/Patterns/"+label+".csv",csv.ToString());
                Check(peak>40&&peak<CombatRound.Limit,label+" live peak "+peak);
                Check(CombatRound.CreatedCount==created,label+" constructs no rounds in combat ("+(CombatRound.CreatedCount-created)+")");
                Check(coverage==15,label+" reaches all four screen bands");
                if(angel!=null)
                {
                    Check(first&&second&&up>30&&down>80,label+" both cards; upward="+up+" direct downward="+down);
                    Check(remote>40,label+" empty-air choir emissions="+remote);
                    Check(rotation>300&&angel.Angel.Phase==phase,label+" continuous phase-scaled wheel rotation="+rotation.ToString("F0"));
                    Check(angel.GetComponent<PolygonCollider2D>().pathCount==37,label+" eye collision paths retained");
                }
                if(sovereign!=null)Check(first&&second,label+" both form-specific cards");
                if(space!=null)Check(far>40&&spawnBands==15,label+" arena-wide circular births; distant="+far+" bands="+spawnBands);
                frames.Sort();if(frames.Count>0)notes.Add(label+" CPU main-thread p95="+frames[(int)(frames.Count*.95)].ToString("F2")+"ms max="+frames[^1].ToString("F2")+"ms (headless)");
            }
            if(phase==boss.PhaseCount)continue;
            while(!(space!=null?space.Reactor.CanReceiveDamage:boss.CanReceiveDamage)||(sovereign!=null&&sovereign.Teleporting))yield return null;
            if(space!=null)space.Reactor.damage(space.Reactor.health*(phase==1?.51f:.55f));else boss.damage(boss.health*(sovereign!=null?.6f:phase==1?.51f:.55f));
            while(boss.CurrentPhase==phase)yield return null;
            if(sovereign!=null)yield return Interlude(sovereign,mode,phase);
        }
    }
    IEnumerator Interlude(VoidSovereignBoss boss,RunDifficulty mode,int chapter)
    {
        string label=mode+"-Void-Interlude-"+chapter;
        int emitted=0,wrongEdge=0,created=CombatRound.CreatedCount,peak=0;float next=0;int frame=0;
        var csv=new StringBuilder("frame,time,bossX,bossY,card,x,y,width,height,angle,type\n");previous.Clear();
        while(boss.Hidden)
        {
            yield return null;Rect r=CombatRound.Border;peak=Mathf.Max(peak,CombatRound.Live.Count);
            foreach(var shot in CombatRound.Live)
            {
                if(previous.Contains(shot))continue;
                emitted++;var p=shot.transform.position;
                float distance=chapter==1?r.yMax-p.y:chapter==2?r.xMax-p.x:p.y-r.yMin;
                if(distance>.55f)wrongEdge++;
            }
            previous.Clear();foreach(var shot in CombatRound.Live)previous.Add(shot);
            if(Time.time<next)continue;next=Time.time+.25f;frame++;
            foreach(var shot in CombatRound.Live)
            {
                var p=shot.transform.position;var size=shot.GetComponent<SpriteRenderer>().bounds.size;
                csv.AppendLine(FormattableString.Invariant($"{frame},{Time.time},0,0,0,{p.x},{p.y},{size.x},{size.y},{shot.transform.eulerAngles.z},{shot.GetType().Name}"));
            }
        }
        File.WriteAllText(VerifyCelestialPatterns.Folder+"/Patterns/"+label+".csv",csv.ToString());
        Check(Mathf.Abs(boss.InterludeElapsed-15)<.05f,label+" exactly fifteen active seconds");
        Check(emitted>80&&wrongEdge==0,label+" single charged edge, births="+emitted+" wrong edge="+wrongEdge);
        Check(CombatRound.CreatedCount==created&&peak<CombatRound.Limit,label+" prepared pool; peak="+peak+" new="+(CombatRound.CreatedCount-created));
        Check(CombatRound.Live.Count==0,label+" clears at reappearance");
    }
}
#endif
