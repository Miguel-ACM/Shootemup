using System.Collections;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed partial class VoidSovereignBoss : DistrictBoss
{
    static Material bodyMaterial;
    public static Material Body=>bodyMaterial!=null?bodyMaterial:bodyMaterial=new Material(Resources.Load<Shader>("VoidBody"));
    public override int PhaseCount=>Difficulty.Easy?3:4;
    protected override bool NextPhaseReady=>HealthFraction<=PhaseEnd(CurrentPhase)+.000001f;
    protected override float PhaseBreakSeconds=>16.1f;
    protected override bool RepositionsOnPhase=>false;
    public bool Hidden {get;private set;}
    public float InterludeElapsed {get;private set;}
    public int Form=>Difficulty.Easy && CurrentPhase==3 ? 4 : CurrentPhase;
    MeshFilter mesh;
    VoidAnatomyVisual body;
    Collider2D contact;
    readonly LineRenderer[] edgeTell=new LineRenderer[4];
    protected override void Configure(){health=4300;hullScale=1.1f;attackInterval=.5f;nextAttack=1;}
    protected override void Start()
    {
        base.Start();mesh=Hull.GetComponent<MeshFilter>();contact=GetComponent<PolygonCollider2D>();
        Hull.GetComponent<MeshRenderer>().sharedMaterial=Body;
        body=Hull.gameObject.AddComponent<VoidAnatomyVisual>();body.Build(this);
        // The source has no engines. Every form has a coherent solid central target.
        GetComponent<CombatHullVisual>().enabled=false;
        for(int i=0;i<4;i++){edgeTell[i]=CombatVisuals.Line(transform,LevelProjectilePalette.Accent(LevelId.Void),.06f);edgeTell[i].positionCount=2;edgeTell[i].enabled=false;}
        SetForm();BuildRifts();
    }
    void SetForm()
    {
        body.SetForm(Form-1);
        Hull.localRotation=Quaternion.identity;
        for(int i=0;i<4;i++)
        {
            firingCue.PlacePort(i,LocalPort(i),i<(Form==4?4:2),false);
            firingCue.FollowPort(i,body.SetPort(i,LocalPort(i)));
        }
    }
    protected override void Update(){if(WorldTimeStop.Frozen)return;base.Update();}
    protected override void MoveBoss(float dt)
    {
        station=new Vector3(0,Camera.UpLimit-3.25f,1);
        if(PhaseTransitioning||teleporting)return;
        if(!openingComplete){base.MoveBoss(dt*2);return;}
        float x=Mathf.Sin(motionAge*(Form==2?.68f:.38f))*(Form==2?2.1f:1.35f);
        float y=Mathf.Sin(motionAge*.76f)*(Form==3?.9f:.45f);
        transform.position=Vector3.MoveTowards(transform.position,perch+new Vector3(x*.18f,y*.25f),dt*.7f);
    }
    public override void damage(float amount)
    {
        if(Hidden||teleporting)return;
        if(CurrentPhase<PhaseCount)amount=Mathf.Min(amount,Mathf.Max(0,health-initialHealth*PhaseEnd(CurrentPhase))/Mathf.Max(.0001f,GameRules.enemyDamageReceived));
        if(amount>0)base.damage(amount);
    }
    protected override void Attack(){if(openingComplete&&!Hidden)OpenCards();}
    protected override void OnPhaseEntered(int phase){CancelTeleport();StartCoroutine(Interlude(phase-1));}
    protected override void Die(){foreach(var line in edgeTell)if(line!=null)line.enabled=false;base.Die();}

    // Ports are the living eyes of the visible anatomy: mask eyes, scythe eyes, mandible eyes, then star-point eyes.
    Vector3 Port(int index)=>body.Port(index,LocalPort(index));
    Vector3 LocalPort(int index)
    {
        Vector3 local;
        if(Form==1)local=new Vector3(index%2==0?-.32f:.32f,-.18f,-.44f);
        else if(Form==2)local=new Vector3(index%2==0?-1.66f:1.66f,.40f,-.30f);
        else if(Form==3)local=new Vector3(index%2==0?-1.36f:1.36f,-.96f,-.34f);
        else local=index%4==0?new Vector3(-1.65f,0,-.30f):index%4==1?new Vector3(1.65f,0,-.30f):index%4==2?new Vector3(0,1.65f,-.30f):new Vector3(0,-1.65f,-.30f);
        return local;
    }
    IEnumerator Prime(float seconds)
    {
        for(float t=0;t<seconds;t+=Holding?0:Time.deltaTime*LevelRun.EnemyTempoScale)
        {for(int i=0;i<(Form==4?4:2);i++)Cue(Port(i),seconds-t);yield return null;}
    }
}
