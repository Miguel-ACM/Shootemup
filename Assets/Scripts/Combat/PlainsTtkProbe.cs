#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// Run once as a batch with Temp/verify-combat.request = ttk.
// Real stock gun, cadence, projectiles and physics. Tracking is an aim benchmark,
// not a human playthrough: no upgrades, pickups or damage to the test pilot.
public sealed class PlainsTtkProbe : MonoBehaviour
{
    readonly List<string> results=new List<string>(), errors=new List<string>();
    Shipcontroller ship;BaseWeapon weapon;Keyboard keyboard;
    float pilotY;
    public sealed class Measurement
    {
        public float health,firstHit=-1,death=-1;
        public bool killed;
    }
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Application.logMessageReceived+=Log;}
    void OnDestroy(){Application.logMessageReceived-=Log;}
    void Log(string text,string trace,LogType kind)
    {if((kind==LogType.Error || kind==LogType.Exception) && !trace.Contains("UnityEditor.Search"))errors.Add(text);}
    IEnumerator Start()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            object step=null;bool more=false;Exception error=null;
            try{more=stack.Peek().MoveNext();if(more)step=stack.Peek().Current;}catch(Exception ex){error=ex;}
            if(error!=null){errors.Add(error.ToString());break;}
            if(!more){stack.Pop();continue;}
            if(step is IEnumerator nested){stack.Push(nested);continue;}yield return step;
        }
        if(weapon!=null)weapon.StopFiring();
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
        string report=(errors.Count==0 ? "PASS" : "FAIL")+"\n"+string.Join("\n",results)+"\n"+string.Join("\n",errors);
        File.WriteAllText("ArtSource/CombatReview/PlainsTtkVerification.txt",report);File.WriteAllText("Temp/CombatResult.txt",report);
    }
    void Check(bool pass,string message){if(!pass)throw new Exception(message);results.Add("PASS "+message);}
    void Fire(bool pressed){InputSystem.QueueStateEvent(keyboard,new KeyboardState(pressed ? new[]{Key.Z} : Array.Empty<Key>()));}
    void Clear()
    {
        foreach(var enemy in FindObjectsByType<AEnemy>())Destroy(enemy.gameObject);
        foreach(var shot in FindObjectsByType<AEnemyBullet>())shot.Vanish();
        foreach(var shot in FindObjectsByType<ABullet>())Destroy(shot.gameObject);
        foreach(var item in FindObjectsByType<APickable>())Destroy(item.gameObject);
    }
    IEnumerator Run()
    {
        GameRules.StartNewRun();LevelRun.Reset();Time.timeScale=1;
        if(SceneManager.GetActiveScene().name!="L1")yield return SceneManager.LoadSceneAsync("L1");
        yield return new WaitForSecondsRealtime(1.2f);Time.timeScale=1;
        ship=FindAnyObjectByType<Shipcontroller>();ship.health=ship.maxHealth=10000;
        var waves=FindAnyObjectByType<WaveOrchestrer>();waves.enabled=false;waves.CancelInvoke();waves.StopAllCoroutines();
        Clear();keyboard=InputSystem.AddDevice<Keyboard>();yield return null;
        weapon=ship.GetComponentInChildren<BaseWeapon>();
        Check(weapon!=null && ship.GetComponentsInChildren<AWeapon>().Length==1,"fresh run uses only the stock BaseWeapon");
        Check(GameRules.playerShootDamage==2 && GameRules.playerShootNumber==2 && GameRules.playerShootSpeed==10,"stock gun: 2 x 2 damage x 10 volleys/sec = 40 nominal DPS");
        pilotY=Camera.DownLimit+1.6f;
        results.Add("TTK is from first hit to death, excluding projectile travel; tracking is capped at the player's normal speed. Departures never count as kills.");
        yield return Target(typeof(ScoutInterceptor),"stationary scout",true,null,1);
        yield return Target(typeof(PatrolGunship),"stationary gunship",true,null,1.6f);
        yield return Target(typeof(HeavyBomber),"stationary bomber",true,null,2.4f);
        // Every distinct authored Plains enemy/path combination, including the mirrored crossing.
        var seen=new HashSet<string>();
        for(int budget=2;budget<=5;budget++)
            foreach(var unit in DistrictWaves.Create(budget).units)
            {
                string key=unit.enemy+"/"+unit.phrase;
                if(!seen.Add(key))continue;
                yield return Target(DistrictWaves.EnemyType(unit.enemy),key,false,unit,unit.flightDuration);
                if(unit.phrase==FlightPhrase.Crossing)
                {
                    var mirror=(WaveUnit)unit.Clone();mirror.side=-mirror.side;
                    yield return Target(DistrictWaves.EnemyType(unit.enemy),key+" mirrored",false,mirror,mirror.flightDuration);
                }
            }
        yield return Target(typeof(FieldsHeavyWing),"moving armored miniboss",false,null,14);
        yield return Target(typeof(FieldsBomberBoss),"stationary boss",true,null,39);
        yield return Target(typeof(FieldsBomberBoss),"moving boss, 2 seconds disengaged every 8 seconds",false,null,55,true);
        LevelRun.Advance(LevelId.Snow);LevelRun.Advance(LevelId.Thunder);LevelRun.Advance(LevelId.Fields);
        var loopBoss=DistrictEnemy.Spawn(typeof(FieldsBomberBoss),Vector3.zero);
        Check(Mathf.Abs(loopBoss.health-1440*1.66f)<.01f && LevelRun.EnemyTempoScale>1,"second-loop Plains retains increased boss health and enemy tempo");
        Clear();LevelRun.Reset();
    }
    IEnumerator Target(Type type,string label,bool stationary,WaveUnit phrase,float maximumTtk,bool disengage=false)
    {
        Fire(false);weapon.StopFiring();Clear();yield return null;
        var target=DistrictEnemy.Spawn(type,new Vector3(0,Camera.UpLimit-2,1));
        if(phrase!=null)target.FlyPhrase(phrase);
        yield return null; // Run the real enemy's Start before freezing a stationary target.
        target.lifeDropChance=target.manaDropChance=target.moneyDropChance=0;
        if(stationary){target.enabled=false;foreach(var mover in target.GetComponents<AMover>())mover.enabled=false;}
        var measurement=new Measurement{health=target.health};
        target.gameObject.AddComponent<PlainsTtkTarget>().Initialize(target,measurement);
        ship.transform.position=new Vector3(Mathf.Clamp(target.transform.position.x,Camera.LeftLimit+.4f,Camera.RightLimit-.4f),pilotY,-1);
        Vector3 previous=target.transform.position;
        float begin=Time.time;bool firing=false;bool phaseTwoSeen=false;
        while(target!=null && Time.time-begin<maximumTtk+4)
        {
            float elapsed=Time.time-begin;
            bool shouldFire=!disengage || elapsed%8<6;
            if(shouldFire!=firing){Fire(shouldFire);firing=shouldFire;}
            if(!stationary)
            {
                Vector3 position=target.transform.position;
                float velocity=(position.x-previous.x)/Mathf.Max(.001f,Time.deltaTime);
                // Aim ahead by flight time, without teleporting or exceeding normal ship speed.
                float flight=Mathf.Max(0,position.y-pilotY-.7f)/GameRules.playerShootTravelSpeed;
                float aim=Mathf.Clamp(position.x+velocity*flight,Camera.LeftLimit+.4f,Camera.RightLimit-.4f);
                var pilot=ship.transform.position;pilot.x=Mathf.MoveTowards(pilot.x,aim,GameRules.playerMoveSpeed*Time.deltaTime);pilot.y=pilotY;ship.transform.position=pilot;
                previous=position;
            }
            if(target is DistrictBoss boss && boss.HealthFraction<.5f)phaseTwoSeen=true;
            yield return null;
        }
        Fire(false);weapon.StopFiring();yield return null;
        Check(measurement.killed,label+" killed rather than leaving the screen");
        float ttk=measurement.death-measurement.firstHit;
        results.Add($"MEASURE {label}: {measurement.health:F2} HP, {ttk:F2}s first-hit-to-kill, {measurement.death-begin:F2}s from firing start.");
        Check(ttk<=maximumTtk,label+$" stays within {maximumTtk:F2}s");
        if(disengage)Check(phaseTwoSeen,"moving boss reaches its harder second phase before death");
        File.WriteAllText("Temp/PlainsTtkProgress.txt",string.Join("\n",results));
    }
}

public sealed class PlainsTtkTarget : MonoBehaviour
{
    AEnemy target;PlainsTtkProbe.Measurement measurement;
    public void Initialize(AEnemy enemy,PlainsTtkProbe.Measurement value){target=enemy;measurement=value;}
    void LateUpdate(){Observe();}
    void Observe(){if(measurement!=null && measurement.firstHit<0 && target.health<measurement.health)measurement.firstHit=Time.time;}
    void OnDestroy()
    {
        if(measurement==null)return;Observe();measurement.killed=target.health<=0;measurement.death=Time.time;
    }
}
#endif
