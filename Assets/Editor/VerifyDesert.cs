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
public static class VerifyDesert
{
    public const string Folder="ArtSource/DesertReview";
    static double deadline;
    static VerifyDesert()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("DesertReview",false))return;
            new GameObject("Desert verification").AddComponent<DesertReview>();deadline=EditorApplication.timeSinceStartup+360;
        };
        EditorApplication.update+=()=>
        {
            if(deadline==0)return;
            if(File.Exists(Folder+"/Verification.txt")){SessionState.SetBool("DesertReview",false);EditorApplication.Exit(File.ReadAllText(Folder+"/Verification.txt").StartsWith("PASS") ? 0 : 1);}
            else if(EditorApplication.timeSinceStartup>deadline){File.WriteAllText(Folder+"/Verification.txt","FAIL timeout");EditorApplication.Exit(1);}
        };
    }
    public static void Run()
    {
        Directory.CreateDirectory(Folder);Directory.CreateDirectory(Folder+"/Frames");
        if(File.Exists(Folder+"/Verification.txt"))File.Delete(Folder+"/Verification.txt");
        VerifyRunProgression.Run();
        var usedEnemies=new HashSet<string>();
        foreach(int seed in new[]{17381,421,9826})
        {
            for(float y=-30;y<50;y+=.5f)for(float x=-13;x<13;x+=1)
                if(Mathf.Abs(DesertGeometry.Height(x,y,seed)-DesertGeometry.Height(x,y+80,seed))>.0001f)throw new Exception("Dune wrap seam");
            LevelRun.Reset(LevelId.Desert,seed);
            for(int budget=2;budget<=20;budget++)
            {
                var wave=DistrictWaves.Create(budget);
                foreach(var unit in wave.units)usedEnemies.Add(unit.enemy);
                if(wave.units.Count<5 || !WaveGeometry.Clear(WaveVarietyAudit.Tracks(wave)))throw new Exception("Invalid desert formation "+budget);
                File.WriteAllText(Folder+"/Progress.txt","Composed seed "+seed+" budget "+budget);
            }
        }
        foreach(var enemy in new[]{"SandWorm","SandEmitter","SandStriker"})if(!usedEnemies.Contains(enemy))throw new Exception("Unreachable desert enemy: "+enemy);
        foreach(var part in WavePartLibrary.Desert)
        {
            bool found=false;for(int variant=0;variant<13;variant++)if(WaveComposer.PreviewPart(part,variant,1,out _)!=null){found=true;break;}
            if(!found)throw new Exception("Unplayable desert motif: "+part.Name);
        }
        File.WriteAllText(Folder+"/Geometry.txt","PASS: preserved serialized IDs; 400 normal loops and 50 extended loops; seamless dune heights; 57 seeded compositions with swept spacing checks.\n");
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");SessionState.SetBool("DesertReview",true);EditorApplication.isPlaying=true;
    }
}

public sealed class DesertReview : MonoBehaviour
{
    readonly List<string> errors=new List<string>(),checks=new List<string>();
    const string Folder=VerifyDesert.Folder;
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Screen.SetResolution(1280,720,false);Application.logMessageReceived+=Log;}
    void OnDestroy(){Application.logMessageReceived-=Log;}
    void Log(string message,string trace,LogType type){if((type==LogType.Error || type==LogType.Exception) && !trace.Contains("UnityEditor.Search"))errors.Add(message+"\n"+trace);}
    void Check(bool condition,string message){if(!condition)throw new Exception(message);checks.Add("PASS "+message);}
    IEnumerator Start()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            bool more=false;object step=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)step=stack.Peek().Current;}catch(Exception ex){failure=ex;}
            if(failure!=null){errors.Add(failure.ToString());break;}
            if(!more){stack.Pop();continue;}
            if(step is IEnumerator nested){stack.Push(nested);continue;}yield return step;
        }
        File.WriteAllText(Folder+"/Verification.txt",(errors.Count==0 ? "PASS" : "FAIL")+"\n"+string.Join("\n",checks)+"\n"+string.Join("\n",errors));
    }
    void Clear()
    {
        foreach(var e in FindObjectsByType<AEnemy>())if(e!=null)Destroy(e.gameObject);
        foreach(var round in CombatRound.Live.ToArray())round.Vanish();
        foreach(var round in FindObjectsByType<ABullet>())Destroy(round.gameObject);
    }
    IEnumerator Run()
    {
        GameRules.StartNewRun();LevelRun.Reset(LevelId.Desert,17381);
        yield return SceneManager.LoadSceneAsync("L1");yield return null;
        var flow=FindAnyObjectByType<WaveOrchestrer>();flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();
        var ship=FindAnyObjectByType<Shipcontroller>();ship.ObserverMode=true;ship.enabled=false;
        foreach(var weapon in ship.GetComponentsInChildren<AWeapon>()){weapon.StopFiring();weapon.enabled=false;}
        foreach(var collider in ship.GetComponentsInChildren<Collider2D>())collider.enabled=false;
        while(CloudTransition.Busy || !WavePreparation.Ready)yield return null;
        GameRules.playerDisabled=false;ship.transform.position=new Vector3(0,Camera.DownLimit+2,-1);
        yield return new WaitForSeconds(1);Capture("Scenery");
        var dust=FindAnyObjectByType<DesertDustStorm>();
        Check(dust!=null,"desert creates its dust storm system");
        float minDust=1,maxDust=0;bool capturedDust=false;
        var buried=DistrictEnemy.Spawn(typeof(SandEmitter),new Vector3(0,2,1));yield return null;
        Check(AEnemy.Live.Contains(buried) && CombatUpgrades.Nearest(buried.transform.position,null,.2f)==null,"buried turrets retain encounter bookkeeping without attracting homing fire");
        yield return new WaitForSeconds(1.7f);
        Check(buried.CanBeTargeted && CombatUpgrades.Nearest(buried.transform.position,null,.2f)==buried,"emerged turret becomes a valid homing target");
        // Real composed movement and hostile bullets, including the on-screen HUD and game filter.
        foreach(int budget in new[]{2,10,20})
        {
            Clear();yield return null;var wave=DistrictWaves.Create(budget);int next=0,peak=0;float start=Time.time,last=-1;
            while(Time.time-start<wave.duration+1)
            {
                float elapsed=Time.time-start;
                while(next<wave.units.Count && wave.units[next].time<=elapsed)
                {
                    var unit=wave.units[next++];var enemy=DistrictEnemy.Spawn(DistrictWaves.EnemyType(unit.enemy),new Vector3(unit.position.x*Camera.RightLimit,unit.position.y*Camera.UpLimit,1));enemy.FlyPhrase(unit);
                }
                peak=Mathf.Max(peak,CombatRound.Live.Count);
                minDust=Mathf.Min(minDust,dust.Strength);maxDust=Mathf.Max(maxDust,dust.Strength);
                if(!capturedDust && dust.Strength>.85f && CombatRound.Live.Count>20){Capture("DustStorm");capturedDust=true;}
                if(elapsed-last>.4f){last=elapsed;Capture("Frames/Wave"+budget+"-"+Mathf.RoundToInt(elapsed*100).ToString("D4"));}
                yield return null;
            }
            Check(next==wave.units.Count && peak>0,"wave "+budget+" completes with real firing; peak rounds "+peak);
        }
        Check(minDust<.05f && maxDust>.95f && capturedDust,"dust alternates between clear weather and full gusts during combat");
        Clear();yield return null;
        var worm=(DesertWormBoss)DistrictEnemy.Spawn(typeof(DesertWormBoss),new Vector3(0,Camera.UpLimit+3,1));
        yield return null;yield return null;
        Check(worm.Cores.Length==8 && worm.RemainingParts==8,"eight independently targetable boss reactors");
        float health=worm.health;worm.damage(1000000);Check(Mathf.Approximately(worm.health,health),"armor cannot bypass the reactors");
        for(int frame=0;frame<45;frame++){yield return new WaitForSeconds(.25f);Capture("Frames/Boss-"+frame.ToString("D3"));}
        Capture("Boss");
        float half=UnityEngine.Camera.main.orthographicSize*405f/720f;
        Check(worm.Cores.All(c=>Mathf.Abs(c.transform.position.x)<half-.45f),"all boss weakpoints fit inside the visible playfield");
        // Physics-driven stock rounds must reach an actual moving red weakpoint.
        var target=worm.Cores[2];float before=target.health;
        for(int shot=0;shot<8;shot++)
        {
            PlayerRound.Spawn(target.transform.position+Vector3.down*.5f,Vector2.up,20,12,PlayerDelivery.Pulse,0);
            yield return new WaitForSeconds(.08f);
        }
        Check(target.health<before,"real player projectiles damage a moving body reactor");
        worm.Cores[0].damage(1000000);yield return null;
        Check(worm!=null && worm.RemainingParts==7,"head destruction alone cannot kill the boss");
        worm.Cores[0].damage(1000000);Check(worm.RemainingParts==7,"destroyed cores ignore repeated damage");
        for(int i=1;i<=5;i++)worm.Cores[i].damage(1000000);
        yield return new WaitForSeconds(1);Capture("DamagedBoss");
        Check(worm!=null && worm.RemainingParts==2,"all remaining body parts are required");
        for(int i=6;i<8;i++)worm.Cores[i].damage(1000000);
        yield return null;yield return null;
        Check(worm==null,"final core triggers normal boss death");
        Check(!AEnemy.Live.OfType<DesertWormCore>().Any(),"boss cleanup removes all reactor targets");
        LevelRun.Reset(LevelId.Thunder,17381);LevelRun.GetChoices(out var left,out var right);
        Check(left==LevelId.Desert && right==LevelId.Fields,"third district offers desert or a new Plains loop");
        var choice=LevelChoice.Open(ship.transform,_=>{});yield return new WaitForSeconds(.5f);Capture("RouteChoice");Destroy(choice.gameObject);
    }
    public static void Capture(string name)
    {
        var source=UnityEngine.Camera.main;source.Render();
        int width=Screen.width,height=Screen.height;
        var target=new RenderTexture(width,height,24);var previous=RenderTexture.active;
        var go=new GameObject("Offscreen desert UI");var camera=go.AddComponent<UnityEngine.Camera>();
        camera.CopyFrom(source);camera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
        camera.enabled=false;camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);camera.cullingMask=1<<31;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var overlays=new List<Canvas>();var layers=new Dictionary<GameObject,int>();
        try
        {
            foreach(var canvas in FindObjectsByType<Canvas>())if(canvas.renderMode==RenderMode.ScreenSpaceOverlay)
            {
                overlays.Add(canvas);canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                foreach(var child in canvas.GetComponentsInChildren<Transform>(true))if(!layers.ContainsKey(child.gameObject)){layers.Add(child.gameObject,child.gameObject.layer);child.gameObject.layer=31;}
            }
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());Destroy(image);
        }
        finally
        {
            foreach(var canvas in overlays){canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}
            foreach(var pair in layers)pair.Key.layer=pair.Value;
            RenderTexture.active=previous;camera.targetTexture=null;Destroy(go);target.Release();Destroy(target);
        }
    }
}
#endif
