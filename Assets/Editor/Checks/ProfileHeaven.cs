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
using Unity.Profiling.LowLevel.Unsafe;

[InitializeOnLoad]
public static class ProfileHeaven
{
    public static string Folder => Path.GetFullPath("ReviewOutput/HeavenProfile");
    static ProfileHeaven()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("HeavenProfile", false)) return;
            MetaProgress.ReviewSaveDirectory = Folder;
            new GameObject("Heaven CPU profile").AddComponent<HeavenProfileRuntime>();
        };
    }
    public static void Run()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\','/').EndsWith("/Temp/UnlockReview/Assets"))
            throw new Exception("Use the isolated command-line review checkout.");
        Directory.CreateDirectory(Folder);
        PlayerSettings.companyName = "ShootemupReview";
        PlayerSettings.productName = "HeavenProfile";
        MetaProgress.ReviewSaveDirectory = Folder;
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("HeavenProfile", true);
        EditorApplication.isPlaying = true;
    }
    public static void RunFinal()
    {
        SessionState.SetBool("HeavenFinalChecks", true);
        Run();
    }
    public static void RunStress()
    {
        SessionState.SetBool("HeavenStressOnly", true);
        RunFinal();
    }
    public static void Build()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\','/').EndsWith("/Temp/UnlockReview/Assets"))
            throw new Exception("Use the isolated command-line review checkout.");
        PlayerSettings.companyName="DefaultCompany";PlayerSettings.productName="seup";
        string output=Path.GetFullPath("Build/BossCorrections/Shootemup.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
            target=BuildTarget.StandaloneWindows64,locationPathName=output,options=BuildOptions.None
        });
        File.WriteAllText(Folder+"/Build.txt",report.summary.result+"\nErrors="+report.summary.totalErrors+"; warnings="+report.summary.totalWarnings);
        EditorApplication.Exit(report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded?0:1);
    }
    public static void CheckWeapons()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\','/').EndsWith("/Temp/UnlockReview/Assets"))
            throw new Exception("Use the isolated command-line review checkout.");
        var notes=new List<string>();
        const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        var target=new GameObject("Moving eye weapon regression",typeof(MeshFilter),typeof(MeshRenderer));
        var enemy=target.AddComponent<HeavenWeaponTarget>();
        var hull=target.AddComponent<PolygonCollider2D>();hull.isTrigger=true;
        hull.points=Enumerable.Range(0,32).Select(i=>new Vector2(Mathf.Cos(i*Mathf.PI/16),Mathf.Sin(i*Mathf.PI/16))*2.7f).ToArray();
        var angel=target.AddComponent<HeavenAngelVisual>();angel.Build();angel.Pose(2.3f);
        Physics2D.SyncTransforms();
        var eye=Enumerable.Range(0,HeavenAngelVisual.EyeCounts[0]).Select(i=>angel.Wheel(0).GetChild(i).position).OrderByDescending(p=>Mathf.Abs(p.x)).First();
        Vector2 a=(Vector2)eye+Vector2.down*.4f,b=(Vector2)eye+Vector2.up*.4f;
        void Check(bool okay,string label){notes.Add((okay?"PASS ":"FAIL ")+label);if(!okay)throw new Exception(label);}
        Check(BeamContact.Touches(hull,a,b,.02f,out _) && BeamContact.Touches(enemy,a,b,.02f,out _),"A beam hits the shared body at an outer eye.");
        Check(enemy.ClosestDamagePoint(eye,out var closest)&&Vector2.Distance(closest,eye)<.001f,"Area damage and burning trails reach an outer eye.");
        var weapon=new GameObject("Beam and drill probe");var beam=weapon.AddComponent<BeamEmitter>();
        var path=new[]{new Vector3(eye.x,-4,0),new Vector3(eye.x,4,0)};
        typeof(BeamEmitter).GetField("paths",hidden).SetValue(beam,new[]{path,path,path});
        int contacts=(int)typeof(BeamEmitter).GetMethod("CountBeamContacts",hidden).Invoke(beam,new object[]{hull,.03f,Vector2.zero,Vector2.up});
        Check(contacts==3,"Three beams each damage once even when they intersect multiple eye shapes.");
        var drill=weapon.AddComponent<Drill>();
        bool drilling=(bool)typeof(Drill).GetMethod("Touches",hidden).Invoke(drill,new object[]{enemy,a,b,Vector2.zero});
        Check(drilling,"The drill reaches an outer eye without touching the core.");
        UnityEngine.Object.DestroyImmediate(weapon);UnityEngine.Object.DestroyImmediate(target);
        Directory.CreateDirectory(Folder);File.WriteAllLines(Folder+"/WeaponContacts.txt",notes);
        EditorApplication.Exit(0);
    }
}

public sealed class HeavenWeaponTarget:AEnemy { }

public sealed class HeavenProfileRuntime : MonoBehaviour
{
    readonly List<(string name, ProfilerRecorder recorder, List<double> samples)> counters = new();
    readonly List<string> errors = new();
    IEnumerator Ready() { yield return null; while (CloudTransition.Busy || !WavePreparation.Ready) yield return null; }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        Application.logMessageReceived += Log;
        QualitySettings.renderPipeline = ScriptableObject.CreateInstance<CelestialNoRenderAsset>();
        AudioListener.pause = true;
        while (CloudTransition.Busy || Time.frameCount < 5) yield return null;
        WaveWatcher.Launch();
        while (WaveWatcher.Current == null || !WaveWatcher.Current.Ready || CloudTransition.Busy || !WavePreparation.Ready) yield return null;
        var watcher = WaveWatcher.Current;
        if (watcher.Looping) watcher.ToggleLoop();
        watcher.SelectDifficulty(RunDifficulty.Hard); watcher.SelectView(WatcherView.Bosses); yield return Ready();
        watcher.SelectLevel(LevelId.Heaven); yield return Ready();
        watcher.SelectEntry(WatcherCatalog.For(WatcherView.Bosses, LevelId.Heaven).FindIndex(e => e.Encounter == WaveWatcher.BossThree));
        yield return Ready();
        SeraphJudicatorBoss boss = null;
        while (boss == null) { boss = FindAnyObjectByType<SeraphJudicatorBoss>(); yield return null; }
        Time.captureDeltaTime = 1f / 60;
        while (boss.CurrentPhase < 3 || boss.Invulnerable) yield return null;
        for (int i = 0; i < 180; i++) yield return null;
        var handles = new List<ProfilerRecorderHandle>();
        ProfilerRecorderHandle.GetAvailable(handles);
        File.WriteAllLines(ProfileHeaven.Folder + "/Markers.txt", handles.Select(h => ProfilerRecorderHandle.GetDescription(h).Name));
        foreach (var h in handles)
        {
            var d = ProfilerRecorderHandle.GetDescription(h);
            if (d.Name == "Main Thread" || d.Name == "PlayerLoop" || d.Name.Contains("Physics2D.") ||
                d.Name.StartsWith("Heaven.") || d.Name == "GC Allocated In Frame" || d.Name == "Update.ScriptRunBehaviourUpdate")
                counters.Add((d.Name, ProfilerRecorder.StartNew(d.Category, d.Name, 1,
                    ProfilerRecorderOptions.Default | ProfilerRecorderOptions.SumAllSamplesInFrame), new List<double>(3000)));
        }
        int peak = 0, created = CombatRound.CreatedCount;
        for (int frame = 0; frame < 1800; frame++)
        {
            yield return null;
            peak = Mathf.Max(peak, CombatRound.Live.Count);
            foreach (var c in counters) c.samples.Add(c.recorder.LastValue / (c.name == "GC Allocated In Frame" ? 1.0 : 1000000.0));
        }
        var report = new List<string> { "Heaven Hard phase 3: 1800 complete frames at 60 simulation Hz, native physics included; rendering excluded.",
            "Peak rounds=" + peak + "; new rounds allocated=" + (CombatRound.CreatedCount-created) };
        foreach (var c in counters)
        {
            c.samples.Sort();
            report.Add($"{c.name}: mean={c.samples.Average():F3} p50={c.samples[900]:F3} p95={c.samples[1710]:F3} max={c.samples[1799]:F3} {(c.name == "GC Allocated In Frame" ? "bytes" : "ms")}");
            c.recorder.Dispose();
        }
        report.AddRange(errors);
        File.WriteAllLines(ProfileHeaven.Folder + "/Profile.txt", report);
        if (SessionState.GetBool("HeavenFinalChecks", false)) yield return FinalChecks(watcher, boss);
        SessionState.SetBool("HeavenProfile", false);
        SessionState.SetBool("HeavenFinalChecks", false);
        SessionState.SetBool("HeavenStressOnly", false);
        EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
    }
    static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    readonly List<string> checks = new();
    void Check(bool okay, string message)
    {
        checks.Add((okay ? "PASS " : "FAIL ") + message);
        if (!okay) errors.Add(message);
        File.WriteAllLines(ProfileHeaven.Folder + "/Checks.txt", checks.Concat(errors));
    }
    static void Clear()
    {
        var release=typeof(CombatRound).GetMethod("Release",Hidden);
        foreach (var round in CombatRound.Live.ToArray()) release.Invoke(round,null);
    }
    IEnumerator FinalChecks(WaveWatcher watcher, SeraphJudicatorBoss boss)
    {
        boss.StopAllCoroutines();
        Clear();
        var colliders = boss.GetComponents<PolygonCollider2D>();
        Check(colliders.Length == 1 && colliders[0].pathCount == 1 && colliders[0].GetComponent<AEnemy>() == boss,
            "One continuous whole-body collider owns all Heaven damage.");
        int allocated = CombatRound.CreatedCount;
        var border = CombatRound.Border;
        for (int i = 0; i < 3600; i++)
        {
            var p = new Vector3(Mathf.Lerp(border.xMin+.3f,border.xMax-.3f,(i%60)/59f),
                Mathf.Lerp(border.yMin+.3f,border.yMax-.3f,(i/60)/59f),1);
            var direction = CombatVisuals.Rotate(Vector2.down, i * 137.5f);
            CombatRound round = i%2==0 ? CombatRound.Launch<SimpleRound>(p,direction,1.1f,.17f) : CombatRound.Launch<NeedleRound>(p,direction,1.1f,.17f);
            round.Bounce(100);
        }
        Check(CombatRound.Live.Count == 3600 && CombatRound.CreatedCount == allocated, "Full 3600-round field uses the warmed pool without new bullet objects.");
        var roundForContact = CombatRound.Live.First();
        var contactSpeed = (Func<float>)typeof(CombatRound).GetProperty("ContactSpeed",Hidden).GetGetMethod(true).CreateDelegate(typeof(Func<float>),roundForContact);
        contactSpeed();
        long bytes=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<10000;i++) contactSpeed();
        long churn=GC.GetAllocatedBytesForCurrentThread()-bytes;
        Check(churn==0,"10,000 projectile contact-speed calculations allocate "+churn+" managed bytes.");
        using (var timer = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1))
        {
            var frames = new List<double>(600);
            int low = 3600;
            for(int i=0;i<600;i++)
            {
                yield return null;
                low = Mathf.Min(low, CombatRound.Live.Count);
                // Bounces reflect off sides/ceiling, but correctly leave the bottom.
                // Refill their returned pool slots to sustain the full-cap workload.
                while(CombatRound.Live.Count<3600)
                {
                    int index=CombatRound.Live.Count;
                    var p=new Vector3(Mathf.Lerp(border.xMin+.4f,border.xMax-.4f,(index%61)/60f),border.yMax-.4f,1);
                    var axis=CombatVisuals.Rotate(Vector2.down,(index%13-6)*10);
                    CombatRound replacement=index%2==0?CombatRound.Launch<SimpleRound>(p,axis,1.1f,.17f):CombatRound.Launch<NeedleRound>(p,axis,1.1f,.17f);
                    replacement.Bounce(100);
                }
                frames.Add(timer.LastValue/1000000.0);
            }
            frames.Sort();
            Check(low>=3500 && CombatRound.CreatedCount==allocated, $"3600-round bouncing stress, 600 native frames (minimum before refill={low}, new objects={CombatRound.CreatedCount-allocated}): CPU p50={frames[300]:F3}, p95={frames[570]:F3}, max={frames[599]:F3} ms (no rendering).");
        }
        Clear();
        if(SessionState.GetBool("HeavenStressOnly",false))yield break;
        watcher.SelectLevel(LevelId.Void); yield return Ready();
        watcher.SelectEntry(WatcherCatalog.For(WatcherView.Bosses, LevelId.Void).FindIndex(e => e.Encounter == WaveWatcher.BossFour));
        yield return Ready();
        VoidSovereignBoss sovereign = null;
        while (sovereign == null) { sovereign = FindAnyObjectByType<VoidSovereignBoss>(); yield return null; }
        while (sovereign.CurrentPhase < 4 || sovereign.Invulnerable || sovereign.Hidden || sovereign.Teleporting) yield return null;
        sovereign.StopAllCoroutines(); sovereign.enabled = false; Clear();
        var ship = FindAnyObjectByType<Shipcontroller>();
        ship.enabled = false;
        foreach (float offset in new[] { .9f, 1.3f, 1.8f, 2.5f })
        {
            Clear();
            ship.transform.position = sovereign.transform.position + new Vector3(0,offset,0);
            yield return (IEnumerator)typeof(VoidSovereignBoss).GetMethod("CrossedGaze",Hidden).Invoke(sovereign,null);
            float closest = 100;
            for(int i=0;i<100;i++)
            {
                yield return null;
                foreach(var shot in CombatRound.Live)
                    closest = Mathf.Min(closest, Vector2.Distance(shot.transform.position,ship.transform.position));
            }
            Check(closest < .13f, $"Final star's charged crossfire reaches stationary pilot {offset:F1} units above its centre (closest={closest:F3}).");
        }
        Clear(); Destroy(sovereign.gameObject); yield return null;
        var laserCases = new (Type type,float hold)[] { (typeof(ArcLancer),.6f),(typeof(BeamDrone),.5f),
            (typeof(LaserSatellite),.65f),(typeof(PursuitFighter),.35f),(typeof(StormOrgan),.55f),
            (typeof(ChoirLancer),.65f),(typeof(AegisReliquary),.85f),(typeof(PrismCantor),.85f),(typeof(ThroneBarge),1.1f) };
        foreach(var entry in laserCases)
        {
            Clear();
            var enemy = DistrictEnemy.Spawn(entry.type,new Vector3(0,Camera.UpLimit-3,1));
            yield return null;
            entry.type.GetMethod("Attack",Hidden).Invoke(enemy,null);
            var beams=CombatRound.Live.OfType<PulseLaser>().ToArray();
            Check(beams.Length>0 && beams.All(b=>Mathf.Abs(b.activeTime-entry.hold)<.001f && b.warning>=1 && b.widthRise<b.activeTime),
                entry.type.Name+": shortened live laser duration, full warning and completed width rise.");
            Destroy(enemy.gameObject); yield return null;
        }
        Clear();
        var flow=FindAnyObjectByType<WaveOrchestrer>();
        Check(LevelRun.FinalDistrict,"Void is the final district.");
        foreach(var coin in FindObjectsByType<Money>()) Destroy(coin.gameObject);
        yield return null;
        int wallet=RunCoins.Count;
        for(int i=0;i<5;i++) Instantiate(Resources.Load<GameObject>("Pickables/money"),new Vector3(Camera.LeftLimit,Camera.UpLimit,1),Quaternion.identity);
        yield return (IEnumerator)typeof(WaveOrchestrer).GetMethod("BossReward",Hidden).Invoke(flow,new object[]{Vector3.zero});
        Check(GameOverState.Active && FindAnyObjectByType<DecisionShop>()==null,"Void ends at victory without opening a boss item decision.");
        Check(RunCoins.Count==wallet+5,"Victory collects and banks all remaining boss coins exactly once.");
        File.WriteAllLines(ProfileHeaven.Folder+"/Checks.txt",checks.Concat(errors));
    }
    void Log(string message, string stack, LogType kind)
    {
        if (kind == LogType.Error || kind == LogType.Exception) errors.Add(message + "\n" + stack);
    }
}
#endif
