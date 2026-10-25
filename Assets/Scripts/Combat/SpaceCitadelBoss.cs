using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SpaceCitadelBoss : DistrictBoss
{
    readonly SpaceBossPart[] parts=new SpaceBossPart[5];
    readonly Transform[] shutters=new Transform[2];
    PulseLaser centralLaser;
    bool completing;
    float iris;
    public SpaceBossPart[] Parts=>parts;
    public bool IsDefeated=>completing;
    public bool CoreExposed=>parts[0]!=null && parts[1]!=null && parts[0].health<=0 && parts[1].health<=0;
    public override bool CanBeTargeted=>false;
    protected override void Configure(){hull=45;hullScale=1;health=2000;attackInterval=.62f;nextAttack=2.8f;}
    protected override void Start()
    {
        base.Start();GetComponent<Collider2D>().enabled=false;GetComponent<CombatHullVisual>().enabled=false;
        parts[0]=SpaceBossPart.Create(this,Hull,0,new Vector3(-1.95f,.25f,-.08f),health*.13f);
        parts[1]=SpaceBossPart.Create(this,Hull,0,new Vector3(1.95f,.25f,-.08f),health*.13f);
        parts[2]=SpaceBossPart.Create(this,Hull,1,new Vector3(-1.13f,-1.0f,-.12f),health*.11f);
        parts[3]=SpaceBossPart.Create(this,Hull,1,new Vector3(1.13f,-1.0f,-.12f),health*.11f);
        parts[4]=SpaceBossPart.Create(this,Hull,2,new Vector3(0,0,-.08f),health*.52f);
        for(int i=0;i<2;i++)
        {
            var go=new GameObject("Retracting reactor shutter "+i);go.transform.SetParent(Hull,false);
            go.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.SpaceModule(3);
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=SpaceMaterials.Fleet;renderer.sortingLayerName="Enemy";
            go.transform.localScale=new Vector3(i==0 ? 1 : -1,1,1);shutters[i]=go.transform;
        }
    }
    protected override bool PhaseTwoReady=>CoreExposed;
    protected override void MoveBoss(float dt)
    {
        var target=new Vector3(centralLaser!=null && centralLaser.isActiveAndEnabled ? 0 : Mathf.Sin(age*.43f)*.82f,Camera.UpLimit-3.0f+Mathf.Sin(age*.31f)*.32f,1);
        transform.position=Vector3.MoveTowards(transform.position,target,dt*3.2f);
        iris=Mathf.MoveTowards(iris,CoreExposed ? 1 : 0,dt*1.8f);
        for(int i=0;i<shutters.Length;i++)if(shutters[i]!=null)
            shutters[i].localPosition=new Vector3((i==0 ? -1 : 1)*iris*.72f,iris*.12f,0);
        if(parts[4]!=null)parts[4].SetExposed(CoreExposed);
    }
    protected override void Attack()
    {
        if(parts[0]==null)return;
        int beat=(volley-1)%12;
        if(beat==4)
        {
            centralLaser=CombatRound.Launch<PulseLaser>(Hull.TransformPoint(new Vector3(0,-1.48f,-.30f)),Vector2.down,0);
            if(centralLaser!=null)
            {centralLaser.Bind(Hull,new Vector3(0,-1.48f,-.30f));centralLaser.warning=1.2f;centralLaser.activeTime=1.05f;centralLaser.width=phaseTwo ? .42f : .34f;}
        }
        else if(beat==0 || beat==1 || beat==8)
        {
            bool fired=false;
            for(int i=2;i<4;i++)if(parts[i]!=null && parts[i].health>0)
            {
                int side=i==2 ? -1 : 1,count=phaseTwo ? 5 : 3;
                // Half-lane replies form nested mirrored arcs, rather than repeating identical rays.
                var axis=CombatVisuals.Rotate(Vector2.down,side*(48+(beat==1 ? 6 : beat==8 ? -6 : 0)));
                PetalFan<RicochetBolt>(parts[i].transform.position,axis,count,12,phaseTwo ? 4.7f : 4.1f,.17f,.16f);
                fired=true;
            }
            if(!fired)for(int s=-1;s<=1;s+=2)Fire<RicochetBolt>(Hull.TransformPoint(new Vector3(s*.4f,-.9f,-.4f)),CombatVisuals.Rotate(Vector2.down,s*56),4.5f,.17f);
        }
        else if(beat==10)
        {
            for(int i=2;i<4;i++)if(parts[i]!=null && parts[i].health>0)
            {
                var port=parts[i].transform.position;var aim=CombatVisuals.Aim(port);
                for(int s=-1;s<=1;s++)Fire<NeedleRound>(port,CombatVisuals.Rotate(aim,s*9),6.2f,.14f);
            }
            if(CoreExposed)Fan<SimpleRound>(5,15,4.8f,.15f);
        }
    }
    public void PartChanged()
    {
        if(completing)return;
        if(parts[4]!=null && parts[4].health<=0)
        {
            completing=true;base.damage(health/Mathf.Max(.001f,GameRules.enemyDamageReceived)+1);return;
        }
        float total=0;foreach(var part in parts)if(part!=null)total+=Mathf.Max(0,part.health);health=total;
        if(parts[4]!=null)parts[4].SetExposed(CoreExposed);
    }
    public override void damage(float amount){} // Hull hits cannot bypass generators or the core gate.
}

public sealed class SpaceBossPart : AEnemy
{
    SpaceCitadelBoss owner;
    bool exposed;
    int kind;
    MeshRenderer model;
    public bool IsExposed=>exposed;
    public override bool CanBeTargeted=>exposed && health>0 && owner!=null && !owner.IsDefeated;
    public static SpaceBossPart Create(SpaceCitadelBoss owner,Transform parent,int kind,Vector3 position,float health)
    {
        var go=new GameObject(kind==0 ? "Breakable shield generator" : kind==1 ? "Breakable ricochet turret" : "Protected reactor core");
        go.transform.SetParent(parent,false);go.transform.localPosition=position;go.layer=8;go.tag="Enemy";
        go.AddComponent<SpriteRenderer>().enabled=false;
        var collider=go.AddComponent<CircleCollider2D>();collider.isTrigger=true;collider.radius=kind==0 ? .48f : kind==1 ? .38f : .51f;
        var visual=new GameObject("Orbital module model");visual.transform.SetParent(go.transform,false);
        visual.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.SpaceModule(kind);
        var renderer=visual.AddComponent<MeshRenderer>();renderer.sharedMaterial=SpaceMaterials.Fleet;renderer.sortingLayerName="Enemy";
        var part=go.AddComponent<SpaceBossPart>();part.owner=owner;part.health=health;part.kind=kind;part.model=renderer;part.SetExposed(kind!=2);
        part.lifeDropChance=part.manaDropChance=part.moneyDropChance=0;part.scoreOnKill=0;return part;
    }
    public void SetExposed(bool value)
    {
        if(exposed==value && GetComponent<Collider2D>().enabled==value)return;
        exposed=value;GetComponent<Collider2D>().enabled=value && health>0;
        var tint=new MaterialPropertyBlock();tint.SetColor("_Tint",value ? Color.white : new Color(.24f,.48f,.70f));model.SetPropertyBlock(tint);
    }
    public override void damage(float amount)
    {
        if(!CanBeTargeted || owner==null)return;
        base.damage(amount);if(owner!=null)owner.PartChanged();
    }
    protected override void Die()
    {
        health=0;GetComponent<Collider2D>().enabled=false;
        if(kind!=2)
        {
            PSXPolygonDebris.Spawn(gameObject);PSXImpactEffects.Explosion(transform.position,.65f);
            model.enabled=false;Score.IncreaseScore(80);
        }
        enabled=false;
    }
    protected override void OnBecameInvisible(){}
}
