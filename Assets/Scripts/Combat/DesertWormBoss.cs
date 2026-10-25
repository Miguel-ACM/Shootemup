using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class DesertWormBoss : DistrictBoss
{
    public const int BodyCount=7;
    readonly DesertWormCore[] cores=new DesertWormCore[BodyCount+1];
    readonly Transform[] sections=new Transform[BodyCount];
    SandSpine spine;
    Vector3 previous;
    float routeWidth,routeHeight,routeCentre;
    bool completing;
    public int RemainingParts {get;private set;}=BodyCount+1;
    public DesertWormCore[] Cores=>cores;
    protected override void Configure(){hull=33;hullScale=1.10f;health=2700;attackInterval=.52f;nextAttack=3;}
    protected override void Start()
    {
        base.Start();GetComponent<Collider2D>().enabled=false;GetComponent<CombatHullVisual>().enabled=false;
        // Only the red reactors are damage targets, including homing, burn and chain weapons.
        Live.Remove(this);previous=transform.position;
        var camera=UnityEngine.Camera.main;
        routeWidth=Mathf.Max(.6f,camera.orthographicSize*405f/720f-1.5f);
        routeHeight=camera.orthographicSize*.37f;routeCentre=camera.transform.position.y+camera.orthographicSize*.28f;
        Hull.GetComponent<MeshRenderer>().sharedMaterial=IndustrialMeshFactory.WhiteWormMaterial;
        cores[0]=DesertWormCore.Create(this,Hull,true,health*670/2700,new Vector3(0,.05f,-.44f));
        spine=new SandSpine(transform.position,1.04f,BodyCount);
        for(int i=0;i<BodyCount;i++)
        {
            sections[i]=SandSpine.Model("White armored vertebra "+(i+1),transform,IndustrialMeshFactory.WhiteWormSection(),.91f-i*.018f);
            sections[i].GetComponent<MeshRenderer>().sharedMaterial=IndustrialMeshFactory.WhiteWormMaterial;
            sections[i].position=transform.position+Vector3.up*(i+1)*1.04f;
            cores[i+1]=DesertWormCore.Create(this,sections[i],false,health*290/2700,new Vector3(0,0,-.44f));
        }
    }
    protected override void MoveBoss(float dt)
    {
        float t=Mathf.Max(0,age-2)*.54f;
        Vector3 target=new Vector3(Mathf.Sin(t)*routeWidth,routeCentre+Mathf.Sin(t*2+.45f)*routeHeight,1);
        transform.position=Vector3.MoveTowards(transform.position,target,dt*(phaseTwo ? 5.9f : 4.5f));
        Vector2 tangent=transform.position-previous;previous=transform.position;
        if(tangent.sqrMagnitude>.00001f)Hull.rotation=Quaternion.FromToRotation(Vector3.down,tangent);
        spine?.Follow(transform.position,sections);
    }
    protected override bool PhaseTwoReady=>RemainingParts<=4;
    protected override void OnPhaseTwo(){nextAttack=age+1.1f;volley=0;}
    protected override void Attack()
    {
        int beat=(volley-1)%12;
        // Body ripple, pause, jaw burst, pause, crossed body fans. Dead sections are permanently silent.
        if(beat<4)
        {
            for(int i=1;i<cores.Length;i++)if((i-1)%4==beat && CanShoot(cores[i]))
            {
                Vector3 port=cores[i].transform.position;Vector2 aim=CombatVisuals.Aim(port);
                for(int j=-2;j<=2;j++)Fire<SimpleRound>(port,CombatVisuals.Rotate(aim,j*16),4.4f,.15f);
            }
        }
        else if((beat==5 || beat==6) && CanShoot(cores[0]))
        {
            Vector3 jaw=Hull.TransformPoint(new Vector3(0,-.92f,-.18f));
            Vector2 aim=CombatVisuals.Aim(jaw);int count=phaseTwo ? 7 : 5;
            for(int j=0;j<count;j++)Fire<NeedleRound>(jaw,CombatVisuals.Rotate(aim,(j-(count-1)*.5f)*8),6.7f,.16f);
        }
        else if(beat==9 || beat==10)
        {
            for(int i=1;i<cores.Length;i++)if(i%2==beat%2 && CanShoot(cores[i]))
            {
                Vector3 port=cores[i].transform.position;
                for(int j=0;j<3;j++)Fire<SimpleRound>(port,CombatVisuals.Rotate(Vector2.down,(j-1)*24+(beat==9 ? -10 : 10)),4,.17f);
            }
        }
        // When only a few reactors remain, each surviving part owns a readable answer on the shared rest.
        if(phaseTwo && beat==7)foreach(var core in cores)if(CanShoot(core))
        {
            Vector3 port=core.transform.position;Vector2 aim=CombatVisuals.Aim(port);
            for(int j=-1;j<=1;j++)Fire<NeedleRound>(port,CombatVisuals.Rotate(aim,j*10),6,.14f);
        }
    }
    bool CanShoot(DesertWormCore core)=>core!=null && core.health>0 && core.transform.position.y<Camera.UpLimit-.7f &&
        core.transform.position.y>Camera.DownLimit+1 && (player==null || Vector2.Distance(core.transform.position,player.position)>2.4f);
    public void PartDamaged()
    {
        if(completing)return;
        float sum=0;RemainingParts=0;
        foreach(var core in cores)if(core!=null && core.health>0){sum+=core.health;RemainingParts++;}
        if(RemainingParts==0)
        {
            completing=true;
            // Use the ordinary boss death path exactly once: score, drops, health-bar teardown and route completion.
            base.damage(health/Mathf.Max(.001f,GameRules.enemyDamageReceived)+1);
        }
        else health=sum;
    }
    public override void damage(float amount){} // Armor and the aggregate health object cannot bypass the reactors.
}

public sealed class DesertWormCore : AEnemy
{
    DesertWormBoss owner;
    MeshRenderer mesh;
    public static DesertWormCore Create(DesertWormBoss owner,Transform parent,bool head,float health,Vector3 local)
    {
        var t=new GameObject(head ? "Head weakpoint" : "Body weakpoint").transform;t.SetParent(parent,false);
        var visual=SandSpine.Model("Exposed red reactor",t,IndustrialMeshFactory.WhiteWormCore(head),1);
        t.localPosition=local;t.gameObject.layer=8;t.gameObject.tag="Enemy";
        var sprite=t.gameObject.AddComponent<SpriteRenderer>();sprite.enabled=false;
        var hit=t.gameObject.AddComponent<CircleCollider2D>();hit.isTrigger=true;hit.radius=head ? .51f : .35f;
        var part=t.gameObject.AddComponent<DesertWormCore>();part.owner=owner;part.health=health;
        part.mesh=visual.GetComponent<MeshRenderer>();part.mesh.sharedMaterial=IndustrialMeshFactory.WhiteWormMaterial;
        part.scoreOnKill=0;part.lifeDropChance=part.manaDropChance=part.moneyDropChance=0;return part;
    }
    public override void damage(float amount)
    {
        if(health<=0 || owner==null)return;
        base.damage(amount);
        if(owner!=null)owner.PartDamaged();
    }
    protected override void Die()
    {
        health=0;GetComponent<Collider2D>().enabled=false;
        PSXImpactEffects.Burst(transform.position,new Color(1,.25f,.08f),true,Vector2.up);
        var tint=new MaterialPropertyBlock();tint.SetColor("_Tint",new Color(.12f,.13f,.14f));mesh.SetPropertyBlock(tint);
        enabled=false; // Keep the dead, dark reactor attached to the articulated wreck.
    }
    protected override void OnBecameInvisible(){}
}
