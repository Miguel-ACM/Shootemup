#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class VerifySolidProjectiles
{
    static double deadline;
    static VerifySolidProjectiles()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("SolidReview",false))
            {new GameObject("Solid projectile review").AddComponent<SolidProjectileReview>();deadline=EditorApplication.timeSinceStartup+600;}
        };
        EditorApplication.update+=()=>
        {
            if(deadline==0)return;
            const string report="ArtSource/SolidProjectileReview/Verification.txt";
            if(File.Exists(report)){deadline=0;SessionState.SetBool("SolidReview",false);EditorApplication.Exit(File.ReadAllText(report).StartsWith("PASS") ? 0 : 1);}
            else if(EditorApplication.timeSinceStartup>deadline){File.WriteAllText(report,"FAIL timed out");EditorApplication.Exit(1);}
        };
    }
    public static void Run()
    {
        Directory.CreateDirectory("ArtSource/SolidProjectileReview");File.Delete("ArtSource/SolidProjectileReview/Verification.txt");
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
        SessionState.SetBool("SolidReview",true);EditorApplication.isPlaying=true;
    }
}
public sealed class SolidProjectileReview : MonoBehaviour
{
    const string Folder="ArtSource/SolidProjectileReview/";
    static readonly Type[] Kinds={typeof(SimpleRound),typeof(NeedleRound),typeof(HelixRound),typeof(BurstShell),typeof(RicochetBolt),typeof(SuspendedRound),typeof(GuidedMissile),typeof(PlasmaOrb),typeof(ProximityMine),typeof(ReturningDisc),typeof(ScatterCarrier),typeof(LaserSideRound)};
    readonly List<string> checks=new List<string>(),errors=new List<string>();
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Application.logMessageReceived+=Log;}
    void OnDestroy(){Application.logMessageReceived-=Log;}
    void Log(string message,string trace,LogType type){if((type==LogType.Error || type==LogType.Exception) && !trace.Contains("UnityEditor.Search"))errors.Add(message);}
    void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add("PASS "+message);}
    static CombatRound Launch(Type type,Vector3 position,float radius=.18f,float speed=0)
        =>(CombatRound)typeof(CombatRound).GetMethod("Launch").MakeGenericMethod(type).Invoke(null,new object[]{position,Vector2.down,speed,radius});
    static void Clear()
    {
        foreach(var b in FindObjectsByType<CombatRound>())b.Vanish();
        foreach(var b in FindObjectsByType<ABullet>())Destroy(b.gameObject);
        foreach(var e in AEnemy.Live.ToArray())Destroy(e.gameObject);
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder+name+".jpg",t.EncodeToJPG(94));Destroy(t);
    }
    IEnumerator Start()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            object step=null;bool more=false;Exception error=null;
            try{more=stack.Peek().MoveNext();if(more)step=stack.Peek().Current;}catch(Exception ex){error=ex;}
            if(error!=null){errors.Add(error.ToString());break;}
            if(!more){stack.Pop();continue;}if(step is IEnumerator nested){stack.Push(nested);continue;}yield return step;
        }
        File.WriteAllText(Folder+"Verification.txt",(errors.Count==0 ? "PASS" : "FAIL")+"\n"+string.Join("\n",checks)+"\n"+string.Join("\n",errors));
    }
    IEnumerator Environment(LevelId level)
    {
        var w=WaveWatcher.Current;w.SelectLevel(level);yield return new WaitForSeconds(2);
        while(!WavePreparation.Ready)yield return null;
        w.StopAllCoroutines();w.enabled=false;Time.timeScale=1;Clear();yield return null;
    }
    void Gallery()
    {
        for(int i=0;i<Kinds.Length;i++)
        {
            var pos=new Vector3(-3.5f+(i%4)*2.25f,5.8f-(i/4)*4);
            var shot=Launch(Kinds[i],pos,.43f);shot.enabled=false;shot.transform.rotation=Quaternion.identity;
            var small=Launch(Kinds[i],pos+new Vector3(.75f,-1),.12f);small.enabled=false;small.transform.rotation=Quaternion.identity;
            Check(shot.GetComponent<ProjectileSolid>()!=null && shot.GetComponent<ProjectileSolid>().Mesh==small.GetComponent<ProjectileSolid>().Mesh,Kinds[i].Name+" uses shared solid geometry");
            var c=shot.GetComponent<CircleCollider2D>();Check(c.radius==.44f && c.offset==Vector2.zero,Kinds[i].Name+" retains centred .44 collision radius");
            Check(shot.GetComponent<ProjectileSolid>().Mesh.bounds.size.z>.1f,Kinds[i].Name+" has physical mesh depth");
        }
        for(int i=0;i<6;i++)
        {
            var p=PlayerRound.Spawn(new Vector3(-3.5f+i*1.4f,-6.6f),Vector2.up,1,0,(PlayerDelivery)i);p.enabled=false;
            Check(p.GetComponent<ProjectileSolid>()!=null,"player "+p.delivery+" uses a solid model");
        }
    }
    IEnumerator Run()
    {
        if(SceneManager.GetActiveScene().name!="Title")yield return SceneManager.LoadSceneAsync("Title");
        yield return null;WaveWatcher.Launch();while(WaveWatcher.Current==null || !WaveWatcher.Current.Ready)yield return null;
        WaveWatcher.Current.SetSeed(7119);WaveWatcher.Current.SelectView(WatcherView.Projectiles);
        yield return Environment(LevelId.Fields);Gallery();yield return null;
        Check(!ShaderUtil.ShaderHasError(ProjectileSolid.SharedMaterial.shader),"instanced projectile shader compiles");
        Check(ProjectileSolid.SharedMaterial.enableInstancing,"GPU instancing enabled on the single shared material");
        foreach(var solid in FindObjectsByType<ProjectileSolid>())
        {
            var sr=solid.GetComponent<SpriteRenderer>();var mr=solid.GetComponent<ProjectileSolid>().Renderer;
            Check(sr.forceRenderingOff && mr.enabled,"solid replaces sprite without duplicate rendering");
            Check(mr.sharedMaterial==ProjectileSolid.SharedMaterial && mr.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.Off,"shared material and no shadow pass");
        }
        var missile=FindObjectsByType<GuidedMissile>().First();var prop=new MaterialPropertyBlock();missile.GetComponent<ProjectileSolid>().Renderer.GetPropertyBlock(prop);
        var stretch=prop.GetVector("_Stretch");var scale=missile.transform.lossyScale;
        Check(Mathf.Abs(scale.x*stretch.x-scale.y*stretch.y)<.0001f,"missile solid core matches circular collision under nonuniform scale");
        Physics2D.SyncTransforms();
        foreach(var round in FindObjectsByType<CombatRound>().Where(x=>x is GuidedMissile || x is NeedleRound))
        {
            var collider=round.GetComponent<CircleCollider2D>();var s=round.transform.lossyScale;
            float radius=.44f*Mathf.Max(Mathf.Abs(s.x),Mathf.Abs(s.y));var center=(Vector2)round.transform.position;
            for(int i=0;i<16;i++)
            {
                var direction=new Vector2(Mathf.Cos(i*Mathf.PI/8),Mathf.Sin(i*Mathf.PI/8));
                Check(collider.OverlapPoint(center+direction*radius*.99f) && !collider.OverlapPoint(center+direction*radius*1.01f),round.GetType().Name+" rendered core agrees with actual physics boundary at angle "+i);
            }
        }
        yield return Capture("Catalog");
        Clear();yield return null;
        foreach(string prefab in new[]{"PlayerBullet","TwerpBullet","EnemyBulletSmall","EnemyBulletMedium","EnemyBulletBig","CircularWave"})
        {
            var obj=Instantiate(Resources.Load<GameObject>("Bullets/"+prefab),Vector3.zero,Quaternion.identity);
            yield return null;
            Check(obj!=null && obj.GetComponent<ProjectileSolid>()!=null,"legacy "+prefab+" uses solid geometry");Destroy(obj);
        }
        yield return null;
        // Pooling must not leave a stale disabled mesh, extra renderer or faded material behind.
        var pooled=CombatRound.Launch<SimpleRound>(Vector3.zero,Vector2.down);var mesh=pooled.GetComponent<ProjectileSolid>().Mesh;
        pooled.GetComponent<SpriteRenderer>().color=Color.gray;pooled.Vanish();
        var reused=CombatRound.Launch<SimpleRound>(Vector3.zero,Vector2.down);yield return null;
        Check(reused==pooled && reused.GetComponentsInChildren<MeshRenderer>().Length==1 && reused.GetComponent<ProjectileSolid>().Mesh==mesh,"pooled rounds reuse one mesh renderer");
        Check(reused.GetComponent<SpriteRenderer>().color==Color.white && reused.GetComponent<ProjectileSolid>().Renderer.enabled,"pooled tint and visibility reset");Clear();
        // Actual moving samples, including delayed stop/release, lateral ports, bounces and split shells.
        foreach(var level in new[]{LevelId.Fields,LevelId.Snow,LevelId.City,LevelId.Thunder,LevelId.Tunnel,LevelId.Desert})
        {
            yield return Environment(level);
            for(int i=0;i<Kinds.Length;i++)Launch(Kinds[i],new Vector3(-3.8f+(i%6)*1.5f,6-(i/6)*3),.18f,2.4f);
            for(int frame=0;frame<8;frame++){yield return new WaitForSeconds(.35f);yield return Capture(level+"Motion"+frame);}
            Check(CombatRound.Live.Any(x=>x is NeedleRound),level+" carrier continues generating modelled lateral bullets");
        }
        yield return Environment(LevelId.Snow);Time.timeScale=0;
        for(int i=0;i<CombatRound.Limit;i++)
        {
            float angle=i*2.39996f,r=Mathf.Sqrt((i+.5f)/CombatRound.Limit);
            var shot=Launch(Kinds[i%Kinds.Length],new Vector3(Mathf.Cos(angle)*4.5f*r,Mathf.Sin(angle)*7.2f*r),.12f);
        }
        for(int i=0;i<180;i++){var p=PlayerRound.Spawn(new Vector3(-4+(i%20)*.42f,-7+(i/20)*1.5f),Vector2.up,1,0,(PlayerDelivery)(i%6));}
        yield return null;Check(CombatRound.Live.Count==280 && PlayerRound.LiveCount==180,"stress scene reaches both caps: 280 hostile + 180 friendly");
        yield return Capture("DenseSnow");
        QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
        var solids=FindObjectsByType<ProjectileSolid>();
        foreach(bool models in new[]{false,true,false,true})
        {
            foreach(var s in solids){s.enabled=models;s.GetComponent<SpriteRenderer>().forceRenderingOff=models;s.GetComponent<ProjectileSolid>().Renderer.enabled=models;}
            for(int i=0;i<20;i++)yield return null;
            var times=new List<float>();long batches=0,triangles=0;
            for(int i=0;i<120;i++){yield return null;times.Add(Time.unscaledDeltaTime*1000);batches+=UnityStats.drawCalls;triangles+=UnityStats.triangles;}
            times.Sort();checks.Add((models ? "3D" : "SPRITE baseline")+": editor frame median "+times[60].ToString("F2")+"ms, p95 "+times[114].ToString("F2")+"ms; whole-scene draw calls "+batches/120+", triangles "+triangles/120);
        }
        checks.Add("Performance is an uncapped Editor comparison, not a target-device GPU benchmark. Six-district moving frames saved for visual review.");
    }
}
#endif
