using UnityEngine;
using System;

public abstract class DistrictEnemy : AEnemy
{
    protected Transform player;
    protected PatternMover movement;
    protected float age, nextAttack=1.2f;
    protected int volley;
    public float hullScale=.7f;
    public int hull;
    protected float attackInterval=2;
    public Transform Hull {get;private set;}
    protected abstract void Configure();
    protected abstract void Attack();
    public static DistrictEnemy Spawn(Type type,Vector3 p)
    {
        var go=new GameObject(type.Name);go.SetActive(false);go.transform.position=p;go.tag="Enemy";go.layer=8;
        var sr=go.AddComponent<SpriteRenderer>();sr.sprite=CombatVisuals.Disc;sr.enabled=false;sr.sortingLayerName="Enemy";
        var rb=go.AddComponent<Rigidbody2D>();rb.gravityScale=0;rb.bodyType=RigidbodyType2D.Kinematic;
        go.AddComponent<CircleCollider2D>().isTrigger=true;
        var enemy=(DistrictEnemy)go.AddComponent(type);enemy.Configure();
        enemy.health*=GameRules.enemyHealth*(1+Mathf.Min(8,LevelRun.Stage-1)*.15f);
        enemy.size=enemy.hullScale;go.GetComponent<CircleCollider2D>().radius=enemy.hullScale*.8f;
        var model=new GameObject("Armored hull");model.transform.SetParent(go.transform,false);model.transform.localScale=Vector3.one*enemy.hullScale;
        model.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.CombatHull(enemy.hull);
        var renderer=model.AddComponent<MeshRenderer>();renderer.sharedMaterial=Resources.Load<Material>("CombatFleet");renderer.sortingLayerName="Enemy";
        enemy.Hull=model.transform;go.AddComponent<CombatHullVisual>().model=model.transform;
        var exhaust=go.AddComponent<EnemyThrusters>();exhaust.visualRoot=model.transform;
        float socket=enemy.hull==22 ? .375f : enemy.hull==2 || enemy.hull==5 || enemy.hull==7 || enemy.hull==8 || enemy.hull==14 || enemy.hull>=20 ? .874f : .562f;
        exhaust.sockets=new[]{new Vector3(-socket,.93f,0),new Vector3(socket,.93f,0)};
        enemy.scoreOnKill=40;enemy.moneyDropChance=.8f;go.SetActive(true);return enemy;
    }
    protected override void Start(){base.Start();var p=GameObject.FindGameObjectWithTag("Player");player=p!=null ? p.transform : null;}
    public void Escort(Transform leader)
    {
        if(movement!=null){movement.enabled=false;Destroy(movement);}
        var escort=gameObject.AddComponent<EscortOrbit>();escort.SetAnchor(leader);movement=escort;
    }
    protected T Mover<T>() where T:PatternMover { var m=gameObject.AddComponent<T>();movement=m;return m; }
    protected virtual void Update()
    {
        age+=Time.deltaTime;
        if(age>28 || transform.position.y<Camera.DownLimit-2){Depart();return;}
        if(GameRules.playerDisabled || transform.position.y>Camera.UpLimit-.5f || transform.position.y<Camera.DownLimit+1)return;
        if(movement!=null && !movement.CanFire)return;
        if(player!=null && Vector2.Distance(player.position,transform.position)<2)return;
        if(age>=nextAttack){volley++;Attack();nextAttack=age+attackInterval/Mathf.Max(.5f,GameRules.enemyShootSpeed);}
    }
    protected Vector2 Aim => CombatVisuals.Aim(transform.position);
    protected void Fan<T>(int count,float spread,float velocity,float radius=.14f) where T:CombatRound
    {
        for(int i=0;i<count;i++)CombatRound.Launch<T>(transform.position,CombatVisuals.Rotate(Aim,(i-(count-1)*.5f)*spread),velocity,radius);
    }
    protected override void OnBecameInvisible() { }
}
