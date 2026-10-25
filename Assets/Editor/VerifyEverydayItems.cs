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
public static class VerifyEverydayItems
{
    const string Key="EverydayItemReview";
    static double deadline;
    static VerifyEverydayItems()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            {new GameObject("Requested item review").AddComponent<EverydayItemReview>();deadline=EditorApplication.timeSinceStartup+480;}
        };
        EditorApplication.update+=()=>
        {
            if(deadline==0)return;
            const string report="ArtSource/EverydayItemReview/Verification.txt";
            if(File.Exists(report)){deadline=0;SessionState.SetBool(Key,false);EditorApplication.Exit(File.ReadAllText(report).StartsWith("PASS") ? 0 : 1);}
            else if(EditorApplication.timeSinceStartup>deadline){File.WriteAllText(report,"FAIL timed out");EditorApplication.Exit(1);}
        };
    }
    public static void Run()
    {
        Directory.CreateDirectory("ArtSource/EverydayItemReview");File.Delete("ArtSource/EverydayItemReview/Verification.txt");
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
}
public sealed class EverydayItemReview : MonoBehaviour
{
    const string Folder="ArtSource/EverydayItemReview/";
    static readonly Type[] Items={typeof(ChewingGum),typeof(HotSauce),typeof(RubberBand),typeof(MagnifyingGlass),typeof(StaticSweater),typeof(PocketFan),typeof(DecoyProjector)};
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
            model.transform.localPosition=new Vector3((i%4-1.5f)*2.45f,1.4f-(i/4)*2.8f,0);model.transform.localScale=Vector3.one*1.35f;
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
        var pool=new HashSet<string>();foreach(var f in typeof(ItemPool).GetFields(BindingFlags.NonPublic|BindingFlags.Static))if(f.GetValue(null) is string[] entries)foreach(var e in entries)pool.Add(e);
        Check(Items.All(t=>pool.Contains(t.Name)),"all seven items are registered for offers and Debug Run");
        yield return Catalog();
        Check(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("ShipDecoy")),"spectral projection shader compiles");
        yield return Grant(typeof(ChewingGum));
        var enemy=Target(new Vector3(0,2));PlayerRound.Spawn(Vector3.zero,Vector2.up,10,10);
        yield return new WaitForSeconds(.24f);
        Check(enemy.GetComponent<StickyGum>()!=null && enemy.health==1000,"physical shot sticks without immediately dealing damage");
        yield return Capture("GumSwelling");yield return new WaitForSeconds(.7f);
        Check(Mathf.Abs(enemy.health-990)<.01f,"gum pops once for the stored hit damage");
        Clear();yield return null;
        yield return Grant(typeof(BouncingBall));var first=Target(new Vector3(0,2));var second=Target(new Vector3(2,2));
        PlayerRound.Spawn(Vector3.zero,Vector2.up,10,10);yield return new WaitForSeconds(.45f);
        Check(first.GetComponent<StickyGum>()!=null && second.GetComponent<StickyGum>()!=null,"one bouncing projectile leaves gum on both enemies");
        yield return new WaitForSeconds(.8f);Check(first.health==990 && second.health==990,"both gum deposits deal damage independently");
        Clear();yield return null;
        yield return Grant(typeof(GasolineLeak));yield return Grant(typeof(HotSauce));
        var burning=Target(new Vector3(-2,2));var crossing=Target(new Vector3(0,2));
        burning.gameObject.AddComponent<BurningHull>().Ignite(6);
        for(int i=0;i<24;i++){burning.transform.position+=Vector3.right*.15f;yield return new WaitForSeconds(.04f);}
        Check(crossing.GetComponent<BurningHull>()!=null && crossing.health<1000,"moving burning enemy leaves a trail that ignites a crossing enemy");
        yield return Capture("HotSauceTrail");
        var trailCount=FindObjectsByType<HotSauceTrail>(FindObjectsSortMode.None).Length;
        Check(trailCount>0 && trailCount<=80,"fire trail remains bounded");
        Clear();yield return new WaitForSeconds(1.5f);
        Check(FindObjectsByType<HotSauceTrail>(FindObjectsSortMode.None).Length==0,"fire patches expire after enemies are gone");
        yield return Grant(typeof(RubberBand));
        float top=UnityEngine.Camera.main.transform.position.y+UnityEngine.Camera.main.orthographicSize;
        var round=PlayerRound.Spawn(new Vector3(0,top-.5f),Vector2.up,10,6);
        yield return new WaitForSeconds(.12f);Check(round!=null && round.transform.up.y<-.9f,"missed round turns down at the visible top edge");
        var returnTarget=Target(new Vector3(0,top-2));yield return new WaitForSeconds(1.1f);
        Check(returnTarget.health<1000 && returnTarget.GetComponent<BurningHull>()!=null,"returning shot can hit and keeps its burn coating");
        Clear();yield return null;
        var legacy=Instantiate(Resources.Load<GameObject>("Bullets/PlayerBullet"),new Vector3(2,top-.5f,-1),Quaternion.identity).GetComponent<ABullet>();
        legacy.setProperties(new Dictionary<string,float>{{"speed",6},{"damage",10},{"followness",0}});
        yield return new WaitForSeconds(.15f);Check(legacy!=null && legacy.transform.up.y<-.9f,"legacy projectile also returns once");
        Clear();yield return null;
        yield return Grant(typeof(MagnifyingGlass));float speed=GameRules.playerShootTravelSpeed;GameRules.playerShootTravelSpeed=1;
        Press(Key.Z);yield return new WaitForSeconds(.65f);Press();yield return null;
        int normal=PlayerRound.LiveCount;var normalRound=FindAnyObjectByType<PlayerRound>();float normalDamage=normalRound.damage;var normalSize=normalRound.transform.localScale;
        Clear();yield return null;
        Press(Key.LeftShift);yield return new WaitForSeconds(1.65f);Press(Key.LeftShift,Key.Z);yield return new WaitForSeconds(.65f);Press(Key.LeftShift);yield return null;
        int focused=PlayerRound.LiveCount;round=FindAnyObjectByType<PlayerRound>();
        Check(focused<normal && focused>=normal*.35f,"fully focused weapon fires roughly half as many rounds");
        Check(Mathf.Abs(round.damage/normalDamage-3)<.01f && Vector3.Distance(round.transform.localScale,normalSize*1.6f)<.01f,"focused rounds are three times stronger and 1.6 times wider");
        Clear();yield return null;GameRules.playerShootTravelSpeed=speed;
        Press(Key.LeftShift,Key.Z);yield return new WaitForSeconds(.4f);Press(Key.LeftShift);
        yield return Capture("MagnifiedRounds");Clear();yield return null;
        Press(Key.Z);yield return new WaitForSeconds(.2f);Press();yield return null;round=FindAnyObjectByType<PlayerRound>();
        Check(round!=null && Mathf.Abs(round.damage-normalDamage)<.01f && Vector3.Distance(round.transform.localScale,normalSize)<.01f,"releasing focus immediately restores normal shot power and size");
        Clear();yield return null;GameRules.playerShootTravelSpeed=speed;
        yield return Grant(typeof(NeedleCannonItem),1);yield return Grant(typeof(DoubleTrouble));
        Press(Key.Z,Key.X);yield return new WaitForSeconds(.15f);Press(Key.Z);yield return new WaitForSeconds(.25f);
        var rounds=FindObjectsByType<PlayerRound>(FindObjectsSortMode.None);
        Check(rounds.Any(b=>b.delivery==PlayerDelivery.Pulse) && rounds.Any(b=>b.delivery==PlayerDelivery.Needle),"both guns still fire when one coupled trigger is released");
        Press();yield return null;Clear();yield return null;
        yield return Grant(typeof(StaticSweater));var charge=ship.GetComponent<StaticCharge>();
        yield return new WaitForSeconds(.2f);Check(charge.Charge==0,"standing still cannot charge the sweater");
        for(int i=0;i<10;i++){Press(i%2==0 ? Key.RightArrow : Key.LeftArrow);yield return new WaitForSeconds(.45f);}
        Press();yield return null;Check(charge.Charge>=.999f,"actual player movement fully charges the sweater");
        ship.transform.position=new Vector3(0,-4,-1);yield return Capture("StaticReady");
        first=Target(new Vector3(0,2));second=Target(new Vector3(2,2));var third=Target(new Vector3(-2,2));
        PlayerRound.Spawn(Vector3.zero,Vector2.up,10,10);yield return new WaitForSeconds(.2f);
        Check(charge.Charge<.1f && second.GetComponent<StickyGum>()!=null && third.GetComponent<StickyGum>()!=null,"charged hit chains to nearby enemies and inherits gum");
        yield return Capture("StaticDischarge");yield return new WaitForSeconds(.9f);
        Check(second.health<1000 && third.health<1000,"chain lightning applies actual delayed damage");
        Clear();yield return null;
        CombatDecision.Begin();Press(Key.RightArrow);yield return new WaitForSeconds(.3f);Press();Check(charge.Charge<.1f,"decisions cannot build static charge");CombatDecision.End();
        yield return Grant(typeof(PocketFan),0);var fan=(PocketFan)granted;ship.transform.position=new Vector3(0,-3,-1);
        var near=CombatRound.Launch<SimpleRound>(new Vector3(0,-2),Vector2.down,4);
        var far=CombatRound.Launch<SimpleRound>(new Vector3(4,0),Vector2.down,4);
        var friendly=PlayerRound.Spawn(new Vector3(-1,-2),Vector2.up,1,0);
        var beam=CombatRound.Launch<PulseLaser>(new Vector3(1,-2),Vector2.up,0);beam.warning=5;
        Check(fan.OnPress(19)<0 && fan.OnPress(20)==0,"Pocket Fan requires and consumes twenty energy");
        Check(fan.OnPress(100)<0,"Pocket Fan respects its cooldown");
        float nearY=near.transform.position.y,farY=far.transform.position.y;var friendlyP=friendly.transform.position;var beamP=beam.transform.position;
        yield return new WaitForSeconds(.25f);
        Check(near.transform.position.y>nearY && far.transform.position.y<farY,"wind pushes nearby bullets upward while distant bullets keep travelling");
        Check(near.GetComponent<Collider2D>().enabled && near.damage>0,"pushed bullets remain dangerous");
        Check(friendly.transform.position==friendlyP && beam.transform.position==beamP,"wind leaves friendly rounds and attached beam positions unchanged");
        yield return Capture("PocketFan");float remaining=PocketWind.Current.Remaining;Time.timeScale=0;
        yield return new WaitForSecondsRealtime(.2f);Check(PocketWind.Current.Remaining==remaining,"pause suspends fan duration");Time.timeScale=1;
        yield return new WaitForSeconds(1.2f);Check(PocketWind.Current.Remaining==0,"wind expires after its short burst");
        Clear();yield return null;
        yield return Grant(typeof(DecoyProjector),0);var projector=(DecoyProjector)granted;
        ship.transform.position=new Vector3(-2,-4,-1);var original=ship.transform.position;
        var oldShot=CombatRound.Launch<SimpleRound>(new Vector3(0,4),Vector2.down,2);var oldHeading=oldShot.Heading;
        Check(projector.OnPress(24)<0 && projector.OnPress(25)==0,"Decoy Projector requires and consumes twenty-five energy");
        Press(Key.RightArrow,Key.Z);yield return new WaitForSeconds(.4f);Press();
        Check(ship.transform.position.x>original.x+.5f && PlayerRound.LiveCount>0,"projecting the copy allows uninterrupted movement and firing");
        Check(SpectralDecoy.Current.transform.position.y>original.y+1 && SpectralDecoy.Current.GetComponent<Collider2D>()==null,"spectral copy travels forward without a player collider");
        var attacker=Target(new Vector3(1,4));yield return null;
        typeof(ScoutInterceptor).GetMethod("Attack",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(attacker,null);
        var shots=CombatRound.Live.Where(b=>b is NeedleRound).ToArray();
        var aim=((Vector2)(SpectralDecoy.Current.transform.position-attacker.transform.position)).normalized;
        Check(shots.Length==2 && shots.All(b=>Vector2.Angle(b.Heading,aim)<6),"real aimed volley targets the moving spectral copy");
        Check(oldShot.Heading==oldHeading,"previously committed volley keeps its direction");
        for(int i=0;i<6;i++){yield return Capture("DecoyMotion"+i.ToString("00"));yield return new WaitForSeconds(.12f);}
        yield return Grant(typeof(BulletTime),1);var stop=(BulletTime)granted;stop.OnPress(100);
        var frozenCopy=SpectralDecoy.Current.transform.position;var frozenShot=oldShot.transform.position;
        Press(Key.Z,Key.RightArrow);int before=PlayerRound.LiveCount;yield return new WaitForSecondsRealtime(.35f);Press();
        Check(SpectralDecoy.Current.transform.position==frozenCopy && oldShot.transform.position==frozenShot,"Bullet Time freezes both decoy flight and hostile rounds");
        Check(PlayerRound.LiveCount>before,"revised weapon timing still emits suspended rounds in Bullet Time");
        Destroy(WorldTimeStop.Current);yield return null;Time.timeScale=1;
        yield return new WaitForSeconds(2.4f);
        Check(SpectralDecoy.Current==null && SpectralDecoy.AimPosition(ship.transform.position)==ship.transform.position,"decoy expires and aiming returns to the player");
        Clear();yield return null;
    }
}
#endif
