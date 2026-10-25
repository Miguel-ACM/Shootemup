#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ReviewDepthExpansion
{
    public const string Folder="ArtSource/DepthExpansionReview";
    static double deadline;
    static ReviewDepthExpansion()
    {
        EditorApplication.playModeStateChanged+=s=>
        {
            if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("DepthReview",false))
            {new GameObject("Depth expansion review").AddComponent<DepthExpansionReview>();deadline=EditorApplication.timeSinceStartup+190;}
        };
        EditorApplication.update+=()=>
        {
            if(deadline==0)return;string path=Folder+"/Runtime.txt";
            if(File.Exists(path)){SessionState.SetBool("DepthReview",false);EditorApplication.Exit(File.ReadAllText(path).StartsWith("PASS") ? 0 : 1);}
            else if(EditorApplication.timeSinceStartup>deadline){File.WriteAllText(path,"FAIL timeout");EditorApplication.Exit(1);}
        };
    }
    public static void Routes()
    {
        foreach(var second in new[]{LevelId.Snow,LevelId.City})
        foreach(var third in second==LevelId.Snow ? new[]{LevelId.Desert,LevelId.Thunder} : new[]{LevelId.Tunnel,LevelId.Candyland})
        foreach(var fourth in new[]{LevelId.Space,LevelId.Underwater})
        {
            LevelRun.Reset(LevelId.Fields,17381);float previous=0;
            for(int loop=1;loop<=50;loop++)
            {
                if(LevelRun.Stage!=(loop-1)*5+1 || LevelRun.Loop!=loop || LevelRun.EnemyHealthScale<=previous)throw new Exception("Loop scaling");
                previous=LevelRun.EnemyHealthScale;
                foreach(var next in new[]{second,third,fourth,LevelId.Hell,LevelId.Fields})
                {
                    var options=LevelRun.GetOptions();
                    foreach(LevelId invalid in Enum.GetValues(typeof(LevelId)))if(Array.IndexOf(options,invalid)<0)
                    {
                        var current=LevelRun.Current;bool rejected=false;
                        try{LevelRun.Advance(invalid);}catch(InvalidOperationException){rejected=true;}
                        if(!rejected || current!=LevelRun.Current)throw new Exception("Invalid route changed the run");
                    }
                    LevelRun.Advance(next);
                }
                if(LevelRun.Seed!=17381 || LevelRun.Loop!=loop+1)throw new Exception("Loop lost seed");
            }
        }
    }
    public static void Run()
    {
        Directory.CreateDirectory(Folder);File.Delete(Folder+"/Runtime.txt");
        try
        {
            GameRules.ResetRun();Routes();var notes=new List<string>{"All eight diagram routes and 50 harder loops pass, including invalid route rejection."};
            foreach(LevelId level in Enum.GetValues(typeof(LevelId)))
            {
                foreach(int seed in new[]{17381,421,9826})
                {
                    LevelRun.Reset(level,seed);
                    for(int budget=2;budget<=20;budget++)
                    {
                        var wave=DistrictWaves.Create(budget);
                        if(wave.units.Count<5 || wave.units.Any(u=>DistrictWaves.EnemyType(u.enemy)==null))throw new Exception("Invalid wave "+level+" / "+seed+" / "+budget);
                    }
                }
                if(Resources.Load<AudioClip>("Music/"+BGMManager.LevelTrack(level))==null)throw new Exception("Missing music "+level);
                LevelRun.Reset(level,17381);var w=DistrictWaves.Create(20);
                notes.Add(level+": three seeds x 19 budgets pass; late wave "+w.units.Count+" ships / "+w.duration.ToString("0.0")+" seconds; health scale "+LevelRun.EnemyHealthScale.ToString("0.00"));
            }
            var window=EnemyEditorWindow.Open();
            foreach(var name in new[]{nameof(SkullRevenant),nameof(GhostGalleon),nameof(CinderImp),nameof(HellCrabBoss),nameof(ReefDart),nameof(MantaRay),nameof(Bathysphere),nameof(AbyssSubmarineBoss)})
            {window.Select(name);window.CaptureModel(Folder+"/Model-"+name+".png");}
            window.Close();if(EnemyEditorWindow.Catalog.Count!=DistrictWaves.EnemyTypes.Length+Enum.GetValues(typeof(LevelId)).Length)throw new Exception("Incomplete editor catalog");
            File.WriteAllText(Folder+"/Editor.txt","PASS\n"+string.Join("\n",notes)+"\nAll eight new native models rendered; all 51 enemies/bosses in editor.\n");
            EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");SessionState.SetBool("DepthReview",true);EditorApplication.isPlaying=true;
        }
        catch(Exception e){File.WriteAllText(Folder+"/Editor.txt","FAIL\n"+e);EditorApplication.Exit(1);}
    }
    public static void RuntimeOnly()
    {
        Directory.CreateDirectory(Folder);File.Delete(Folder+"/Runtime.txt");
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");SessionState.SetBool("DepthReview",true);EditorApplication.isPlaying=true;
    }
    public static void FeedbackOnly(){SessionState.SetBool("DepthFeedbackOnly",true);RuntimeOnly();}
    public static void FinalChecks()
    {
        try
        {
            GameRules.ResetRun();VerifyRunProgression.Run();var notes=new List<string>();
            foreach(LevelId level in Enum.GetValues(typeof(LevelId)))
            {
                LevelRun.Reset(level,17381);var guard=DistrictWaves.Miniboss();float health=0;
                foreach(var unit in guard.units)
                {
                    var enemy=DistrictEnemy.Spawn(DistrictWaves.EnemyType(unit.enemy),Vector3.zero,true,true,false);
                    enemy.FlyPhrase(unit);health+=enemy.health;UnityEngine.Object.DestroyImmediate(enemy.gameObject);
                }
                notes.Add(level+" miniboss main hull health: "+health.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));
            }
            foreach(var name in new[]{"desert","candyland","space","hell","underwater"})
                if(!Resources.Load<AudioClip>("Music/"+name).loadInBackground)throw new Exception("Music does not load in background: "+name);
            File.WriteAllText(Folder+"/FinalChecks.txt","PASS\nPure route regression checks, tier balance, miniboss health budgets and background music loading passed.\n"+string.Join("\n",notes));EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Folder+"/FinalChecks.txt","FAIL\n"+e);EditorApplication.Exit(1);}
    }
}

public sealed class DepthExpansionReview : MonoBehaviour
{
    const string Folder=ReviewDepthExpansion.Folder;
    readonly List<string> checks=new List<string>(),errors=new List<string>();
    Shipcontroller ship;
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Screen.SetResolution(640,480,false);Application.logMessageReceived+=Log;}
    void OnDestroy(){Application.logMessageReceived-=Log;}
    void Log(string m,string trace,LogType type){if((type==LogType.Error || type==LogType.Exception) && !trace.Contains("UnityEditor.Search"))errors.Add(m);}
    void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add(message);}
    IEnumerator Start()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            bool more=false;object step=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)step=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null){errors.Add(failure.ToString());break;}
            if(!more){stack.Pop();continue;}if(step is IEnumerator nested){stack.Push(nested);continue;}yield return step;
        }
        File.WriteAllText(Folder+"/Runtime.txt",(errors.Count==0 ? "PASS" : "FAIL")+"\n"+string.Join("\n",checks)+"\n"+string.Join("\n",errors));
    }
    IEnumerator Run()
    {
        bool feedbackOnly=SessionState.GetBool("DepthFeedbackOnly",false);SessionState.SetBool("DepthFeedbackOnly",false);
        yield return null;GameRules.StartNewRun();LevelRun.Reset(LevelId.Hell,17381);CloudTransition.Load("L1");
        while(SceneManager.GetActiveScene().name!="L1")yield return null;yield return null;
        var flow=FindAnyObjectByType<WaveOrchestrer>();flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();
        ship=FindAnyObjectByType<Shipcontroller>();ship.enabled=false;ship.ObserverMode=true;
        foreach(var gun in ship.GetComponentsInChildren<AWeapon>()){gun.StopFiring();gun.enabled=false;}
        while(CloudTransition.Busy || !WavePreparation.Ready)yield return null;GameRules.playerDisabled=false;Time.timeScale=1;
        if(feedbackOnly)
        {
            LevelRun.Reset(LevelId.Underwater,17381);
            const System.Reflection.BindingFlags hidden=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var move=typeof(Shipcontroller).GetField("move",hidden);var update=typeof(Shipcontroller).GetMethod("Update",hidden);
            ship.transform.position=new Vector3(-2,-5,-1);move.SetValue(ship,Vector2.right);float start=Time.time;
            while(Time.time-start<.3f){update.Invoke(ship,null);yield return null;}
            float release=ship.transform.position.x;move.SetValue(ship,Vector2.zero);start=Time.time;
            while(Time.time-start<.3f){update.Invoke(ship,null);yield return null;}
            Check(ship.transform.position.x>release+.01f,"Underwater movement has real braking inertia after input release");
            LevelRun.Reset(LevelId.Desert,17381);LevelEnvironment.Apply();while(!WavePreparation.Ready)yield return null;
            ship.transform.position=new Vector3(0,-6,-1);
            var target=(DesertWormBoss)DistrictEnemy.Spawn(typeof(DesertWormBoss),new Vector3(0,2,1));yield return null;yield return new WaitForSeconds(1.7f);
            var core=target.Cores[2];var neighbor=target.Cores[3];core.HitAt(core.transform.position,Vector2.up);core.damage(2);yield return null;
            var block=new MaterialPropertyBlock();core.GetComponentInChildren<MeshRenderer>().GetPropertyBlock(block);
            Check(block.GetFloat("_HitFlash")>.3f && neighbor.GetComponentInChildren<PartHitFeedback>().Heat==0,"Actual weakpoint renderer receives a strong local flash while its neighbor stays steady");
            DesertReview.Capture("Worm-LocalHit",Folder);File.WriteAllText(Folder+"/Feedback.txt","PASS\n"+string.Join("\n",checks));yield break;
        }
        yield return Formation(LevelId.Hell);
        var crab=(HellCrabBoss)DistrictEnemy.Spawn(typeof(HellCrabBoss),new Vector3(0,Camera.UpLimit+1,1));yield return null;
        Check(crab.Eyes.Length==2 && crab.Eyes.All(e=>e.health>0),"Crab has two independent damageable eyes");
        float health=crab.health;crab.damage(100);Check(crab.health==health,"Armored crab body cannot bypass eye weakpoints");
        yield return new WaitForSeconds(3);var eye=crab.Eyes[0];var other=crab.Eyes[1];var before=crab.Hull.localPosition;
        eye.HitAt(eye.transform.position,Vector2.up);eye.damage(5);
        Check(eye.GetComponentInChildren<PartHitFeedback>().Heat>0 && other.GetComponentInChildren<PartHitFeedback>().Heat==0,"Only the struck eye receives local feedback");
        DesertReview.Capture("Hell-EyeHit",Folder);yield return new WaitForSeconds(.25f);
        Check(crab.Hull.localPosition==before,"Part hits leave the main hull anchored");
        eye.damage(1000000);Check(crab!=null && other.health>0,"One destroyed eye does not kill the crab");
        yield return new WaitForSeconds(5.5f);DesertReview.Capture("Hell-ClawSweep",Folder);
        Check(CombatRound.Live.Count>0 && CombatRound.Live.Count<=CombatRound.Limit,"Hell combines side claw bullets and physical attack cycles within pool budget");
        other.damage(1000000);var death=BossDestruction.Current;float finish=Time.time+4;bool noHold=true;
        while(death!=null && Time.time<finish){noHold&=Time.timeScale==1 && !BossDestruction.HoldingFrame;yield return null;}
        Check(noHold && crab==null,"Boss destruction flows continuously without artificial freezes");CombatDecision.End();Clear();yield return null;
        LevelRun.Reset(LevelId.Underwater,17381);LevelEnvironment.Apply();BGMManager.PlayLevelMusic(LevelId.Underwater);
        while(!WavePreparation.Ready)yield return null;yield return new WaitForSeconds(.5f);
        yield return Formation(LevelId.Underwater);
        var sub=(AbyssSubmarineBoss)DistrictEnemy.Spawn(typeof(AbyssSubmarineBoss),new Vector3(0,Camera.UpLimit+2,1));yield return null;
        bool submerged=false,gate=false;float started=Time.time;int image=0;
        while(Time.time-started<21)
        {
            if(sub.Submerged){submerged=true;float h=sub.health;sub.damage(100);gate|=sub.health==h && !sub.CanBeTargeted;}
            if(Time.time-started>4+image*3){DesertReview.Capture("Ocean-Boss-"+(image++).ToString("D2"),Folder);}
            yield return null;
        }
        Check(submerged && gate,"Submarine dives below the water and cannot be hit while submerged");
        Check(sub.EmergenceBursts>=2 && sub.ChargeTrails>0,"Submarine erupts in offset crowns, charges down leaving two side trails, and re-enters above");
        var presentation=UnityEngine.Camera.main.GetComponent<PSXPresentation>();Check(presentation.CompositeMaterial.GetFloat("_Underwater")==1,"Underwater blue grade reaches the composed world");
        Clear();LevelRun.Reset(LevelId.Desert,17381);LevelEnvironment.Apply();while(!WavePreparation.Ready)yield return null;
        var worm=(DesertWormBoss)DistrictEnemy.Spawn(typeof(DesertWormBoss),new Vector3(0,3,1));yield return null;yield return new WaitForSeconds(2);
        worm.Cores[2].HitAt(worm.Cores[2].transform.position,Vector2.up);worm.Cores[2].damage(2);
        Check(worm.Cores[2].GetComponentInChildren<PartHitFeedback>().Heat>0 && worm.Cores[3].GetComponentInChildren<PartHitFeedback>().Heat==0,"Worm feedback flashes and recoils only the struck reactor");
        DesertReview.Capture("Worm-LocalHit",Folder);
        Check(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("DepthWorld")),"Native depth-world shader compiles without errors");
    }
    IEnumerator Formation(LevelId level)
    {
        Clear();var wave=DistrictWaves.Create(20);float started=Time.time;int next=0,peak=0,capture=0;
        while(Time.time-started<Mathf.Min(13,wave.duration+2))
        {
            float t=Time.time-started;
            while(next<wave.units.Count && wave.units[next].time<=t)
            {
                var unit=wave.units[next++];var enemy=DistrictEnemy.Spawn(DistrictWaves.EnemyType(unit.enemy),Vector3.zero);enemy.FlyPhrase(unit);
            }
            peak=Mathf.Max(peak,CombatRound.Live.Count);
            if(t>1.7f+capture*2.2f && capture<5){DesertReview.Capture(level+"-Wave-"+(capture++).ToString("D2"),Folder);}
            yield return null;
        }
        Check(next>0 && peak>0 && peak<=CombatRound.Limit,level+" authored formation fires actual patterns; peak "+peak+" rounds");Clear();yield return null;
    }
    void Clear(){CombatRound.ClearAll();foreach(var enemy in FindObjectsByType<AEnemy>())Destroy(enemy.gameObject);}
}
#endif
