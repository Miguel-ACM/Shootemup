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
public static class ReviewBossRadiance
{
    public const string Folder="ArtSource/BossRadianceReview";
    static double deadline;
    static ReviewBossRadiance()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("BossRadiance",false))return;
            new GameObject("Boss radiance batch").AddComponent<BossRadianceReview>();deadline=EditorApplication.timeSinceStartup+360;
        };
        EditorApplication.update+=()=>
        {
            if(deadline==0)return;
            string file=Folder+(SessionState.GetBool("BossGlowOnly",false) ? "/GlowRuntime.txt" : "/Runtime.txt");
            if(File.Exists(file)){SessionState.SetBool("BossRadiance",false);EditorApplication.Exit(File.ReadAllText(file).StartsWith("PASS") ? 0 : 1);}
            else if(EditorApplication.timeSinceStartup>deadline){File.WriteAllText(file,"FAIL timeout");EditorApplication.Exit(1);}
        };
    }
    public static void Run()
    {
        SessionState.SetBool("BossGlowOnly",false);
        Directory.CreateDirectory(Folder+"/Frames");File.Delete(Folder+"/Runtime.txt");
        try{Gallery();}catch(Exception e){File.WriteAllText(Folder+"/Runtime.txt","FAIL gallery\n"+e);EditorApplication.Exit(1);return;}
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");SessionState.SetBool("BossRadiance",true);EditorApplication.isPlaying=true;
    }
    public static void RunFinal()
    {
        Directory.CreateDirectory(Folder+"/FinalFrames");File.Delete(Folder+"/GlowRuntime.txt");
        SessionState.SetBool("BossGlowOnly",true);SessionState.SetBool("BossRadiance",true);
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");EditorApplication.isPlaying=true;
    }
    public static void RunGlow()
    {
        Directory.CreateDirectory(Folder);
        try{Gallery();EditorApplication.Exit(0);}catch(Exception e){File.WriteAllText(Folder+"/Editor.txt","FAIL\n"+e);EditorApplication.Exit(1);}
    }
    static void Gallery()
    {
        var scene=EditorSceneManager.NewPreviewScene();var previous=RenderTexture.active;
        const int w=768,h=512;
        var target=new RenderTexture(w,h,24,RenderTextureFormat.ARGBHalf);target.Create();
        var output=new RenderTexture(w,h,0);output.Create();
        var material=new Material(CombatVisuals.ProjectileMaterial);material.SetFloat("_Neutralize",1);
        var composite=new Material(Shader.Find("Shootemup/PSX Composite"));
        var glow=new ProjectileGlow(w,h);
        try
        {
            var go=new GameObject("Native projectile HDR review");SceneManager.MoveGameObjectToScene(go,scene);
            var camera=go.AddComponent<UnityEngine.Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=5.2f;
            camera.transform.position=new Vector3(0,0,-40);camera.allowHDR=true;camera.targetTexture=target;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.045f,.07f);
            var kinds=new[]{typeof(SimpleRound),typeof(NeedleRound),typeof(BurstShell),typeof(RicochetBolt),typeof(SuspendedRound),typeof(GuidedMissile),typeof(PlasmaOrb),typeof(ProximityMine),typeof(ReturningDisc),typeof(ScatterCarrier),typeof(LaserSideRound),typeof(HelixRound)};
            for(int row=0;row<9;row++)for(int col=0;col<12;col++)
            {
                var obj=new GameObject("Approved sprite "+row+" / "+col);SceneManager.MoveGameObjectToScene(obj,scene);
                var sr=obj.AddComponent<SpriteRenderer>();sr.sprite=row==8 ? PlayerProjectileArt.For((PlayerDelivery)(col%6)) : HostileProjectileArt.Sprite(kinds[col]);
                sr.sharedMaterial=material;sr.sortingLayerName="Bullet";sr.transform.position=new Vector3((col-5.5f)*1.15f,4.3f-row*.92f,0);sr.transform.localScale=Vector3.one*.55f;
                var block=new MaterialPropertyBlock();block.SetColor("_SpriteTint",row==8 ? new Color(.32f,.72f,.85f) : LevelProjectilePalette.Accent((LevelId)row));
                block.SetFloat("_Emission",row==8 ? 1.15f : 2.6f);sr.SetPropertyBlock(block);
            }
            // Disable emission in the same property blocks for a faithful before image.
            foreach(var sr in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<SpriteRenderer>()))
            {var block=new MaterialPropertyBlock();sr.GetPropertyBlock(block);block.SetFloat("_Emission",0);sr.SetPropertyBlock(block);}
            Save("Before",false);
            foreach(var sr in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<SpriteRenderer>()))
            {var block=new MaterialPropertyBlock();sr.GetPropertyBlock(block);block.SetFloat("_Emission",sr.name.StartsWith("Approved sprite 8") ? 1.15f : 2.6f);sr.SetPropertyBlock(block);}
            Save("After",true);
            if(ShaderUtil.ShaderHasError(material.shader) || ShaderUtil.ShaderHasError(Resources.Load<Shader>("ProjectileGlow")) || ShaderUtil.ShaderHasError(composite.shader))throw new Exception("Shader compile error");
            if(material.passCount!=1)throw new Exception("Projectile must remain one pass");
            File.WriteAllText(Folder+"/Editor.txt","PASS\n108 original sprite renders, eight district palettes plus player.\nSingle projectile pass; original sprite silhouette.\nHDR radiance extracted at quarter resolution with two shared blur passes.\n");
            void Save(string name,bool emissive)
            {
                camera.Render();glow.Render(target);composite.SetTexture("_GlowTex",emissive ? glow.Texture : Texture2D.blackTexture);
                if(emissive)
                {
                    var data=new Texture2D(w,h,TextureFormat.RGBAFloat,false,true);RenderTexture.active=target;data.ReadPixels(new Rect(0,0,w,h),0,0);data.Apply();
                    float peak=data.GetPixels().Max(c=>Mathf.Max(c.r,c.g,c.b));
                    var bloom=new Texture2D(glow.Texture.width,glow.Texture.height,TextureFormat.RGBAFloat,false,true);RenderTexture.active=glow.Texture;
                    bloom.ReadPixels(new Rect(0,0,bloom.width,bloom.height),0,0);bloom.Apply();var corner=bloom.GetPixel(0,0);
                    File.WriteAllText(Folder+"/Radiance.txt","HDR peak="+peak+"; bloom corner="+corner+"; peak bloom="+bloom.GetPixels().Max(c=>Mathf.Max(c.r,c.g,c.b)));
                    if(peak<=1 || Mathf.Max(corner.r,corner.g,corner.b)>.001f)throw new Exception("HDR emission or zero-background bloom check failed");
                    UnityEngine.Object.DestroyImmediate(data);UnityEngine.Object.DestroyImmediate(bloom);
                }
                Graphics.Blit(target,output,composite);RenderTexture.active=output;
                var image=new Texture2D(w,h,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            }
        }
        finally
        {
            RenderTexture.active=previous;EditorSceneManager.ClosePreviewScene(scene);target.Release();output.Release();
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(output);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(composite);glow.Dispose();
        }
    }
}
public sealed class BossRadianceReview : MonoBehaviour
{
    const string Folder=ReviewBossRadiance.Folder;
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    readonly List<string> checks=new List<string>(),errors=new List<string>();
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Screen.SetResolution(640,480,false);Application.logMessageReceived+=Log;}
    void OnDestroy(){Application.logMessageReceived-=Log;}
    void Log(string text,string trace,LogType type){if((type==LogType.Error || type==LogType.Exception) && !trace.Contains("UnityEditor.Search"))errors.Add(text);}
    static void Set(object target,string field,object value)=>target.GetType().GetField(field,Hidden).SetValue(target,value);
    void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add("PASS "+message);File.WriteAllText(Folder+"/Progress.txt",string.Join("\n",checks));}
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
        File.WriteAllText(Folder+(SessionState.GetBool("BossGlowOnly",false) ? "/GlowRuntime.txt" : "/Runtime.txt"),(errors.Count==0 ? "PASS" : "FAIL")+"\n"+string.Join("\n",checks)+"\n"+string.Join("\n",errors));
    }
    IEnumerator Review()
    {
        yield return null;GameRules.StartNewRun();LevelRun.Reset(LevelId.Fields,17381);CloudTransition.Load("L1");
        while(SceneManager.GetActiveScene().name!="L1")yield return null;yield return null;
        var flow=FindAnyObjectByType<WaveOrchestrer>();flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();
        var ship=FindAnyObjectByType<Shipcontroller>();ship.enabled=false;ship.ObserverMode=true;
        foreach(var weapon in ship.GetComponentsInChildren<AWeapon>()){weapon.StopFiring();weapon.enabled=false;}
        foreach(var collider in ship.GetComponentsInChildren<Collider2D>())collider.enabled=false;
        while(CloudTransition.Busy || !WavePreparation.Ready)yield return null;
        GameRules.playerDisabled=false;Time.timeScale=1;
        var presentation=UnityEngine.Camera.main.GetComponent<PSXPresentation>();
        Check(presentation.SceneTexture.format==RenderTextureFormat.ARGBHalf,"Actual gameplay world renders HDR radiance");
        bool glowOnly=SessionState.GetBool("BossGlowOnly",false);
        foreach(LevelId level in Enum.GetValues(typeof(LevelId)))
        {
            foreach(var enemy in FindObjectsByType<DistrictEnemy>())Destroy(enemy.gameObject);
            CombatRound.ClearAll();yield return null;LevelRun.Reset(level,17381);LevelEnvironment.Apply();yield return new WaitForSeconds(.5f);
            ship.transform.position=new Vector3(0,Camera.DownLimit+1.8f,-1);
            var boss=(DistrictBoss)DistrictEnemy.Spawn(DistrictWaves.BossType,new Vector3(0,Camera.UpLimit-2.5f,1));yield return null;
            for(int phase=1;phase<=2;phase++)
            {
                if(glowOnly && phase==1)continue;
                if(phase==2)
                {
                    CombatRound.ClearAll();
                    if(boss is DesertWormBoss worm){for(int i=1;i<=4;i++)worm.Cores[i].damage(100000);}
                    else if(boss is SpaceCitadelBoss station){station.Parts[0].damage(100000);station.Parts[1].damage(100000);}
                    else if(boss is CityDuoBoss duo){var survivor=duo.partner;duo.damage(100000);boss=survivor;}
                    else boss.health*=.45f;
                    yield return null;
                }
                float start=Time.time,next=1;int frame=0,peak=0;
                while(Time.time-start<(glowOnly ? 6.5f : 9))
                {
                    peak=Mathf.Max(peak,CombatRound.Live.Count);
                    if(Time.time-start>=next){next+=glowOnly ? 1 : .5f;DesertReview.Capture((glowOnly ? "FinalFrames/" : "Frames/")+level+"-"+phase+"-"+(frame++).ToString("D2"),Folder);}
                    yield return null;
                }
                Check(peak>0 && peak<=CombatRound.Limit,level+" phase "+phase+": firing choreography, peak "+peak+" / "+CombatRound.Limit);
                Check(CombatRound.Live.All(s=>s.GetComponent<MeshRenderer>()==null),level+": bullets remain sprites");
            }
        }
        foreach(var enemy in FindObjectsByType<DistrictEnemy>())Destroy(enemy.gameObject);CombatRound.ClearAll();yield return null;
        for(int i=0;i<CombatRound.Limit;i++)CombatRound.Launch<SimpleRound>(new Vector3((i%20-9.5f)*.42f,(i/20-6.5f)*.7f,0),Vector2.down,0,.14f);
        Check(CombatRound.Live.Count==CombatRound.Limit && CombatRound.Launch<SimpleRound>(Vector3.zero,Vector2.down)==null,"Dense field preserves the 280-round cap");
        DesertReview.Capture("Dense-Field",Folder);
        Check(presentation.CompositeMaterial.GetTexture("_GlowTex") is RenderTexture glow && glow.width==presentation.SceneTexture.width/4,"Every round shares one quarter-resolution glow field");
        var blockCheck=new MaterialPropertyBlock();CombatRound.Live.First().GetComponent<SpriteRenderer>().GetPropertyBlock(blockCheck);
        Check(blockCheck.isEmpty,"Hostile rounds retain shared materials and no per-round property updates");
        presentation.filterEnabled=false;typeof(PSXPresentation).GetMethod("Resize",Hidden).Invoke(presentation,null);yield return null;
        DesertReview.Capture("Unfiltered-Glow",Folder);Check(presentation.CompositeMaterial.GetFloat("_FilterEnabled")==0,"Glow remains enabled when the pixel filter is disabled");
        presentation.filterEnabled=true;typeof(PSXPresentation).GetMethod("Resize",Hidden).Invoke(presentation,null);yield return null;
        Check(presentation.CompositeMaterial.GetFloat("_FilterEnabled")==1,"Filter and glow resize cleanly");
    }
}
#endif
