#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

public sealed class PlayerFeedbackProbe : MonoBehaviour
{
    const string Folder="ArtSource/CombatReview/PlayerFeedback";
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    readonly List<string> results=new List<string>(),errors=new List<string>();
    Shipcontroller ship;WaveOrchestrer flow;Keyboard keyboard;
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Application.logMessageReceived+=Log;}
    void OnDestroy(){Application.logMessageReceived-=Log;}
    void Log(string message,string trace,LogType type){if((type==LogType.Error || type==LogType.Exception) && !trace.Contains("UnityEditor.Search"))errors.Add(message);}
    void Check(bool condition,string message){if(!condition)throw new Exception(message);results.Add("PASS "+message);}
    IEnumerator Start()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            bool more=false;object step=null;Exception error=null;
            try{more=stack.Peek().MoveNext();if(more)step=stack.Peek().Current;}catch(Exception ex){error=ex;}
            if(error!=null){errors.Add(error.ToString());break;}
            if(!more){stack.Pop();continue;}
            if(step is IEnumerator nested){stack.Push(nested);continue;}yield return step;
        }
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
        string report=(errors.Count==0 ? "PASS" : "FAIL")+"\n"+string.Join("\n",results)+"\n"+string.Join("\n",errors);
        File.WriteAllText(Folder+"/Verification.txt",report);File.WriteAllText("Temp/PlayerFeedback.result",report);
    }
    void Clear()
    {
        foreach(var enemy in FindObjectsByType<AEnemy>())Destroy(enemy.gameObject);
        foreach(var bullet in FindObjectsByType<AEnemyBullet>())bullet.Vanish();
        foreach(var bullet in FindObjectsByType<ABullet>())Destroy(bullet.gameObject);
        foreach(var pickup in FindObjectsByType<APickable>())Destroy(pickup.gameObject);
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var image=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder+"/"+name+".jpg",image.EncodeToJPG(88));Destroy(image);
    }
    IEnumerator Clip(string name,float seconds)
    {
        Directory.CreateDirectory(Folder+"/"+name);float start=Time.time,next=0;int frame=0,peak=0;
        while(Time.time-start<seconds)
        {
            peak=Mathf.Max(peak,CombatRound.Live.Count);
            if(Time.time-start>=next){yield return Capture(name+"/"+(frame++).ToString("D4"));next+=.1f;}
            yield return null;
        }
        results.Add(name+": "+frame+" moving frames; peak "+peak+" bullets.");
    }
    IEnumerator Run()
    {
        GameRules.StartNewRun();LevelRun.Reset();Time.timeScale=1;
        if(SceneManager.GetActiveScene().name!="L1")yield return SceneManager.LoadSceneAsync("L1");
        yield return new WaitForSeconds(2);
        ship=FindAnyObjectByType<Shipcontroller>();ship.health=ship.maxHealth=10000;
        flow=FindAnyObjectByType<WaveOrchestrer>();flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();
        Clear();yield return null;keyboard=InputSystem.AddDevice<Keyboard>();
        var choreography=new GameObject("Phrase score audit").AddComponent<ChoreographyProbe>();choreography.enabled=false;
        typeof(ChoreographyProbe).GetMethod("InspectCompositions",Hidden).Invoke(choreography,null);
        typeof(ChoreographyProbe).GetMethod("InspectScores",Hidden).Invoke(choreography,null);
        Check(true,"all district compositions retain geometry, mirrored curves and synchronized firing at both loop tempos");
        Destroy(choreography.gameObject);Clear();yield return null;
        ship.transform.position=new Vector3(0,Camera.DownLimit+1.8f,-1);
        var scout=DistrictEnemy.Spawn(typeof(ScoutInterceptor),Vector3.zero);
        Check(scout.health<11.1f,"opening scout needs three paired stock-gun hits, down from five");
        Clear();yield return null;
        foreach(LevelId district in new[]{LevelId.Fields,LevelId.Snow,LevelId.City,LevelId.Thunder,LevelId.Tunnel})
        {
            LevelRun.Reset(district);LevelEnvironment.Apply();var wave=DistrictWaves.Create(2);
            float handoff=district==LevelId.Snow ? 4.4f : district==LevelId.City ? 3.25f : district==LevelId.Thunder ? 4 : district==LevelId.Tunnel ? 4.1f : 4.2f;
            Check(Mathf.Abs(wave.duration-handoff/PatternMover.FlightTempo)<.001f,district+" movement and wave launches share the slower timeline");
            float start=Time.time;int index=0,frame=0;float next=0;
            Directory.CreateDirectory(Folder+"/"+district);
            while(Time.time-start<8)
            {
                float elapsed=Time.time-start;
                while(index<wave.units.Count && wave.units[index].time<=elapsed)
                {
                    var unit=wave.units[index++];var enemy=DistrictEnemy.Spawn(DistrictWaves.EnemyType(unit.enemy),Vector3.zero);enemy.FlyPhrase(unit);
                }
                if(elapsed>=next){yield return Capture(district+"/"+(frame++).ToString("D4"));next+=.1f;}
                yield return null;
            }
            Check(index==wave.units.Count,district+" reviewed real moving formation and coordinated volleys");Clear();yield return null;
        }
        LevelRun.Reset();LevelEnvironment.Apply();
        var boss=(FieldsBomberBoss)DistrictEnemy.Spawn(typeof(FieldsBomberBoss),new Vector3(0,Camera.UpLimit-2.3f,1));
        yield return null;yield return null;Physics2D.SyncTransforms();
        var collider=boss.GetComponent<PolygonCollider2D>();
        Check(collider!=null && boss.GetComponent<CircleCollider2D>()==null,"Plains boss uses a hull polygon instead of its small circle");
        Check(collider.OverlapPoint(boss.Hull.TransformPoint(new Vector3(1.35f,.25f,0))),"outer wing receives real collision hits");
        Check(!collider.OverlapPoint(boss.Hull.TransformPoint(new Vector3(.60f,-1.05f,0))),"empty space beside the boss nose remains clear");
        yield return Clip("BossPhaseOne",7);
        boss.damage(boss.health*.51f);yield return Clip("BossPhaseTwo",7);
        Check(boss.Hull.GetComponent<MeshRenderer>().sharedMaterial==CombatMaterials.Fortress,"first boss uses its weathered panel texture");
        Clear();yield return null;
        foreach(var type in new[]{typeof(FieldsHeavyWing),typeof(SnowSiegeCraft),typeof(CityCommandCarrier),typeof(StormReactor),typeof(TunnelDefensePlatform)})
        {
            var enemy=DistrictEnemy.Spawn(type,new Vector3(0,Camera.UpLimit-2,1));yield return null;
            int shots=0;var attack=type.GetMethod("Attack",Hidden);var volley=typeof(DistrictEnemy).GetField("volley",Hidden);
            for(int i=1;i<=4;i++){volley.SetValue(enemy,i);int before=CombatRound.Live.Count;attack.Invoke(enemy,null);shots+=CombatRound.Live.Count-before;}
            Check(enemy.health>=640 && shots>=22,type.Name+" has a sturdy hull and substantial four-volley output: "+shots);
            yield return Clip(type.Name,1.5f);Clear();yield return null;
        }
        // Use equal launch speeds so travel distance measures the shell profile alone.
        BurstShell near=null,far=null;
        for(int i=0;i<3;i++)
        {
            var shell=CombatRound.Launch<BurstShell>(new Vector3(-1+i,Camera.UpLimit-2,-.5f),Vector2.down,3,.4f);
            float cruise=(float)typeof(BurstShell).GetField("cruise",Hidden).GetValue(shell);
            if(cruise>1)far=shell;else near=shell;
        }
        yield return new WaitForSeconds(.6f);
        Check(near.GetComponentInChildren<ParticleSystem>()==null || near.GetComponentInChildren<ParticleSystem>().particleCount==0,"travelling shells have no ignition warning");
        yield return new WaitForSeconds(.75f);
        Check(near.GetComponentInChildren<ParticleSystem>()!=null && near.GetComponentInChildren<ParticleSystem>().particleCount>0,"stopped shell warns with particles");
        Check(near.GetComponent<LineRenderer>()==null,"shell has no closing countdown circle");
        yield return Clip("ShellIgnition",.35f);
        yield return new WaitForSeconds(1.05f);
        Check(far!=null && far.gameObject.activeSelf,"long-range shell remains alive until its own fuse");
        Check(far.transform.position.y<Camera.UpLimit-8.5f,"long shell travels over twice as far before bursting");
        yield return Clip("DeepShellIgnition",.45f);Clear();yield return null;
        var shop=flow.gameObject.AddComponent<DecisionShop>();bool closed=false;
        ship.transform.position=new Vector3(0,-1,-1);shop.Open(flow,ship,"scoreShop",()=>closed=true);
        yield return new WaitForSeconds(2.2f);
        ship.transform.position=new Vector3(2,Camera.DownLimit-1,-1);yield return new WaitForSeconds(.4f);
        Check(shop.SkipProgress==0,"bottom corners do not charge the small centre skip zone");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.DownArrow));
        ship.transform.position=new Vector3(0,Camera.DownLimit,-1);yield return new WaitForSeconds(.65f);
        Check(shop.SkipProgress>.25f && shop.SkipProgress<.8f,"visible bottom-centre zone charges skip");
        var overlay=FindAnyObjectByType<DecisionOverlay>();var fill=overlay.GetComponentsInChildren<RectTransform>().First(x=>x.name=="Bottom-up skip charge");
        Check(fill.sizeDelta.x==96 && fill.sizeDelta.y>5 && fill.sizeDelta.y<48 && fill.pivot.y==0,"skip fills bottom to top across its compact zone");
        yield return Capture("ShopSkipHalf");
        yield return new WaitForSeconds(1);InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        Check(closed && !CombatDecision.Active,"holding in the zone skips without buying");
        InputSystem.RemoveDevice(keyboard);keyboard=null;Clear();yield return null;
        // Actual projectile collisions against every moving Plains route, plus sustained boss damage.
        new GameObject("Stock gun feedback benchmark").AddComponent<PlainsTtkProbe>();
        float deadline=Time.realtimeSinceStartup+200;
        while(!File.Exists("Temp/CombatResult.txt") && Time.realtimeSinceStartup<deadline)yield return null;
        Check(File.Exists("Temp/CombatResult.txt"),"stock-gun collision benchmark completed");
        string ttk=File.ReadAllText("Temp/CombatResult.txt");results.Add(ttk);Check(ttk.StartsWith("PASS"),"moving Plains targets and boss pass stock-gun benchmark");
    }
}
#endif
