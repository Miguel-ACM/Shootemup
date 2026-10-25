using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

public abstract class CombatRound : AEnemyBullet
{
    public const int Limit=280;
    public static readonly HashSet<CombatRound> Live=new HashSet<CombatRound>();
    static readonly Dictionary<Type,Stack<CombatRound>> pools=new Dictionary<Type,Stack<CombatRound>>();
    static int pooled;
    const int PoolBudget=768;
    public static int CreatedCount {get;private set;}
    protected Vector2 heading;
    protected Vector3 origin;
    protected float age;
    protected SpriteRenderer visual;
    protected Rigidbody2D body;
    protected Collider2D collision;
    ProjectileSpriteTint tint;
    Vector2 orientedHeading;
    bool needsOrientation;
    protected float lifetime=9;
    protected bool ended;
    public int generation;
    public Vector2 Heading => heading;
    public Color Accent=>HostileProjectileArt.Accent(GetType());
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPools(){pools.Clear();Live.Clear();pooled=CreatedCount=0;SceneManager.sceneUnloaded-=PrunePools;SceneManager.sceneUnloaded+=PrunePools;}
    static void PrunePools(Scene scene)
    {
        pooled=0;
        foreach(var pool in pools.Values)
        {
            var retained=pool.ToArray();pool.Clear();
            foreach(var shot in retained)if(shot!=null){pool.Push(shot);pooled++;}
        }
    }
    public static T Launch<T>(Vector3 p,Vector2 direction,float velocity=4,float radius=.18f) where T:CombatRound
    {
        if(Live.Count>=Limit) return null;
        T shot=null;
        if(pools.TryGetValue(typeof(T),out var pool))
            while(pool.Count>0 && shot==null){shot=pool.Pop() as T;pooled--;}
        if(shot==null)shot=Create<T>();
        shot.gameObject.layer=ProjectileCollisionLayers.Hostile;
        shot.tint.SetHostile();shot.needsOrientation=true;
        shot.transform.SetPositionAndRotation(new Vector3(p.x,p.y,-.5f),Quaternion.identity);
        shot.transform.localScale=Vector3.one*radius*2;shot.visual.sprite=HostileProjectileArt.Sprite(typeof(T),radius>=.25f);shot.visual.color=Color.white;
        shot.visual.enabled=true;shot.collision.enabled=true;
        shot.origin=shot.transform.position;shot.heading=direction.sqrMagnitude>.001f ? direction.normalized : Vector2.down;
        shot.speed=velocity*GameRules.enemyBulletSpeed*LevelRun.EnemyShotSpeedScale;shot.damage=10*GameRules.enemyDamage;
        shot.age=0;shot.generation=0;shot.lifetime=9;shot.ended=false;
        shot.enabled=true;shot.gameObject.SetActive(true);Live.Add(shot);shot.Initialize();return shot;
    }
    static T Create<T>() where T:CombatRound
    {
        var go=new GameObject(typeof(T).Name);go.SetActive(false);go.tag="EnemyBullet";
        go.layer=ProjectileCollisionLayers.Hostile;
        var rb=go.AddComponent<Rigidbody2D>();rb.gravityScale=0;rb.bodyType=RigidbodyType2D.Kinematic;
        var sr=go.AddComponent<SpriteRenderer>();sr.sprite=CombatVisuals.Projectile();sr.sharedMaterial=CombatVisuals.ProjectileMaterial;sr.sortingOrder=15;
        sr.sortingLayerName="Bullet";
        var collider=go.AddComponent<CustomCollider2D>();collider.isTrigger=true;
        sr.sprite=HostileProjectileArt.Sprite(typeof(T));ProjectileSprites.Fit(collider,sr.sprite);
        var shot=go.AddComponent<T>();shot.body=rb;shot.visual=sr;shot.collision=collider;
        ProjectileSpriteTint.Apply(go,shot.Accent);shot.tint=go.GetComponent<ProjectileSpriteTint>();
        CreatedCount++;return shot;
    }
    public static IEnumerator PrepareBursts()
    {
        // Reserve exactly the existing 768-object budget, across every delivery.
        // Previously only SimpleRound was ready: a first needle/ricochet volley
        // constructed its GameObjects, pixel colliders and special effects in combat.
        yield return Warm<SimpleRound>(280);
        yield return Warm<NeedleRound>(128);
        yield return Warm<RicochetBolt>(96);
        yield return Warm<BurstShell>(40);
        yield return Warm<SuspendedRound>(48);
        yield return Warm<GuidedMissile>(24);
        yield return Warm<ScatterCarrier>(24);
        yield return Warm<HelixRound>(32);
        yield return Warm<PlasmaOrb>(24);
        yield return Warm<ProximityMine>(16);
        yield return Warm<ReturningDisc>(24);
        yield return Warm<LaserSideRound>(16);
        yield return Warm<PulseLaser>(16);
    }
    static IEnumerator Warm<T>(int count) where T:CombatRound
    {
        if(!pools.TryGetValue(typeof(T),out var pool))pools[typeof(T)]=pool=new Stack<CombatRound>();
        while(pool.Count<count && pooled<PoolBudget)
        {
            for(int i=0;i<8 && pool.Count<count && pooled<PoolBudget;i++)
            {
                var shot=Create<T>();shot.heading=Vector2.down;shot.transform.localScale=Vector3.one*.3f;
                shot.Initialize();shot.ended=true;pool.Push(shot);pooled++;
            }
            yield return null;
        }
    }
    public static void ClearAll()
    {
        foreach(var shot in new List<CombatRound>(Live))if(shot!=null)shot.Release();
    }
    protected override void Start() { }
    protected virtual void Initialize() { }
    protected virtual float ClockRate=>1;
    protected override void Update()
    {
        if(ended || WorldTimeStop.Frozen || Time.deltaTime<=0)return;
        float dt=Time.deltaTime*ClockRate*(this is PulseLaser ? 1 : CombatUpgrades.HostileSpeedAt(transform.position));
        age+=dt;
        if(age>lifetime || transform.position.y<Camera.DownLimit-2 || transform.position.y>Camera.UpLimit+5 ||
            transform.position.x<Camera.LeftLimit-3 || transform.position.x>Camera.RightLimit+3){Release();return;}
        Tick(dt);
        if(!ended && !(this is PulseLaser) && PocketWind.Current!=null)transform.position+=PocketWind.Velocity(transform.position)*Time.deltaTime;
    }
    protected virtual void Tick(float dt) { transform.position+=(Vector3)heading*speed*dt;Orient(); }
    protected void Orient()
    {
        if(!needsOrientation && heading==orientedHeading && !(this is ReturningDisc))return;
        if(heading.sqrMagnitude>.001f){transform.rotation=Quaternion.FromToRotation(this is LaserSideRound ? Vector3.right : Vector3.up,heading);orientedHeading=heading;needsOrientation=false;}
    }
    protected void Release()
    {
        if(ended)return;ended=true;body.linearVelocity=Vector2.zero;gameObject.SetActive(false);
        if(!pools.TryGetValue(GetType(),out var pool))pools[GetType()]=pool=new Stack<CombatRound>();
        int standard=pools.TryGetValue(typeof(SimpleRound),out var simple) ? simple.Count : 0;
        bool room=this is SimpleRound ? pooled<PoolBudget : pooled-standard<PoolBudget-Limit;
        if(pool.Count<Limit && room){pool.Push(this);pooled++;}else Destroy(gameObject);
    }
    public override void Vanish() { if(!ended)PSXImpactEffects.Vanish(transform.position,Accent,.25f);Release(); }
    protected override void OnBecameInvisible() { } // Lifetime and arena bounds also cover offscreen spawns.
    void OnDisable() { Live.Remove(this); }
    protected bool Ring(int count,float velocity,float radius=.13f,float phase=0)
    {
        if(generation>0 || count<1 || Live.Count+count>Limit)return false;
        // Commit the entire ring together. A charged shell waits for capacity
        // instead of losing whichever angles happened to be emitted last.
        for(int i=0;i<count;i++)
        {
            var s=Launch<SimpleRound>(transform.position,CombatVisuals.Rotate(Vector2.down,phase+i*360f/count),velocity,radius);
            if(s!=null)s.generation=1;
        }
        return true;
    }
}
public sealed class SimpleRound : CombatRound { }
