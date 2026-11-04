#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Profiling;

[InitializeOnLoad]
public static class VerifyHeavenVoid
{
    public const string Folder="C:/Users/Miguel/Shootemup/ArtSource/HeavenVoidReview";
    static double deadline;
    static VerifyHeavenVoid()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("HeavenVoidReview",false))return;
            deadline=EditorApplication.timeSinceStartup+900;
            new GameObject("Heaven and Void review").AddComponent<HeavenVoidReview>();
        };
        EditorApplication.update+=()=>
        {
            if(deadline==0)return;
            if(EditorApplication.timeSinceStartup>deadline){File.WriteAllText(Folder+"/Runtime.txt","FAIL timeout");EditorApplication.Exit(1);}
        };
    }
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/Temp/DecisionHoverReview/"))throw new Exception("Use the isolated review checkout");
        Directory.CreateDirectory(Folder+"/Models");Directory.CreateDirectory(Folder+"/Frames");
        PlayerSettings.companyName="ShootemupReview";PlayerSettings.productName="HeavenVoidReview";
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        Gallery();
        var assembly=typeof(EditorWindow).Assembly;
        var view=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
        var sizes=typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new object[]{1});
        var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),Enum.ToObject(assembly.GetType("UnityEditor.GameViewSizeType"),1),1280,720,"Heaven Void review");
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
        int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
        view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(view,count-1);
        view.Show();
        SessionState.SetBool("HeavenVoidReview",true);
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Title.unity");
        EditorApplication.isPlaying=true;
    }
    public static void RunFixes()
    {
        SessionState.SetBool("HeavenVoidFixes",true);
        Run();
    }
    static void Gallery()
    {
        var camera=new GameObject("Model review camera").AddComponent<UnityEngine.Camera>();
        camera.transform.position=new Vector3(1000,0,-10);camera.orthographic=true;camera.orthographicSize=2.7f;
        camera.cullingMask=1<<31;camera.backgroundColor=new Color(.07f,.08f,.11f);camera.clearFlags=CameraClearFlags.SolidColor;
        var target=new RenderTexture(480,480,24);camera.targetTexture=target;
        var models=new[]{typeof(ChoirLancer),typeof(AegisReliquary),typeof(PrismCantor),typeof(VotiveSpear),typeof(ThroneBarge),typeof(DawnGatekeeper),typeof(SeraphJudicatorBoss),typeof(VoidSovereignBoss)};
        foreach(var type in models)
        {
            int count=type==typeof(VoidSovereignBoss)?4:1;
            for(int form=0;form<count;form++)
            {
                var go=new GameObject(type.Name){layer=31};go.transform.position=new Vector3(1000,0,0);
                var mesh=type==typeof(VoidSovereignBoss)?IndustrialMeshFactory.VoidForm(form):IndustrialMeshFactory.CombatHull((int)EnemyCatalog.Get(type).Hull);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=type==typeof(VoidSovereignBoss)?VoidSovereignBoss.Body:HeavenMaterials.Ivory;
                camera.orthographicSize=Mathf.Max(mesh.bounds.size.x,mesh.bounds.size.y)*.65f;
                for(int angle=0;angle<2;angle++)
                {
                    go.transform.rotation=Quaternion.Euler(angle==0?0:22,angle==0?0:24,0);camera.Render();
                    var previous=RenderTexture.active;RenderTexture.active=target;
                    var png=new Texture2D(480,480,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,480,480),0,0);png.Apply();
                    File.WriteAllBytes(Folder+"/Models/"+type.Name+"-"+form+"-"+angle+".png",png.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(png);RenderTexture.active=previous;
                }
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
        UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(target);
        foreach(string shader in new[]{"HeavenClouds","HeavenMist","VoidFalls","VoidBody"})
            if(ShaderUtil.ShaderHasError(Resources.Load<Shader>(shader)))throw new Exception("Shader error "+shader);
    }
    public static void Build()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/Temp/DecisionHoverReview/")||!File.ReadAllText(Folder+"/Runtime.txt").StartsWith("PASS"))throw new Exception("Complete isolated runtime review first");
        SessionState.SetBool("HeavenVoidReview",false);
        PlayerSettings.companyName="DefaultCompany";PlayerSettings.productName="seup";
        string path="C:/Users/Miguel/Shootemup/Build/HeavenVoid/Shootemup.exe";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),target=BuildTarget.StandaloneWindows64,locationPathName=path});
        bool ok=result.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded;
        File.WriteAllText(Folder+"/Build.txt",(ok?"PASS":"FAIL")+"\n"+path+"\nErrors="+result.summary.totalErrors+"; warnings="+result.summary.totalWarnings+"\n");
        EditorApplication.Exit(ok?0:1);
    }
    public static void RunPorts()
    {
        SessionState.SetBool("HeavenVoidPorts",true);
        Run();
    }
}

public sealed class HeavenVoidReview:MonoBehaviour
{
    readonly List<string> notes=new List<string>(),errors=new List<string>();
    WaveWatcher watcher;
    string folder=>VerifyHeavenVoid.Folder;
    void Check(bool ok,string description){notes.Add((ok?"PASS ":"FAIL ")+description);if(!ok)errors.Add(description);}
    void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors.Add(message+"\n"+stack);}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Application.logMessageReceived+=Log;
        while(CloudTransition.Busy||Time.frameCount<5)yield return null;
        WaveWatcher.Launch();
        while(WaveWatcher.Current==null||!WaveWatcher.Current.Ready||CloudTransition.Busy||!WavePreparation.Ready)yield return null;
        watcher=WaveWatcher.Current;if(watcher.Looping)watcher.ToggleLoop();
        Time.captureDeltaTime=1f/30;
        if(SessionState.GetBool("HeavenVoidPorts",false))
        {
            yield return Ports();yield break;
        }
        if(SessionState.GetBool("HeavenVoidFixes",false))
        {
            yield return Fixes();yield break;
        }
        foreach(var from in new[]{LevelId.Space,LevelId.Underwater})foreach(var next in new[]{LevelId.Hell,LevelId.Heaven})
        {
            LevelRun.Reset(from,17381);Check(LevelRun.GetOptions().SequenceEqual(new[]{LevelId.Hell,LevelId.Heaven}),from+" offers Hell / Heaven");
            LevelRun.Advance(next);Check(LevelRun.AutomaticNext==LevelId.Void,next+" automatically continues to Void");LevelRun.Advance(LevelId.Void);
            Check(LevelRun.FinalDistrict&&LevelRun.Seed==17381&&DistrictWaves.Sequence().SequenceEqual(new[]{"boss"}),"Void preserves seed and contains only boss");
        }
        watcher.SelectLevel(LevelId.Heaven);yield return Ready();
        foreach(int seed in new[]{17381,221113,83019})
        {
            LevelRun.Reset(LevelId.Heaven,seed);yield return WaveComposer.Prepare();
            for(int budget=2;budget<=20;budget++)
            {
                Wave wave=null;try{wave=WaveComposer.Create(budget);}catch(Exception e){errors.Add(e.ToString());}
                Check(wave!=null&&wave.units.Count>0&&wave.units.All(u=>WatcherCatalog.Enemies(LevelId.Heaven).Any(t=>t.Name==u.enemy)),"Heaven authored score "+seed+" / "+budget);
                yield return null;
            }
        }
        watcher.SetSeed(17381);watcher.SelectDifficulty(RunDifficulty.Normal);yield return Ready();
        watcher.SelectView(WatcherView.Waves);watcher.SelectEncounter(8);yield return Clip("Heaven-Wave",16);
        watcher.SelectView(WatcherView.Bosses);watcher.SelectEncounter(WaveWatcher.Miniboss);yield return Clip("Heaven-Miniboss",18);
        watcher.SelectEncounter(WaveWatcher.BossOne);yield return Ready();
        DistrictBoss seraph=null;while(seraph==null){seraph=FindAnyObjectByType<SeraphJudicatorBoss>();yield return null;}
        for(int phase=1;phase<=3;phase++)
        {
            while(seraph.Invulnerable)yield return null;
            yield return Clip("Heaven-Boss-"+phase,14);
            if(phase<3){seraph.health*=phase==1?.49f:.45f;while(seraph.CurrentPhase<=phase)yield return null;}
        }
        watcher.SelectLevel(LevelId.Void);yield return Ready();
        foreach(var difficulty in new[]{RunDifficulty.Normal,RunDifficulty.Hard,RunDifficulty.Easy})
        {
            watcher.SelectDifficulty(difficulty);yield return Ready();watcher.SelectEncounter(WaveWatcher.BossOne);
            yield return null;yield return null;
            VoidSovereignBoss boss=null;while(boss==null){boss=FindAnyObjectByType<VoidSovereignBoss>();yield return null;}
            Check(boss.PhaseCount==(difficulty==RunDifficulty.Easy?3:4),difficulty+" Void phase count");
            int livePeak=0,created=CombatRound.CreatedCount;
            for(int phase=1;phase<=boss.PhaseCount;phase++)
            {
                while(boss.Invulnerable)yield return null;
                yield return Clip("Void-"+difficulty+"-"+phase,difficulty==RunDifficulty.Normal?15:8);
                livePeak=Mathf.Max(livePeak,CombatRound.Live.Count);
                if(phase==boss.PhaseCount)break;
                float before=Time.time;boss.damage(10000000);
                Check(boss.health>0,"Burst damage cannot skip Void phase "+phase);
                while(!boss.Hidden)yield return null;
                Check(!boss.CanBeTargeted&&!boss.CanReceiveDamage&&!boss.Hull.gameObject.activeSelf,"Hidden boss rejects damage and homing");
                float hiddenAt=Time.time;
                if(difficulty==RunDifficulty.Normal)yield return Clip("Void-Interlude-"+phase,15);
                while(boss.Hidden)yield return null;
                Check(Mathf.Abs(boss.InterludeElapsed-15)<.05f&&Time.time-hiddenAt>=14.9f,"15 second absence / "+difficulty+" / "+phase);
                Check(Time.time-before<17,"Interlude timing independent of difficulty tempo");
            }
            Check(livePeak<CombatRound.Limit,difficulty+" dense phases retain live budget; sampled peak "+livePeak);
            notes.Add(difficulty+" pooled objects created during fight: "+(CombatRound.CreatedCount-created));
        }
        // Profile complete frames, including native physics, without screenshot I/O.
        watcher.SelectDifficulty(RunDifficulty.Hard);yield return Ready();watcher.SelectEncounter(WaveWatcher.BossFour);
        yield return null;VoidSovereignBoss last=null;while(last==null){last=FindAnyObjectByType<VoidSovereignBoss>();yield return null;}
        while(last.CurrentPhase<4||last.Invulnerable)yield return null;
        Time.captureDeltaTime=0;
        using(var recorder=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",512))
        {
            float until=Time.time+9;while(Time.time<until)yield return null;
            var samples=recorder.ToArray().Select(s=>s.Value/1000000.0).OrderBy(v=>v).ToArray();
            if(samples.Length>0)notes.Add("Hard Void full-frame main thread ms: median="+samples[samples.Length/2].ToString("F2")+" p99="+samples[(int)((samples.Length-1)*.99)].ToString("F2")+" max="+samples.Last().ToString("F2"));
        }
        // Verify ordinary death/finale ownership and reward notification still use the shared pipeline.
        last.damage(10000000);yield return null;Check(BossDestruction.Current!=null,"Void final phase starts continuous boss finale");
        yield return Clip("Void-Finale",5);
        File.WriteAllText(folder+"/Runtime.txt",(errors.Count==0?"PASS":"FAIL")+"\n"+string.Join("\n",notes)+"\n"+string.Join("\n",errors));
        SessionState.SetBool("HeavenVoidReview",false);EditorApplication.Exit(errors.Count==0?0:1);
    }
    IEnumerator Ready(){yield return null;while(CloudTransition.Busy||!WavePreparation.Ready)yield return null;yield return null;}
    IEnumerator Ports()
    {
        watcher.SelectDifficulty(RunDifficulty.Normal);watcher.SelectLevel(LevelId.Void);yield return Ready();
        watcher.SelectEncounter(WaveWatcher.BossOne);yield return null;
        VoidSovereignBoss boss=null;
        while(boss==null){boss=FindAnyObjectByType<VoidSovereignBoss>();yield return null;}
        var position=typeof(VoidSovereignBoss).GetMethod("Port",BindingFlags.Instance|BindingFlags.NonPublic);
        for(int phase=1;phase<=4;phase++)
        {
            while(boss.Invulnerable)yield return null;
            var seen=new HashSet<int>();bool aligned=true;float until=Time.time+8;int frame=0;
            string path=folder+"/Frames/Void-Cues-"+phase;Directory.CreateDirectory(path);
            while(Time.time<until)
            {
                yield return new WaitForEndOfFrame();
                foreach(var sprite in boss.GetComponentsInChildren<SpriteRenderer>())
                {
                    if(!sprite.enabled||sprite.name!="Weapon charge and release")continue;
                    float nearest=float.PositiveInfinity;int closest=-1;
                    for(int i=0;i<(boss.Form==4?4:2);i++)
                    {
                        var expected=(Vector3)position.Invoke(boss,new object[]{i})+Vector3.back*.08f;
                        float distance=Vector3.Distance(sprite.transform.position,expected);
                        if(distance<nearest){nearest=distance;closest=i;}
                    }
                    aligned&=nearest<.01f;seen.Add(closest);
                }
                if(frame++%6==0)
                {
                    var capture=ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(path+"/"+(frame/6).ToString("D4")+".jpg",capture.EncodeToJPG(88));Destroy(capture);
                }
            }
            Check(aligned&&seen.Count==(boss.Form==4?4:2),"Form "+boss.Form+" charge and release sprites follow every anatomical port");
            Check(boss.GetComponent<EnemyFiringCue>().PortCount==4,"Form "+boss.Form+" reuses four prepared cues");
            if(phase<4){boss.damage(10000000);while(boss.CurrentPhase==phase)yield return null;}
        }
        File.WriteAllText(folder+"/Ports.txt",(errors.Count==0?"PASS":"FAIL")+"\n"+string.Join("\n",notes)+"\n"+string.Join("\n",errors));
        SessionState.SetBool("HeavenVoidPorts",false);SessionState.SetBool("HeavenVoidReview",false);EditorApplication.Exit(errors.Count==0?0:1);
    }
    IEnumerator Fixes()
    {
        watcher.SelectLevel(LevelId.Heaven);yield return Ready();
        foreach(var difficulty in new[]{RunDifficulty.Normal,RunDifficulty.Hard,RunDifficulty.Easy})
        {
            Difficulty.Select(difficulty);
            foreach(int seed in new[]{17381,221113,83019,1,789321,94367,20261104})
            {
                LevelRun.Reset(LevelId.Heaven,seed);yield return WaveComposer.Prepare();
                for(int budget=2;budget<=20;budget++)
                {
                    Wave wave=null;try{wave=WaveComposer.Create(budget);}catch(Exception e){errors.Add(e.ToString());}
                    Check(wave!=null&&wave.units.Count>=5,"Revised Heaven score / "+difficulty+" / "+seed+" / "+budget);
                    yield return null;
                }
            }
        }
        watcher.SelectDifficulty(RunDifficulty.Normal);watcher.SetSeed(83019);yield return Ready();
        watcher.SelectView(WatcherView.Waves);watcher.SelectEncounter(3);yield return Clip("Heaven-Revised-Clouds-Wave",18);
        watcher.SelectEncounter(WaveWatcher.BossOne);yield return Clip("Heaven-Revised-Lasers",14);
        string original=File.ReadAllText(folder+"/Runtime-Initial.txt");
        Check(original.Contains("PASS Void final phase starts continuous boss finale")&&original.Contains("Hard pooled objects created during fight: 0"),"Original Void lifecycle and pool review remains valid");
        File.WriteAllText(folder+"/Runtime.txt",(errors.Count==0?"PASS":"FAIL")+"\nTargeted recheck of Heaven's failed seeds and revised scenery. Full Void results: Runtime-Initial.txt\n"+string.Join("\n",notes)+"\n"+string.Join("\n",errors));
        SessionState.SetBool("HeavenVoidFixes",false);SessionState.SetBool("HeavenVoidReview",false);EditorApplication.Exit(errors.Count==0?0:1);
    }
    IEnumerator Clip(string name,float seconds)
    {
        string path=folder+"/Frames/"+name;Directory.CreateDirectory(path);int frame=0;float start=Time.time,next=0;
        while(Time.time-start<seconds)
        {
            yield return new WaitForEndOfFrame();
            if(Time.time-start>=next)
            {
                next+=.20f;var image=ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(path+"/"+(frame++).ToString("D4")+".jpg",image.EncodeToJPG(88));Destroy(image);
            }
        }
        File.AppendAllText(folder+"/Progress.txt",name+" captured\n");
    }
    void OnDestroy(){Application.logMessageReceived-=Log;}
}
#endif
