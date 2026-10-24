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
    [Min(.1f)] public float shellTravelDistance=3.8f;
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
        enemy.health*=GameRules.enemyHealth*LevelRun.EnemyHealthScale;
        // Light wave ships should die during a clean pass with the stock gun.
        if(!(enemy is DistrictBoss)){enemy.health*=enemy.hullScale<.8f ? .84f : 1.35f;enemy.attackInterval*=.78f;}
        enemy.size=enemy.hullScale;go.GetComponent<CircleCollider2D>().radius=enemy.hullScale*.8f;
        var model=new GameObject("Armored hull");model.transform.SetParent(go.transform,false);model.transform.localScale=Vector3.one*enemy.hullScale;
        model.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.CombatHull(enemy.hull);
        var renderer=model.AddComponent<MeshRenderer>();renderer.sharedMaterial=enemy is FieldsBomberBoss ? CombatMaterials.Fortress : CombatMaterials.Fleet;renderer.sortingLayerName="Enemy";
        enemy.Hull=model.transform;go.AddComponent<CombatHullVisual>().model=model.transform;
        if(!(enemy is TunnelSawBoss))
        {
            var exhaust=go.AddComponent<EnemyThrusters>();exhaust.visualRoot=model.transform;
            exhaust.sockets=IndustrialMeshFactory.CombatEngineSockets(enemy.hull);
        }
        enemy.scoreOnKill=40;enemy.moneyDropChance=.8f;go.SetActive(true);return enemy;
    }
    protected override void Start(){base.Start();var p=GameObject.FindGameObjectWithTag("Player");player=p!=null ? p.transform : null;}
    public void FlyPhrase(WaveUnit unit)
    {
        if(unit.phrase==FlightPhrase.None)return;
        if(movement!=null){movement.enabled=false;Destroy(movement);}
        var flight=gameObject.AddComponent<FormationFlight>();flight.phrase=unit.phrase;flight.side=unit.side;flight.lane=unit.lane;
        flight.duration=unit.flightDuration;flight.fireEvery=unit.fireEvery;flight.curve=unit.curve;
        flight.syncFire=unit.syncFire;flight.firePhase=unit.firePhase;
        flight.fireTimes=unit.fireTimes;flight.formationDepth=unit.formationDepth;flight.fixedDepth=unit.fixedDepth;movement=flight;
        var p=flight.PositionAt(0);
        transform.position=new Vector3(p.x*Camera.RightLimit,Mathf.LerpUnclamped(Camera.DownLimit,Camera.UpLimit,p.y),transform.position.z);
    }
    public void Escort(Transform leader)
    {
        if(movement!=null){movement.enabled=false;Destroy(movement);}
        var escort=gameObject.AddComponent<EscortOrbit>();escort.SetAnchor(leader);movement=escort;
    }
    protected T Mover<T>() where T:PatternMover { var m=gameObject.AddComponent<T>();movement=m;return m; }
    protected virtual void Update()
    {
        age+=Time.deltaTime*LevelRun.EnemyTempoScale;
        if(age>28 || transform.position.y<Camera.DownLimit-2){Depart();return;}
        if(GameRules.playerDisabled || transform.position.y>Camera.UpLimit-.5f || transform.position.y<Camera.DownLimit+1)return;
        if(movement!=null && !movement.CanFire)return;
        if(player!=null && Vector2.Distance(player.position,transform.position)<2)return;
        if(movement is FormationFlight flight)
        {
            if(flight.TakeFireBeat()){volley++;Attack();}
        }
        else if(age>=nextAttack){volley++;Attack();nextAttack=age+attackInterval/Mathf.Max(.5f,GameRules.enemyShootSpeed);}
    }
    protected Vector2 Aim => movement!=null && movement.LockedAim.HasValue ? ((Vector2)(movement.LockedAim.Value-transform.position)).normalized : CombatVisuals.Aim(transform.position);
    protected T Fire<T>(Vector3 position,Vector2 direction,float velocity,float radius) where T:CombatRound
    {
        var shot=CombatRound.Launch<T>(position,direction,velocity,radius);
        if(shot is BurstShell shell)shell.SetTravelDistance(shellTravelDistance);
        return shot;
    }
    protected void Fan<T>(int count,float spread,float velocity,float radius=.14f) where T:CombatRound
    {
        for(int i=0;i<count;i++)Fire<T>(transform.position,CombatVisuals.Rotate(Aim,(i-(count-1)*.5f)*spread),velocity,radius);
    }
    protected void FanFrom<T>(int count,float spread,float velocity,float radius,params Vector3[] ports) where T:CombatRound
    {
        Vector2 aim=Aim;
        for(int i=0;i<count;i++)
        {
            int port=count==1 ? (volley+ports.Length-1)%ports.Length : Mathf.RoundToInt(i*(ports.Length-1f)/(count-1));
            Fire<T>(Hull.TransformPoint(ports[port]),CombatVisuals.Rotate(aim,(i-(count-1)*.5f)*spread),velocity,radius);
        }
    }
    protected override void OnBecameInvisible() { }
}
