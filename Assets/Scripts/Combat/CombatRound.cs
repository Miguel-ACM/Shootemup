using UnityEngine;
using System;
using System.Collections.Generic;

public abstract class CombatRound : AEnemyBullet
{
    public const int Limit=280;
    public static readonly HashSet<CombatRound> Live=new HashSet<CombatRound>();
    static readonly Dictionary<Type,Stack<CombatRound>> pools=new Dictionary<Type,Stack<CombatRound>>();
    protected Vector2 heading;
    protected Vector3 origin;
    protected float age;
    protected SpriteRenderer visual;
    protected Rigidbody2D body;
    protected float lifetime=9;
    protected bool ended;
    public int generation;
    public Vector2 Heading => heading;
    public static T Launch<T>(Vector3 p,Vector2 direction,float velocity=4,float radius=.18f) where T:CombatRound
    {
        if(Live.Count>=Limit) return null;
        T shot=null;
        if(pools.TryGetValue(typeof(T),out var pool))
            while(pool.Count>0 && shot==null)shot=pool.Pop() as T;
        if(shot==null)
        {
            var go=new GameObject(typeof(T).Name);go.SetActive(false);go.tag="EnemyBullet";
            go.layer=LayerMask.NameToLayer("Bullet");
            var rb=go.AddComponent<Rigidbody2D>();rb.gravityScale=0;rb.bodyType=RigidbodyType2D.Kinematic;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=CombatVisuals.Projectile();sr.sharedMaterial=CombatVisuals.ProjectileMaterial;sr.sortingOrder=15;
            sr.sortingLayerName="Bullet";
            var collider=go.AddComponent<CircleCollider2D>();collider.isTrigger=true;collider.radius=.44f;
            shot=go.AddComponent<T>();shot.body=rb;shot.visual=sr;
        }
        shot.transform.SetPositionAndRotation(new Vector3(p.x,p.y,-.5f),Quaternion.identity);
        shot.transform.localScale=Vector3.one*radius*2;shot.visual.sprite=CombatVisuals.Projectile(false,radius>=.25f);shot.visual.color=Color.white;
        shot.visual.enabled=true;shot.GetComponent<Collider2D>().enabled=true;
        shot.origin=shot.transform.position;shot.heading=direction.sqrMagnitude>.001f ? direction.normalized : Vector2.down;
        shot.speed=velocity*GameRules.enemyBulletSpeed*LevelRun.EnemyShotSpeedScale;shot.damage=10*GameRules.enemyDamage;
        shot.age=0;shot.generation=0;shot.lifetime=9;shot.ended=false;
        shot.gameObject.SetActive(true);Live.Add(shot);shot.Initialize();return shot;
    }
    protected override void Start() { }
    protected virtual void Initialize() { }
    protected virtual float ClockRate=>1;
    protected override void Update()
    {
        if(ended)return;
        float dt=Time.deltaTime*ClockRate;
        age+=dt;
        if(age>lifetime || transform.position.y<Camera.DownLimit-2 || transform.position.y>Camera.UpLimit+5 ||
            transform.position.x<Camera.LeftLimit-3 || transform.position.x>Camera.RightLimit+3){Release();return;}
        Tick(dt);
    }
    protected virtual void Tick(float dt) { transform.position+=(Vector3)heading*speed*dt;Orient(); }
    protected void Orient() { if(heading.sqrMagnitude>.001f)transform.rotation=Quaternion.FromToRotation(Vector3.up,heading); }
    protected void Release()
    {
        if(ended)return;ended=true;body.linearVelocity=Vector2.zero;gameObject.SetActive(false);
        if(!pools.TryGetValue(GetType(),out var pool))pools[GetType()]=pool=new Stack<CombatRound>();
        if(pool.Count<48)pool.Push(this);else Destroy(gameObject);
    }
    public override void Vanish() { if(!ended)PSXImpactEffects.Vanish(transform.position,visual.color,.25f);Release(); }
    protected override void OnBecameInvisible() { } // Lifetime and arena bounds also cover offscreen spawns.
    void OnDisable() { Live.Remove(this); }
    protected void Ring(int count,float velocity,float radius=.13f,float phase=0)
    {
        if(generation>0)return;
        // One broad escape sector, even when several shells burst together.
        for(int i=0;i<count;i++)
        {
            if(i==0 || i==1)continue;
            var s=Launch<SimpleRound>(transform.position,CombatVisuals.Rotate(Vector2.down,phase+i*360f/count),velocity,radius);
            if(s!=null)s.generation=1;
        }
    }
}
public sealed class SimpleRound : CombatRound { }
