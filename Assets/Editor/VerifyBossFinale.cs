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
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class VerifyBossFinale
{
    public const string Folder="ArtSource/BossFinaleReview";
    static double deadline;
    static VerifyBossFinale()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("BossFinaleReview",false))return;
            new GameObject("Boss finale verification").AddComponent<BossFinaleReview>();deadline=EditorApplication.timeSinceStartup+900;
        };
        EditorApplication.update+=()=>
        {
            if(deadline==0)return;
            var report=ReportPath;
            if(File.Exists(report)){SessionState.SetBool("BossFinaleReview",false);EditorApplication.Exit(File.ReadAllText(report).StartsWith("PASS") ? 0 : 1);}
            else if(EditorApplication.timeSinceStartup>deadline){File.WriteAllText(report,"FAIL timeout");EditorApplication.Exit(1);}
        };
    }
    public static string ReportPath=>Folder+(SessionState.GetBool("BossFinaleBaseline",false) ? "/Baseline.txt" : SessionState.GetBool("BossFinaleLoopsOnly",false) ? "/LoopsVerification.txt" : "/Verification.txt");
    public static void Baseline()=>Begin(true,false);
    public static void Run()=>Begin(false,false);
    public static void RunRemaining()=>Begin(false,true);
    static void Begin(bool baseline,bool loopsOnly)
    {
        Directory.CreateDirectory(Folder+"/Frames");SessionState.SetBool("BossFinaleLoopsOnly",loopsOnly);SessionState.SetBool("BossFinaleBaseline",baseline);File.Delete(ReportPath);
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");SessionState.SetBool("BossFinaleReview",true);SessionState.SetBool("BossFinaleBaseline",baseline);EditorApplication.isPlaying=true;
    }
}
public sealed class BossFinaleReview : MonoBehaviour
{
    const string Folder=VerifyBossFinale.Folder;
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    readonly List<string> checks=new List<string>(),errors=new List<string>();
    Shipcontroller ship;WaveOrchestrer flow;
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Application.logMessageReceived+=Log;}
    void OnDestroy(){Application.logMessageReceived-=Log;}
    void Log(string message,string trace,LogType type){if((type==LogType.Error || type==LogType.Exception) && !trace.Contains("UnityEditor.Search"))errors.Add(message+"\n"+trace);}
    void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add("PASS "+message);File.WriteAllText(Folder+"/Progress.txt",string.Join("\n",checks));}
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Hidden).SetValue(owner,value);
    static void Call(object owner,string name,params object[] values)=>owner.GetType().GetMethod(name,Hidden).Invoke(owner,values);
    void Capture(string name)=>DesertReview.Capture(name,Folder);
    IEnumerator Start()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            bool more=false;object step=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)step=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null){errors.Add(failure.ToString());break;}if(!more){stack.Pop();continue;}
            if(step is IEnumerator nested){stack.Push(nested);continue;}yield return step;
        }
        File.WriteAllText(VerifyBossFinale.ReportPath,(errors.Count==0 ? "PASS" : "FAIL")+"\n"+string.Join("\n",checks)+"\n"+string.Join("\n",errors));
    }
    void Clear()
    {
        flow.StopAllCoroutines();flow.CancelInvoke();flow.enabled=false;
        foreach(var choice in FindObjectsByType<LevelChoice>())Destroy(choice.gameObject);
        foreach(var shop in FindObjectsByType<DecisionShop>())shop.Abort();
        foreach(var enemy in FindObjectsByType<AEnemy>())Destroy(enemy.gameObject);
        CombatRound.ClearAll();foreach(var shot in FindObjectsByType<ABullet>())Destroy(shot.gameObject);
        foreach(var pickup in FindObjectsByType<APickable>())Destroy(pickup.gameObject);
        CombatDecision.End();Set(flow,"choosingRoute",false);Set(flow,"openingWavePending",false);Set(flow,"isWaveComingOut",false);
    }
    IEnumerator Run()
    {
        GameRules.StartNewRun();LevelRun.Reset(LevelId.City,17381);CloudTransition.Load("L1");
        while(SceneManager.GetActiveScene().name!="L1")yield return null;yield return null;
        flow=FindAnyObjectByType<WaveOrchestrer>();flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();
        ship=FindAnyObjectByType<Shipcontroller>();ship.ObserverMode=true;ship.enabled=false;
        foreach(var weapon in ship.GetComponentsInChildren<AWeapon>()){weapon.StopFiring();weapon.enabled=false;}
        while(CloudTransition.Busy || !WavePreparation.Ready)yield return null;
        ship.transform.position=new Vector3(0,-5,-1);GameRules.playerDisabled=false;
        if(SessionState.GetBool("BossFinaleBaseline",false))yield return ReproduceCityLock();else yield return FinaleChecks();
    }
    IEnumerator ReproduceCityLock()
    {
        Set(flow,"currentWaveNumber",DistrictWaves.Sequence().Length);Call(flow,"SpawnBoss");yield return new WaitForSeconds(3);
        var fast=FindObjectsByType<CityDuoBoss>().First(b=>!b.heavy);var heavy=fast.partner;
        bool missing=typeof(AEnemy).GetField("orchestrer",Hidden).GetValue(heavy)==null;
        Check(missing,"reproduced missing encounter ownership on the heavy City ship");
        fast.damage(100000);yield return null;Check(heavy!=null && heavy.health>0 && !LevelChoice.Active,"first City death preserves its survivor");
        heavy.damage(100000);yield return new WaitForSeconds(1.5f);
        Check(FindObjectsByType<CityDuoBoss>().Length==0 && !LevelChoice.Active && FindAnyObjectByType<DecisionShop>()==null,"reproduced City lock after fast-then-heavy deaths: no route or reward");
        Capture("Before-CityLock");
    }
    static void Kill(AEnemy enemy){enemy.damage(enemy.health/Mathf.Max(.001f,GameRules.enemyDamageReceived)+1);}
    IEnumerator Environment(LevelId level)
    {
        Clear();yield return null;LevelRun.Reset(level,17381);LevelEnvironment.Apply();
        while(!WavePreparation.Ready || CloudTransition.Busy)yield return null;
        ship.transform.position=new Vector3(0,UnityEngine.Camera.main.transform.position.y-UnityEngine.Camera.main.orthographicSize+.3f,-1);
        Set(flow,"currentWaveNumber",DistrictWaves.Sequence().Length);GameRules.playerDisabled=false;
        Call(flow,"SpawnBoss");yield return new WaitForSeconds(3);
    }
    IEnumerator FinaleChecks()
    {
        // Full equipment forces a real replacement decision if the random boss item uses a slot.
        var loadout=ship.GetComponent<ShipLoadout>();loadout.EquipWeapon("NeedleCannon",1);
        foreach(var pair in new[]{(typeof(Overdrive),0),(typeof(MirrorBurst),1)})
        {
            var go=Instantiate(Resources.Load<GameObject>("Items/Item"),new Vector3(30,30,1),Quaternion.identity);
            var item=(AItem)go.AddComponent(pair.Item1);yield return null;item.GrantTo(ship,pair.Item2);yield return null;
        }
        var districts=SessionState.GetBool("BossFinaleLoopsOnly",false) ? new[]{LevelId.Desert,LevelId.Candyland} : new[]{LevelId.City,LevelId.City,LevelId.Fields,LevelId.Snow,LevelId.Thunder,LevelId.Tunnel,LevelId.Desert,LevelId.Candyland};
        foreach(var level in districts)
        {
            int cityCase=checks.Count(x=>x.Contains("City order complete"));
            string label=level==LevelId.City ? cityCase==0 ? "City-FastFirst" : "City-HeavyFirst" : level.ToString();
            yield return Environment(level);
            var boss=FindObjectsByType<DistrictBoss>().First(b=>!(b is TunnelSawBoss saw) || !saw.miniature);
            if(level==LevelId.City)
            {
                var fast=FindObjectsByType<CityDuoBoss>().First(b=>!b.heavy);var heavy=fast.partner;
                var encounterOwner=typeof(AEnemy).GetField("orchestrer",Hidden);
                Check(ReferenceEquals(encounterOwner.GetValue(fast),flow) && ReferenceEquals(encounterOwner.GetValue(heavy),flow),"both City ships share the real encounter owner");
                var first=cityCase==0 ? fast : heavy;var survivor=cityCase==0 ? heavy : fast;
                float maximum=(float)typeof(DistrictBoss).GetField("initialHealth",Hidden).GetValue(survivor);survivor.damage(maximum*.3f/Mathf.Max(.001f,GameRules.enemyDamageReceived));float hurt=survivor.health;
                int score=Score.CurrentScore;Kill(first);yield return null;
                Check(BossDestruction.Current==null && !LevelChoice.Active && FindAnyObjectByType<DecisionShop>()==null && Score.CurrentScore==score,"first City ship drops no reward and does not complete the encounter");
                Check(Mathf.Abs(survivor.health-(hurt+(maximum-hurt)*.5f))<.1f && (bool)typeof(CityDuoBoss).GetField("enraged",Hidden).GetValue(survivor),"City survivor still heals half its missing health and becomes aggressive");boss=survivor;
            }
            if(level==LevelId.Tunnel)
            {
                boss.damage(boss.health*.6f/Mathf.Max(.001f,GameRules.enemyDamageReceived));yield return null;yield return null;
                var mini=FindObjectsByType<TunnelSawBoss>().First(b=>b.miniature);Kill(mini);yield return null;
                Check(BossDestruction.Current==null && !LevelChoice.Active,"miniature saw destruction does not trigger a boss finale or reward");
            }
            if(level==LevelId.Snow){var stop=ship.gameObject.AddComponent<WorldTimeStop>();stop.Begin(2);}
            if(boss is DesertWormBoss worm)foreach(var core in worm.Cores)Kill(core);else Kill(boss);
            var death=BossDestruction.Current;
            Check(death!=null && death.Source==boss && boss.gameObject.activeInHierarchy && !boss.enabled && CombatDecision.Active,label+": lethal damage retains the intact hull while stopping combat");
            Check(death.Wreckage!=null && !death.Wreckage.gameObject.activeSelf,label+": wreckage remains hidden until the shatter accent");
            Check(!WorldTimeStop.Frozen,label+": active bullet-time effects relinquish the cinematic clock");
            var music=(AudioSource)typeof(BGMManager).GetField("audioSource",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            bool preMusic=true,intact=true,noEarlyRoute=true,paused=false;float pauseDuration=0,freezeWorld=-1,freezeStart=-1;
            float started=Time.unscaledTime,last=-1;int frame=0;
            while(death!=null && death.Stage!=BossDestructionStage.Breakup && Time.unscaledTime-started<6)
            {
                intact&=boss!=null && boss.Hull.GetComponent<MeshRenderer>().enabled;
                preMusic&=music.isPlaying;noEarlyRoute&=!LevelChoice.Active && FindAnyObjectByType<DecisionShop>()==null;
                if(death.Stage==BossDestructionStage.Freeze)
                {
                    if(freezeStart<0){freezeStart=Time.unscaledTime;freezeWorld=Time.time;Capture(label+"-Freeze");}
                    CheckFrozen(Time.timeScale==0 && Mathf.Abs(Time.time-freezeWorld)<.002f,label);
                    if(level==LevelId.Fields && !paused)
                    {
                        paused=true;var menu=FindAnyObjectByType<Menu>();menu.setMenuOpen(true);float p=Time.unscaledTime;
                        yield return new WaitForSecondsRealtime(.25f);pauseDuration=Time.unscaledTime-p;
                        Check(death.Stage==BossDestructionStage.Freeze && Time.timeScale==0,"pausing cannot finish the half-second boss hold behind the menu");menu.setMenuOpen(false);
                        Check(Time.timeScale==0,"resuming the menu preserves the remaining boss freeze");
                    }
                }
                float elapsed=Time.unscaledTime-started;
                if(elapsed-last>.06f){last=elapsed;Capture("Frames/"+label+"-"+(frame++).ToString("D4"));}
                yield return null;
            }
            Check(death!=null && death.Stage==BossDestructionStage.Breakup && death.PreBlasts==5 && intact && preMusic && noEarlyRoute,label+": five explosions play over the held model before any breakup, music cut or route");
            float held=death.ShatteredAt-death.FreezeStarted-pauseDuration;
            Check(held>=.49f && held<.65f && !music.isPlaying && Time.timeScale==1,label+": half-second freeze then synchronized shatter/music cut; hold "+held.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"s");
            Check(death.Wreckage!=null && death.Wreckage.gameObject.activeSelf,label+": prepared polygon wreckage releases on the shatter accent");
            if(level==LevelId.Desert)Check(death.Wreckage.GetComponentsInChildren<MeshFilter>(true).Length>=8,"Desert worm breakup includes the complete articulated body");
            yield return null;Capture(label+"-Breakup");
            float wait=Time.unscaledTime+4;while(FindAnyObjectByType<DecisionShop>()==null && Time.unscaledTime<wait)yield return null;
            var reward=FindAnyObjectByType<DecisionShop>();Check(reward!=null && !reward.paid && !LevelChoice.Active,label+": final explosion drops an item before any route selection");
            var offered=((List<AItem>)typeof(DecisionShop).GetField("items",Hidden).GetValue(reward)).Where(i=>i!=null).ToArray();
            Check(offered.Length==1,label+": exactly one random boss item");
            yield return new WaitForSeconds(2.1f);Capture(label+"-Item");
            Check(reward!=null && !LevelChoice.Active && reward.SkipProgress==0,label+": waiting at the bottom cannot bypass the item");
            var overlay=FindAnyObjectByType<DecisionOverlay>();Check(!overlay.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Any(t=>t.text=="SKIP"),label+": mandatory item has no skip control");
            var itemToPick=offered[0];ship.transform.position=itemToPick.transform.position+Vector3.back*2;yield return null;
            if(itemToPick!=null && !itemToPick.Collected && (itemToPick.Kind==AItem.ItemType.weapon || itemToPick.Kind==AItem.ItemType.active))
                Check(itemToPick.TryPick(0),label+": a full loadout can choose a replacement for the mandatory item");
            wait=Time.unscaledTime+2;while(reward!=null && Time.unscaledTime<wait)yield return null;
            Check(reward==null,label+": actual item collection completes the reward");
            yield return new WaitForSeconds(.3f);
            if(level==LevelId.Desert || level==LevelId.Candyland)
            {
                wait=Time.unscaledTime+12;while((CloudTransition.Busy || LevelRun.Current!=LevelId.Fields || !WavePreparation.Ready) && Time.unscaledTime<wait)yield return null;
                Check(LevelRun.Current==LevelId.Fields && LevelRun.Loop==2 && !CloudTransition.Busy && Time.timeScale==1,label+": the final-district loop waits for the item and resumes cleanly");
            }
            else
            {
                Check(LevelChoice.Active && FindAnyObjectByType<LevelChoice>()!=null,label+": path choice opens only after collecting the item");Capture(label+"-Route");
            }
            if(level==LevelId.City)checks.Add("PASS City order complete: "+label);
        }
    }
    static void CheckFrozen(bool ok,string label){if(!ok)throw new Exception(label+": freeze frame changed world time or resumed simulation");}
}
#endif
