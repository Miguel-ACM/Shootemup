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
using UnityEngine.SceneManagement;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;

// One isolated graphical session. Optional command-line --rework-levels and
// --rework-focus rerun only a failed district/case, never the entire tour.
[InitializeOnLoad]
public static class VerifyCombatRework
{
    public const string Folder="ArtSource/CombatReworkReview";
    const string Key="CombatReworkReview";
    static double deadline;
    static VerifyCombatRework()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))
            {new GameObject("Batched combat rework review").AddComponent<CombatReworkReview>();deadline=EditorApplication.timeSinceStartup+2400;}
        };
        EditorApplication.update+=()=>
        {
            if(deadline==0||!SessionState.GetBool(Key,false))return;
            var report=Folder+"/Verification.txt";
            if(!File.Exists(report)&&EditorApplication.timeSinceStartup<deadline)return;
            SessionState.SetBool(Key,false);
            CheckHarness.Exit(File.Exists(report)&&File.ReadAllText(report).StartsWith("PASS")?0:1);
        };
    }
    public static string Argument(string flag,string fallback)
    {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,flag);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/Temp/CombatReworkReview/"))throw new Exception("Use the isolated CombatReworkReview project");
        Directory.CreateDirectory(Folder+"/Models");Directory.CreateDirectory(Folder+"/Frames");
        if(File.Exists(Folder+"/Verification.txt"))File.Delete(Folder+"/Verification.txt");
        SessionState.SetString(Key+"Levels",Argument("--rework-levels","all"));
        SessionState.SetString(Key+"Focus",Argument("--rework-focus","all"));
        try
        {
            PureChecks();
            if(Argument("--rework-focus","all")=="all")Models();
            File.WriteAllText(Folder+"/Editor.txt","PASS\nTwenty new actor registrations, finite 3D models, real formation spacing, Easy/Hard catalogs, route scaling and deterministic wave replay.\n");
        }
        catch(Exception e){File.WriteAllText(Folder+"/Verification.txt","FAIL editor\n"+e);EditorApplication.Exit(1);return;}
        SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        EditorApplication.isPlaying=true;
    }
    static void Require(bool pass,string message){if(!pass)throw new Exception(message);}
    static void PureChecks()
    {
        GameRules.StartNewRun();VerifyRunProgression.Run();
        Require(EnemyExpansion.All.Length==20,"Exactly two authored additions per district");
        var notes=new List<string>();var invalid=new List<string>();
        foreach(LevelId level in Enum.GetValues(typeof(LevelId)))
        {
            var additions=EnemyExpansion.All.Where(x=>x.level==level).ToArray();
            Require(additions.Length==2,"Two additions missing: "+level);
            foreach(var entry in additions)
            {
                Require(DistrictWaves.EnemyType(entry.type.Name)==entry.type&&WatcherCatalog.Enemies(level).Contains(entry.type),"Missing production/viewer registration: "+entry.type.Name);
                var mesh=entry.model();
                Require(mesh.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z))&&mesh.bounds.size.z>.08f,"Flat or invalid new hull: "+entry.type.Name);
                Require(entry.ports.Length>0&&entry.parts.Length>=3,"Missing weapons or scored parts: "+entry.type.Name);
            }
            foreach(RunDifficulty difficulty in Enum.GetValues(typeof(RunDifficulty)))
            {
                Difficulty.Select(difficulty);LevelRun.Reset(level,17381);
                foreach(var entry in additions)foreach(var part in entry.parts)foreach(int side in new[]{-1,1})
                {
                    var preview=WaveComposer.PreviewPart(part,0,side,out _);
                    if(preview==null)invalid.Add("No safe new formation: "+level+" / "+part.Name+" / "+difficulty+" / "+side);
                    else if(!WaveGeometry.Clear(WaveVarietyAudit.Tracks(preview)))invalid.Add("New formation intersects its modeled hulls: "+part.Name);
                }
                foreach(int seed in new[]{17381,421})
                {
                    LevelRun.Reset(level,seed);
                    foreach(int budget in new[]{2,4,8,12,16,20})
                    {
                        var wave=DistrictWaves.Create(budget);
                        Require(wave.units.Count>=5&&WaveGeometry.Clear(WaveVarietyAudit.Tracks(wave)),"Invalid composition: "+level+" / "+difficulty+" / "+seed+" / "+budget);
                        Require(WaveVarietyAudit.Signature(wave)==WaveVarietyAudit.Signature(DistrictWaves.Create(budget)),"Replay changed its wave");
                        notes.Add(level+" / "+difficulty+" / "+seed+" / "+budget+": "+string.Join(" + ",wave.parts));
                    }
                }
            }
        }
        File.WriteAllLines(Folder+"/Compositions.txt",notes);
        Require(invalid.Count==0,string.Join("\n",invalid));
        Difficulty.Select(RunDifficulty.Hard);LevelRun.Reset();
    }
    public static void ValidateAuthoring() => PureChecks();
    static void Models()
    {
        foreach(var entry in EnemyExpansion.All)Model(entry.type.Name,entry.model(),EnemyExpansion.Material(entry.hull));
        foreach(var pair in new[]{(41,"OrbitalScout"),(42,"LaserSatellite"),(43,"OrbitalCarrier"),(44,"OrbitalMoonlet")})Model(pair.Item2,IndustrialMeshFactory.CombatHull(pair.Item1),SpaceMaterials.Fleet);
    }
    static void Model(string name,Mesh mesh,Material material)
    {
        var preview=new PreviewRenderUtility();var resolution=Shader.GetGlobalVector("_PSXSnapResolution");
        try
        {
            preview.BeginPreview(new Rect(0,0,480,360),GUIStyle.none);
            var camera=preview.camera;camera.orthographic=true;camera.orthographicSize=Mathf.Max(.7f,mesh.bounds.extents.magnitude)*1.12f;
            camera.transform.SetPositionAndRotation(new Vector3(0,0,-15),Quaternion.identity);camera.nearClipPlane=.1f;camera.farClipPlane=50;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.035f,.05f);
            var rotation=Quaternion.Euler(-18,22,0);
            Shader.SetGlobalVector("_PSXSnapResolution",new Vector4(480,360,0,0));
            preview.DrawMesh(mesh,Matrix4x4.TRS(-(rotation*mesh.bounds.center),rotation,Vector3.one),material,0);preview.Render(true);
            var texture=preview.EndPreview();var target=RenderTexture.GetTemporary(480,360,24);var before=RenderTexture.active;
            Graphics.Blit(texture,target);RenderTexture.active=target;
            var image=new Texture2D(480,360,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,480,360),0,0);image.Apply();
            File.WriteAllBytes(Folder+"/Models/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            RenderTexture.active=before;RenderTexture.ReleaseTemporary(target);
        }
        finally{Shader.SetGlobalVector("_PSXSnapResolution",resolution);preview.Cleanup();}
    }
}

public sealed class CombatReworkReview : MonoBehaviour
{
    const string Folder=VerifyCombatRework.Folder;
    const BindingFlags Hidden=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly;
    readonly List<string> notes=new List<string>(),errors=new List<string>();
    readonly List<(string name,ProfilerRecorder recorder)> recorders=new List<(string,ProfilerRecorder)>();
    readonly StringBuilder frames=new StringBuilder("case,frame,dt_ms,live,created,main_ns,physics_ns\n");
    Shipcontroller ship;
    int serial;
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Screen.SetResolution(960,720,false);QualitySettings.vSyncCount=0;Application.targetFrameRate=60;AudioListener.volume=0;Application.logMessageReceived+=Log;}
    void OnDestroy(){Application.logMessageReceived-=Log;foreach(var item in recorders)item.recorder.Dispose();}
    void Log(string message,string trace,LogType type){if((type==LogType.Error||type==LogType.Exception)&&!trace.Contains("UnityEditor.Search"))errors.Add(message);}
    static FieldInfo Field(Type type,string name){for(;type!=null;type=type.BaseType){var f=type.GetField(name,Hidden);if(f!=null)return f;}return null;}
    static void Set(object target,string name,object value){var f=Field(target.GetType(),name);if(f==null)throw new Exception("Missing field "+name);f.SetValue(target,value);}
    void Check(bool pass,string text){if(!pass)throw new Exception(text);notes.Add("PASS "+text);File.WriteAllLines(Folder+"/Progress.txt",notes);}
    IEnumerator Start()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            bool more=false;object current=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null){errors.Add(failure.ToString());break;}
            if(!more){stack.Pop();continue;}
            if(current is IEnumerator nested){stack.Push(nested);continue;}
            yield return current;
        }
        Time.timeScale=1;File.WriteAllText(Folder+"/Frames.csv",frames.ToString());
        File.WriteAllText(Folder+"/Verification.txt",(errors.Count==0?"PASS":"FAIL")+"\n"+string.Join("\n",notes)+"\n"+string.Join("\n",errors));
    }
    void Clear()
    {
        CombatRound.ClearAll();foreach(var e in FindObjectsByType<AEnemy>())Destroy(e.gameObject);
        foreach(var pickup in FindObjectsByType<APickable>())Destroy(pickup.gameObject);
    }
    IEnumerator Context(LevelId level,RunDifficulty difficulty)
    {
        Clear();yield return null;CombatDecision.End();GameRules.StartNewRun(difficulty);LevelRun.Reset(level,17381);
        LevelEnvironment.Apply();while(!WavePreparation.Ready)yield return null;yield return new WaitForSeconds(.4f);
        GameRules.playerDisabled=false;ship.transform.position=new Vector3(0,Camera.DownLimit+2,-1);
    }
    void Capture(string label,int frame)
    {
        string dir=Folder+"/Frames/"+label;Directory.CreateDirectory(dir);
        DesertReview.Capture(frame.ToString("D3"),dir);
    }
    void Record(string label,int frame)
    {
        long main=0,physics=0;foreach(var r in recorders)if(r.recorder.Valid){if(r.name=="Main Thread")main=r.recorder.LastValue;else physics+=r.recorder.LastValue;}
        frames.Append(label).Append(',').Append(frame).Append(',').Append((Time.unscaledDeltaTime*1000).ToString("F3",System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(CombatRound.Live.Count).Append(',').Append(CombatRound.CreatedCount).Append(',').Append(main).Append(',').Append(physics).AppendLine();
    }
    IEnumerator Observe(string label,float duration,Action tick=null,bool capture=true,float captureInterval=.5f)
    {
        float start=Time.time,next=0;int frame=0,peak=0;bool fired=false;
        while(Time.time-start<duration)
        {
            tick?.Invoke();peak=Mathf.Max(peak,CombatRound.Live.Count);fired|=CombatRound.Live.Count>0;
            if(capture&&Time.time-start>=next){Capture(label,frame);next+=captureInterval;}
            Record(label,frame++);yield return null;
        }
        Check(peak<=CombatRound.Limit,label+": "+frame+" complete frames, peak "+peak+", firing "+fired);
    }
    IEnumerator Wave(string label,Wave wave)
    {
        Clear();yield return null;int cursor=0;float start=Time.time;
        yield return Observe(label,wave.duration+.3f,()=>
        {
            while(cursor<wave.units.Count&&wave.units[cursor].time<=Time.time-start)
            {var unit=wave.units[cursor++];var actor=DistrictEnemy.Spawn(DistrictWaves.EnemyType(unit.enemy),Vector3.zero);actor.FlyPhrase(unit);}
        });
        Check(cursor==wave.units.Count,label+": all "+cursor+" authored actors spawned");
    }
    void PhaseTwo(DistrictBoss boss)
    {
        if(boss is CityDuoBoss duo){duo.partner.health*=.55f;duo.damage(1000000);}
        else if(boss is DesertWormBoss worm){for(int i=1;i<=4;i++)worm.Cores[i].damage(1000000);}
        else if(boss is SpaceCitadelBoss station){station.Parts[0].damage(1000000);station.Parts[1].damage(1000000);}
        else if(boss is HellCrabBoss crab)crab.Eyes[0].damage(1000000);
        else boss.health*=.49f;
    }
    IEnumerator Bosses(LevelId level,RunDifficulty difficulty)
    {
        Clear();yield return null;
        var boss=(DistrictBoss)DistrictEnemy.Spawn(DistrictWaves.BossType,new Vector3(0,Camera.UpLimit+2.5f,1));
        yield return new WaitForSeconds(1.6f);
        string label=level+"-"+difficulty+"-Boss";
        for(int phase=1;phase<=2;phase++)
        {
            if(phase==2)PhaseTwo(boss);
            float minY=float.PositiveInfinity;int nonBoss=0;
            yield return Observe(label+phase,phase==1?12:14,()=>
            {
                if(level==LevelId.Candyland&&boss!=null)minY=Mathf.Min(minY,boss.transform.position.y);
                if(level==LevelId.Hell)nonBoss=Mathf.Max(nonBoss,FindObjectsByType<DistrictEnemy>().Count(e=>!(e is DistrictBoss)));
                if(level==LevelId.Thunder)foreach(var beam in CombatRound.Live.OfType<PulseLaser>())if(beam.IsAttachedTo(boss.Hull)&&Vector2.Dot(beam.Heading,Vector2.down)<.999f)throw new Exception("Diagonal Thunder beam");
            });
            if(level==LevelId.Candyland)Check(minY>(Camera.UpLimit+Camera.DownLimit)*.5f+.8f,"Candy boss keeps its complete route above the lower arena");
            if(level==LevelId.Hell)Check(nonBoss==0,"Hell boss remains free of minions");
        }
        if(boss is SpaceCitadelBoss space)
        {
            space.Parts[2].damage(1000000);space.Parts[3].damage(1000000);CombatRound.ClearAll();
            yield return Observe(label+"-CoreOnly",9,null,false);Check(CombatRound.Live.Count>0,"Exposed reactor keeps pressure after both optional guns die");
        }
        if(level==LevelId.City)
        {
            Clear();yield return null;var duo=(CityDuoBoss)DistrictEnemy.Spawn(typeof(CityDuoBoss),new Vector3(0,4,1));yield return null;yield return null;
            duo.health*=.55f;duo.partner.damage(1000000);yield return Observe(label+"-FastSurvivor",12);
        }
    }
    IEnumerator Run()
    {
        yield return null;GameRules.StartNewRun();LevelRun.Reset();CloudTransition.Load("L1");
        while(SceneManager.GetActiveScene().name!="L1")yield return null;yield return null;
        var flow=FindAnyObjectByType<WaveOrchestrer>();flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();Set(flow,"openingWavePending",false);
        ship=FindAnyObjectByType<Shipcontroller>();ship.enabled=false;ship.ObserverMode=true;
        foreach(var weapon in ship.GetComponentsInChildren<AWeapon>()){weapon.StopFiring();weapon.enabled=false;}
        while(CloudTransition.Busy||!WavePreparation.Ready)yield return null;
        var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
        foreach(var handle in handles){var d=ProfilerRecorderHandle.GetDescription(handle);if(d.Name=="Main Thread"||d.Name=="Physics2D.Simulate"||d.Name=="Physics2D.SyncTransforms")recorders.Add((d.Name,ProfilerRecorder.StartNew(d.Category,d.Name,1,ProfilerRecorderOptions.Default|ProfilerRecorderOptions.SumAllSamplesInFrame)));}
        string focus=SessionState.GetString("CombatReworkReviewFocus","all"),filter=SessionState.GetString("CombatReworkReviewLevels","all");
        foreach(LevelId level in Enum.GetValues(typeof(LevelId)))
        {
            if(filter!="all"&&!filter.Split(',').Contains(level.ToString()))continue;
            foreach(RunDifficulty difficulty in Enum.GetValues(typeof(RunDifficulty)))
            {
                if(focus=="additions"&&difficulty==RunDifficulty.Easy)continue;
                yield return Context(level,difficulty);
                if(focus=="all"||focus=="waves")yield return Wave(level+"-"+difficulty+"-Wave",DistrictWaves.Create(12));
                if((focus=="all"||focus=="enemies")&&difficulty==RunDifficulty.Hard)
                {
                    foreach(var entry in EnemyExpansion.All.Where(e=>e.level==level))
                    {
                        var preview=WaveComposer.PreviewPart(entry.parts[0],0,1,out _);yield return Wave(entry.type.Name,preview);
                    }
                    foreach(var type in WatcherCatalog.Enemies(level).Where(t=>EnemyExpansion.Find(t.Name)==null))
                    {
                        Clear();yield return null;var actor=DistrictEnemy.Spawn(type,new Vector3(0,Camera.UpLimit-2,1));
                        yield return Observe(type.Name,4.6f,null,true);
                        Check(actor==null||actor.GetComponent<EnemyFiringCue>().PortCount>0,"Audited physical weapon layout: "+type.Name);
                    }
                }
                if(focus=="additions")
                {
                    foreach(var entry in EnemyExpansion.All.Where(e=>e.level==level))
                        foreach(var part in entry.parts)
                            yield return Wave(part.Name,WaveComposer.PreviewPart(part,0,1,out _));
                    Clear();yield return null;
                    var phaseBoss=(DistrictBoss)DistrictEnemy.Spawn(DistrictWaves.BossType,new Vector3(0,Camera.UpLimit-2,1));
                    yield return new WaitForSeconds(1.6f);PhaseTwo(phaseBoss);
                    yield return Observe(level+"-PhaseEntry",2.4f,null,true,.125f);
                }
                if(focus=="all"||focus=="bosses")yield return Bosses(level,difficulty);
                if(level==LevelId.Underwater&&difficulty==RunDifficulty.Hard)
                {
                    Clear();yield return null;var school=FindAnyObjectByType<OceanLife>();Check(school!=null,"Live underwater boid schools are installed");
                    var fish=school.GetComponentsInChildren<MeshFilter>().Where(m=>m.name=="Schooling reef fish").Select(m=>m.transform).ToArray();
                    var before=fish.Select(t=>t.position).ToArray();yield return Observe("Underwater-Boids",5);
                    Check(fish.Length>=30&&fish.Select((t,i)=>Vector3.Distance(t.position,before[i])).Count(d=>d>1)>fish.Length*.8f,"Fish schools actually travel and steer through the seabed");
                }
                serial++;File.WriteAllText(Folder+"/CurrentCase.txt",serial+": "+level+" / "+difficulty+" complete");
            }
        }
        if(focus=="additions")
        {
            foreach(RunDifficulty difficulty in Enum.GetValues(typeof(RunDifficulty)))
            {
                yield return Context(LevelId.Hell,difficulty);
                yield return Bosses(LevelId.Hell,difficulty);
            }
            yield return PlainsHits();
            yield return Context(LevelId.Tunnel,RunDifficulty.Hard);
            Clear();yield return null;
            var saw=(TunnelSawBoss)DistrictEnemy.Spawn(typeof(TunnelSawBoss),new Vector3(0,Camera.UpLimit-2,1));
            float began=Time.time;bool phase=false;
            yield return Observe("Tunnel-UncapturedProfile",30,()=>
            {
                if(!phase&&Time.time-began>9){phase=true;saw.health*=.45f;}
            },false);
        }
        Clear();yield return null;Check(errors.Count==0,"No runtime exceptions during the complete batched review");
    }
    IEnumerator PlainsHits()
    {
        yield return Context(LevelId.Fields,RunDifficulty.Hard);
        var weapon=ship.GetComponentInChildren<BaseWeapon>();
        Check(weapon!=null&&GameRules.playerShootDamage==2&&GameRules.playerShootNumber==2&&GameRules.playerShootSpeed==10,"Moving Plains benchmark uses the unmodified stock gun");
        foreach(var entry in EnemyExpansion.All.Where(e=>e.level==LevelId.Fields))
            foreach(var part in entry.parts)
            {
                Clear();yield return null;
                var unit=WaveComposer.PreviewPart(part,0,1,out _).units[0];
                var target=DistrictEnemy.Spawn(entry.type,Vector3.zero);target.FlyPhrase(unit);
                var m=new PlainsTtkProbe.Measurement{health=target.health};
                target.gameObject.AddComponent<PlainsTtkTarget>().Initialize(target,m);
                Vector3 previous=target.transform.position;float began=Time.time;
                weapon.enabled=true;weapon.isStopingPrimary=false;weapon.WeaponPressed(true);
                while(target!=null&&Time.time-began<10)
                {
                    var position=target.transform.position;
                    float velocity=(position.x-previous.x)/Mathf.Max(.001f,Time.deltaTime);
                    float flight=Mathf.Max(0,position.y-ship.transform.position.y-.7f)/GameRules.playerShootTravelSpeed;
                    float aim=Mathf.Clamp(position.x+velocity*flight,Camera.LeftLimit+.4f,Camera.RightLimit-.4f);
                    var pilot=ship.transform.position;pilot.x=Mathf.MoveTowards(pilot.x,aim,GameRules.playerMoveSpeed*Time.deltaTime);ship.transform.position=pilot;
                    previous=position;yield return null;
                }
                weapon.StopFiring();weapon.enabled=false;yield return null;
                Check(m.killed&&m.firstHit>=0,part.Name+": actual player projectiles kill the moving hull before departure");
                float ttk=m.death-m.firstHit;
                Check(ttk<4.5f,part.Name+": "+m.health+" HP, "+ttk.ToString("F2")+"s from first projectile hit to kill (predictive pilot capped at normal movement speed)");
            }
    }
}
#endif
