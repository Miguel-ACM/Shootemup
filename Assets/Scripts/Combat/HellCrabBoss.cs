using UnityEngine;
using System.Collections.Generic;

[UnityEngine.Scripting.Preserve]
public sealed class HellCrabBoss : DistrictBoss
{
    readonly Transform[] claws=new Transform[2];
    const int ArmSections=4;
    readonly Transform[,] arms=new Transform[2,ArmSections];
    readonly LineRenderer[] tells=new LineRenderer[2];
    readonly HellCrabEye[] eyes=new HellCrabEye[2];
    readonly List<DistrictEnemy> minions=new List<DistrictEnemy>();
    float clawClock,lockedY;
    bool completing;
    [EnemyTunable("Claws / Cycle seconds",5,12)] public float clawPeriod=7.4f;
    [EnemyTunable("Claws / Fan count",3,9)] public int clawShots=7;
    public HellCrabEye[] Eyes=>eyes;
    public override bool CanBeTargeted=>false;
    protected override void Configure(){hull=49;hullScale=1;health=3000;attackInterval=.38f;nextAttack=2.2f;}
    protected override void Start()
    {
        base.Start();GetComponent<Collider2D>().enabled=false;GetComponent<CombatHullVisual>().enabled=false;
        for(int i=0;i<2;i++)
        {
            int side=i==0 ? -1 : 1;
            eyes[i]=HellCrabEye.Create(this,Hull,new Vector3(side*.67f,-.52f,-.96f),health*.5f);
            claws[i]=Model("Jointed side pincer",IndustrialMeshFactory.CombatHull(54),Hull);
            claws[i].localScale=Vector3.one*1.75f;
            for(int segment=0;segment<ArmSections;segment++)arms[i,segment]=Model("Articulated pincer vertebra "+segment,IndustrialMeshFactory.CrabArm(),Hull);
            tells[i]=CombatVisuals.Line(Hull,LevelProjectilePalette.Accent(LevelId.Hell),.045f);tells[i].enabled=false;
        }
    }
    static Transform Model(string name,Mesh mesh,Transform parent)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=CombatMaterials.Fleet;renderer.sortingLayerName="Enemy";return go.transform;
    }
    protected override bool PhaseTwoReady=>eyes[0]!=null && (eyes[0].health<=0 || eyes[1].health<=0);
    protected override void MoveBoss(float dt)
    {
        var target=new Vector3(Mathf.Sin(motionAge*.58f)*.70f,Camera.UpLimit-2.55f,1);
        transform.position=Vector3.MoveTowards(transform.position,target,dt*3.4f);
        if(claws[0]==null)return;
        float before=clawClock;clawClock+=dt*(phaseTwo ? 1.18f : 1);
        float t=Mathf.Repeat(clawClock,clawPeriod);
        float shoulderY=transform.position.y;
        if(before==0 || t<Mathf.Repeat(before,clawPeriod))lockedY=Mathf.Clamp(player!=null ? player.position.y : shoulderY-2,shoulderY-2.7f,shoulderY-.8f);
        lockedY=Mathf.Clamp(lockedY,shoulderY-2.7f,shoulderY-.8f);
        float reach=Mathf.Min(Camera.RightLimit-1.5f,2.9f),sweep=0,y=shoulderY-.75f;
        if(t<1.1f)y=Mathf.Lerp(y,y+.45f,Mathf.Sin(t/1.1f*Mathf.PI));
        else if(t<2.4f)y=Mathf.Lerp(y,lockedY,Mathf.SmoothStep(0,1,(t-1.1f)/1.3f));
        else if(t<3.4f){y=lockedY;sweep=Mathf.Sin((t-2.4f)*Mathf.PI)*.32f;}
        else if(t<4.6f)y=Mathf.Lerp(lockedY,y,Mathf.SmoothStep(0,1,(t-3.4f)/1.2f));
        for(int i=0;i<2;i++)
        {
            int side=i==0 ? -1 : 1;var p=new Vector3(side*reach*(1-sweep),y,0);
            claws[i].position=p;claws[i].rotation=Quaternion.Euler(0,0,-side*(12+sweep*50));
            var shoulder=Hull.TransformPoint(new Vector3(side*1.55f,.25f,0));
            var elbow=(shoulder+p)*.5f+new Vector3(side*.45f,.25f,.15f);
            Vector3 Point(float u)=>Vector3.Lerp(Vector3.Lerp(shoulder,elbow,u),Vector3.Lerp(elbow,p,u),u);
            for(int segment=0;segment<ArmSections;segment++)
            {
                var a=Point(segment/(float)ArmSections);var b=Point((segment+1)/(float)ArmSections);var bone=arms[i,segment];
                bone.position=(a+b)*.5f+Vector3.forward*.12f;bone.rotation=Quaternion.FromToRotation(Vector3.up,b-a);
                bone.localScale=new Vector3(1.05f-segment*.025f,Mathf.Max(.42f,Vector3.Distance(a,b)/.95f),1);
            }
            tells[i].enabled=t<1.1f;
            if(tells[i].enabled){tells[i].SetPosition(0,p);tells[i].SetPosition(1,new Vector3(side*reach,lockedY,-.3f));}
            if(t>1.1f && t<4.6f && player!=null && Vector2.Distance(player.position,claws[i].TransformPoint(new Vector3(0,-.45f,0)))<1.25f)
                player.SendMessage("ReceiveHazardDamage",18f,SendMessageOptions.DontRequireReceiver);
        }
        if(player!=null && Vector2.Distance(player.position,transform.position)<1.55f)
            player.SendMessage("ReceiveHazardDamage",18f,SendMessageOptions.DontRequireReceiver);
    }
    protected override void Attack()
    {
        if(claws[0]==null)return;
        int beat=(volley-1)%12;
        if(beat<3 || beat==6 || beat==7)
        {
            for(int i=0;i<2;i++)
            {
                var port=claws[i].TransformPoint(new Vector3(0,-.32f,-.4f));var axis=CombatVisuals.Aim(port);
                if(player!=null && Vector2.Distance(port,player.position)<1.8f)continue;
                PetalFan<SimpleRound>(port,CombatVisuals.Rotate(axis,(beat%3-1)*7),clawShots,12,phaseTwo ? 4.6f : 4.1f,.15f,.15f);
            }
        }
        else if(beat==4 || beat==9)
        {
            for(int i=0;i<2;i++)if(eyes[i].health>0)PetalFan<NeedleRound>(eyes[i].transform.position,Aim,5,10,5.8f,.13f);
        }
        else if(beat==11)
        {
            minions.RemoveAll(e=>e==null);
            if(minions.Count<4)for(int side=-1;side<=1;side+=2)
                minions.Add(Spawn(typeof(SkullRevenant),new Vector3(side*2.4f,Camera.UpLimit+1.5f,1)));
        }
    }
    public void EyeChanged()
    {
        if(completing)return;
        health=Mathf.Max(0,eyes[0].health)+Mathf.Max(0,eyes[1].health);
        if(health<=0){completing=true;health=1;base.damage(2/Mathf.Max(.001f,GameRules.enemyDamageReceived));}
    }
    public override void damage(float amount) { }
}

public sealed class HellCrabEye : AEnemy
{
    HellCrabBoss owner;
    MeshRenderer model;
    public static HellCrabEye Create(HellCrabBoss owner,Transform parent,Vector3 position,float health)
    {
        var go=new GameObject("Breakable amber eye");go.transform.SetParent(parent,false);go.transform.localPosition=position;go.tag="Enemy";go.layer=8;
        go.AddComponent<SpriteRenderer>().enabled=false;var hit=go.AddComponent<CircleCollider2D>();hit.isTrigger=true;hit.radius=.34f;
        var visual=new GameObject("Eye lens");visual.transform.SetParent(go.transform,false);visual.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.CombatHull(55);
        var renderer=visual.AddComponent<MeshRenderer>();renderer.sharedMaterial=CombatMaterials.Fleet;renderer.sortingLayerName="Enemy";
        PartHitFeedback.Bind(visual.transform);var part=go.AddComponent<HellCrabEye>();part.health=health;part.owner=owner;part.model=renderer;
        part.lifeDropChance=part.manaDropChance=part.moneyDropChance=0;return part;
    }
    public override void damage(float amount){if(health<=0 || owner==null)return;base.damage(amount);owner.EyeChanged();}
    protected override void Die()
    {
        health=0;GetComponent<Collider2D>().enabled=false;PSXImpactEffects.Explosion(transform.position,.5f);
        var ink=new MaterialPropertyBlock();ink.SetColor("_Tint",new Color(.16f,.12f,.10f));model.SetPropertyBlock(ink);enabled=false;
    }
    protected override void OnBecameInvisible() { }
}
