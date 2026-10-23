#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

public sealed class ChoreographyProbe : MonoBehaviour
{
    public bool SnowOnly;
    readonly List<string> checks=new List<string>(),errors=new List<string>();
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    Shipcontroller ship;WaveOrchestrer flow;Keyboard keyboard;
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Application.logMessageReceived+=Log;}
    void OnDestroy(){Application.logMessageReceived-=Log;}
    void Log(string text,string trace,LogType kind)
    {if((kind==LogType.Error || kind==LogType.Exception) && !trace.Contains("UnityEditor.Search"))errors.Add(text+"\n"+trace);}
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
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
        if(flow!=null){flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();}
        string report=(errors.Count==0 ? "PASS" : "FAIL")+"\n"+string.Join("\n",checks)+"\n"+string.Join("\n",errors);
        File.WriteAllText("ArtSource/CombatReview/"+(SnowOnly ? "SnowChoreographyRefinement.txt" : "ChoreographyVerification.txt"),report);
        if(errors.Count>0 || SnowOnly)File.WriteAllText("Temp/CombatResult.txt",report);
        else
        {
            // Finish the same play session with the real stock-gun collision benchmark.
            LevelRun.Reset();LevelEnvironment.Apply();Clear();
            new GameObject("New Plains formation TTK").AddComponent<PlainsTtkProbe>().FormationsOnly=true;
        }
    }
    void Check(bool condition,string message){if(!condition)throw new Exception(message);checks.Add("PASS "+message);}
    static bool Outside(Vector2 p)=>Mathf.Abs(p.x)>1.08f || p.y>1.07f || p.y<-.07f;
    static string Signature(Wave wave)=>string.Join(";",wave.units.Select(x=>$"{x.enemy}:{x.curve.Name}:{x.time:F2}:{x.lane:F2}:{x.formationDepth:F2}:{x.flightDuration:F2}:{string.Join(",",x.fireTimes??Array.Empty<float>())}"));
    void InspectCompositions()
    {
        var owners=new Dictionary<FlightCurve,LevelId>();
        var sampleObject=new GameObject("Formation geometry audit");
        var sample=sampleObject.AddComponent<FormationFlight>();sample.enabled=false;
        try
        {
        foreach(LevelId level in Enum.GetValues(typeof(LevelId)))
        {
            LevelRun.Reset(level);var recipes=new HashSet<string>();var routes=new HashSet<FlightCurve>();
            for(int budget=2;budget<=9;budget++)
            {
                var wave=DistrictWaves.Create(budget);recipes.Add(Signature(wave));
                if(wave.units.Count<5 || wave.units[0].time!=0 || wave.duration>4.5f)throw new Exception(level+" empty or slow composition");
                float previous=0;
                foreach(var unit in wave.units)
                {
                    if(unit.curve==null || unit.phrase!=FlightPhrase.Flow)throw new Exception(level+" reuses a legacy route");
                    if(unit.time<previous || unit.time-previous>2 || unit.time>wave.duration+.1f)throw new Exception(level+" broken handoff timing");
                    previous=unit.time;routes.Add(unit.curve);
                    if(owners.TryGetValue(unit.curve,out var owner) && owner!=level)throw new Exception("Shared route between districts");
                    owners[unit.curve]=level;
                    sample.curve=unit.curve;sample.side=unit.side;sample.lane=unit.lane;sample.formationDepth=unit.formationDepth;
                    if(!Outside(sample.PositionAt(0)) || !Outside(sample.PositionAt(1)))throw new Exception(unit.curve.Name+" appears or disappears inside combat");
                    var copy=(WaveUnit)unit.Clone();
                    if(copy.curve!=unit.curve || copy.firePhase!=unit.firePhase || copy.syncFire!=unit.syncFire || copy.formationDepth!=unit.formationDepth || copy.fireTimes==null || !copy.fireTimes.SequenceEqual(unit.fireTimes))throw new Exception("Cloning loses choreography");
                    if(unit.fireTimes==null || unit.fireTimes.Length==0 || unit.fireTimes.Any(t=>!float.IsFinite(t)) || !unit.fireTimes.SequenceEqual(unit.fireTimes.OrderBy(t=>t)))throw new Exception("Missing or unordered phrase score");
                    float length=0;var p=sample.PositionAt(0);int visible=0;
                    for(int i=1;i<=256;i++)
                    {
                        var next=sample.PositionAt(i/256f);
                        if(!float.IsFinite(next.x) || !float.IsFinite(next.y))throw new Exception("Non-finite route");
                        length+=Vector2.Scale(next-p,new Vector2(Camera.RightLimit,Camera.UpLimit-Camera.DownLimit)).magnitude;
                        if(Mathf.Abs(next.x)<1 && next.y>.15f && next.y<1)visible++;
                        p=next;
                    }
                    if(visible<128 || length/unit.flightDuration<2.1f || length/unit.flightDuration>11)throw new Exception(unit.curve.Name+" has poor exposure or pace: "+visible+" samples; "+length/unit.flightDuration+" units/s");
                }
            }
            Check(recipes.Count==8 && routes.Count==4,level+": eight different compositions and four exclusive continuous routes");
            for(int budget=3;budget<=20;budget++)
                if(Signature(DistrictWaves.Create(budget))==Signature(DistrictWaves.Create(budget-1)))throw new Exception("Consecutive repeated composition");
            checks.Add("PASS "+level+": actual formation depth, offscreen entry/exit, visible attack time, mobile pace, ordered scores, cloned timing and no adjacent repeats");
        }
        Check(owners.Count==20,"all twenty routes belong to exactly one district");
        }
        finally{Destroy(sampleObject);}
    }
    FormationFlight ScheduledMember(float at,float[] beats,FlightCurve route=null,float lane=0,float depth=0)
    {
        var enemy=DistrictEnemy.Spawn(typeof(ScoutInterceptor),Vector3.zero);enemy.enabled=false;
        enemy.FlyPhrase(new WaveUnit("ScoutInterceptor",at,Vector3.zero){phrase=FlightPhrase.Flow,curve=route??PlainsChoreography.Swoop,
            flightDuration=6,firePhase=at,fireTimes=beats,lane=lane,formationDepth=depth});
        var flight=enemy.GetComponent<FormationFlight>();flight.enabled=false;return flight;
    }
    static void StepManually(FormationFlight flight,float seconds)
    {flight.enabled=true;flight.Step(seconds);}
    void InspectScores()
    {
        float[] accents={2f,2.6f,3.2f};float[] launches={0,.4f};
        foreach(bool faster in new[]{false,true})
        {
            LevelRun.Reset();
            if(faster){LevelRun.Advance(LevelId.Snow);LevelRun.Advance(LevelId.Thunder);LevelRun.Advance(LevelId.Fields);}
            float tempo=GameRules.enemySpeed*LevelRun.EnemyTempoScale;
            var members=launches.Select(at=>ScheduledMember(at,accents)).ToArray();
            var observed=new[]{new List<float>(),new List<float>()};
            for(int tick=1;tick<=180;tick++)
            {
                float phraseTime=tick*.02f;
                for(int i=0;i<members.Length;i++)
                {
                    if(phraseTime<=launches[i]+.0001f)continue;
                    StepManually(members[i],.02f/tempo);
                    if(members[i].CanFire && members[i].TakeFireBeat())observed[i].Add(phraseTime);
                    if(members[i].TakeFireBeat())throw new Exception("A scored ship fires twice on one accent");
                }
            }
            for(int i=0;i<members.Length;i++)
            {
                Check(observed[i].Count==accents.Length,(faster ? "loop" : "first pass")+": staggered member "+i+" fires exactly the three scored accents, with silence between them");
                for(int j=0;j<accents.Length;j++)
                    if(Mathf.Abs(observed[i][j]-accents[j])>.025f)throw new Exception("A phrase accent drifted from its authored time");
            }
            Check(observed[0].Zip(observed[1],(a,b)=>Mathf.Abs(a-b)).All(delta=>delta<.025f),"staggered members remain synchronized at tempo "+tempo.ToString("F2"));
            foreach(var member in members)Destroy(member.gameObject);
        }
        LevelRun.Reset();
        var late=ScheduledMember(0,accents);StepManually(late,2.25f);
        Check(!late.TakeFireBeat(),"a missed or offscreen accent expires instead of becoming a late catch-up volley");
        StepManually(late,.351f);
        Check(late.TakeFireBeat() && !late.TakeFireBeat(),"an expired accent does not suppress or duplicate the following beat");Destroy(late.gameObject);
        foreach(var route in new[]{PlainsChoreography.Fan,PlainsChoreography.Loop,SnowChoreography.Petal,TunnelChoreography.Helix})
        {
            var left=ScheduledMember(0,accents,route,-.24f,-.07f);var right=ScheduledMember(0,accents,route,.24f,.07f);
            StepManually(left,2);StepManually(right,2);
            float parameter=(float)typeof(FormationFlight).GetMethod("TravelParameter",Hidden).Invoke(left,new object[]{2f/6});
            Vector2 expected=Vector2.Scale(right.PositionAt(parameter)-left.PositionAt(parameter),new Vector2(Camera.RightLimit,Camera.UpLimit-Camera.DownLimit));
            Vector2 actual=right.transform.position-left.transform.position;
            Check(Vector2.Distance(expected,actual)<.001f,route.Name+": members retain their formation geometry through the same turn parameter");
            Destroy(left.gameObject);Destroy(right.gameObject);
        }
        LevelRun.Reset(LevelId.Thunder);var initial=DistrictWaves.Create(5);
        LevelRun.Advance(LevelId.Fields);LevelRun.Advance(LevelId.Snow);LevelRun.Advance(LevelId.Thunder);
        var accelerated=DistrictWaves.Create(5);float speed=LevelRun.EnemyTempoScale;
        Check(initial.units.Count==accelerated.units.Count && Mathf.Abs(initial.duration-accelerated.duration*speed)<.001f,"harder-loop handoff scales with flight tempo without changing the composition");
        for(int i=0;i<initial.units.Count;i++)
        {
            var before=initial.units[i];var after=accelerated.units[i];
            if(Mathf.Abs(before.time-after.time*speed)>.001f || before.firePhase!=after.firePhase || !before.fireTimes.SequenceEqual(after.fireTimes))throw new Exception("Loop tempo breaks authored launch/attack alignment");
        }
        Check(true,"harder-loop launches, movement and attack scores share the same tempo");LevelRun.Reset();
    }
    void Clear()
    {
        foreach(var enemy in FindObjectsByType<AEnemy>())Destroy(enemy.gameObject);
        foreach(var shot in FindObjectsByType<AEnemyBullet>())shot.Vanish();
        foreach(var shot in FindObjectsByType<ABullet>())Destroy(shot.gameObject);
        foreach(var pickup in FindObjectsByType<APickable>())Destroy(pickup.gameObject);
    }
    static void Set(WaveOrchestrer target,string field,object value)=>typeof(WaveOrchestrer).GetField(field,Hidden).SetValue(target,value);
    static int WaveNumber(WaveOrchestrer target)=>(int)typeof(WaveOrchestrer).GetField("currentWaveNumber",Hidden).GetValue(target);
    IEnumerator Run()
    {
        GameRules.StartNewRun();InspectCompositions();LevelRun.Reset();Time.timeScale=1;
        if(SceneManager.GetActiveScene().name!="L1")yield return SceneManager.LoadSceneAsync("L1");
        yield return new WaitForSecondsRealtime(1.2f);Time.timeScale=1;
        ship=FindAnyObjectByType<Shipcontroller>();ship.health=ship.maxHealth=10000;
        flow=FindAnyObjectByType<WaveOrchestrer>();keyboard=InputSystem.AddDevice<Keyboard>();
        flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();Clear();yield return null;
        InspectScores();Clear();yield return null;
        foreach(var level in SnowOnly ? new[]{LevelId.Snow} : new[]{LevelId.Fields,LevelId.Snow,LevelId.Thunder,LevelId.Tunnel,LevelId.City})yield return PlayDistrict(level);
    }
    IEnumerator PlayDistrict(LevelId level)
    {
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        foreach(var weapon in ship.GetComponentsInChildren<AWeapon>())weapon.StopFiring();
        Clear();yield return null;LevelRun.Reset(level);LevelEnvironment.Apply();
        ship.transform.position=new Vector3(0,Camera.DownLimit+1.8f,-1);
        string directory="Temp/ChoreographyReview/"+level;Directory.CreateDirectory(directory);
        foreach(var previousFrame in Directory.GetFiles(directory,"????.jpg"))File.Delete(previousFrame);
        Set(flow,"currentWaveNumber",0);Set(flow,"stopingWave",false);Set(flow,"isWaveComingOut",false);
        flow.waves=Enumerable.Range(2,8).Select(x=>x.ToString()).ToArray();flow.enabled=true;flow.BeginWaves();
        float start=Time.time,nextFrame=0,lastChange=0;int lastWave=0,frames=0,peakEnemies=0,peakBullets=0;
        bool firing=false;var routes=new HashSet<string>();var timing=new List<string>();
        while(Time.time-start<48)
        {
            float elapsed=Time.time-start;int wave=WaveNumber(flow);
            if(wave!=lastWave)
            {
                if(lastWave>0 && elapsed-lastChange>7)throw new Exception(level+" has a stalled wave handoff");
                lastWave=wave;lastChange=elapsed;timing.Add(wave+":"+elapsed.ToString("F2",System.Globalization.CultureInfo.InvariantCulture));
            }
            if(wave>=8 && elapsed-lastChange>7)break;
            foreach(var flight in FindObjectsByType<FormationFlight>())if(flight.curve!=null)routes.Add(flight.curve.Name);
            peakEnemies=Mathf.Max(peakEnemies,AEnemy.Live.Count);peakBullets=Mathf.Max(peakBullets,CombatRound.Live.Count);
            // First half exposes the complete choreography. Second half includes stock-gun play.
            if(wave>=5 && !firing){InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Z));firing=true;}
            ship.transform.position=new Vector3(Mathf.Sin(elapsed*.65f)*2.7f,Camera.DownLimit+1.8f,-1);
            if(elapsed>=nextFrame)
            {
                yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(directory+"/"+frames.ToString("D4")+".jpg",image.EncodeToJPG(78));Destroy(image);
                frames++;nextFrame+=1f/6;
            }
            yield return null;
        }
        flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();
        Check(lastWave==8 && routes.Count==4,level+": all eight compositions and all four routes exercised through the real wave director");
        Check(peakEnemies>=5 && peakEnemies<55 && peakBullets>0,level+": active formations stay within the encounter budget");
        Check(FindAnyObjectByType<DecisionShop>()==null,level+": no unexpected decision interrupts the eight-wave combat passage");
        checks.Add(level+" observed peaks: "+peakEnemies+" enemies, "+peakBullets+" hostile rounds; handoffs "+string.Join(", ",timing));
        File.WriteAllText(directory+"/timing.txt",string.Join("\n",timing));
        File.WriteAllText("Temp/ChoreographyProgress.txt",string.Join("\n",checks));
    }
}
#endif
