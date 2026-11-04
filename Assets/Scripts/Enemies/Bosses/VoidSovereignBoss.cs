using System.Collections;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed partial class VoidSovereignBoss : DistrictBoss
{
    static Material bodyMaterial;
    public static Material Body=>bodyMaterial!=null?bodyMaterial:bodyMaterial=new Material(Resources.Load<Shader>("VoidBody"));
    public override int PhaseCount=>Difficulty.Easy?3:4;
    protected override float PhaseEnd(int phase)=>Mathf.Max(0,(PhaseCount-phase)/(float)PhaseCount);
    protected override bool NextPhaseReady=>HealthFraction<=PhaseEnd(CurrentPhase)+.000001f;
    protected override float PhaseBreakSeconds=>16.1f;
    protected override bool RepositionsOnPhase=>false;
    public bool Hidden {get;private set;}
    public float InterludeElapsed {get;private set;}
    public int Form=>Difficulty.Easy && CurrentPhase==3 ? 4 : CurrentPhase;
    MeshFilter mesh;
    CircleCollider2D contact;
    readonly LineRenderer[] edgeTell=new LineRenderer[4];
    protected override void Configure(){health=3900;hullScale=1.1f;attackInterval=.5f;nextAttack=1;}
    protected override void Start()
    {
        base.Start();mesh=Hull.GetComponent<MeshFilter>();contact=GetComponent<CircleCollider2D>();
        Hull.GetComponent<MeshRenderer>().sharedMaterial=Body;
        // The source has no engines. Every form has a coherent solid central target.
        GetComponent<CombatHullVisual>().enabled=false;
        for(int i=0;i<4;i++){edgeTell[i]=CombatVisuals.Line(transform,LevelProjectilePalette.Accent(LevelId.Void),.06f);edgeTell[i].positionCount=2;edgeTell[i].enabled=false;}
        SetForm();
    }
    void SetForm()
    {
        mesh.sharedMesh=IndustrialMeshFactory.VoidForm(Form-1);
        contact.radius=Form==1?.68f:Form==2?.88f:Form==3?1.0f:.74f;
        Hull.localRotation=Quaternion.identity;
    }
    protected override void Update(){if(WorldTimeStop.Frozen)return;base.Update();}
    protected override void MoveBoss(float dt)
    {
        station=new Vector3(0,Camera.UpLimit-3.25f,1);
        if(PhaseTransitioning)return;
        if(!openingComplete){base.MoveBoss(dt*2);return;}
        float x=Mathf.Sin(motionAge*(Form==2?.68f:.38f))*(Form==2?2.1f:1.35f);
        float y=Mathf.Sin(motionAge*.76f)*(Form==3?.9f:.45f);
        transform.position=Vector3.MoveTowards(transform.position,station+new Vector3(x,y),dt*(Form==2?2.5f:1.4f));
        Hull.localRotation=Quaternion.Euler(5*Mathf.Sin(motionAge),9*Mathf.Sin(motionAge*.7f),Form==4?12*Mathf.Sin(motionAge*.5f):5*Mathf.Sin(motionAge*.4f));
    }
    public override void damage(float amount)
    {
        if(CurrentPhase<PhaseCount)amount=Mathf.Min(amount,Mathf.Max(0,health-initialHealth*PhaseEnd(CurrentPhase)));
        if(amount>0)base.damage(amount);
    }
    protected override void Attack(){if(openingComplete&&!Hidden)OpenCards();}
    protected override void OnPhaseEntered(int phase){StartCoroutine(Interlude(phase-1));}
    protected override void Die(){foreach(var line in edgeTell)if(line!=null)line.enabled=false;base.Die();}

    // Ports move with the currently visible anatomy: eyes, mantis blades, jaw lips, then star points.
    Vector3 Port(int index)
    {
        Vector3 local;
        if(Form==1)local=new Vector3(index%2==0?-.32f:.32f,-.18f,-.44f);
        else if(Form==2)local=new Vector3(index%2==0?-1.65f:1.65f,-.46f,-.22f);
        else if(Form==3)local=new Vector3(index%2==0?-.75f:.75f,-.85f,-.32f);
        else local=index%4==0?new Vector3(-1.65f,0,-.22f):index%4==1?new Vector3(1.65f,0,-.22f):index%4==2?new Vector3(0,1.65f,-.22f):new Vector3(0,-1.65f,-.22f);
        return Hull.TransformPoint(local);
    }
    IEnumerator Prime(float seconds)
    {
        for(float t=0;t<seconds;t+=Holding?0:Time.deltaTime*LevelRun.EnemyTempoScale)
        {for(int i=0;i<(Form==4?4:2);i++)Cue(Port(i),seconds-t);yield return null;}
    }
    protected override IEnumerator Cards()
    {
        while(true)
        {
            yield return Prime(.7f);
            if(Form==1)
            {
                // Unblinking: stacked diamonds separate by speed and then bend into falling shards.
                yield return Every(6.6f,.34f,n=>
                {
                    int count=Dense(9);
                    for(int s=-1;s<=1;s+=2)for(int i=0;i<count;i++)
                        Fire<NeedleRound>(Port(s<0?0:1),CombatVisuals.Rotate(Vector2.down,s*(8+i*9+n%3*4)),1.5f+i*.12f,.18f).Fall(new Vector2(-s*.13f,-.25f)).FadeAt(9);
                });
                yield return Wait(Rest(1));yield return Prime(.65f);
                // Tear the ceiling: slow tears split into forward cones; mirrored arrows rebound behind the face.
                yield return Every(6.0f,.58f,n=>
                {
                    for(int s=-1;s<=1;s+=2)
                    {
                        Fire<SimpleRound>(Port(s<0?0:1),CombatVisuals.Rotate(Vector2.down,s*(18+n%3*16)),1.45f,.36f).SplitAt(1.4f,Dense(7),55,2.4f);
                        for(int i=0;i<Dense(6);i++)Fire<NeedleRound>(Port(s<0?0:1),CombatVisuals.Rotate(Vector2.up,s*(12+i*13)),2.6f,.17f).Bounce(1);
                    }
                });
            }
            else if(Form==2)
            {
                // Scissor scripture: the two blade tips cut interleaving ribbons across one another.
                yield return Every(7.2f,.20f,n=>
                {
                    for(int s=-1;s<=1;s+=2)for(int i=0;i<Dense(5);i++)
                        Fire<NeedleRound>(Port(s<0?0:1),CombatVisuals.Rotate(Vector2.down,-s*(32+Mathf.Sin(n*.28f)*32+i*5)),2.3f+i*.22f,.17f).Sway(.12f,.45f);
                });
                yield return Wait(Rest(1.1f));yield return Prime(.65f);
                // Crossed-out lines: fast darts overtake slow sheets, which turn ninety degrees after stopping.
                yield return Every(6.2f,.64f,n=>
                {
                    for(int s=-1;s<=1;s+=2)for(int i=0;i<Dense(10);i++)
                    {
                        var a=CombatVisuals.Rotate(Vector2.down,s*(i*10-35));
                        Fire<NeedleRound>(Port(s<0?0:1),a,2.0f,.18f).Pause(1.0f,.4f,CombatRound.Turn.Rotate,s*75,1.35f).FadeAt(8);
                        if(i%3==0)Fire<SimpleRound>(Port(s<0?0:1),a,3.8f,.17f).FadeAt(5);
                    }
                });
            }
            else if(Form==3)
            {
                // Hunger: jagged pressure fronts leave the jaw at alternating speeds.
                yield return Every(7,.42f,n=>
                {
                    int count=Dense(13);
                    for(int s=-1;s<=1;s+=2)for(int i=0;i<count;i++)
                        Fire<NeedleRound>(Port(s<0?0:1),CombatVisuals.Rotate(Vector2.down,s*(i*7-30+n%2*4)),n%2==0?1.6f:3.2f,.18f).Zigzag(.9f,12).FadeAt(8);
                });
                yield return Wait(Rest(.9f));yield return Prime(.8f);
                // Gravity of teeth: heavy upward fangs descend under gravity; the next row comes from the lips.
                yield return Every(6.5f,.56f,n=>
                {
                    for(int s=-1;s<=1;s+=2)for(int i=0;i<Dense(8);i++)
                    {
                        Fire<SimpleRound>(Port(s<0?0:1),CombatVisuals.Rotate(Vector2.up,s*(22+i*9)),2.6f,.26f).Fall(Vector2.down*1.5f).LivesFor(8);
                        if(n%2==0)Fire<NeedleRound>(Port(s<0?0:1),CombatVisuals.Rotate(Vector2.down,s*(i*8-24)),2.7f,.17f);
                    }
                });
            }
            else
            {
                // Unwriting: four arms fold rectangular sheets inward, with different speeds on each diagonal.
                yield return Every(7.5f,.31f,n=>
                {
                    for(int p=0;p<4;p++)for(int i=0;i<Dense(7);i++)
                    {
                        Vector2 axis=p<2?Vector2.down:p==2?Vector2.up:Vector2.down;
                        float sign=p%2==0?-1:1;
                        Fire<NeedleRound>(Port(p),CombatVisuals.Rotate(axis,sign*(i*10-30+n%3*5)),1.9f+(p%2)*.6f,.18f)
                            .Pause(.85f,.22f,CombatRound.Turn.Rotate,sign*65,1.45f).Bounce(1).FadeAt(8);
                    }
                });
                yield return Wait(Rest(1.2f));yield return Prime(.75f);
                // The last margin: wall-bound arrows peel off while split seeds fill the centre in staggered cones.
                yield return Every(7.5f,.46f,n=>
                {
                    for(int s=-1;s<=1;s+=2)
                    {
                        for(int i=0;i<Dense(9);i++)Fire<NeedleRound>(Port(s<0?0:1),CombatVisuals.Rotate(new Vector2(s,0),(i-4)*8),2.6f,.17f).RunWalls(1.4f,s).FadeAt(8);
                        Fire<SimpleRound>(Port(3),CombatVisuals.Rotate(Vector2.down,s*(18+n%3*13)),1.6f,.38f).SplitAt(1.3f,Dense(9),72,2.6f);
                    }
                });
            }
            yield return Wait(Rest(1.2f));
        }
    }
}
