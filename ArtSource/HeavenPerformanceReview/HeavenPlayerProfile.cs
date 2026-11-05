#if UNITY_INCLUDE_INSTRUMENTATION
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Unity.Profiling;

public sealed class HeavenPlayerProfile : MonoBehaviour
{
    const string Folder="C:/Users/Miguel/Shootemup/ArtSource/HeavenPerformanceReview";
    static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch()
    {
        if(Environment.GetCommandLineArgs().Contains("-heaven-profile"))
            new GameObject("Standalone Heaven CPU profile").AddComponent<HeavenPlayerProfile>();
    }
    IEnumerator Ready(){yield return null;while(CloudTransition.Busy||!WavePreparation.Ready)yield return null;}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject); AudioListener.pause=true;
        QualitySettings.renderPipeline=ScriptableObject.CreateInstance<HeavenProfilePipelineAsset>();
        while(CloudTransition.Busy||Time.frameCount<5)yield return null;
        WaveWatcher.Launch();
        while(WaveWatcher.Current==null||!WaveWatcher.Current.Ready||CloudTransition.Busy||!WavePreparation.Ready)yield return null;
        var watcher=WaveWatcher.Current;
        if(watcher.Looping)watcher.ToggleLoop();
        watcher.SelectDifficulty(RunDifficulty.Hard);watcher.SelectView(WatcherView.Bosses);yield return Ready();
        watcher.SelectLevel(LevelId.Heaven);yield return Ready();
        watcher.SelectEntry(WatcherCatalog.For(WatcherView.Bosses,LevelId.Heaven).FindIndex(e=>e.Encounter==WaveWatcher.BossThree));yield return Ready();
        SeraphJudicatorBoss boss=null;
        while(boss==null){boss=FindAnyObjectByType<SeraphJudicatorBoss>();yield return null;}
        Time.captureDeltaTime=1f/60;
        while(boss.CurrentPhase<3||boss.Invulnerable)yield return null;
        for(int i=0;i<180;i++)yield return null;
        var report=new List<string>{"Standalone development player, complete native CPU frames, 60 simulation Hz; no rendering, profiler overhead included."};
        yield return Measure("Heaven Hard phase 3",1800,false,report);
        boss.StopAllCoroutines();
        var release=typeof(CombatRound).GetMethod("Release",Hidden);
        foreach(var round in CombatRound.Live.ToArray())release.Invoke(round,null);
        Fill();
        yield return Measure("3600-round bouncing stress",600,true,report);
        File.WriteAllLines(Folder+"/StandaloneCPU.txt",report);
        Application.Quit(0);
    }
    static void Fill()
    {
        var r=CombatRound.Border;
        while(CombatRound.Live.Count<3600)
        {
            int i=CombatRound.Live.Count;
            var p=new Vector3(Mathf.Lerp(r.xMin+.4f,r.xMax-.4f,(i%60)/59f),Mathf.Lerp(r.yMin+.4f,r.yMax-.4f,(i/60)/59f),1);
            var axis=CombatVisuals.Rotate(Vector2.down,i*137.5f);
            CombatRound shot=i%2==0?CombatRound.Launch<SimpleRound>(p,axis,1.1f,.17f):CombatRound.Launch<NeedleRound>(p,axis,1.1f,.17f);
            shot.Bounce(100);
        }
    }
    IEnumerator Measure(string label,int count,bool refill,List<string> report)
    {
        using var timer=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);
        using var physics=ProfilerRecorder.StartNew(ProfilerCategory.Physics,"Physics2D.Simulate",1,ProfilerRecorderOptions.Default|ProfilerRecorderOptions.SumAllSamplesInFrame);
        var frames=new List<double>(count);var steps=new List<double>(count);
        int peak=0,created=CombatRound.CreatedCount;
        for(int i=0;i<count;i++)
        {
            yield return null;
            if(refill)Fill();
            peak=Mathf.Max(peak,CombatRound.Live.Count);
            frames.Add(timer.LastValue/1000000.0);steps.Add(physics.LastValue/1000000.0);
        }
        frames.Sort();steps.Sort();
        report.Add($"{label}: frames={count}, peak={peak}, new bullet objects={CombatRound.CreatedCount-created}; CPU mean={frames.Average():F3} p50={frames[count/2]:F3} p95={frames[(int)(count*.95)]:F3} max={frames[^1]:F3} ms; physics p95={steps[(int)(count*.95)]:F3} ms.");
        File.WriteAllLines(Folder+"/StandaloneCPU.txt",report);
    }
}
public sealed class HeavenProfilePipelineAsset:UnityEngine.Rendering.RenderPipelineAsset<HeavenProfilePipeline>
{
    public override string renderPipelineShaderTag=>string.Empty;
    protected override UnityEngine.Rendering.RenderPipeline CreatePipeline()=>new HeavenProfilePipeline();
}
public sealed class HeavenProfilePipeline:UnityEngine.Rendering.RenderPipeline
{
    protected override void Render(UnityEngine.Rendering.ScriptableRenderContext context,List<UnityEngine.Camera> cameras){}
}
#endif
