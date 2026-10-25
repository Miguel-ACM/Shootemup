#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ReviewSpaceGameplay
{
    static double deadline;
    static ReviewSpaceGameplay()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("SpaceGameplay",false))return;
            new GameObject("Space gameplay batch").AddComponent<SpaceGameplayReview>();deadline=EditorApplication.timeSinceStartup+180;
        };
        EditorApplication.update+=()=>
        {
            if(deadline==0)return;string file=ReviewSpaceExpansion.Folder+(SessionState.GetBool("SpacePacingOnly",false) ? "/Pacing.txt" : "/Runtime.txt");
            if(File.Exists(file)){SessionState.SetBool("SpaceGameplay",false);EditorApplication.Exit(File.ReadAllText(file).StartsWith("PASS") ? 0 : 1);}
            else if(EditorApplication.timeSinceStartup>deadline){File.WriteAllText(file,"FAIL timeout");EditorApplication.Exit(1);}
        };
    }
    public static void Run()=>Begin(false);
    public static void RunPacing()=>Begin(true);
    static void Begin(bool pacingOnly)
    {
        SessionState.SetBool("SpacePacingOnly",pacingOnly);
        Directory.CreateDirectory(ReviewSpaceExpansion.Folder+"/Frames");File.Delete(ReviewSpaceExpansion.Folder+(pacingOnly ? "/Pacing.txt" : "/Runtime.txt"));
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");SessionState.SetBool("SpaceGameplay",true);EditorApplication.isPlaying=true;
    }
}
public sealed class SpaceGameplayReview : MonoBehaviour
{
    const string Folder=ReviewSpaceExpansion.Folder;
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    readonly List<string> checks=new List<string>(),errors=new List<string>();
    readonly Dictionary<CombatRound,float> ricochetHeading=new Dictionary<CombatRound,float>();
    Shipcontroller ship;WaveOrchestrer flow;
    bool moveShip,bounced;
    float moveStarted,peakBank;
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Screen.SetResolution(1280,720,false);Application.logMessageReceived+=Log;}
    void OnDestroy(){Application.logMessageReceived-=Log;}
    void Log(string text,string trace,LogType type){if((type==LogType.Error || type==LogType.Exception) && !trace.Contains("UnityEditor.Search"))errors.Add(text);}
    static void Set(object target,string field,object value)=>target.GetType().GetField(field,Hidden).SetValue(target,value);
    static T Get<T>(object target,string field)=>(T)target.GetType().GetField(field,Hidden).GetValue(target);
    static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Hidden).Invoke(target,args);
    void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add("PASS "+message);File.WriteAllText(Folder+"/Progress.txt",string.Join("\n",checks));}
    void Capture(string name)=>DesertReview.Capture(name,Folder);
    void Clear()
    {
        foreach(var enemy in AEnemy.Live.ToArray())if(enemy!=null)Destroy(enemy.gameObject);
        CombatRound.ClearAll();foreach(var shot in FindObjectsByType<ABullet>())Destroy(shot.gameObject);
        foreach(var decision in FindObjectsByType<DecisionShop>())decision.Abort();
        foreach(var item in FindObjectsByType<AItem>())Destroy(item.gameObject);
        CombatDecision.End();Set(flow,"decision",null);flow.CancelInvoke();
    }
    void Update()
    {
        if(moveShip && ship!=null)
        {
            ship.transform.position=new Vector3(Mathf.PingPong((Time.time-moveStarted)*7,6.8f)-3.4f,Camera.DownLimit+2,-1);
            var visual=ship.GetComponent<PSXShipVisual>();peakBank=Mathf.Max(peakBank,Mathf.Abs(Get<float>(visual,"bank")));
        }
        foreach(var round in CombatRound.Live)if(round is RicochetBolt)
        {
            if(ricochetHeading.TryGetValue(round,out float old) && old*round.Heading.x<0 && Mathf.Abs(round.transform.position.x)>Camera.RightLimit-.6f)bounced=true;
            ricochetHeading[round]=round.Heading.x;
        }
    }
    IEnumerator Start()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Review());
        while(stack.Count>0)
        {
            bool more=false;object step=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)step=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null){errors.Add(failure.ToString());break;}
            if(!more){stack.Pop();continue;}if(step is IEnumerator nested){stack.Push(nested);continue;}yield return step;
        }
        File.WriteAllText(Folder+(SessionState.GetBool("SpacePacingOnly",false) ? "/Pacing.txt" : "/Runtime.txt"),(errors.Count==0 ? "PASS" : "FAIL")+"\n"+string.Join("\n",checks)+"\n"+string.Join("\n",errors));
    }
    IEnumerator Review()
    {
        yield return null;GameRules.StartNewRun();LevelRun.Reset(LevelId.Space,17381);CloudTransition.Load("L1");
        while(SceneManager.GetActiveScene().name!="L1")yield return null;yield return null;
        flow=FindAnyObjectByType<WaveOrchestrer>();flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();
        ship=FindAnyObjectByType<Shipcontroller>();ship.enabled=false;ship.ObserverMode=true;
        foreach(var weapon in ship.GetComponentsInChildren<AWeapon>()){weapon.StopFiring();weapon.enabled=false;}
        foreach(var collider in ship.GetComponentsInChildren<Collider2D>())collider.enabled=false;
        while(CloudTransition.Busy || !WavePreparation.Ready)yield return null;
        GameRules.playerDisabled=false;Time.timeScale=1;Clear();yield return null;
        Check(FindAnyObjectByType<SpaceBackdrop>()!=null,"Space creates its starfield and soft planets");
        bool pacingOnly=SessionState.GetBool("SpacePacingOnly",false);
        if(!pacingOnly)
        {
        Set(flow,"openingWavePending",false);Set(flow,"waves",new[]{"8","miniboss","20","boss"});Set(flow,"currentWaveNumber",0);
        Call(flow,"ResolveEncounter",EncounterKind.Challenge);flow.CancelInvoke();Call(flow,"GenerateWave");
        Check(flow.ChallengeWavesRemaining==3 && Get<Wave>(flow,"currentWave").cost==8,"Challenge starts three unmodified waves at the normal budget");
        ship.ObserverMode=false;ship.ReceiveHazardDamage(1);ship.ObserverMode=true;
        Check(flow.ChallengeWavesRemaining==0,"Taking a real hit cancels the challenge immediately");
        Set(flow,"currentWaveNumber",0);Call(flow,"ResolveEncounter",EncounterKind.Challenge);flow.CancelInvoke();Call(flow,"GenerateWave");
        Call(flow,"FinishCombatWave");Check(flow.ChallengeWavesRemaining==2,"First clean challenge wave counts down");
        Call(flow,"FinishCombatWave");Check(flow.ChallengeWavesRemaining==1 && Get<Wave>(flow,"currentWave").cost==20,"Challenge keeps the final wave at normal difficulty");
        Call(flow,"FinishCombatWave");Check(flow.ChallengeWavesRemaining==0 && FindAnyObjectByType<DecisionShop>()!=null,"Three clean waves open the item reward");
        }
        Clear();yield return null;
        Set(flow,"waves",new[]{"2","8","boss"});Set(flow,"currentWaveNumber",0);Set(flow,"openingWavePending",false);Set(flow,"stopingWave",false);
        Call(flow,"GenerateWave");flow.enabled=true;moveShip=true;moveStarted=Time.time;
        float start=Time.time,nextFrame=0;int frame=0;bool overlap=false,escorts=false,visibleOverlap=false;int maxActors=0;
        while(Time.time-start<12)
        {
            var live=AEnemy.Live.OfType<DistrictEnemy>().Where(e=>e!=null).ToArray();maxActors=Mathf.Max(maxActors,live.Length);
            overlap|=live.Select(e=>typeof(AEnemy).GetField("orchesterWaveNumber",Hidden).GetValue(e)).Distinct().Count()>1;
            visibleOverlap|=live.Where(e=>e.transform.position.y>Camera.DownLimit && e.transform.position.y<Camera.UpLimit && Mathf.Abs(e.transform.position.x)<Camera.RightLimit)
                .Select(e=>typeof(AEnemy).GetField("orchesterWaveNumber",Hidden).GetValue(e)).Distinct().Count()>1;
            escorts|=live.Any(e=>e is OrbitalMoonlet && e.GetComponent<SatelliteOrbit>()!=null);
            if(Time.time-start>=nextFrame){nextFrame+=.25f;Capture("Frames/"+(pacingOnly ? "Pacing-" : "Wave-")+(frame++).ToString("D3"));}
            yield return null;
        }
        flow.enabled=false;flow.CancelInvoke();moveShip=false;
        Check(overlap,"The next wave launches while members of the previous wave remain alive");
        Check(escorts,"Space formations include independently destroyable orbiting satellites");
        Check(peakBank>22 && Quaternion.Angle(ship.transform.rotation,Quaternion.identity)<.01f,"Stronger lateral banking leaves the player collider unrotated");
        checks.Add("INFO visible cohort overlap="+visibleOverlap+", peak actors="+maxActors+", peak bank="+peakBank.ToString("F1"));
        if(pacingOnly){Check(visibleOverlap,"Members of both wave cohorts are visible together during the handoff");Capture("Overlapping-Waves");yield break;}
        Capture("Orbiting-Formations");Clear();yield return null;
        ship.transform.position=new Vector3(0,Camera.DownLimit+1.8f,-1);Set(flow,"currentWaveNumber",3);Set(flow,"choosingRoute",false);Call(flow,"SpawnBoss");
        yield return new WaitForSeconds(2.6f);
        var boss=FindAnyObjectByType<SpaceCitadelBoss>();Check(boss!=null && boss.Parts.All(p=>p!=null),"Orbital station has five separately modeled, damageable modules");
        var core=boss.Parts[4];float hp=core.health,total=boss.health;core.damage(100000);boss.damage(100000);
        Check(core.health==hp && boss.health==total && !core.CanBeTargeted && !core.GetComponent<Collider2D>().enabled,"Protected center rejects direct damage and targeting");
        Capture("Boss-Shielded");
        bool beam=false;float until=Time.time+6.5f;int bossFrame=0;nextFrame=Time.time;
        while(Time.time<until)
        {
            var laser=CombatRound.Live.OfType<PulseLaser>().FirstOrDefault(l=>l.IsAttachedTo(boss.Hull) && l.IsFiring && l.Power>.5f);
            if(laser!=null && !beam){beam=true;Check(Mathf.Abs(laser.transform.position.x)<.06f && Vector2.Dot(laser.Heading,Vector2.down)>.99f,"Charged boss beam fires straight down the arena center");Capture("Boss-CenterLaser");}
            if(Time.time>=nextFrame){nextFrame+=.25f;Capture("Frames/Boss-"+(bossFrame++).ToString("D3"));}yield return null;
        }
        Check(beam && bounced,"Boss uses an attached charged center laser and genuinely bouncing bullets");
        boss.Parts[0].damage(100000);Check(!boss.CoreExposed && !core.CanBeTargeted,"One generator is insufficient to expose the reactor");
        boss.Parts[1].damage(100000);Check(boss.CoreExposed && core.CanBeTargeted && core.GetComponent<Collider2D>().enabled,"Breaking both generators opens the core");
        boss.Parts[2].damage(100000);yield return new WaitForSeconds(.7f);Capture("Boss-Exposed");
        Check(!boss.Parts[2].CanBeTargeted && boss.Parts[3].health>0,"Gun pods can be destroyed independently of the shield generators");
        core.damage(20);Check(core.health<hp,"Exposed reactor accepts damage");yield return new WaitForSeconds(1.4f);
        int stage=LevelRun.Stage;core.damage(100000);yield return null;
        Check(BossDestruction.Current!=null && !LevelChoice.Active,"Core destruction enters the staged boss finale");
        until=Time.unscaledTime+10;
        while(FindAnyObjectByType<DecisionShop>()==null && Time.unscaledTime<until)yield return null;
        var reward=FindAnyObjectByType<DecisionShop>();Check(reward!=null && LevelRun.Current==LevelId.Space && !LevelChoice.Active,"Space completion waits for its mandatory boss item");
        yield return new WaitForSeconds(1.3f);Capture("Boss-Reward");
        var item=FindObjectsByType<AItem>().First(i=>!i.Collected);ship.transform.position=item.transform.position+Vector3.back*2;
        yield return null;if(item!=null && !item.Collected)item.TryPick(0);
        until=Time.unscaledTime+15;
        while((LevelRun.Current!=LevelId.Fields || CloudTransition.Busy || !WavePreparation.Ready) && Time.unscaledTime<until)yield return null;
        Check(LevelRun.Current==LevelId.Fields && LevelRun.Stage==stage+1 && LevelRun.Loop==2,"Collecting the Space boss item loops into harder Plains");
        Capture("Loop-To-Plains");
    }
}
#endif
