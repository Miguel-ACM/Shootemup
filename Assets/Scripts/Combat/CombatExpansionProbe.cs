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

public sealed class CombatExpansionProbe : MonoBehaviour
{
    public bool FlowOnly;
    readonly List<string> checks=new List<string>();
    readonly List<string> errors=new List<string>();
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    Shipcontroller ship;
    WaveOrchestrer flow;
    Keyboard keyboard;
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Application.logMessageReceived+=Log;}
    void Log(string message,string trace,LogType kind)
    {
        if(kind==LogType.Exception || kind==LogType.Error)
        {
            if(trace.Contains("UnityEditor.Search"))return;
            if(!errors.Contains(message))errors.Add(message+"\n"+trace);
        }
    }
    IEnumerator Start()
    {
        var work=Run();
        while(true)
        {
            object step=null;bool more=false;Exception failure=null;
            try{more=work.MoveNext();if(more)step=work.Current;}catch(Exception ex){failure=ex;}
            if(failure!=null){errors.Add(failure.ToString());break;}
            if(!more)break;
            yield return step;
        }
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
        Time.timeScale=1;
        string result=(errors.Count==0 ? "PASS" : "FAIL")+"\n"+string.Join("\n",checks)+"\n"+string.Join("\n",errors);
        File.WriteAllText("ArtSource/CombatReview/"+(FlowOnly ? "FlowVerification.txt" : "Verification.txt"),result);
        File.WriteAllText("Temp/CombatResult.txt",result);
    }
    void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);checks.Add("PASS "+text);}
    static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,Private|BindingFlags.Public).SetValue(obj,value);
    void Clear()
    {
        foreach(var e in new List<AEnemy>(AEnemy.Live))if(e!=null)Destroy(e.gameObject);
        foreach(var b in FindObjectsByType<AEnemyBullet>())b.Vanish();
        foreach(var b in FindObjectsByType<ABullet>())Destroy(b.gameObject);
        foreach(var p in FindObjectsByType<APickable>())Destroy(p.gameObject);
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var image=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes("ArtSource/CombatReview/"+name+".png",image.EncodeToPNG());Destroy(image);
    }
    IEnumerator ModelSheet()
    {
        var studio=new GameObject("Fleet contact sheet");studio.transform.position=new Vector3(900,0,0);
        var go=new GameObject("Fleet review camera");go.transform.SetParent(studio.transform,false);go.transform.localPosition=new Vector3(0,0,-15);
        var camera=go.AddComponent<UnityEngine.Camera>();camera.orthographic=true;camera.orthographicSize=7.2f;camera.aspect=1.2f;
        camera.backgroundColor=new Color(.025f,.04f,.05f);camera.clearFlags=CameraClearFlags.SolidColor;
        var texture=new RenderTexture(1536,1280,24);camera.targetTexture=texture;camera.farClipPlane=30;
        var kinds=new[]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,20,21,22,23,24,25,26,27,28};
        for(int i=0;i<kinds.Length;i++)
        {
            var model=new GameObject("Hull "+kinds[i]);model.transform.SetParent(studio.transform,false);
            model.transform.localPosition=new Vector3((i%6-2.5f)*2.8f,(2f-i/6)*2.7f,0);
            model.transform.localScale=Vector3.one*.76f;model.transform.localRotation=Quaternion.Euler(-15,18,0);
            model.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.CombatHull(kinds[i]);model.AddComponent<MeshRenderer>().sharedMaterial=CombatMaterials.Fleet;
            var label=new GameObject("Hull label");label.transform.SetParent(studio.transform,false);label.transform.localPosition=model.transform.localPosition+new Vector3(0,-1.2f,-1);
            var text=label.AddComponent<TMPro.TextMeshPro>();text.text=kinds[i].ToString("00");text.fontSize=2.4f;text.alignment=TMPro.TextAlignmentOptions.Center;text.color=new Color(.65f,.75f,.77f);text.rectTransform.sizeDelta=new Vector2(2,1);
        }
        yield return new WaitForSeconds(.2f);yield return new WaitForEndOfFrame();
        var previous=RenderTexture.active;RenderTexture.active=texture;
        var image=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);image.Apply();RenderTexture.active=previous;
        File.WriteAllBytes("ArtSource/CombatReview/FleetModels.png",image.EncodeToPNG());Destroy(image);Destroy(studio);texture.Release();Destroy(texture);
    }
    void Press(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();}
    IEnumerator Choreography()
    {
        foreach(var level in new[]{LevelId.Fields,LevelId.Tunnel,LevelId.Snow,LevelId.Thunder,LevelId.City})
        {
            LevelRun.Reset(level);LevelEnvironment.Apply();Clear();yield return null;
            var wave=DistrictWaves.Create(4);int next=0,frame=0;float clock=0,nextFrame=.9f;
            while(clock<5)
            {
                while(next<wave.units.Count && wave.units[next].time<=clock)
                {
                    var unit=wave.units[next++];
                    var enemy=DistrictEnemy.Spawn(DistrictWaves.EnemyType(unit.enemy),new Vector3(unit.position.x*Camera.RightLimit,Camera.UpLimit+2,1));
                    enemy.FlyPhrase(unit);
                }
                if(clock>=nextFrame)
                {
                    yield return Capture(level+"Formation"+frame++);nextFrame+=.8f;
                }
                if(clock>2.2f)Press(Key.Z);
                clock+=Time.deltaTime;yield return null;
            }
            Press();Clear();yield return null;
        }
        LevelRun.Reset(LevelId.Fields);LevelEnvironment.Apply();
    }
    IEnumerator SawBounce()
    {
        Clear();yield return null;LevelRun.Reset(LevelId.Tunnel);LevelEnvironment.Apply();
        var saw=(TunnelSawBoss)DistrictEnemy.Spawn(typeof(TunnelSawBoss),new Vector3(0,3,1));yield return null;
        Check(saw.GetComponent<EnemyThrusters>()==null,"tunnel saw has no thrusters");
        Check(saw.GetComponent<CombatHullVisual>().spin>=720,"tunnel saw spins at least twice per second");
        saw.damage(1);yield return new WaitForSeconds(.3f);
        Check(CombatRound.Live.Count==0,"damaging the saw produces no bullets");
        float elapsed=0;while(CombatRound.Live.Count==0 && elapsed<9){elapsed+=Time.deltaTime;yield return null;}
        Check(CombatRound.Live.Count>=60,"a saw wall bounce emits a dense mixed-size burst");
        var camera=UnityEngine.Camera.main;float radius=saw.Hull.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x*saw.Hull.localScale.x;
        Check(Mathf.Abs(saw.transform.position.x+radius-(camera.transform.position.x+camera.orthographicSize*405/720))<.12f &&
            Mathf.Abs(saw.transform.position.y-radius-(camera.transform.position.y-camera.orthographicSize))<.12f,"saw reaches the actual bottom-right arena corner");
        yield return Capture("SawCornerBounce");Clear();yield return null;
    }
    IEnumerator RealWaveFlow()
    {
        Set(flow,"currentWaveNumber",0);Set(flow,"stopingWave",false);flow.waves=DistrictWaves.Sequence();flow.enabled=true;flow.BeginWaves();
        var survivor=CombatRound.Launch<SimpleRound>(new Vector3(-4,3),Vector2.down,0,.13f);
        float elapsed=0,nextCapture=2;int frame=0,peakEnemies=0,peakBullets=0;bool carried=false;
        while(elapsed<25)
        {
            ship.transform.position=new Vector3(Mathf.Sin(elapsed*.7f)*2.6f,-5,-1);Press(Key.Z);
            peakEnemies=Mathf.Max(peakEnemies,AEnemy.Live.Count);peakBullets=Mathf.Max(peakBullets,CombatRound.Live.Count);
            int phrase=(int)typeof(WaveOrchestrer).GetField("currentWaveNumber",Private).GetValue(flow);
            if(phrase>=2 && elapsed<8 && survivor.gameObject.activeSelf)carried=true;
            if(elapsed>=nextCapture){yield return Capture("LiveWaveFlow"+frame++);nextCapture+=3;}
            elapsed+=Time.deltaTime;yield return null;
        }
        Press();flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();
        Check(carried,"hostile bullets survive a real combat-wave transition");
        Check((int)typeof(WaveOrchestrer).GetField("currentWaveNumber",Private).GetValue(flow)>=5,"real orchestration advances through at least five phrases in 25 seconds");
        Check(!FindAnyObjectByType<DecisionShop>(),"no shop interrupts the first 25 seconds of combat");
        checks.Add("Observed peak concurrent enemies: "+peakEnemies+"; peak hostile rounds: "+peakBullets+". Player fires the base weapon and sweeps laterally.");
        Clear();yield return null;
    }
    IEnumerator Run()
    {
        GameRules.StartNewRun();LevelRun.Reset(LevelId.Fields);Time.timeScale=1;
        if(SceneManager.GetActiveScene().name!="L1")yield return SceneManager.LoadSceneAsync("L1");
        yield return new WaitForSecondsRealtime(1);
        Time.timeScale=1;CombatDecision.End();
        ship=GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();
        ship.health=ship.maxHealth=10000;ship.transform.position=new Vector3(0,-5,-1);
        flow=FindAnyObjectByType<WaveOrchestrer>();
        Check(flow.waves[0]!="reward" && flow.waves[0]!="shop","levels begin with combat");
        flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();Clear();yield return null;
        keyboard=InputSystem.AddDevice<Keyboard>();
        if(FlowOnly){yield return RealWaveFlow();yield break;}
        yield return ModelSheet();
        Check(DistrictWaves.Sequence().Count(x=>x.EndsWith("Shop"))==1,"one shop interrupts each district");
        Check(Array.IndexOf(DistrictWaves.Sequence(),"reward")>=6,"six combat phrases precede the first reward");
        var visibleShot=PlayerRound.Spawn(new Vector3(0,0),Vector2.up,1,0);
        yield return null;
        Check(visibleShot.GetComponent<SpriteRenderer>().bounds.size.x>=.18f,"base player fire has a visible world-space width");
        yield return Capture("BaseGunVisibility");Clear();yield return null;
        yield return Choreography();
        yield return SawBounce();
        var hud=ship.GetComponent<LoadoutHUD>();Check(hud!=null,"octagonal loadout HUD is installed");
        foreach(var panel in hud.Panels)
        {
            var corners=new Vector3[4];panel.GetWorldCorners(corners);float half=202.5f*Screen.height/720f;
            Check(corners[2].x<Screen.width*.5f-half || corners[0].x>Screen.width*.5f+half,"equipment instrument stays outside combat area");
        }
        Check(CombatVisuals.Projectile().texture==Resources.Load<GameObject>("Bullets/EnemyBulletSmall").GetComponent<SpriteRenderer>().sprite.texture,"hostile rounds preserve authored projectile artwork");
        foreach(var type in DistrictWaves.EnemyTypes)
        {
            var enemy=DistrictEnemy.Spawn(type,new Vector3(0,5,1));yield return null;
            Check(enemy.health>0 && enemy.Hull.GetComponent<MeshFilter>().sharedMesh.vertexCount>100,type.Name+" spawns with a detailed hull");
            Check(enemy.GetComponent<EnemyThrusters>()!=null,type.Name+" has independent thrusters");
            Clear();yield return null;
        }
        var carrier=DistrictEnemy.Spawn(typeof(ArmoredCarrier),new Vector3(0,4,1));yield return null;yield return null;
        Check(AEnemy.Live.Count==3,"armored carrier deploys two escorts");
        Clear();yield return null;
        var shell=CombatRound.Launch<BurstShell>(new Vector3(0,5),Vector2.down,3,.4f);
        yield return new WaitForSeconds(1.5f);shell.Vanish();int clearedCount=CombatRound.Live.Count;
        yield return new WaitForSeconds(.8f);Check(clearedCount==0 && CombatRound.Live.Count==0,"cleared burst shell suppresses fragments");
        shell=CombatRound.Launch<BurstShell>(new Vector3(0,5),Vector2.down,3,.4f);
        yield return new WaitForSeconds(2.2f);Check(CombatRound.Live.Count>=8,"live burst shell emits its delayed pattern");
        Clear();yield return null;
        var ricochet=CombatRound.Launch<RicochetBolt>(new Vector3(Camera.RightLimit-.3f,3),Vector2.right,4);
        yield return new WaitForSeconds(.15f);Check(ricochet.Heading.x<0,"tunnel bolt reflects from the marked boundary");
        Clear();yield return null;
        var orb=CombatRound.Launch<PlasmaOrb>(new Vector3(0,3),Vector2.down,0,.55f);
        PlayerRound.Spawn(new Vector3(0,2),Vector2.up,30,8);
        yield return new WaitForSeconds(.3f);Check(!orb.gameObject.activeSelf,"player fire destroys plasma orbs through real physics");
        Clear();yield return null;
        var suspended=CombatRound.Launch<SuspendedRound>(new Vector3(-2,5),Vector2.down,3);
        yield return new WaitForSeconds(.95f);float hanging=suspended.transform.position.y;
        yield return new WaitForSeconds(.5f);Check(Mathf.Abs(hanging-suspended.transform.position.y)<.03f,"suspended shots hold still before accelerating");
        yield return new WaitForSeconds(.7f);Check(suspended.transform.position.y<hanging-.2f,"suspended shots resume their original trajectory");Clear();yield return null;
        var missile=CombatRound.Launch<GuidedMissile>(new Vector3(2,6),Vector2.down,1);
        yield return new WaitForSeconds(1.15f);var committed=missile.Heading;ship.transform.position=new Vector3(-4,-5,-1);
        yield return new WaitForSeconds(.4f);Check(Vector2.Dot(committed,missile.Heading)>.999f,"guided missiles stop tracking after committing");Clear();yield return null;
        ship.transform.position=new Vector3(0,-5,-1);
        var scatter=CombatRound.Launch<ScatterCarrier>(new Vector3(0,5),Vector2.down,1);
        yield return new WaitForSeconds(2.05f);Check(FindObjectsByType<NeedleRound>().Length==6,"scatter carriers release exactly three lateral pairs");Clear();yield return null;
        var disc=CombatRound.Launch<ReturningDisc>(new Vector3(0,5),Vector2.down,2);
        yield return new WaitForSeconds(2.3f);Check(disc.Heading.y>0,"returning discs curve back toward their launch point");Clear();yield return null;
        var mine=CombatRound.Launch<ProximityMine>(ship.transform.position,Vector2.down,0);
        yield return new WaitForSeconds(1.6f);Check(!mine.GetComponent<Collider2D>().enabled,"mines do not arm underneath the player");
        ship.transform.position+=Vector3.right*3;yield return new WaitForSeconds(.15f);Check(mine.GetComponent<Collider2D>().enabled,"mines arm once the player has a safe separation");Clear();yield return null;
        ship.transform.position=new Vector3(0,-5,-1);
        var laser=CombatRound.Launch<PulseLaser>(ship.transform.position+Vector3.up*4,Vector2.down,0);
        float before=ship.health;yield return new WaitForSeconds(.45f);Check(ship.health==before,"laser warning is harmless");
        yield return new WaitForSeconds(.6f);Check(ship.health<before,"locked laser deals damage while active");
        Clear();yield return null;
        foreach(var level in new[]{LevelId.Fields,LevelId.Tunnel,LevelId.Snow,LevelId.Thunder,LevelId.City})
        {
            LevelRun.Reset(level);LevelEnvironment.Apply();yield return new WaitForSeconds(.2f);
            var boss=(DistrictBoss)DistrictEnemy.Spawn(DistrictWaves.BossType,new Vector3(0,5,1));
            yield return new WaitForSeconds(.2f);
            Check(boss is CityDuoBoss duoHealth ? boss.health+duoHealth.partner.health>=3500 : boss.health>=2500,level+" boss durability meets the restored baseline");
            boss.damage(boss.health*.55f);yield return new WaitForSeconds(3);
            if(level==LevelId.Tunnel)Check(FindObjectsByType<TunnelSawBoss>().Count(x=>x.miniature)==2,"tunnel phase two deploys two sturdy miniatures");
            if(level==LevelId.Thunder)Check(FindObjectsByType<PulseLaser>().Length>=7,"thunder phase two launches seven sweeping beams");
            if(level==LevelId.City)
            {
                var duo=(CityDuoBoss)boss;var survivor=duo.partner;
                float maximum=survivor.health;survivor.damage(200);float wounded=survivor.health;
                int drops=FindObjectsByType<APickable>().Length;duo.damage(100000);yield return null;
                Check(Mathf.Abs(survivor.health-(wounded+(maximum-wounded)*.5f))<.01f,"city survivor heals half its missing health");
                Check(FindObjectsByType<APickable>().Length==drops,"first city boss kill drops nothing");
            }
            yield return Capture(level+"BossPhaseTwo");Clear();yield return null;
        }
        LevelRun.Reset(LevelId.City);LevelEnvironment.Apply();yield return new WaitForSeconds(.3f);
        for(int i=0;i<3;i++)DistrictEnemy.Spawn(DistrictWaves.EnemyTypes[12+i],new Vector3((i-1)*2.4f,3+i,1));
        yield return new WaitForSeconds(1);yield return Capture("CityFleet");Clear();yield return null;
        var loadout=ship.GetComponent<ShipLoadout>();
        foreach(string name in new[]{"NeedleCannon","BurstLauncher","BeamEmitter","DiscThrower","SwarmRack","ScatterCannon","FlameProjector","RailDriver","ElectricityWeapon"})
        {
            loadout.EquipWeapon(name,0);yield return null;
            Press(Key.Z);yield return new WaitForSeconds(.6f);Press();
            Check(loadout.Weapons[0]!=null && ship.GetComponentsInChildren<AWeapon>().Count(x=>x.Slot==AWeapon.WeaponInput.primary)==1,name+" replaces the primary slot without stacking");
            loadout.Weapons[0].StopFiring();Clear();yield return null;
        }
        loadout.EquipWeapon("NeedleCannon",0);loadout.EquipWeapon("BurstLauncher",1);yield return null;
        foreach(string activeName in new[]{"GravityWell","MirrorBurst"})
        {
            var item=Instantiate(Resources.Load<GameObject>("Items/Item"));var active=(AActiveItem)item.AddComponent(Type.GetType(activeName));yield return null;
            typeof(AItem).GetProperty("ReplacementSlot").SetValue(active,activeName=="GravityWell" ? 0 : 1);active.OnPickup();
        }
        yield return null;yield return Capture("LoadoutHUD");
        CombatDecision.Begin();Press(Key.X);yield return null;Press();
        Check(loadout.HoveredItem==null,"fire away from an item does not preselect a replacement");yield return Capture("ReplacementHUD");
        Press(Key.A);yield return null;Press();Check(loadout.HoveredItem==null,"active away from an item does not preselect a replacement");CombatDecision.End();
        var upgrades=ship.GetComponent<CombatUpgrades>();
        foreach(UpgradeKind kind in Enum.GetValues(typeof(UpgradeKind)))upgrades.Add(kind);
        var target=DistrictEnemy.Spawn(typeof(ShieldTransport),new Vector3(0,3,1));yield return null;
        float health=target.health;float dealt=upgrades.DirectHit(target,4);target.damage(dealt);
        yield return new WaitForSeconds(.6f);Check(target.health<health-dealt,"Lighter applies damage over time after direct fire");
        Clear();yield return null;
        for(int i=0;i<400;i++)CombatRound.Launch<SimpleRound>(new Vector3(0,6),Vector2.down,1);
        Check(CombatRound.Live.Count==CombatRound.Limit,"hostile projectile stress remains capped at 280");
        for(int i=0;i<250;i++)PlayerRound.Spawn(new Vector3(0,-3),Vector2.up,1,3);
        Check(PlayerRound.LiveCount<=180,"friendly secondary-effect stress remains capped at 180");
        Clear();yield return null;
        Check(DecisionShop.RewardCount(0,.9f)==2 && DecisionShop.RewardCount(0,.04f)==3 && DecisionShop.RewardCount(0,.001f)==4,"reward count is two by default with rare third and fourth choices");
        Check(DecisionShop.RewardCount(.5f,.15f)>DecisionShop.RewardCount(0,.15f),"luck increases the chance of extra reward choices");
        var a=CityDistrictGeometry.Build(170,0);var b=CityDistrictGeometry.Build(171,0);
        Check(a.vertexCount!=b.vertexCount,"city seeds produce different building layouts");Destroy(a);Destroy(b);
        // Real shop purchase, affordability, close-range claim and bottom skip.
        foreach(string kind in new[]{"scoreShop","multiplierShop","reward"})
        {
            Score.ResetRun();Score.IncreaseScore(15000);
            typeof(Score).GetMethod("SetMultiplier",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{5});
            typeof(Score).GetField("scoreThisMult",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,0f);
            ship.maxHealth=100;ship.health=30;ship.transform.position=new Vector3(0,-3,-1);
            bool complete=false;var shop=flow.gameObject.AddComponent<DecisionShop>();
            typeof(WaveOrchestrer).GetField("decision",Private).SetValue(flow,shop);
            shop.Open(flow,ship,kind,()=>complete=true);
            float progress=Score.GetPercentageNextMultiplier();int mult=Score.CurrentMultiplier;
            yield return new WaitForSeconds(2.4f);
            Check(Score.CurrentMultiplier==mult && Mathf.Approximately(progress,Score.GetPercentageNextMultiplier()),kind+" freezes multiplier decay");
            var options=FindObjectsByType<AItem>().Where(x=>!x.Collected && !x.IsDismissing).ToArray();
            Check(options.Length==(kind=="reward" ? 2 : options.Length) && options.Length>=2 && options.Length<=4,kind+" has the expected option count");
            yield return Capture(kind);
            if(kind!="reward")
            {
                var kit=options.OfType<LargeHealthKit>().FirstOrDefault();Check(kit!=null,kind+" offers a large health kit at low health");
                ship.transform.position=kit.transform.position+Vector3.down*1.0f;
                yield return new WaitForSeconds(.2f);Check(!kit.IsAttracting,"passing one unit away does not claim "+kind+" item");
                int score=Score.CurrentScore;ship.transform.position=kit.transform.position+Vector3.down*.3f;
                yield return new WaitForSeconds(.4f);
                Check(complete && ship.health>30,kind+" purchase applies healing exactly once");
                Check(kind=="scoreShop" ? Score.CurrentScore==score-900 && Score.CurrentMultiplier==mult : Score.CurrentMultiplier==mult-1 && Score.CurrentScore==score,kind+" spends only its own currency");
            }
            else
            {
                ship.transform.position=new Vector3(0,Camera.DownLimit,-1);yield return new WaitForSeconds(1.8f);
                Check(complete,"holding at the bottom skips the decision");
            }
            yield return new WaitForSeconds(.6f);
        }
        Check(!CombatDecision.Active,"decision freeze releases after leaving the shop");
        foreach(var kind in new[]{"BlankBullet","Overdrive","GravityWell","MirrorBurst"})
        {
            var go=Instantiate(Resources.Load<GameObject>("Items/Item"),new Vector3(3,5,1),Quaternion.identity);
            var active=(AActiveItem)go.AddComponent(Type.GetType(kind));yield return null;active.OnPickup();
            float mana=active.OnPress(100);Check(mana>=0 && mana<100,kind+" spends energy on successful use");
            Check(active.OnPress(100)<0,kind+" respects its cooldown");Destroy(go);yield return null;
        }
        checks.Add("Batch covered 23 enemies/minibosses, 5 bosses, projectile lifecycles, nine weapon slots, upgrade interactions, both shop currencies and close pickup controls.");
    }
    void OnDestroy(){Application.logMessageReceived-=Log;}
}
#endif
