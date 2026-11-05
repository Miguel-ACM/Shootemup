#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Profiling;

[InitializeOnLoad]
public static class VerifyTunnelVariety
{
    public const string Folder="C:/Users/Miguel/Shootemup/ArtSource/TunnelVarietyReview";
    static double deadline;
    static VerifyTunnelVariety()
    {
        EditorApplication.playModeStateChanged+=s=>{
            if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("TunnelVarietyReview",false))return;
            deadline=EditorApplication.timeSinceStartup+1200;new GameObject("Tunnel variety checks").AddComponent<TunnelVarietyRuntime>();
        };
        EditorApplication.update+=()=>{if(deadline>0&&EditorApplication.timeSinceStartup>deadline){File.WriteAllText(Folder+"/Runtime.txt","FAIL timeout");EditorApplication.Exit(1);}};
    }
    static void Guard()
    {
        if(!Application.isBatchMode||!Application.dataPath.Replace('\\','/').Contains("/Temp/DecisionHoverReview/"))throw new Exception("Isolated command-line review only");
    }
    public static void Run()
    {
        Guard();Directory.CreateDirectory(Folder);PlayerSettings.companyName="ShootemupReview";PlayerSettings.productName="TunnelVarietyReview";
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");SessionState.SetBool("TunnelVarietyReview",true);EditorApplication.isPlaying=true;
    }
    public static void Build()
    {
        Guard();
        var batch=File.ReadAllText(Folder+"/Runtime.txt");
        if(!batch.StartsWith("PASS"))
        {
            var expected=new[]{"FAIL actual scrolling-wall contact costs one segment and releases ship","FAIL wall contact respects normal hit invulnerability"};
            var failed=batch.Split('\n').Select(s=>s.Trim()).Where(s=>s.StartsWith("FAIL ")).ToArray();
            if(!failed.SequenceEqual(expected)||!File.ReadAllText(Folder+"/Fixes.txt").StartsWith("PASS"))throw new Exception("Batch failures need targeted verification");
        }
        PlayerSettings.companyName="DefaultCompany";PlayerSettings.productName="seup";
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
            target=BuildTarget.StandaloneWindows64,locationPathName="C:/Users/Miguel/Shootemup/Build/TunnelVariety/Shootemup.exe"});
        bool ok=report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded;
        File.WriteAllText(Folder+"/Build.txt",(ok?"PASS":"FAIL")+"\nErrors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings);EditorApplication.Exit(ok?0:1);
    }
    public static void Fixes(){Guard();SessionState.SetBool("TunnelVarietyFixes",true);Run();}
}

public sealed class TunnelVarietyRuntime : MonoBehaviour
{
    readonly List<string> notes=new(),errors=new();
    WaveWatcher watcher;
    void Check(bool value,string text)
    {
        notes.Add((value?"PASS ":"FAIL ")+text);if(!value)errors.Add(text);File.WriteAllLines(VerifyTunnelVariety.Folder+"/Progress.txt",notes);
    }
    void Log(string message,string stack,LogType kind){if(kind==LogType.Error||kind==LogType.Exception)errors.Add(message+"\n"+stack);}
    IEnumerator Ready(){yield return null;while(CloudTransition.Busy||!WavePreparation.Ready)yield return null;yield return null;}
    static void Clear()
    {
        TunnelPassage.Clear();foreach(var enemy in AEnemy.Live.ToArray())Destroy(enemy.gameObject);
        foreach(var round in CombatRound.Live.ToArray())round.Vanish();
    }
    static SolidSceneryRuntime.MeshDump Mesh(Mesh mesh,Matrix4x4 matrix)=>new SolidSceneryRuntime.MeshDump{
        name=mesh.name,vertices=mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray(),uv=mesh.uv,triangles=mesh.triangles};
    void Export(string name)
    {
        var meshes=new List<SolidSceneryRuntime.MeshDump>();
        foreach(var filter in FindObjectsByType<MeshFilter>())
            if(filter.sharedMesh!=null&&(filter.GetComponentInParent<TunnelPassage>()!=null||filter.GetComponentInParent<DistrictEnemy>()!=null))
                meshes.Add(Mesh(filter.sharedMesh,filter.transform.localToWorldMatrix));
        File.WriteAllText(VerifyTunnelVariety.Folder+"/"+name+".json",JsonUtility.ToJson(new SolidSceneryRuntime.SceneDump{
            meshes=meshes.ToArray(),rounds=CombatRound.Live.Select(r=>r.transform.position).ToArray()}));
    }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Application.logMessageReceived+=Log;QualitySettings.renderPipeline=ScriptableObject.CreateInstance<CelestialNoRenderAsset>();
        while(CloudTransition.Busy||Time.frameCount<5)yield return null;
        WaveWatcher.Launch();while(WaveWatcher.Current==null||!WaveWatcher.Current.Ready||CloudTransition.Busy||!WavePreparation.Ready)yield return null;
        watcher=WaveWatcher.Current;if(watcher.Looping)watcher.ToggleLoop();
        Time.captureDeltaTime=1f/60;QualitySettings.renderPipeline=ScriptableObject.CreateInstance<CelestialNoRenderAsset>();
        watcher.SelectLevel(LevelId.Tunnel);watcher.SelectView(WatcherView.Enemies);yield return Ready();
        if(SessionState.GetBool("TunnelVarietyFixes",false)){Clear();yield return null;yield return ContactAndLoad();Finish(true);yield break;}
        File.WriteAllBytes(VerifyTunnelVariety.Folder+"/Atlas.png",((Texture2D)CombatMaterials.Fleet.mainTexture).EncodeToPNG());
        for(int layout=1;layout<=TunnelPassage.LayoutCount;layout++)
            File.WriteAllText(VerifyTunnelVariety.Folder+"/Layout-"+layout+".json",JsonUtility.ToJson(Mesh(IndustrialMeshFactory.TunnelPassageMesh(layout),Matrix4x4.identity)));
        foreach(var difficulty in new[]{RunDifficulty.Normal,RunDifficulty.Hard,RunDifficulty.Easy})
        {
            watcher.SelectDifficulty(difficulty);yield return Ready();
            for(int layout=1;layout<=TunnelPassage.LayoutCount;layout++)
            {
                Clear();yield return null;
                var wave=TunnelPassageWaves.Create(16,layout);float start=Time.time;int unit=0,penetrations=0,peak=0,frame=0;
                float clearedAt=0;bool seen=false;int created=CombatRound.CreatedCount;
                while(Time.time-start<wave.duration+.2f)
                {
                    float t=Time.time-start;
                    while(unit<wave.units.Count&&wave.units[unit].time<=t)
                    {
                        var next=wave.units[unit++];var enemy=DistrictEnemy.Spawn(EnemyCatalog.Resolve(next.enemy),Vector3.zero);enemy.FlyPhrase(next);
                    }
                    yield return null;seen|=TunnelPassage.Active;
                    if(seen&&!TunnelPassage.Active&&clearedAt==0)clearedAt=Time.time-start;
                    foreach(var enemy in AEnemy.Live)
                        if(CombatWalls.Overlaps(enemy.transform.position,WaveGeometry.Radius(enemy.GetType().Name)))penetrations++;
                    peak=Mathf.Max(peak,CombatRound.Live.Count);
                    if(difficulty==RunDifficulty.Normal&&layout==6&&t>=frame*.5f&&frame<25){Export("Motion-"+frame.ToString("D2"));frame++;}
                }
                Check(seen&&clearedAt>7&&clearedAt<14,difficulty+" layout "+layout+" scrolls out after "+clearedAt.ToString("F2")+" seconds");
                Check(penetrations==0,difficulty+" layout "+layout+" enemy envelopes clear sloped walls: "+penetrations);
                Check(peak>10&&CombatRound.CreatedCount==created,difficulty+" layout "+layout+" sustained warmed fire, peak "+peak);
            }
        }
        Clear();yield return null;
        int trapped=0,samples=0;
        for(int layout=1;layout<=TunnelPassage.LayoutCount;layout++)
        {
            var sections=TunnelPassage.Sections(layout);
            for(float travel=8;travel<TunnelPassage.Length(layout)+22;travel+=2)
            {
                CombatWalls.Solid.Clear();foreach(var wall in sections){var r=wall.Translated(-travel);if(r.Top>=Camera.DownLimit-4&&r.Bottom<=Camera.UpLimit+6)CombatWalls.Solid.Add(r);}
                for(float x=Camera.LeftLimit;x<=Camera.RightLimit;x+=.6f)for(float y=Camera.DownLimit;y<=Camera.UpLimit;y+=.9f)
                {
                    var p=CombatWalls.Constrain(new Vector3(x,y),new Vector3(x+.04f,y+.03f));samples++;
                    if(CombatWalls.Overlaps(p,CombatWalls.HullRadius-.001f)||p.x<Camera.LeftLimit||p.x>Camera.RightLimit||p.y<Camera.DownLimit||p.y>Camera.UpLimit)trapped++;
                }
            }
        }
        Check(trapped==0,"sloped joins and bottom corners release pilot: "+samples+" samples, "+trapped+" trapped");
        yield return ContactAndLoad();Finish(false);
    }
    IEnumerator ContactAndLoad()
    {
        CombatWalls.Solid.Clear();CombatWalls.Solid.Add(new CombatWall(-2,2,1,-1,4,4));
        Check(CombatWalls.Sweep(new Vector2(-3,0),new Vector2(3,0),0,out var hit,out var normal)&&Mathf.Abs(hit.x)<.001f&&normal.x<-.8f,"fast shot meets exact diagonal face");
        Check(!CombatWalls.Sweep(new Vector2(-1,-2),new Vector2(-1,0),0,out _,out _),"empty space inside diagonal bounding box stays open");
        var player=CombatVisuals.Player;var ship=player.GetComponent<Shipcontroller>();ship.ObserverMode=false;ship.enabled=true;ship.Repair(6);
        while(ship.IsInvulnerable)yield return null;
        player.position=new Vector3(1,0,-1);float health=ship.health;
        yield return null;yield return null;
        Check(ship.health==health-1&&!CombatWalls.Overlaps(player.position,CombatWalls.HullRadius-.001f),"actual scrolling-wall contact costs one segment and releases ship");
        player.position=new Vector3(1,0,-1);yield return null;yield return null;
        Check(ship.health==health-1,"wall contact respects normal hit invulnerability");
        CombatWalls.Solid.Clear();while(ship.IsInvulnerable)yield return null;
        TunnelPassage.Begin(2);player.position=new Vector3(Camera.LeftLimit+.5f,Camera.DownLimit+1,-1);health=ship.health;
        float caughtEnd=Time.time+2.5f;while(Time.time<caughtEnd&&ship.health==health)yield return null;
        Check(ship.health==health-1,"a scrolling passage catches and damages a stationary pilot");
        ship.ObserverMode=true;ship.enabled=false;TunnelPassage.Clear();player.position=new Vector3(0,Camera.DownLimit+1,-1);
        float shieldEnd=Time.time+2.3f;while(Time.time<shieldEnd)yield return null;
        int allocations=CombatRound.CreatedCount;
        foreach(bool walls in new[]{false,true})
        {
            foreach(var round in CombatRound.Live.ToArray())round.Vanish();TunnelPassage.Clear();yield return null;
            if(walls)TunnelPassage.Begin(6);
            using(var recorder=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1))
            {
                yield return null;yield return null;
                for(int i=0;i<3000;i++)CombatRound.Launch<SimpleRound>(new Vector3((i%31-15)*.08f,Camera.UpLimit-1-(i/31%21)*.12f),CombatVisuals.Rotate(Vector2.down,i*137.5f),2,.18f);
                var times=new List<double>();float end=Time.time+1.3f;while(Time.time<end){yield return null;times.Add(recorder.LastValue/1000000.0);}
                times.Sort();notes.Add((walls?"Walls":"Baseline")+" headless complete frames around 3000-round burst: max "+times.Last().ToString("F2")+" ms, p95 "+times[(int)(times.Count*.95)].ToString("F2")+" ms (no graphics)");
            }
        }
        Check(CombatRound.CreatedCount==allocations,"dense diagonal-wall burst creates no new rounds");
        CombatDecision.Begin();yield return null;Check(!TunnelPassage.Active&&!CombatWalls.Active,"decisions still clear all passage geometry");CombatDecision.End();
    }
    void Finish(bool fixes)
    {
        SessionState.SetBool("TunnelVarietyReview",false);SessionState.SetBool("TunnelVarietyFixes",false);
        File.WriteAllText(VerifyTunnelVariety.Folder+(fixes?"/Fixes.txt":"/Runtime.txt"),(errors.Count==0?"PASS":"FAIL")+"\nNative headless review; CPU previews exclude the gameplay renderer.\n"+string.Join("\n",notes)+"\n"+string.Join("\n",errors));
        Application.logMessageReceived-=Log;EditorApplication.Exit(errors.Count==0?0:1);
    }
}
#endif
