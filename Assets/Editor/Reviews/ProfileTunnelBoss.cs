#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;

[InitializeOnLoad]
public static class ProfileTunnelBoss
{
    public const string Folder="ArtSource/TunnelProfile";
    static double deadline;
    static ProfileTunnelBoss()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("TunnelProfile",false))return;
            new GameObject("Full tunnel frame profile").AddComponent<TunnelFrameProfile>();deadline=EditorApplication.timeSinceStartup+180;
        };
        EditorApplication.update+=()=>
        {
            if(deadline==0)return;string file=Folder+"/"+SessionState.GetString("TunnelProfileLabel","Baseline")+".txt";
            if(File.Exists(file)){SessionState.SetBool("TunnelProfile",false);TunnelProfileExport.Export(SessionState.GetString("TunnelProfileLabel","Baseline"));EditorApplication.Exit(File.ReadAllText(file).StartsWith("PASS") ? 0 : 1);}
            else if(EditorApplication.timeSinceStartup>deadline){File.WriteAllText(file,"FAIL timeout");EditorApplication.Exit(1);}
        };
    }
    public static void Run()=>Begin("Baseline");
    public static void RunOptimized()=>Begin("Optimized");
    public static void RunInvestigate()=>Begin("Investigate");
    static void Begin(string label)
    {
        Directory.CreateDirectory(Folder);File.Delete(Folder+"/"+label+".txt");
        SessionState.SetString("TunnelProfileLabel",label);SessionState.SetBool("TunnelProfile",true);
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");EditorApplication.isPlaying=true;
    }
}
public sealed class TunnelFrameProfile : MonoBehaviour
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
    readonly List<string> errors=new List<string>();
    readonly List<(string name,ProfilerRecorder recorder)> recorders=new List<(string,ProfilerRecorder)>();
    readonly StringBuilder csv=new StringBuilder();
    readonly List<double> deltas=new List<double>();
    string label;
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Screen.SetResolution(1280,720,false);Application.logMessageReceived+=Log;label=SessionState.GetString("TunnelProfileLabel","Baseline");}
    void OnDestroy(){Application.logMessageReceived-=Log;foreach(var item in recorders)item.recorder.Dispose();}
    void Log(string text,string trace,LogType type){if((type==LogType.Error || type==LogType.Exception) && !trace.Contains("UnityEditor.Search"))errors.Add(text);}
    static void Set(object target,string field,object value)=>target.GetType().GetField(field,Hidden).SetValue(target,value);
    IEnumerator Start()
    {
        yield return null;GameRules.StartNewRun();LevelRun.Reset(LevelId.Tunnel,17381);CloudTransition.Load("L1");
        while(SceneManager.GetActiveScene().name!="L1")yield return null;yield return null;
        var flow=FindAnyObjectByType<WaveOrchestrer>();flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();
        var ship=FindAnyObjectByType<Shipcontroller>();ship.enabled=false;ship.ObserverMode=true;
        foreach(var weapon in ship.GetComponentsInChildren<AWeapon>()){weapon.StopFiring();weapon.enabled=false;}
        while(CloudTransition.Busy || !WavePreparation.Ready)yield return null;
        GameRules.playerDisabled=false;Time.timeScale=1;CombatRound.ClearAll();
        foreach(var enemy in FindObjectsByType<DistrictEnemy>())Destroy(enemy.gameObject);yield return null;
        ship.transform.position=new Vector3(0,Camera.DownLimit+2,-1);
        // Register script marker recorders explicitly before their first use.
        foreach(var name in new[]{"TunnelSaw.BounceBurst","CombatRound.Launch","CombatRound.Create","PSXImpact.Vanish","ProjectileGlow.Render"})Add(ProfilerCategory.Scripts,name);
        var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
        var names=new List<string>();
        foreach(var handle in handles)
        {
            var d=ProfilerRecorderHandle.GetDescription(handle);names.Add(d.Category.Name+" / "+d.Name);
            if(d.Name=="Main Thread" || d.Name=="GC.Alloc" || d.Name.Contains("Physics2D.") && (d.Name.Contains("Simulate") || d.Name.Contains("SyncTransforms")) || d.Name.Contains("Gfx.WaitFor"))Add(d.Category,d.Name);
        }
        File.WriteAllLines(ProfileTunnelBoss.Folder+"/Markers.txt",names.OrderBy(s=>s));
        csv.Append("frame,dt_ms,live,created");foreach(var item in recorders)csv.Append(","+item.name.Replace(',','_'));csv.AppendLine();
        UnityEngine.Profiling.Profiler.logFile=Path.GetFullPath("Temp/Tunnel-"+label+".raw");
        UnityEngine.Profiling.Profiler.enableBinaryLog=true;UnityEngine.Profiling.Profiler.enabled=true;
        var boss=(TunnelSawBoss)DistrictEnemy.Spawn(typeof(TunnelSawBoss),new Vector3(0,Camera.UpLimit-2,1));yield return null;
        Set(boss,"entered",true);Set(boss,"velocity",new Vector2(1,-.7f).normalized);
        // Natural wall impacts first. Then force phase two and all three saws' ordinary patrols.
        float started=Time.realtimeSinceStartup;bool phase=false;int frame=0;
        while(Time.realtimeSinceStartup-started<22)
        {
            yield return null;
            double ms=Time.unscaledDeltaTime*1000;deltas.Add(ms);
            csv.Append(frame++).Append(',').Append(ms.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(CombatRound.Live.Count).Append(',').Append(CombatRound.CreatedCount);
            foreach(var item in recorders)csv.Append(',').Append(item.recorder.Valid ? item.recorder.LastValue : -1);csv.AppendLine();
            var physics=recorders.FirstOrDefault(r=>r.name=="Physics2D.Simulate").recorder;
            if(label=="Investigate" && physics.Valid && physics.LastValue>20000000)
            {
                var active=CombatRound.Live.Where(s=>s.GetComponent<Rigidbody2D>().simulated).ToArray();
                File.WriteAllText(ProfileTunnelBoss.Folder+"/StallState.txt","Ignored hostile-hostile="+Physics2D.GetIgnoreLayerCollision(9,9)+"; hostile-enemy="+Physics2D.GetIgnoreLayerCollision(9,8)+
                    "; player="+ship.transform.position+" bounds="+ship.GetComponent<Collider2D>().bounds+"\nSimulated="+active.Length+" / "+CombatRound.Live.Count+"\n"+
                    string.Join("\n",active.Select(s=>s.name+" layer="+s.gameObject.layer+" at="+s.transform.position+" radius="+s.transform.localScale+" shapes="+s.GetComponent<CustomCollider2D>().shapeCount)));
                break;
            }
            if(!phase && Time.realtimeSinceStartup-started>7){phase=true;boss.health*=.45f;}
        }
        UnityEngine.Profiling.Profiler.enabled=false;UnityEngine.Profiling.Profiler.enableBinaryLog=false;
        File.WriteAllText(ProfileTunnelBoss.Folder+"/"+label+".csv",csv.ToString());
        DesertReview.Capture(label,ProfileTunnelBoss.Folder);
        deltas.Sort();
        File.WriteAllText(ProfileTunnelBoss.Folder+"/"+label+".txt",(errors.Count==0 ? "PASS" : "FAIL")+"\nActual 22-second full-frame tunnel profile, active player collider, both phases and natural bounces.\n"+
            "Frames="+frame+"; median="+deltas[deltas.Count/2].ToString("F3")+"ms; p99="+deltas[(int)(deltas.Count*.99)].ToString("F3")+"ms; max="+deltas.Last().ToString("F3")+"ms\n"+string.Join("\n",errors));
    }
    void Add(ProfilerCategory category,string name)
    {
        if(recorders.Any(r=>r.name==name))return;
        var recorder=ProfilerRecorder.StartNew(category,name,1,ProfilerRecorderOptions.Default|ProfilerRecorderOptions.SumAllSamplesInFrame);
        recorders.Add((name,recorder));
    }
}
#endif
