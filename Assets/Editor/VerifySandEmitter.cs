#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class VerifySandEmitter
{
    public const string Folder="ArtSource/SandEmitterReview";
    static double deadline;
    static VerifySandEmitter()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("SandEmitterReview",false))return;
            new GameObject("Grounded sand emitter review").AddComponent<SandEmitterReview>();deadline=EditorApplication.timeSinceStartup+240;
        };
        EditorApplication.update+=()=>
        {
            if(deadline==0)return;
            if(File.Exists(Folder+"/Verification.txt")){SessionState.SetBool("SandEmitterReview",false);EditorApplication.Exit(File.ReadAllText(Folder+"/Verification.txt").StartsWith("PASS") ? 0 : 1);}
            else if(EditorApplication.timeSinceStartup>deadline){File.WriteAllText(Folder+"/Verification.txt","FAIL timeout");EditorApplication.Exit(1);}
        };
    }
    public static void Run()
    {
        Directory.CreateDirectory(Folder+"/Frames");if(File.Exists(Folder+"/Verification.txt"))File.Delete(Folder+"/Verification.txt");
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");SessionState.SetBool("SandEmitterReview",true);EditorApplication.isPlaying=true;
    }
}
public sealed class SandEmitterReview : MonoBehaviour
{
    const string Folder=VerifySandEmitter.Folder;
    readonly List<string> checks=new List<string>(),errors=new List<string>();
    bool hitBackfaces;
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Application.logMessageReceived+=Log;hitBackfaces=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;}
    void OnDestroy(){Application.logMessageReceived-=Log;Physics.queriesHitBackfaces=hitBackfaces;}
    void Log(string message,string trace,LogType type){if((type==LogType.Error || type==LogType.Exception) && !trace.Contains("UnityEditor.Search"))errors.Add(message+"\n"+trace);}
    void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add("PASS "+message);}
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
        File.WriteAllText(Folder+"/Verification.txt",(errors.Count==0 ? "PASS" : "FAIL")+"\n"+string.Join("\n",checks)+"\n"+string.Join("\n",errors));
    }
    static bool BuildingAt(Vector3 p)=>Physics.Raycast(new Vector3(p.x,p.y,50),Vector3.forward,55,1<<30);
    static bool FitsActualBuildings(Vector3 p)
    {
        if(BuildingAt(p))return false;
        for(int i=0;i<16;i++)if(BuildingAt(p+new Vector3(Mathf.Cos(i*Mathf.PI/8),Mathf.Sin(i*Mathf.PI/8),0)*.88f))return false;
        return true;
    }
    void Clear()
    {
        foreach(var enemy in AEnemy.Live.ToArray())if(enemy!=null)Destroy(enemy.gameObject);
        foreach(var round in CombatRound.Live.ToArray())round.Vanish();
    }
    IEnumerator Run()
    {
        GameRules.StartNewRun();LevelRun.Reset(LevelId.Desert,17381);yield return SceneManager.LoadSceneAsync("L1");yield return null;
        var flow=FindAnyObjectByType<WaveOrchestrer>();flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();
        var ship=FindAnyObjectByType<Shipcontroller>();ship.ObserverMode=true;ship.enabled=false;
        foreach(var weapon in ship.GetComponentsInChildren<AWeapon>()){weapon.StopFiring();weapon.enabled=false;}
        foreach(var collider in ship.GetComponentsInChildren<Collider2D>())collider.enabled=false;
        while(CloudTransition.Busy || !WavePreparation.Ready)yield return null;
        GameRules.playerDisabled=false;ship.transform.position=new Vector3(0,Camera.DownLimit+2,-1);
        var buildings=FindObjectsByType<MeshFilter>().Where(f=>f.sharedMesh!=null && f.sharedMesh.name.StartsWith("Desert silos")).ToArray();
        foreach(var model in buildings){model.gameObject.layer=30;model.gameObject.AddComponent<MeshCollider>().sharedMesh=model.sharedMesh;}
        Physics.SyncTransforms();int sites=0,blocked=0;
        for(float y=Camera.DownLimit+3.2f;y<Camera.UpLimit-1.1f;y+=.5f)for(float x=-3.5f;x<=3.5f;x+=.5f)
        {
            var request=new Vector3(x,y,1);if(BuildingAt(request))blocked++;
            if(!LevelEnvironment.TryDesertGround(request,.9f,null,out var tile,out var local))continue;
            if(!FitsActualBuildings(tile.TransformPoint(local)))throw new Exception("A proposed sand site overlaps an actual building mesh");sites++;
        }
        Check(blocked>10 && sites>100,"building avoidance checked against actual roof, foundation and silo mesh projections across "+sites+" sites");
        var emitter=(SandEmitter)DistrictEnemy.Spawn(typeof(SandEmitter),new Vector3(0,4,1));yield return null;
        Check(!emitter.CanBeTargeted && CombatUpgrades.Nearest(emitter.transform.position,null,.2f)==null,"buried emitter is not a homing or damage target");
        float timeout=Time.time+3;while(!emitter.Grounded && Time.time<timeout)yield return null;
        Check(emitter.Grounded,"emitter chooses a clear terrain anchor before disturbing sand");Capture("Warning");
        yield return new WaitForSeconds(.72f);Capture("Emerging");
        Check(emitter.GetComponent<SandEmergenceEffects>().ActiveGrains>0,"surfacing ejects visible ballistic sand particles");
        timeout=Time.time+2;while(!emitter.CanBeTargeted && Time.time<timeout)yield return null;
        Check(emitter.CanBeTargeted,"emerged ground turret becomes targetable");yield return new WaitForSeconds(.12f);Capture("Exposed");
        var anchor=emitter.GroundPoint;var tileAnchor=emitter.GroundSection;
        float before=emitter.health;PlayerRound.Spawn(emitter.transform.position+Vector3.down*.3f,Vector2.up,20,3,PlayerDelivery.Pulse,0);
        ship.transform.position+=Vector3.right*1.4f;yield return new WaitForSeconds(.35f);
        Check(emitter.health<before,"real player rounds hit the terrain-bound turret");
        var floor=tileAnchor.TransformPoint(anchor);
        Check(Vector2.Distance(emitter.transform.position,floor)<.03f && emitter.GroundPoint==anchor,"turret stays fixed to scrolling ground through player parallax");
        Check(emitter.Hull.localScale==Vector3.one*emitter.hullScale && Quaternion.Angle(emitter.Hull.rotation,Quaternion.identity)<.1f,"ground hull rises at constant width without airborne banking");
        ship.transform.position=new Vector3(0,Camera.DownLimit+2,-1);ship.transform.position=emitter.transform.position+Vector3.down*1.8f;
        yield return new WaitForSeconds(.12f);Capture("Burrowing");
        Check(!emitter.CanBeTargeted && emitter.GetComponent<SandEmergenceEffects>().ActiveGrains>0,"retreat throws sand and disables the buried hit region");
        Clear();yield return null;ship.transform.position=new Vector3(0,Camera.DownLimit+2,-1);
        var seen=new HashSet<SandEmitter>();bool safe=true,attached=true;int peakRounds=0,totalGrains=0,shots=0;
        foreach(int budget in new[]{2,10,20})
        {
            var wave=DistrictWaves.Create(budget);int next=0;float start=Time.time,last=-1;
            while(Time.time-start<wave.duration+1)
            {
                float elapsed=Time.time-start;
                while(next<wave.units.Count && wave.units[next].time<=elapsed)
                {
                    var unit=wave.units[next++];var enemy=DistrictEnemy.Spawn(DistrictWaves.EnemyType(unit.enemy),Vector3.up*(Camera.UpLimit+2));enemy.FlyPhrase(unit);
                }
                Physics.SyncTransforms();
                foreach(var enemy in AEnemy.Live)if(enemy is SandEmitter turret && turret.Grounded)
                {
                    var position=turret.GroundSection.TransformPoint(turret.GroundPoint);safe&=FitsActualBuildings(position);
                    attached&=Vector2.Distance(enemy.transform.position,position)<.15f;seen.Add(turret);
                    totalGrains=Mathf.Max(totalGrains,turret.GetComponent<SandEmergenceEffects>().ActiveGrains);
                }
                peakRounds=Mathf.Max(peakRounds,CombatRound.Live.Count);
                if(elapsed-last>.12f){last=elapsed;Capture("Frames/Wave"+budget+"-"+Mathf.RoundToInt(elapsed*100).ToString("D4"));shots++;}
                yield return null;
            }
            Clear();yield return null;
        }
        Check(seen.Count>=6 && safe,"all "+seen.Count+" live wave turrets keep clear of buildings for their entire grounded life");
        Check(attached && totalGrains>20 && peakRounds>40,"terrain attachment, particles and hostile firing remain active in composed waves");
        Check(shots>100,"captured moving ground encounters through the game filter");
    }
}
#endif
