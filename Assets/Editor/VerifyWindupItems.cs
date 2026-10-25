#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// Run in a separate project copy: never takes over an existing player session.
[InitializeOnLoad]
public static class VerifyWindupItems
{
    const string Key="ItemFeedbackReview";
    static double deadline;
    static VerifyWindupItems()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            {new GameObject("Requested item review").AddComponent<ItemFeedbackReview>();deadline=EditorApplication.timeSinceStartup+480;}
        };
        EditorApplication.update+=()=>
        {
            if(deadline==0)return;
            const string report="ArtSource/ItemFeedbackReview/Verification.txt";
            if(File.Exists(report)){deadline=0;SessionState.SetBool(Key,false);EditorApplication.Exit(File.ReadAllText(report).StartsWith("PASS") ? 0 : 1);}
            else if(EditorApplication.timeSinceStartup>deadline){File.WriteAllText(report,"FAIL timed out");EditorApplication.Exit(1);}
        };
    }
    public static void Run()
    {
        Directory.CreateDirectory("ArtSource/ItemFeedbackReview");File.Delete("ArtSource/ItemFeedbackReview/Verification.txt");
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
}
public sealed class ItemFeedbackReview : MonoBehaviour
{
    const string Folder="ArtSource/ItemFeedbackReview/";
    static readonly Type[] Items={typeof(WindUpCannonItem),typeof(Popcorn),typeof(Icicle)};
    readonly List<string> checks=new List<string>(),errors=new List<string>();
    Shipcontroller ship;Keyboard keyboard;AItem granted;
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Application.logMessageReceived+=Log;}
    void OnDestroy(){Application.logMessageReceived-=Log;}
    void Log(string message,string trace,LogType type){if((type==LogType.Exception || type==LogType.Error) && !trace.Contains("UnityEditor.Search"))errors.Add(message+"\n"+trace);}
    void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add("PASS "+message);}
    void Press(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();}
    IEnumerator Grant(Type type,int slot=0)
    {
        var go=Instantiate(Resources.Load<GameObject>("Items/Item"),new Vector3(100,100,1),Quaternion.identity);
        granted=(AItem)go.AddComponent(type);yield return null;granted.GrantTo(ship,slot);yield return null;
    }
    static void Clear()
    {
        foreach(var b in UnityEngine.Object.FindObjectsByType<AEnemyBullet>(FindObjectsSortMode.None))b.Vanish();
        foreach(var b in UnityEngine.Object.FindObjectsByType<ABullet>(FindObjectsSortMode.None))Destroy(b.gameObject);
        foreach(var e in AEnemy.Live.ToArray())if(e!=null)Destroy(e.gameObject);
        foreach(var p in UnityEngine.Object.FindObjectsByType<APickable>(FindObjectsSortMode.None))Destroy(p.gameObject);
    }
    DistrictEnemy Target(Vector3 p)
    {
        var e=DistrictEnemy.Spawn(typeof(ScoutInterceptor),p);e.health=1000;
        typeof(DistrictEnemy).GetField("nextAttack",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(e,10000f);
        foreach(var mover in e.GetComponents<PatternMover>())mover.enabled=false;
        return e;
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder+name+".png",image.EncodeToPNG());Destroy(image);
    }
    IEnumerator Catalog()
    {
        var studio=new GameObject("Item model review studio");studio.transform.position=new Vector3(1000,0,0);
        var cameraObject=new GameObject("Item review camera");cameraObject.transform.SetParent(studio.transform,false);cameraObject.transform.localPosition=new Vector3(0,0,-12);
        var camera=cameraObject.AddComponent<UnityEngine.Camera>();camera.orthographic=true;camera.orthographicSize=3.6f;camera.aspect=1.5f;camera.farClipPlane=25;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.04f,.055f);
        var texture=new RenderTexture(1500,1000,24);camera.targetTexture=texture;
        var models=new List<Transform>();
        for(int i=0;i<Items.Length;i++)
        {
            var model=new GameObject(Items[i].Name);model.transform.SetParent(studio.transform,false);
            model.transform.localPosition=new Vector3((i-1)*2.8f,0,0);model.transform.localScale=Vector3.one*1.35f;
            model.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.ExpansionItem(Items[i].Name);model.AddComponent<MeshRenderer>().sharedMaterial=EverydayItemMaterials.For(Items[i].Name);models.Add(model.transform);
            var title=new GameObject("Label");title.transform.SetParent(studio.transform,false);title.transform.localPosition=model.transform.localPosition+new Vector3(0,-.87f,-1);
            var text=title.AddComponent<TMPro.TextMeshPro>();text.text=System.Text.RegularExpressions.Regex.Replace(Items[i].Name.Replace("Item",""),"(?<=[a-z])(?=[A-Z])"," ");text.fontSize=1.65f;text.color=new Color(.75f,.83f,.85f);text.alignment=TMPro.TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(2,1);
            Check(model.GetComponent<MeshFilter>().sharedMesh.vertexCount>30,Items[i].Name+" has a dedicated 3D model");
        }
        for(int view=0;view<2;view++)
        {
            foreach(var model in models)model.localRotation=Quaternion.Euler(-22,view==0 ? 0 : 32,0);
            yield return new WaitForEndOfFrame();var previous=RenderTexture.active;RenderTexture.active=texture;
            var image=new Texture2D(1500,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1500,1000),0,0);image.Apply();RenderTexture.active=previous;
            File.WriteAllBytes(Folder+"Models"+view+".png",image.EncodeToPNG());Destroy(image);
        }
        camera.targetTexture=null;Destroy(studio);texture.Release();Destroy(texture);
    }
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
        if(keyboard!=null){Press();InputSystem.RemoveDevice(keyboard);}
        if(WorldTimeStop.Current!=null)Destroy(WorldTimeStop.Current);Time.timeScale=1;
        File.WriteAllText(Folder+"Verification.txt",(errors.Count==0 ? "PASS" : "FAIL")+"\n"+string.Join("\n",checks)+"\n"+string.Join("\n",errors));
    }
    IEnumerator Run()
    {
        GameRules.StartNewRun();LevelRun.Reset(LevelId.Fields,7921);Time.timeScale=1;
        CloudTransition.Load("L1");while(CloudTransition.Busy || SceneManager.GetActiveScene().name!="L1")yield return null;
        yield return null;Time.timeScale=1;CombatDecision.End();
        ship=FindAnyObjectByType<Shipcontroller>();ship.ObserverMode=true;ship.transform.position=new Vector3(0,-4,-1);
        var flow=FindAnyObjectByType<WaveOrchestrer>();flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();
        Clear();yield return null;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        foreach(var device in InputSystem.devices.ToArray())if(device is Keyboard || device is Gamepad)InputSystem.DisableDevice(device);
        keyboard=InputSystem.AddDevice<Keyboard>();
        yield return Catalog();
        var pool=new HashSet<string>();foreach(var f in typeof(ItemPool).GetFields(BindingFlags.NonPublic|BindingFlags.Static))if(f.GetValue(null) is string[] entries)foreach(var e in entries)pool.Add(e);
        Check(Items.All(t=>pool.Contains(t.Name)),"three new items registered in reward pools");
        foreach(string cue in new[]{"windup","ready","cannon","popcorn","frost","shatter","capacitor","overdrive","powerdown","static","roll","fan","decoy","heal","timeStop"})
            Check(ItemSound.Clip(cue)!=null,"audio imports: "+cue);
        yield return Grant(typeof(WindUpCannonItem));
        var cannon=ship.GetComponent<ShipLoadout>().Weapons[0] as WindUpCannon;
        Check(cannon!=null,"Wind-up Cannon equips through normal loadout");
        for(int tier=1;tier<=3;tier++)
        {
            Press(Key.Z);yield return new WaitForSeconds(tier==1 ? .2f : tier==2 ? .65f : 1.4f);
            Check(cannon.Level==tier && FindObjectsByType<PlayerRound>(FindObjectsSortMode.None).Length==0,"charge tier "+tier+" holds fire until release");
            yield return Capture("Charge"+tier);
            Press();yield return null;yield return null;
            var shots=FindObjectsByType<PlayerRound>(FindObjectsSortMode.None);
            Check(shots.Length==GameRules.playerShootNumber,"one released volley at tier "+tier);
            Check(shots.All(s=>Mathf.Abs(s.damage-GameRules.playerShootDamage*(tier==1 ? 2 : tier==2 ? 7 : 16))<.01f),"tier "+tier+" damage scaling");
            Check(shots.All(s=>s.pierces==(tier==1 ? 0 : tier==2 ? 2 : 5)),"tier "+tier+" piercing budget");
            yield return Capture("Release"+tier);Clear();yield return new WaitForSeconds(.2f);
        }
        Press(Key.Z);yield return new WaitForSeconds(.25f);float charge=cannon.Charge;Time.timeScale=0;
        yield return new WaitForSecondsRealtime(.2f);Check(cannon.Charge==charge,"pause does not build cannon charge");Time.timeScale=1;
        cannon.StopFiring();Press();yield return new WaitForSeconds(.2f);
        var freeze=ship.gameObject.AddComponent<WorldTimeStop>();freeze.Begin(2);
        Press(Key.Z);yield return new WaitForSecondsRealtime(.6f);Check(cannon.Level==2,"cannon charges during Bullet Time");
        Press();yield return null;yield return null;var frozenShot=FindAnyObjectByType<PlayerRound>();Check(frozenShot!=null,"cannon can release during Bullet Time");
        var position=frozenShot.transform.position;yield return new WaitForSecondsRealtime(.15f);Check(position==frozenShot.transform.position,"released cannon rounds remain suspended");
        Destroy(freeze);yield return null;Time.timeScale=1;Clear();yield return new WaitForSeconds(.2f);
        Press(Key.Z);yield return new WaitForSeconds(.3f);GameRules.playerDisabled=true;yield return null;Press();GameRules.playerDisabled=false;
        Check(!cannon.Charging && FindObjectsByType<PlayerRound>(FindObjectsSortMode.None).Length==0,"decision interruption cancels charge without firing");
        yield return Grant(typeof(DoubleTrouble));ship.GetComponent<ShipLoadout>().EquipWeapon("WindUpCannon",1);yield return null;
        Press(Key.Z);yield return new WaitForSeconds(.2f);Press(Key.Z,Key.X);yield return null;Press(Key.X);yield return new WaitForSeconds(.2f);
        Check(cannon.Charging && FindObjectsByType<PlayerRound>(FindObjectsSortMode.None).Length==0,"Double Trouble keeps charging while either fire control is held");
        Press();yield return null;yield return null;Check(FindObjectsByType<PlayerRound>(FindObjectsSortMode.None).Length==GameRules.playerShootNumber*2,"Double Trouble releases both cannons once");
        Clear();yield return null;
        yield return Grant(typeof(Popcorn));var enemy=Target(new Vector3(0,1));var u=ship.GetComponent<CombatUpgrades>();
        for(int i=0;i<4;i++)u.DirectHit(enemy,10);
        Check(FindObjectsByType<PlayerRound>(FindObjectsSortMode.None).Length==0,"Popcorn waits for fifth primary hit");
        u.DirectHit(enemy,10);Check(FindObjectsByType<PlayerRound>(FindObjectsSortMode.None).Length==3,"fifth hit creates three Popcorn pellets");
        u.DirectHit(enemy,10,1);Check(FindObjectsByType<PlayerRound>(FindObjectsSortMode.None).Length==3,"secondary fragments never recursively pop");
        yield return new WaitForSeconds(.12f);yield return Capture("Popcorn");yield return new WaitForSeconds(.5f);
        Check(FindObjectsByType<PlayerRound>(FindObjectsSortMode.None).Length==0,"Popcorn pellets have short bounded lifetime");
        enemy.gameObject.AddComponent<BurningHull>().Ignite(2);u.DirectHit(enemy,10);
        Check(FindObjectsByType<PlayerRound>(FindObjectsSortMode.None).Length==3,"burning target pops on next hit immediately");
        Destroy(ship.GetComponent<PopcornHits>());Clear();yield return null;
        yield return Grant(typeof(Icicle));enemy=Target(new Vector3(0,1));
        for(int i=0;i<3;i++){u.DirectHit(enemy,10);yield return new WaitForSeconds(.13f);}
        var ice=enemy.GetComponent<FrozenHull>();Check(ice.Frozen,"three primary hits create a frozen shell");
        Check(ice.GetComponentsInChildren<SpriteRenderer>().Count(s=>s.name=="Hull frost" && s.enabled)==3,"frost buildup is visible with three sprite crystals");
        yield return Capture("IcicleFrozen");
        Check(Mathf.Abs(u.DirectHit(enemy,10)-15)<.01f && !ice.Frozen,"next hit shatters for bonus damage and releases hold");
        Check(FindObjectsByType<PlayerRound>(FindObjectsSortMode.None).Length==5,"shatter releases five nonrecursive sprite fragments");
        yield return new WaitForSeconds(.1f);yield return Capture("IcicleShatter");Clear();yield return null;
        // Actual moving formation: its path clock must stop rather than jumping ahead on thaw.
        enemy=DistrictEnemy.Spawn(typeof(ScoutInterceptor),new Vector3(0,3));enemy.health=1000;yield return new WaitForSeconds(.2f);
        ice=enemy.gameObject.AddComponent<FrozenHull>();for(int i=0;i<3;i++){ice.Hit(1);yield return new WaitForSeconds(.13f);}
        position=enemy.transform.position;yield return new WaitForSeconds(.15f);
        Check(Vector3.Distance(position,enemy.transform.position)<.01f,"Icicle actually holds a moving Plains enemy");ice.Hit(1);yield return new WaitForSeconds(.2f);
        Check(Vector3.Distance(position,enemy.transform.position)>.01f,"enemy movement resumes after shattering");Clear();yield return null;
        yield return Grant(typeof(Capacitor));yield return new WaitForSeconds(2.4f);
        Check(u.Charge>.99f,"Capacitor still reaches full charge");Press(Key.Z);yield return new WaitForSeconds(.2f);Press();yield return null;yield return null;
        Check(FindObjectsByType<ChargedShotCue>(FindObjectsSortMode.None).Length>0,"charged volley carries its sprite wake");yield return Capture("Capacitor");Clear();yield return null;
        yield return Grant(typeof(Overdrive),0);float damage=GameRules.playerShootDamage;((Overdrive)granted).OnPress(100);yield return null;
        Check(GameRules.playerShootDamage==damage*2 && ship.transform.Find("Overdrive port")!=null,"Overdrive doubles damage and shows hull cues");yield return Capture("Overdrive");
        var boost=ship.GetComponent<OverdriveBoost>();boost.until=Time.time;yield return null;yield return null;
        Check(ship.GetComponent<OverdriveBoost>()==null && GameRules.playerShootDamage==damage,"Overdrive expiry restores damage and removes cues");
        yield return Grant(typeof(BadGlasses));
        CombatRound.Launch<SimpleRound>(ship.transform.position+new Vector3(-3.2f,0,0),Vector2.right,2);
        yield return new WaitForSeconds(.2f);yield return Capture("GlassesEntry");
        Check(ship.GetComponent<GlassesFieldCue>()!=null && Mathf.Abs(CombatUpgrades.HostileSpeedAt(ship.transform.position)-.9f)<.001f,"local glasses feedback preserves 10 percent slowdown");
        yield return Grant(typeof(EnergyCore));ship.mana=50;yield return new WaitForSeconds(.4f);Check(ship.mana>51,"Energy Core regeneration remains active");yield return Capture("EnergyCore");
        yield return Grant(typeof(StaticSweater));var sweater=ship.GetComponent<StaticCharge>();
        for(int i=0;i<150;i++){ship.transform.position+=Vector3.right*(i%2==0 ? .2f : -.2f);yield return null;}
        Check(sweater.Charge>0,"movement charges Static Sweater");yield return Capture("StaticBuildup");
        typeof(StaticCharge).GetField("<Charge>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(sweater,1f);
        yield return null;yield return Capture("StaticReady");enemy=Target(new Vector3(0,2));var neighbor=Target(new Vector3(1.5f,3));yield return null;sweater.Discharge(enemy,10);
        Check(sweater.Charge==0 && neighbor.health<1000,"static discharge consumes charge and damages neighbor");yield return Capture("StaticDischarge");Clear();yield return null;
        // Exercise the complete firing/impact combination under normal moving combat.
        flow.enabled=true;yield return Grant(typeof(Popcorn));yield return Grant(typeof(GasolineLeak));
        for(int i=0;i<4;i++)
        {
            var moving=DistrictEnemy.Spawn(typeof(ScoutInterceptor),new Vector3((i-1.5f)*1.2f,4+i*.6f));moving.health=30;
        }
        for(int i=0;i<6;i++){Press(Key.Z);yield return new WaitForSeconds(.65f);Press();yield return new WaitForSeconds(.18f);yield return Capture("Combat"+i);}
        Clear();
    }
}
#endif
