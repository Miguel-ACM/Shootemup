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
using UnityEngine.Rendering;
using Unity.Profiling;

[InitializeOnLoad]
public static class ReviewBossBodies
{
    public static string Folder => Path.GetFullPath("ReviewOutput/BossBodies");
    static ReviewBossBodies()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("BossBodies", false)) return;
            MetaProgress.ReviewSaveDirectory = Folder + "/Save";
            new GameObject("Whole boss collider review").AddComponent<BossBodiesRuntime>();
        };
    }
    static void Guard()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\','/').EndsWith("/Temp/UnlockReview/Assets"))
            throw new Exception("Use the isolated command-line review checkout.");
        Directory.CreateDirectory(Folder + "/Save");
    }
    public static void Run() => StartReview(false);
    public static void RunRemaining() => StartReview(true);
    static void StartReview(bool resume)
    {
        Guard(); PlayerSettings.companyName="ShootemupReview"; PlayerSettings.productName="BossBodies";
        SessionState.SetBool("BossBodies.Resume",resume);
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("BossBodies",true); EditorApplication.isPlaying=true;
    }
    public static void Build()
    {
        Guard();
        MetaProgress.ReviewSaveDirectory=Folder+"/Save";
        var rules=new ReviewRegressionTests();
        foreach(var mode in new[]{RunDifficulty.Easy,RunDifficulty.Normal,RunDifficulty.Hard})
            rules.MultipartMetersTrackPhaseObjectives(mode);
        rules.PhaseInvulnerabilityRejectsDirectHitsBeforeEffectsAndScore();
        rules.HomingSkipsProtectedBossHull();
        File.WriteAllText(Folder+"/Rules.txt","PASS shared-health phase meters on Easy, Normal and Hard.\nPASS protected hull targeting and damage/coating/score rejection.\n");
        PlayerSettings.companyName="DefaultCompany"; PlayerSettings.productName="seup";
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
            target=BuildTarget.StandaloneWindows64,locationPathName="Build/VoidBackdrop/Shootemup.exe",options=BuildOptions.None
        });
        File.WriteAllText(Folder+"/Build.txt",result.summary.result+"\nErrors="+result.summary.totalErrors+"; warnings="+result.summary.totalWarnings);
        EditorApplication.Exit(result.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded?0:1);
    }
}

public sealed class BossBodiesRuntime : MonoBehaviour
{
    readonly List<string> notes=new(),errors=new();
    RenderPipelineAsset visible,invisible;
    WaveWatcher watcher;
    static readonly MethodInfo release=typeof(CombatRound).GetMethod("Release",BindingFlags.Instance|BindingFlags.NonPublic);
    void Check(bool okay,string text)
    {
        notes.Add((okay?"PASS ":"FAIL ")+text); if(!okay)errors.Add(text);
        File.WriteAllLines(ReviewBossBodies.Folder+"/Progress.txt",notes);
    }
    IEnumerator Ready(){yield return null;while(CloudTransition.Busy||!WavePreparation.Ready||!watcher.Ready)yield return null;yield return null;}
    IEnumerator DamageReady(DistrictBoss boss)
    {
        int frames=0;
        while(boss!=null&&!boss.CanReceiveDamage&&frames++<2400)yield return null;
        if(boss==null||!boss.CanReceiveDamage)throw new Exception("Boss did not become damageable.");
    }
    void ClearRounds(){foreach(var shot in CombatRound.Live.ToArray())release.Invoke(shot,null);}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);AudioListener.pause=true;Time.captureDeltaTime=1f/60;
        Application.logMessageReceived+=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception)errors.Add(message+"\n"+stack);};
        visible=QualitySettings.renderPipeline;invisible=ScriptableObject.CreateInstance<CelestialNoRenderAsset>();QualitySettings.renderPipeline=invisible;
        var work=new Stack<IEnumerator>();work.Push(Run());
        while(work.Count>0)
        {
            bool more=false;object next=null;
            try{more=work.Peek().MoveNext();if(more)next=work.Peek().Current;}
            catch(Exception ex){errors.Add(ex.ToString());break;}
            if(!more){work.Pop();continue;}
            if(next is IEnumerator nested)work.Push(nested);else yield return next;
        }
        notes.AddRange(errors);File.WriteAllLines(ReviewBossBodies.Folder+"/Checks.txt",notes);
        SessionState.SetBool("BossBodies",false);EditorApplication.Exit(errors.Count==0?0:1);
    }
    IEnumerator Run()
    {
        if(SessionState.GetBool("BossBodies.Resume",false))
            notes.AddRange(File.ReadAllLines(ReviewBossBodies.Folder+"/Completed.txt"));
        while(CloudTransition.Busy||Time.frameCount<5)yield return null;
        WaveWatcher.Launch();while(WaveWatcher.Current==null||!WaveWatcher.Current.Ready||CloudTransition.Busy||!WavePreparation.Ready)yield return null;
        watcher=WaveWatcher.Current;if(watcher.Looping)watcher.ToggleLoop();
        watcher.SelectDifficulty(RunDifficulty.Normal);watcher.SelectView(WatcherView.Bosses);yield return Ready();
        foreach(LevelId level in Enum.GetValues(typeof(LevelId)))
        {
            watcher.SelectLevel(level);yield return Ready();
            var entries=WatcherCatalog.For(WatcherView.Bosses,level);
            foreach(var type in DistrictWaves.Bosses(level))
            {
                if(notes.Any(n=>n=="PASS "+type.Name+" has one whole-body death and disables contact."))continue;
                int entry=entries.FindIndex(e=>(e.Enemy==type||(e.Enemy==null&&type==DistrictWaves.Bosses(level)[0]))&&e.Encounter==WaveWatcher.BossOne);
                if(entry<0)throw new Exception("Missing boss entry: "+type.Name);
                watcher.SelectEntry(entry);watcher.Replay();yield return Ready();
                DistrictBoss boss=null;
                while(boss==null){boss=FindObjectsByType<DistrictBoss>().FirstOrDefault(b=>b.GetType()==type);yield return null;}
                yield return DamageReady(boss);
                var partner=(boss as CityDuoBoss)?.partner;
                if(partner!=null){yield return DamageReady(partner);yield return Inspect(partner,"CityHeavy-1");}
                while(boss!=null)
                {
                    yield return DamageReady(boss);
                    int phase=boss.CurrentPhase;
                    string label=type.Name+"-"+phase;
                    yield return Inspect(boss,label);
                    if(boss is SeraphJudicatorBoss&&phase==1)yield return Stress(boss);
                    if(phase==boss.PhaseCount)break;
                    yield return DamageReady(boss);
                    float initial=boss.health/boss.HealthFraction;
                    if(boss is CityDuoBoss duo&&duo.partner!=null)
                    {
                        duo.partner.damage(1000000);
                        // The surviving hull retains ownership, health and one body collider.
                        for(int frame=0;frame<10;frame++)yield return null;
                    }
                    else
                    {
                        float goal=boss is VoidSovereignBoss ? (boss.PhaseCount-phase)/(float)boss.PhaseCount : phase==1?.49f:boss is CityDuoBoss?.43f:.24f;
                        boss.damage(Mathf.Max(1,boss.health-initial*goal)/Mathf.Max(.001f,GameRules.enemyDamageReceived));
                        yield return null;
                    }
                    Check(boss.CurrentPhase==phase+1&&boss.Invulnerable,label+" advances through shared health and protects its phase break.");
                    float health=boss.health;
                    Check(CombatHit.Apply(boss,5,boss.transform.position,Vector2.up,fire:true)==0&&boss.health==health,
                        label+" rejects damage and coatings during the break.");
                }
                yield return DamageReady(boss);
                boss.damage(1000000);
                yield return null;
                Check(boss.health<=0&&BossDestruction.Current!=null&&BossDestruction.Current.Source==boss&&
                    boss.GetComponentsInChildren<Collider2D>().All(c=>!c.enabled),type.Name+" has one whole-body death and disables contact.");
                while(BossDestruction.Current!=null)yield return null;
                Check(boss==null,type.Name+" completes its finale and removes the shared damage owner.");
                CombatDecision.End(); // Wave Watcher's campaign orchestrator is intentionally suspended.
            }
            if(level!=LevelId.Void&&!notes.Contains("PASS "+level+" miniboss retains one body collider and damage owner."))
            {
                watcher.SelectEncounter(WaveWatcher.Miniboss);yield return Ready();
                DistrictMiniboss mini=null;
                while(mini==null||!mini.Engaged){mini=FindAnyObjectByType<DistrictMiniboss>();yield return null;}
                Check(mini.GetComponentsInChildren<Collider2D>().Length==1&&mini.GetComponentsInChildren<AEnemy>().Length==1,
                    level+" miniboss retains one body collider and damage owner.");
            }
        }
    }
    IEnumerator Inspect(DistrictBoss boss,string label)
    {
        for(int frame=0;frame<120;frame++)yield return null;
        yield return DamageReady(boss);
        var shapes=boss.GetComponentsInChildren<Collider2D>();
        Check(shapes.Length==1&&shapes[0].gameObject==boss.gameObject&&shapes[0].enabled&&
            (!(shapes[0] is PolygonCollider2D poly)||poly.pathCount==1),label+" has exactly one root collider with one continuous path.");
        Check(boss.GetComponentsInChildren<AEnemy>().Length==1&&AEnemy.Live.Contains(boss),label+" has one shared damage owner.");
        Physics2D.SyncTransforms();
        var collider=shapes[0];
        var thrusters=boss.GetComponent<EnemyThrusters>();
        if(thrusters!=null)
        {
            bool outside=true;
            foreach(var flame in boss.Hull.GetComponentsInChildren<MeshFilter>().Where(f=>thrusters.IsExhaust(f.transform)))
            {
                var tip=flame.sharedMesh.vertices.Select(v=>flame.transform.TransformPoint(v))
                    .OrderByDescending(v=>((Vector2)(v-boss.Hull.position)).sqrMagnitude).First();
                outside&=!collider.OverlapPoint(tip);
            }
            Check(outside,label+" engine flames remain outside the damage body.");
        }
        Vector2 point=collider.bounds.center;
        if(boss is DesertWormBoss worm)
        {
            bool covered=worm.Cores.All(p=>collider.OverlapPoint(p.position));
            Check(covered,label+" continuous outline contains every articulated section.");
            point=worm.Cores.Where(p=>CombatRound.Border.Contains(p.position)).OrderBy(p=>Mathf.Abs(p.position.y)).First().position;
        }
        else if(boss is SeraphJudicatorBoss angel)
        {
            bool covered=true;
            for(int ring=0;ring<3;ring++)for(int i=0;i<HeavenAngelVisual.EyeCounts[ring];i++)
                covered&=collider.OverlapPoint(angel.Angel.Wheel(ring).GetChild(i).position);
            Check(covered,label+" the single body contains every moving eye.");
            point=angel.Angel.Emitter(3).position;
        }
        else if(boss is SpaceCitadelBoss)point=boss.Hull.TransformPoint(new Vector3(1.8f,0,0));
        else if(boss is HellCrabBoss)point=boss.Hull.Find("Jointed side pincer").position;
        else point=collider.ClosestPoint(point);
        Check(collider.OverlapPoint(point)&&BeamContact.Touches(boss,point+Vector2.down*.2f,point+Vector2.up*.2f,.04f,out _)&&
            boss.ClosestDamagePoint(point,out var nearest)&&(nearest-point).sqrMagnitude<.001f,
            label+" body contact is shared by bullets, beams and area effects.");
        Check(CombatTargets.Nearest(point,null,.2f)==boss,label+" homing can select the nearby body.");
        float before=boss.health;
        PlayerRound.Spawn(point,Vector2.up,2,0);
        for(int frame=0;frame<5;frame++)yield return new WaitForFixedUpdate();
        Check(boss.health<before&&before-boss.health<=2*GameRules.enemyDamageReceived+.01f,label+" a native projectile damages shared health exactly once.");
        Capture(boss,label);
    }
    IEnumerator Stress(DistrictBoss boss)
    {
        boss.StopAllCoroutines();ClearRounds();
        int created=CombatRound.CreatedCount;
        for(int i=0;i<3200;i++)CombatRound.Launch<SimpleRound>(new Vector3((i%60-29.5f)*.13f,7-(i/60)*.25f),Vector2.down,0,.14f);
        for(int i=0;i<400;i++)CombatRound.Launch<GiantOrb>(new Vector3((i%20-9.5f)*.35f,6-(i/20)*.5f),Vector2.down,0,.55f);
        using var cpu=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"PlayerLoop",1);
        using var physics=ProfilerRecorder.StartNew(ProfilerCategory.Physics2D,"Physics2D.Simulate",1,ProfilerRecorderOptions.Default|ProfilerRecorderOptions.SumAllSamplesInFrame);
        var frames=new List<double>(600);var steps=new List<double>(600);
        for(int frame=0;frame<600;frame++){yield return null;frames.Add(cpu.LastValue/1e6);steps.Add(physics.LastValue/1e6);}
        frames.Sort();steps.Sort();
        Check(CombatRound.CreatedCount==created&&boss.GetComponents<Collider2D>().Length==1,"Dense Heaven frame/physics review uses one boss collider and warmed bullets.");
        notes.Add($"Heaven, 3600-round stress, 600 complete frames: CPU p95={frames[570]:F3}ms max={frames[599]:F3}ms; physics p95={steps[570]:F3}ms. Rendering excluded.");
        ClearRounds();
    }
    void Capture(DistrictBoss boss,string label)
    {
        var shape=boss.GetComponent<Collider2D>();
        var line=CombatVisuals.Line(boss.transform,Color.cyan,.035f);line.useWorldSpace=true;line.loop=true;
        line.sortingLayerName="Bullet";line.sortingOrder=30;
        if(shape is PolygonCollider2D poly)
        {
            var points=poly.GetPath(0);line.positionCount=points.Length;
            for(int i=0;i<points.Length;i++){var p=shape.transform.TransformPoint(points[i]);p.z=-4;line.SetPosition(i,p);}
        }
        else
        {
            line.positionCount=32;
            for(int i=0;i<32;i++){float a=i*Mathf.PI/16;line.SetPosition(i,new Vector3(shape.bounds.center.x+Mathf.Cos(a)*shape.bounds.extents.x,shape.bounds.center.y+Mathf.Sin(a)*shape.bounds.extents.y,-4));}
        }
        QualitySettings.renderPipeline=visible;
        var camera=UnityEngine.Camera.main;var presentation=camera.GetComponent<PSXPresentation>();camera.Render();
        var target=RenderTexture.GetTemporary(1280,720,24);Graphics.Blit(presentation.SceneTexture,target,presentation.CompositeMaterial);
        var previous=RenderTexture.active;RenderTexture.active=target;
        var pixels=new Texture2D(405,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect((1280-405)/2,0,405,720),0,0);pixels.Apply();
        File.WriteAllBytes(ReviewBossBodies.Folder+"/"+label+".png",pixels.EncodeToPNG());Destroy(pixels);Destroy(line.gameObject);
        RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);QualitySettings.renderPipeline=invisible;
    }
}
#endif
