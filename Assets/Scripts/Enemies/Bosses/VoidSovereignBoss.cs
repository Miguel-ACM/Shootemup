using System.Collections;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed partial class VoidSovereignBoss : DistrictBoss
{
    static Material bodyMaterial;
    public static Material Body=>bodyMaterial!=null?bodyMaterial:bodyMaterial=new Material(Resources.Load<Shader>("VoidBody"));
    public override int PhaseCount => 6;
    protected override float PhaseEnd(int phase) => phase >= 6 ? 0 : (7-phase)/7f;
    bool LongBreak => CurrentPhase == 4 || CurrentPhase == 6;
    readonly VoidEchoVisual[] echoes = new VoidEchoVisual[3];
    VoidEchoVisual Memory => CurrentPhase <= 3 ? echoes[CurrentPhase-1] : null;
    public bool EchoTarget(MeshFilter filter)
    {
        foreach (var echo in echoes)
            if (echo != null && filter.transform.IsChildOf(echo.root))
                return filter.transform == echo.Target;
        return true;
    }
    protected override bool NextPhaseReady=>HealthFraction<=PhaseEnd(CurrentPhase)+.000001f;
    protected override float PhaseBreakSeconds=>LongBreak ? 16.1f : 1.5f;
    protected override void PlayPhaseBreak() => PhaseBreak.Play(transform.position, local: true);
    protected override bool RepositionsOnPhase=>false;
    public bool Hidden {get;private set;}
    public float InterludeElapsed {get;private set;}
    public int Form => CurrentPhase <= 3 ? 0 : CurrentPhase - 2;
    VoidAnatomyVisual body;
    Collider2D contact;
    readonly LineRenderer[] edgeTell=new LineRenderer[4];
    protected override void Configure(){health=7525;hullScale=1.1f;attackInterval=.5f;nextAttack=1;}
    protected override void Start()
    {
        Shader.SetGlobalColor("_VoidRouteLight", LevelProjectilePalette.Accent(LevelId.Void));
        base.Start();contact=GetComponent<PolygonCollider2D>();
        Hull.GetComponent<MeshRenderer>().sharedMaterial=Body;
        body=Hull.gameObject.AddComponent<VoidAnatomyVisual>();body.Build(this);
        // The source has no engines. Every form has a coherent solid central target.
        GetComponent<CombatHullVisual>().enabled=false;
        for(int i=0;i<4;i++){edgeTell[i]=CombatVisuals.Line(transform,LevelProjectilePalette.Accent(LevelId.Void),.06f);edgeTell[i].positionCount=2;edgeTell[i].enabled=false;}
        for (int i=0;i<echoes.Length;i++) echoes[i]=new VoidEchoVisual(LevelRun.VoidMemory(i),Hull);
        SetForm();BuildRifts();
    }
    void SetForm()
    {
        holdMemory = submergedMemory = false;
        var echo = Memory;
        foreach (var memory in echoes) memory.root.gameObject.SetActive(memory == echo);
        Hull.GetComponent<MeshRenderer>().enabled = echo == null;
        body.enabled = echo == null;
        body.SetForm(echo == null ? (Form==4 ? 4+LevelProjectilePalette.VoidRoute : Form-1) : -1);
        if (echo != null)
        {
            memoryAge = 0;
            holdMemory = submergedMemory = false;
            sawTravel = Vector2.zero;
            echo.Pose(0);
            for (int i=0;i<4;i++) firingCue.FollowPort(i,echo.Port(i));
            return;
        }
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
        if(PhaseTransitioning || changingForm || teleporting)return;
        if(!openingComplete){base.MoveBoss(dt*2);return;}
        if (Memory != null)
        {
            MoveMemory(dt);
            Memory.Pose(motionAge);
            return;
        }
        float beat = motionAge - flightStart;
        // The scythes glide in a figure eight. The heavy jaw rises, hangs, then drops;
        // the finale circles its perch as its four apertures take turns.
        Vector2 offset;
        float bank;
        if (Form == 2)
        {
            offset = new Vector2(1.12f * Mathf.Sin(beat * 1.12f), .38f * Mathf.Sin(beat * 2.24f));
            bank = -12 * Mathf.Cos(beat * 1.12f);
        }
        else if (Form == 3)
        {
            float drop = Mathf.Pow(.5f - .5f * Mathf.Cos(beat * 1.7f), 3);
            offset = new Vector2(.68f * Mathf.Sin(beat * .92f), .28f - .90f * drop);
            bank = 6 * Mathf.Sin(beat * .92f);
        }
        else
        {
            offset = new Vector2(.84f * Mathf.Sin(beat * .97f), .54f * Mathf.Cos(beat * .97f));
            bank = 9 * Mathf.Sin(beat * .97f);
        }
        float ease = Mathf.SmoothStep(0,1,beat / .45f);
        var target = perch + (Vector3)(offset * ease);
        target.x = Mathf.Clamp(target.x,Camera.LeftLimit+1.95f,Camera.RightLimit-1.95f);
        target.y = Mathf.Clamp(target.y,Camera.DownLimit+2.7f,Camera.UpLimit-2.05f);
        transform.position = Vector3.MoveTowards(transform.position,target,dt*3.8f);
        Hull.localRotation = Quaternion.Euler(0,bank*ease*.75f,bank*ease);
    }
    public override void damage(float amount)
    {
        if(Hidden||changingForm||teleporting||submergedMemory)return;
        if(CurrentPhase<PhaseCount)amount=Mathf.Min(amount,Mathf.Max(0,health-initialHealth*PhaseEnd(CurrentPhase))/Mathf.Max(.0001f,GameRules.enemyDamageReceived));
        if(amount>0)base.damage(amount);
    }
    protected override void Attack(){if(openingComplete && !Hidden && !changingForm) OpenCards();}
    protected override void OnPhaseEntered(int phase)
    {
        if (teleporting) CancelTeleport();
        changingForm = true;
        contact.enabled = false;
        StartCoroutine(LongBreak ? Interlude(phase == 4 ? 1 : 2) : ChangeForm());
    }
    protected override void Die(){foreach(var line in edgeTell)if(line!=null)line.enabled=false;base.Die();}

    // Ports are the living eyes of the visible anatomy: mask eyes, scythe eyes, mandible eyes, then star-point eyes.
    Vector3 Port(int index)=>body.Port(index,LocalPort(index));
    Vector3 LocalPort(int index)
    {
        Vector3 local;
        if(Form==1)local=new Vector3(index%2==0?-.32f:.32f,-.18f,-.44f);
        else if(Form==2)local=new Vector3(index%2==0?-1.66f:1.66f,.40f,-.30f);
        else if(Form==3)local=new Vector3(index%2==0?-1.36f:1.36f,-.96f,-.34f);
        else local=IndustrialMeshFactory.VoidFinalAperture(LevelProjectilePalette.VoidRoute,index);
        return local;
    }
    IEnumerator Prime(float seconds)
    {
        for(float t=0;t<seconds;t+=Holding?0:Time.deltaTime*LevelRun.EnemyTempoScale)
        {for(int i=0;i<(Form==4?4:2);i++)Cue(Port(i),seconds-t);yield return null;}
    }
}
