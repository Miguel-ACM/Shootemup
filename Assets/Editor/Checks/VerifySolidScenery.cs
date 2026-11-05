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
public static class VerifySolidScenery
{
    public const string Folder="C:/Users/Miguel/Shootemup/ArtSource/SolidSceneryReview";
    static double deadline;
    static VerifySolidScenery()
    {
        EditorApplication.playModeStateChanged+=s=>{
            if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("SolidSceneryReview",false))return;
            deadline=EditorApplication.timeSinceStartup+1200;
            new GameObject("Solid scenery checks").AddComponent<SolidSceneryRuntime>();
        };
        EditorApplication.update+=()=>{if(deadline>0&&EditorApplication.timeSinceStartup>deadline){File.WriteAllText(Folder+"/Runtime.txt","FAIL timeout");EditorApplication.Exit(1);}};
    }
    static void Guard()
    {
        if(!Application.isBatchMode||!Application.dataPath.Replace('\\','/').Contains("/Temp/DecisionHoverReview/"))throw new Exception("Isolated command-line review only");
    }
    public static void Run()
    {
        Guard();Directory.CreateDirectory(Folder);
        PlayerSettings.companyName="ShootemupReview";PlayerSettings.productName="SolidSceneryReview";
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("SolidSceneryReview",true);EditorApplication.isPlaying=true;
    }
    public static void Extras()
    {
        Guard();SessionState.SetBool("SolidSceneryExtras",true);Run();
    }
    public static void Mines()
    {
        Guard();SessionState.SetBool("SolidSceneryMines",true);Run();
    }
    public static void Build()
    {
        Guard();if(!File.ReadAllText(Folder+"/Runtime.txt").StartsWith("PASS"))throw new Exception("Review must pass");
        if(!File.ReadAllText(Folder+"/Extras.txt").StartsWith("PASS"))throw new Exception("Edge review must pass");
        if(!File.ReadAllText(Folder+"/Mines.txt").StartsWith("PASS"))throw new Exception("Weak-shot mine review must pass");
        PlayerSettings.companyName="DefaultCompany";PlayerSettings.productName="seup";
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
            scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),target=BuildTarget.StandaloneWindows64,
            locationPathName="C:/Users/Miguel/Shootemup/Build/SolidScenery/Shootemup.exe"});
        bool ok=report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded;
        File.WriteAllText(Folder+"/Build.txt",(ok?"PASS":"FAIL")+"\nErrors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings);
        EditorApplication.Exit(ok?0:1);
    }
}

public sealed class SolidSceneryRuntime : MonoBehaviour
{
    readonly List<string> notes=new(),errors=new();
    WaveWatcher watcher;
    bool extras,minesOnly;
    void Check(bool value,string text)
    {
        notes.Add((value?"PASS ":"FAIL ")+text);if(!value)errors.Add(text);
        File.WriteAllLines(VerifySolidScenery.Folder+(minesOnly?"/Mines-Progress.txt":extras?"/Extras-Progress.txt":"/Progress.txt"),notes);
    }
    void Log(string message,string stack,LogType kind){if(kind==LogType.Error||kind==LogType.Exception)errors.Add(message+"\n"+stack);}
    IEnumerator Ready(){yield return null;while(CloudTransition.Busy||!WavePreparation.Ready)yield return null;yield return null;}
    [Serializable] public class MeshDump { public string name;public Vector3[] vertices;public Vector2[] uv;public int[] triangles; }
    [Serializable] public class SceneDump { public MeshDump[] meshes;public Vector3[] rounds; }
    static MeshDump Mesh(Mesh mesh,Matrix4x4 matrix)=>new MeshDump{name=mesh.name,vertices=mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray(),uv=mesh.uv,triangles=mesh.triangles};
    void ExportScene(string label)
    {
        var meshes=new List<MeshDump>();
        foreach(var filter in FindObjectsByType<MeshFilter>())
            if(filter.sharedMesh!=null&&(filter.GetComponentInParent<TunnelPassage>()!=null||filter.GetComponentInParent<DistrictEnemy>()!=null))
                meshes.Add(Mesh(filter.sharedMesh,filter.transform.localToWorldMatrix));
        File.WriteAllText(VerifySolidScenery.Folder+"/"+label+".json",JsonUtility.ToJson(new SceneDump{meshes=meshes.ToArray(),rounds=CombatRound.Live.Select(r=>r.transform.position).ToArray()}));
    }
    IEnumerator Start()
    {
        extras=SessionState.GetBool("SolidSceneryExtras",false);
        minesOnly=SessionState.GetBool("SolidSceneryMines",false);
        DontDestroyOnLoad(gameObject);Application.logMessageReceived+=Log;
        QualitySettings.renderPipeline=ScriptableObject.CreateInstance<CelestialNoRenderAsset>();
        while(CloudTransition.Busy||Time.frameCount<5)yield return null;
        WaveWatcher.Launch();
        while(WaveWatcher.Current==null||!WaveWatcher.Current.Ready||CloudTransition.Busy||!WavePreparation.Ready)yield return null;
        watcher=WaveWatcher.Current;if(watcher.Looping)watcher.ToggleLoop();
        Time.captureDeltaTime=1f/60;QualitySettings.renderPipeline=ScriptableObject.CreateInstance<CelestialNoRenderAsset>();
        File.WriteAllBytes(VerifySolidScenery.Folder+"/Atlas.png",((Texture2D)CombatMaterials.Fleet.mainTexture).EncodeToPNG());
        File.WriteAllText(VerifySolidScenery.Folder+"/Mine.json",JsonUtility.ToJson(Mesh(IndustrialMeshFactory.DepthMineMesh(),Matrix4x4.identity)));
        if(minesOnly){yield return MineCases();Finish();yield break;}
        if(extras){yield return EdgeCases();Finish();yield break;}
        foreach(var mode in new[]{RunDifficulty.Normal,RunDifficulty.Hard,RunDifficulty.Easy})
        {
            watcher.SelectDifficulty(mode);watcher.SelectLevel(LevelId.Tunnel);watcher.SelectView(WatcherView.Waves);yield return Ready();
            foreach(int budget in new[]{6,10,16})
            {
                watcher.SelectEncounter(budget);yield return Ready();
                float start=Time.time;bool seen=false,clear=false;int penetrations=0,peak=0;bool captured=false;
                int created=CombatRound.CreatedCount;
                while(Time.time-start<19)
                {
                    yield return null;
                    seen|=TunnelPassage.Active;
                    peak=Mathf.Max(peak,CombatRound.Live.Count);
                    if(seen&&!TunnelPassage.Active)clear=true;
                    foreach(var enemy in AEnemy.Live)
                    {
                        float radius=WaveGeometry.Radius(enemy.GetType().Name);
                        Vector2 p=enemy.transform.position;
                        foreach(var wall in CombatWalls.Solid)
                        {
                            Vector2 nearest=new Vector2(Mathf.Clamp(p.x,wall.xMin,wall.xMax),Mathf.Clamp(p.y,wall.yMin,wall.yMax));
                            if((nearest-p).sqrMagnitude<radius*radius)penetrations++;
                        }
                    }
                    if(mode==RunDifficulty.Normal&&!captured&&Time.time-start>3.5f){ExportScene("Tunnel-"+budget);captured=true;}
                }
                Check(seen&&clear,mode+" passage "+budget+" enters from above and scrolls fully out");
                Check(penetrations==0,mode+" passage "+budget+" enemy envelopes clear masonry ("+penetrations+")");
                Check(peak>5&&CombatRound.CreatedCount==created,mode+" passage "+budget+" sustained fire and warmed rounds; peak="+peak);
            }
            watcher.SelectLevel(LevelId.Underwater);watcher.SelectView(WatcherView.Enemies);yield return Ready();
            watcher.SelectEntry(WatcherCatalog.For(WatcherView.Enemies,LevelId.Underwater).FindIndex(e=>e.Enemy==typeof(DepthMine)));yield return Ready();
            foreach(var enemy in AEnemy.Live.ToArray())Destroy(enemy.gameObject);
            foreach(var shot in CombatRound.Live.ToArray())shot.Vanish();
            yield return null;
            var mine=(DepthMine)DistrictEnemy.Spawn(typeof(DepthMine),new Vector3(0,2,1));yield return null;
            int before=CombatRound.Live.Count;float until=Time.time+1.8f;
            while(Time.time<until)yield return null;
            Check(mine!=null&&!mine.Primed&&CombatRound.Live.Count==before,mode+" mine remains inert without a shot");
            int count=CombatRound.CreatedCount;
            PlayerRound.Spawn(new Vector3(0,0,-1),Vector2.up,10,12);
            until=Time.time+.8f;while(Time.time<until&&mine!=null&&!mine.Primed)yield return null;
            Check(mine!=null&&mine.Primed&&CombatRound.Live.Count==before,mode+" player projectile starts visible fuse before the burst");
            until=Time.time+.7f;while(Time.time<until)yield return null;
            int rounds=CombatRound.Live.OfType<SimpleRound>().Count();
            int quadrants=0;foreach(var round in CombatRound.Live){var d=round.Heading;quadrants|=1<<((d.x<0?1:0)+(d.y<0?2:0));}
            Check(mine==null&&rounds==Difficulty.Dense(24)&&quadrants==15,mode+" shot mine bursts once in all directions; rounds="+rounds);
            Check(CombatRound.CreatedCount==count,mode+" mine burst reuses prepared rounds");
            var cleared=(DepthMine)DistrictEnemy.Spawn(typeof(DepthMine),new Vector3(2,2,1));yield return null;
            before=CombatRound.Live.Count;cleared.Depart();yield return null;
            Check(CombatRound.Live.Count==before,mode+" clearing an unshot mine causes no explosion");
        }
        watcher.SelectLevel(LevelId.Tunnel);yield return Ready();
        foreach(var enemy in AEnemy.Live.ToArray())Destroy(enemy.gameObject);
        foreach(var shot in CombatRound.Live.ToArray())shot.Vanish();yield return null;
        TunnelPassage.Clear();CombatWalls.Solid.Add(new Rect(-1,0,2,1));
        Check(CombatWalls.Sweep(new Vector2(0,-3),new Vector2(0,3),0,out var hit,out _)&&Mathf.Abs(hit.y)<.001f,"fast segment cannot tunnel through wall");
        var slide=CombatWalls.Constrain(new Vector3(-2,-1),new Vector3(.8f,.7f));
        Check(slide.y<-.33f&&slide.x>.6f,"pilot slides along the solid face");
        Check(CombatWalls.BeamLength(new Vector2(0,-3),Vector2.up,10)==3,"continuous beam stops at solid face");
        var path=new[]{new Vector3(0,-3),new Vector3(0,-1),new Vector3(0,2),new Vector3(2,3)};
        CombatWalls.ClipPath(path,0);Check(path[2]==Vector3.zero&&path[3]==Vector3.zero,"curved player beam has no damage path beyond wall");
        foreach(PlayerDelivery delivery in Enum.GetValues(typeof(PlayerDelivery)))
        {
            var shot=PlayerRound.Spawn(new Vector3(0,-1,-1),Vector2.up,10,120,delivery);
            yield return null;yield return null;
            Check(shot==null||!shot.gameObject.activeSelf,delivery+" player shot is absorbed by wall");
        }
        var hostile=CombatRound.Launch<SimpleRound>(new Vector3(0,-1),Vector2.up,120,.2f);
        yield return null;yield return null;
        Check(hostile==null||!hostile.gameObject.activeSelf,"hostile projectile is absorbed by wall");
        var player=CombatVisuals.Player;Vector3 saved=player.position;player.position=new Vector3(0,2,-1);
        var laser=CombatRound.Launch<PulseLaser>(new Vector3(0,-3),Vector2.up,0);
        laser.warning=.01f;laser.activeTime=.5f;laser.length=10;
        var ship=player.GetComponent<Shipcontroller>();ship.ObserverMode=false;float health=ship.health;
        float end=Time.time+.25f;while(Time.time<end)yield return null;
        Check(ship.health==health,"wall shelters pilot from hostile continuous beam");
        CombatWalls.Solid.Clear();
        end=Time.time+.2f;while(Time.time<end)yield return null;
        Check(ship.health<health,"same hostile beam damages the unsheltered pilot");
        ship.ObserverMode=true;player.position=saved;
        var free=PlayerRound.Spawn(new Vector3(0,-1,-1),Vector2.up,10,20);yield return null;yield return null;
        Check(free!=null&&free.gameObject.activeSelf&&free.transform.position.y>-1,"projectiles travel normally when walls are absent");
        Finish();
    }
    void Finish()
    {
        SessionState.SetBool("SolidSceneryReview",false);SessionState.SetBool("SolidSceneryExtras",false);SessionState.SetBool("SolidSceneryMines",false);
        File.WriteAllText(VerifySolidScenery.Folder+(minesOnly?"/Mines.txt":extras?"/Extras.txt":"/Runtime.txt"),(errors.Count==0?"PASS":"FAIL")+"\nNative headless simulation; offline mesh previews exclude the game renderer.\n"+string.Join("\n",notes)+"\n"+string.Join("\n",errors));
        Application.logMessageReceived-=Log;EditorApplication.Exit(errors.Count==0?0:1);
    }
    IEnumerator EdgeCases()
    {
        watcher.SelectDifficulty(RunDifficulty.Normal);watcher.SelectLevel(LevelId.Tunnel);watcher.SelectView(WatcherView.Waves);yield return Ready();
        foreach(int budget in new[]{6,10,16})
        {
            watcher.SelectEncounter(budget);yield return Ready();
            float start=Time.time;int frame=0;bool captured=false;
            while(Time.time-start<6)
            {
                yield return null;
                if(budget==6&&Time.time-start>frame*.25f){ExportScene("Motion-"+frame.ToString("D2"));frame++;}
                if(!captured&&Time.time-start>3.5f){ExportScene("Tunnel-"+budget);captured=true;}
            }
        }
        TunnelPassage.Clear();foreach(var enemy in AEnemy.Live.ToArray())Destroy(enemy.gameObject);
        foreach(var shot in CombatRound.Live.ToArray())shot.Vanish();yield return null;
        int trapped=0,cases=0;
        for(int layout=1;layout<=3;layout++)
            for(float travel=0;travel<60;travel+=.5f)
            {
                CombatWalls.Solid.Clear();
                foreach(var r in TunnelPassage.Sections(layout))CombatWalls.Solid.Add(new Rect(r.x,r.y-travel,r.width,r.height));
                for(float x=Camera.LeftLimit;x<=Camera.RightLimit;x+=.4f)
                    for(float y=Camera.DownLimit;y<=Camera.UpLimit;y+=.7f)
                    {
                        Vector3 p=CombatWalls.Constrain(new Vector3(x,y),new Vector3(x,y));cases++;
                        if(p.x<Camera.LeftLimit||p.x>Camera.RightLimit||p.y<Camera.DownLimit||p.y>Camera.UpLimit)trapped++;
                        foreach(var r in CombatWalls.Solid)
                            if(Rect.MinMaxRect(r.xMin-CombatWalls.HullRadius+.001f,r.yMin-CombatWalls.HullRadius+.001f,r.xMax+CombatWalls.HullRadius-.001f,r.yMax+CombatWalls.HullRadius-.001f).Contains(p))trapped++;
                    }
            }
        Check(trapped==0,"moving wall recovery stays inside arena and outside masonry, including bottom corners: "+cases+" samples, "+trapped+" trapped");
        CombatWalls.Solid.Clear();
        using(var recorder=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1))
        {
            yield return null;yield return null;
            TunnelPassage.Begin(1);double maximum=0;
            for(int i=0;i<12;i++){yield return null;maximum=Math.Max(maximum,recorder.LastValue/1000000.0);}
            notes.Add("Measured headless main-thread maximum around wall creation and following physics frames: "+maximum.ToString("F2")+" ms (no graphics rendering)");
        }
        CombatDecision.Begin();yield return null;
        Check(!TunnelPassage.Active&&!CombatWalls.Active,"decisions clear foreground passages");CombatDecision.End();
        watcher.SelectLevel(LevelId.Underwater);watcher.SelectView(WatcherView.Enemies);yield return Ready();
        foreach(int seed in new[]{17381,42042,86753})
        {
            LevelRun.Preview(LevelId.Underwater,seed,1);int mines=0;
            foreach(int budget in new[]{2,3,4,6,8,10,12,14,16,20})
            {mines+=WaveComposer.Create(budget).units.Count(u=>u.enemy==nameof(DepthMine));yield return null;}
            Check(mines>0,"seed "+seed+" includes mines in ordinary underwater waves: "+mines);
        }
        yield return MineCases();
    }
    IEnumerator MineCases()
    {
        watcher.SelectLevel(LevelId.Underwater);watcher.SelectView(WatcherView.Enemies);yield return Ready();
        watcher.SelectLoop(12);yield return Ready();
        foreach(var mode in new[]{RunDifficulty.Normal,RunDifficulty.Hard,RunDifficulty.Easy})
        {
            watcher.SelectDifficulty(mode);yield return Ready();
            foreach(var enemy in AEnemy.Live.ToArray())Destroy(enemy.gameObject);
            foreach(var shot in CombatRound.Live.ToArray())shot.Vanish();yield return null;
            var mine=(DepthMine)DistrictEnemy.Spawn(typeof(DepthMine),new Vector3(0,2,1));yield return null;
            float startY=mine.transform.position.y,end=Time.time+.6f;while(Time.time<end)yield return null;
            Check(mine!=null&&!mine.Primed&&mine.transform.position.y<startY-.25f&&CombatRound.Live.Count==0,mode+" standalone mine drifts without shooting");
            PlayerRound.Spawn(mine.transform.position+Vector3.down,Vector2.up,.01f,15);
            end=Time.time+.5f;while(Time.time<end&&!mine.Primed)yield return null;
            Check(mine.Primed,mode+" loop 12 moving mine responds to one weak .01-damage player round");
            var p=mine.transform.position;end=Time.time+.2f;while(Time.time<end)yield return null;
            Check(mine!=null&&mine.transform.position==p,mode+" mine holds position throughout its fuse");
            end=Time.time+.5f;while(Time.time<end)yield return null;
            Check(mine==null&&CombatRound.Live.Count==Difficulty.Dense(24),mode+" revised mine stops for fuse and releases one complete burst from "+p);
        }
    }
}
#endif
