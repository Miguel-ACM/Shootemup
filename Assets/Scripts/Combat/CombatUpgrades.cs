using UnityEngine;
using System.Collections.Generic;

public enum UpgradeKind { Splitter, Rebound, Proximity, Capacitor, Focus, Recovery, Salvage, Drone, Reserve, Return, Bearing, Burn, Battery, ChainReactor, QualityGlasses, BadGlasses, Gasoline, Poison, BouncingBall, FocusTime, DoubleTrouble }
public sealed class CombatUpgrades : MonoBehaviour
{
    public static CombatUpgrades Current {get;private set;}
    readonly Dictionary<UpgradeKind,int> stacks=new Dictionary<UpgradeKind,int>();
    readonly Dictionary<string,float> gates=new Dictionary<string,float>();
    Shipcontroller ship;
    Inputs inputs;
    float capacitor, battery, reserve, focusUntil;
    int volleys, focus, coins, blocks;
    AEnemy focused;
    public bool ReturnThisVolley {get;private set;}
    public float Charge=>capacitor;
    public float SalvageProgress=>coins/5f;
    public float Reserve=>reserve;
    void Awake(){gameObject.AddComponent<UpgradeMeters>();Current=this;ship=GetComponent<Shipcontroller>();inputs=new Inputs();inputs.Enable();}
    void OnDestroy(){if(Current==this)Current=null;inputs?.Disable();inputs?.Dispose();}
    public int Count(UpgradeKind kind)=>stacks.TryGetValue(kind,out int n) ? n : 0;
    public static float HostileSpeedAt(Vector3 position)
    {
        if(Current==null)return 1;
        float rate=Current.Count(UpgradeKind.QualityGlasses)>0 ? .9f : 1;
        if(Current.Count(UpgradeKind.BadGlasses)>0 && ((Vector2)(position-Current.transform.position)).sqrMagnitude<=9)rate*=.9f;
        return rate;
    }
    public void Add(UpgradeKind kind)
    {
        stacks[kind]=Count(kind)+1;
        if(kind==UpgradeKind.Recovery && ship.GetComponentsInChildren<BlockerOrbital>().Length==0 && FindAnyObjectByType<BlockerOrbital>()==null)ship.AddCompanion("AMoon");
        if(kind==UpgradeKind.Drone)gameObject.AddComponent<SupportDrone>();
    }
    void Update()
    {
        if(GameRules.playerDisabled)return;
        if(Count(UpgradeKind.Capacitor)>0 && !inputs.PlayerControls.FirePrimary.IsPressed() && !inputs.PlayerControls.FireSecondary.IsPressed())
            capacitor=Mathf.Min(1,capacitor+Time.deltaTime*.45f*(GameRules.playerShootSpeed/10));
    }
    bool Ready(string key,float cooldown)
    {
        if(gates.TryGetValue(key,out float until) && Time.time<until)return false;
        gates[key]=Time.time+cooldown;return true;
    }
    public float Volley()
    {
        volleys++;ReturnThisVolley=Count(UpgradeKind.Return)>0 && volleys%4==0;
        float multiplier=1+capacitor*2;capacitor=0;return multiplier;
    }
    public bool ProximityReady=>Count(UpgradeKind.Proximity)>0 && (!gates.TryGetValue("fuse",out float t)||Time.time>=t);
    public bool SpendProximity()=>Count(UpgradeKind.Proximity)>0 && Ready("fuse",.75f);
    public bool TryRebound()=>Count(UpgradeKind.Rebound)>0 && Ready("rebound",.18f);
    public float DirectHit(AEnemy enemy,float damage,int generation=0,bool companion=false,bool fire=false)
    {
        if(enemy==null || enemy.health<=0)return 0;
        if(companion)damage*=1+Count(UpgradeKind.Drone)*.2f;
        if(Count(UpgradeKind.Focus)>0 && generation==0)
        {
            if(focused!=enemy || Time.time>focusUntil){focus=0;focused=enemy;}
            if(Ready("focus",.08f))focus=Mathf.Min(10,focus+1);
            focusUntil=Time.time+1.1f;damage*=1+focus*.045f;
        }
        if(fire || Count(UpgradeKind.Gasoline)>0 || (Count(UpgradeKind.Burn)>0 && Ready("burn",companion ? .4f : .18f)))
        {
            var burn=enemy.GetComponent<BurningHull>();if(burn==null)burn=enemy.gameObject.AddComponent<BurningHull>();
            burn.Ignite(Mathf.Max(1,damage*.4f)*(companion ? .5f : 1));
        }
        if(Count(UpgradeKind.Poison)>0)
        {
            var poison=enemy.GetComponent<PoisonedHull>();if(poison==null)poison=enemy.gameObject.AddComponent<PoisonedHull>();
            poison.Infect(damage*(companion ? .5f : 1));
        }
        if(generation>0)return damage; // Secondary hits never produce more secondary effects or charge.
        if(Count(UpgradeKind.Splitter)>0 && Ready("split",.45f))Fragments(enemy.transform.position,damage,2,25);
        if(Count(UpgradeKind.Bearing)>0 && Ready("bearing",.65f))Fragments(enemy.transform.position,damage*.55f,3,60);
        if(Count(UpgradeKind.Battery)>0)
        {
            battery+=Mathf.Min(damage,enemy.health)*(companion ? .25f : .3f);
            if(battery>=35 && Ready("battery",.7f))
            {battery-=35;PlayerRound.Spawn(transform.position,Vector2.up,28,12,PlayerDelivery.Missile,1);}
        }
        return damage;
    }
    void Fragments(Vector3 p,float damage,int count,float angle)
    {
        for(int i=0;i<count;i++)PlayerRound.Spawn(p,CombatVisuals.Rotate(Vector2.up,(i-(count-1)*.5f)*angle),damage*.45f,11,PlayerDelivery.Pulse,1);
    }
    public void Coin()
    {
        if(Count(UpgradeKind.Salvage)==0)return;
        if(++coins>=5){coins-=5;PlayerRound.Spawn(transform.position,Vector2.up,12,10,PlayerDelivery.Missile,1);}
    }
    public void Block()
    {
        if(Count(UpgradeKind.Recovery)==0 || !Ready("block",.04f))return;
        if(++blocks>=8){blocks=0;PlayerRound.Spawn(transform.position,Vector2.up,14,12,PlayerDelivery.Missile,1);}
    }
    public void StoreOverflow(float amount){if(Count(UpgradeKind.Reserve)>0 && amount>0)reserve=Mathf.Min(12,reserve+amount*.5f);}
    public float SpendReserve(float amount=float.PositiveInfinity){float value=Mathf.Min(reserve,amount);reserve-=value;return value;}
    public void Killed(AEnemy enemy)
    {
        if(Count(UpgradeKind.ChainReactor)>0 && enemy.GetComponent<BurningHull>()!=null && Ready("chain",.3f))
            Fragments(enemy.transform.position,8,6,60);
    }
    public static AEnemy Nearest(Vector3 p,HashSet<AEnemy> excluded,float radius)
    {
        AEnemy result=null;float nearest=radius*radius;
        foreach(var enemy in AEnemy.Live)
        {
            if(enemy==null || !enemy.CanBeTargeted || (excluded!=null && excluded.Contains(enemy)))continue;
            float d=((Vector2)(enemy.transform.position-p)).sqrMagnitude;
            if(d<nearest){nearest=d;result=enemy;}
        }
        return result;
    }
}
