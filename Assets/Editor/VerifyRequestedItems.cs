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
public static class VerifyRequestedItems
{
    const string Key="RequestedItemReview";
    static double deadline;
    static VerifyRequestedItems()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            {new GameObject("Requested item review").AddComponent<RequestedItemReview>();deadline=EditorApplication.timeSinceStartup+480;}
        };
        EditorApplication.update+=()=>
        {
            if(deadline==0)return;
            const string report="ArtSource/RequestedItemReview/Verification.txt";
            if(File.Exists(report)){deadline=0;SessionState.SetBool(Key,false);EditorApplication.Exit(File.ReadAllText(report).StartsWith("PASS") ? 0 : 1);}
            else if(EditorApplication.timeSinceStartup>deadline){File.WriteAllText(report,"FAIL timed out");EditorApplication.Exit(1);}
        };
    }
    public static void Run()
    {
        Directory.CreateDirectory("ArtSource/RequestedItemReview");File.Delete("ArtSource/RequestedItemReview/Verification.txt");
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
}
public sealed class RequestedItemReview : MonoBehaviour
{
    const string Folder="ArtSource/RequestedItemReview/";
    static readonly Type[] Items={typeof(ShrinkerJuice),typeof(EmpPulse),typeof(QualityGlasses),typeof(BadGlasses),typeof(GasolineLeak),typeof(Poison),typeof(BarrelRoll),typeof(BouncingBall),typeof(FocusTime),typeof(DoubleTrouble),typeof(EnergyCore),typeof(HealthPacks),typeof(LaserDroneItem),typeof(BulletDroneItem),typeof(BulletTime)};
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
            model.transform.localPosition=new Vector3((i%5-2)*2.05f,2.5f-(i/5)*2.25f,0);model.transform.localScale=Vector3.one*1.35f;
            model.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.ExpansionItem(Items[i].Name);model.AddComponent<MeshRenderer>().sharedMaterial=CombatMaterials.Fleet;models.Add(model.transform);
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
        yield return null;
        Time.timeScale=1;CombatDecision.End();
        ship=FindAnyObjectByType<Shipcontroller>();ship.ObserverMode=true;ship.transform.position=new Vector3(0,-4,-1);
        var flow=FindAnyObjectByType<WaveOrchestrer>();flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();
        Clear();yield return null;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        foreach(var device in InputSystem.devices.ToArray())if(device is Keyboard || device is Gamepad)InputSystem.DisableDevice(device);
        keyboard=InputSystem.AddDevice<Keyboard>();
        var pool=new HashSet<string>();foreach(var f in typeof(ItemPool).GetFields(BindingFlags.NonPublic|BindingFlags.Static))if(f.GetValue(null) is string[] entries)foreach(var e in entries)pool.Add(e);
        Check(Items.All(t=>pool.Contains(t.Name)),"all 15 items appear in normal offer pools");
        yield return Catalog();
        Check(!ShaderUtil.ShaderHasError(ProjectileSolid.SharedMaterial.shader),"projectile coating shader compiles");
        var root=ship.transform.localScale;var hull=ship.GetComponent<PSXShipVisual>().Model.localScale;
        Physics2D.SyncTransforms();var hitbox=ship.GetComponent<Collider2D>().bounds.size;
        var shot=PlayerRound.Spawn(Vector3.zero,Vector2.up,1,0);var shotScale=shot.transform.localScale;Destroy(shot.gameObject);
        yield return Grant(typeof(ShrinkerJuice));Physics2D.SyncTransforms();
        shot=PlayerRound.Spawn(Vector3.zero,Vector2.up,1,0);
        Check(ship.transform.localScale==root && shot.transform.localScale==shotScale,"Shrinker leaves the player root and projectile scale unchanged");
        Check(Vector3.Distance(ship.GetComponent<PSXShipVisual>().Model.localScale,hull*.7f)<.001f && Mathf.Abs(ship.GetComponent<Collider2D>().bounds.size.x/hitbox.x-.7f)<.01f,"Shrinker reduces hull and hitbox to 70 percent");
        yield return Capture("ShrunkenShip");Clear();yield return null;
        yield return Grant(typeof(QualityGlasses));
        var hostile=CombatRound.Launch<SimpleRound>(new Vector3(-3,4),Vector2.down,4);float y=hostile.transform.position.y,t=Time.time,speed=hostile.speed;
        yield return new WaitForSeconds(.5f);
        Check(Mathf.Abs((y-hostile.transform.position.y)/(speed*(Time.time-t))-.9f)<.04f,"Quality glasses slows an actual moving projectile by ten percent");
        yield return Grant(typeof(BadGlasses));
        Check(Mathf.Abs(CombatUpgrades.HostileSpeedAt(ship.transform.position)-.81f)<.001f && Mathf.Abs(CombatUpgrades.HostileSpeedAt(ship.transform.position+Vector3.up*4)-.9f)<.001f,"Bad glasses combines locally and releases outside its radius");
        Clear();yield return null;
        yield return Grant(typeof(GasolineLeak));var burnTarget=Target(new Vector3(-1,2));
        PlayerRound.Spawn(new Vector3(-1,0),Vector2.up,10,10);yield return new WaitForSeconds(.3f);
        Check(burnTarget.GetComponent<BurningHull>()!=null,"actual player hit ignites the enemy");
        yield return Capture("Burn");
        yield return Grant(typeof(Poison));var poisonTarget=Target(new Vector3(1,2));
        PlayerRound.Spawn(new Vector3(1,0),Vector2.up,10,10);yield return new WaitForSeconds(.3f);
        Check(poisonTarget.GetComponent<BurningHull>()!=null && poisonTarget.GetComponent<PoisonedHull>()!=null,"burn and poison coexist on a projectile hit");
        float health=poisonTarget.health;yield return new WaitForSeconds(.7f);Check(poisonTarget.health<health,"status damage continues after the projectile is gone");
        for(int i=0;i<6;i++)PlayerRound.Spawn(new Vector3(-2.5f+i,-1),Vector2.up,1,0,(PlayerDelivery)i).enabled=false;
        yield return Capture("CombinedCoatings");Clear();yield return null;
        yield return Grant(typeof(BouncingBall));
        var first=Target(new Vector3(-1,2));var second=Target(new Vector3(1,3));var third=Target(new Vector3(3,4));
        PlayerRound.Spawn(new Vector3(-1,0),Vector2.up,10,10);yield return new WaitForSeconds(.75f);
        Check(first.health<1000 && second.health<1000 && third.health==1000,"one real collision redirects into a second target, never a third");
        Clear();yield return null;
        yield return Grant(typeof(BarrelRoll));var roll=(BarrelRoll)granted;
        ship.ObserverMode=false;float startX=ship.transform.position.x;health=ship.health;
        var upgrades=ship.GetComponent<CombatUpgrades>();upgrades.Add(UpgradeKind.Reserve);upgrades.StoreOverflow(8);ship.mana=0;
        float rollEnergy=(float)typeof(Shipcontroller).GetMethod("UseActive",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ship,new object[]{roll});
        Check(rollEnergy==0 && ship.IsInvulnerable,"Barrel roll activates at zero energy and grants immediate i-frames");
        Check(upgrades.Reserve==4,"free roll preserves stored reserve energy");
        ship.ReceiveHazardDamage(10);Check(ship.health==health,"roll i-frames reject hazard damage");
        yield return new WaitForSeconds(.18f);yield return Capture("BarrelRoll");yield return new WaitForSeconds(.25f);
        Check(!ship.IsInvulnerable && ship.transform.position.x<startX-2.4f,"roll moves left and its i-frames expire");
        ship.ReceiveHazardDamage(1);Check(ship.health<health,"hazard damage resumes after roll");ship.ObserverMode=true;
        yield return Grant(typeof(FocusTime));ship.transform.position=new Vector3(0,-4,-1);Press(Key.RightArrow,Key.LeftShift);
        float start=ship.transform.position.x;t=Time.time;yield return new WaitForSeconds(.25f);Press();
        float focusRate=(ship.transform.position.x-start)/(GameRules.playerMoveSpeed*(Time.time-t));
        Check(Mathf.Abs(focusRate-.3f)<.05f,"held focus movement is 40 percent slower than original half-speed; measured "+focusRate+", focus multiplier "+ship.slowMultiplier);
        yield return Grant(typeof(DoubleTrouble));ship.GetComponent<ShipLoadout>().EquipWeapon("NeedleCannon",1);yield return null;
        Press(Key.Z);yield return new WaitForSeconds(.24f);
        var rounds=FindObjectsByType<PlayerRound>(FindObjectsSortMode.None);
        Check(rounds.Any(b=>b.delivery==PlayerDelivery.Pulse) && rounds.Any(b=>b.delivery==PlayerDelivery.Needle),"one trigger fires both equipped weapons");
        Press(Key.Z,Key.X);yield return new WaitForSeconds(.12f);Press(Key.Z);yield return new WaitForSeconds(.12f);
        Check(ship.GetComponent<ShipLoadout>().Weapons.All(w=>w.isShootingPrimary || w.isShootingSecondary),"releasing one held trigger keeps both guns firing");
        Press();yield return new WaitForSeconds(.4f);Clear();yield return null;
        yield return Grant(typeof(EnergyCore));ship.mana=20;t=Time.time;yield return new WaitForSeconds(.5f);
        Check(Mathf.Abs((ship.mana-20)/(Time.time-t)-3)<.2f,"Energy core restores three energy per combat second");
        float energy=ship.mana;CombatDecision.Begin();yield return new WaitForSeconds(.25f);Check(ship.mana==energy,"decisions cannot farm passive energy");CombatDecision.End();
        yield return Grant(typeof(HealthPacks),1);var pack=(HealthPacks)granted;ship.health=50;
        Check(pack.OnPress(29)<0 && ship.health==50,"Health packs requires sufficient energy");
        Check(pack.OnPress(30)==0 && ship.health==70,"Health packs converts 30 energy into 20 health");
        pack.lastCD=pack.cd;ship.health=ship.maxHealth;Check(pack.OnPress(30)<0,"full health cannot waste a health pack activation");
        yield return Grant(typeof(LaserDroneItem));yield return Grant(typeof(BulletDroneItem));yield return Grant(typeof(LaserDroneItem));
        ship.transform.position=new Vector3(0,-2,-1);var droneTarget=Target(new Vector3(0,4));
        yield return new WaitForSeconds(1.2f);var drones=ship.GetComponentsInChildren<TrailDrone>();
        Check(drones.Length==3 && drones[0].transform.position.y>drones[1].transform.position.y && drones[1].transform.position.y>drones[2].transform.position.y,"mixed drones form an ordered trailing line");
        Check(drones[0].transform.lossyScale.x>drones[2].transform.lossyScale.x && droneTarget.health<1000,"drones taper in size and deal actual damage");
        Press(Key.RightArrow);
        for(int i=0;i<12;i++)
        {
            if(i==6)Press(Key.LeftArrow);
            yield return new WaitForSeconds(.1f);yield return Capture("DroneMotion"+i.ToString("00"));
        }
        Press();
        ship.transform.position=new Vector3(Camera.RightLimit,Camera.DownLimit,-1);yield return new WaitForSeconds(.7f);
        float visibleBottom=UnityEngine.Camera.main.transform.position.y-UnityEngine.Camera.main.orthographicSize;
        float visibleRight=UnityEngine.Camera.main.transform.position.x+UnityEngine.Camera.main.orthographicSize*405f/720f;
        Check(drones.All(d=>d.GetComponent<MeshRenderer>().bounds.max.x<visibleRight && d.GetComponent<MeshRenderer>().bounds.min.y>visibleBottom),"entire drone hulls remain inside the actual visible arena at the bottom corner");
        yield return Capture("DroneCorner");
        foreach(var drone in drones)Destroy(drone.gameObject);Clear();yield return null;
        yield return Grant(typeof(BulletTime),1);var bulletTime=(BulletTime)granted;
        ship.transform.position=new Vector3(0,-2,-1);
        var frozenDrone=TrailDrone.Attach(ship,false);
        var enemy=Target(new Vector3(0,4));enemy.GetComponent<PatternMover>().enabled=true;
        hostile=CombatRound.Launch<SimpleRound>(new Vector3(-2,3),Vector2.down,4);
        shot=PlayerRound.Spawn(new Vector3(2,-2),Vector2.up,1,8);yield return null;
        var hostilePosition=hostile.transform.position;var shotPosition=shot.transform.position;var enemyPosition=enemy.transform.position;
        Check(bulletTime.OnPress(35)==0,"Bullet time consumes 35 energy");
        var dronePosition=frozenDrone.transform.position;
        var frozenBeam=CombatRound.Launch<PulseLaser>(ship.transform.position+Vector3.up*2,Vector2.down,0);
        frozenBeam.warning=0;frozenBeam.activeTime=5;
        typeof(CombatRound).GetField("age",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(frozenBeam,.3f);
        ship.ObserverMode=false;health=ship.health;
        yield return new WaitForSecondsRealtime(.2f);
        Check(ship.health==health,"frozen active beams cannot damage the player");ship.ObserverMode=true;
        Press(Key.Z,Key.RightArrow);start=ship.transform.position.x;
        yield return new WaitForSecondsRealtime(.5f);
        yield return new WaitForEndOfFrame(); // Followers undo inherited parent motion in LateUpdate, before rendering.
        Check(hostile.transform.position==hostilePosition && shot.transform.position==shotPosition,"hostile and friendly projectiles freeze together");
        Check(enemy.transform.position==enemyPosition,"enemy movement freezes with the world");
        Check(frozenDrone.transform.position==dronePosition,"drones stay frozen while the player moves");
        Check(ship.transform.position.x>start+.5f && FindObjectsByType<PlayerRound>(FindObjectsSortMode.None).Length>4,"player can move and spawn suspended shots");
        Press();yield return Capture("BulletTime");
        var menu=FindAnyObjectByType<Menu>();menu.setMenuOpen(true);float remaining=WorldTimeStop.Current.Remaining;
        yield return new WaitForSecondsRealtime(.3f);Check(Mathf.Abs(WorldTimeStop.Current.Remaining-remaining)<.03f,"pause suspends the freeze duration");
        menu.setMenuOpen(false);Check(Time.timeScale==0,"closing pause does not prematurely resume the world");
        yield return new WaitForSecondsRealtime(remaining+.15f);
        Check(!WorldTimeStop.Frozen && Time.timeScale==1,"Bullet time restores normal time after two active seconds");
        yield return new WaitForSeconds(.1f);Check(hostile.transform.position!=hostilePosition,"suspended projectiles resume travelling");
        Destroy(frozenDrone.gameObject);Clear();yield return null;
        yield return Grant(typeof(EmpPulse));var emp=ship.GetComponent<PeriodicEmp>();
        Check(emp.Remaining>19.8f,"EMP begins with a full twenty-second countdown");
        yield return new WaitForSeconds(19.5f);
        CombatRound.Launch<SimpleRound>(new Vector3(-2,3),Vector2.down,0);PlayerRound.Spawn(new Vector3(2,3),Vector2.up,1,0);
        yield return new WaitForSeconds(.15f);
        Check(FindObjectsByType<AEnemyBullet>(FindObjectsSortMode.None).Length>0,"EMP does not discharge before twenty seconds");
        yield return new WaitForSeconds(.45f);
        Check(FindObjectsByType<AEnemyBullet>(FindObjectsSortMode.None).Length==0 && FindObjectsByType<ABullet>(FindObjectsSortMode.None).Length==0,"EMP timer clears both friendly and hostile projectiles");
        Check(emp.Remaining>19.7f && emp.Remaining<=20,"EMP restarts its twenty-second interval");
        yield return Capture("EmpPulse");
    }
}
#endif
