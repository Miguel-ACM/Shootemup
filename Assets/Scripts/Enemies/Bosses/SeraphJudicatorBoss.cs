using System.Collections;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SeraphJudicatorBoss : ArmoredHullBoss
{
    static readonly Vector2[] outline={new(0,-.82f),new(-.6f,-.6f),new(-.91f,0),new(-.6f,.6f),new(0,.82f),new(.6f,.6f),new(.91f,0),new(.6f,-.6f)};
    protected override Vector2[] Outline=>outline;
    public HeavenAngelVisual Angel { get; private set; }
    bool anchored;
    protected override void Configure(){health=2850;hullScale=1.05f;attackInterval=.7f;}
    protected override void Start()
    {
        base.Start();
        GetComponent<CombatHullVisual>().enabled=false;
        Angel=Hull.gameObject.AddComponent<HeavenAngelVisual>();
        Angel.Build(GetComponent<PolygonCollider2D>());
        PartHitFeedback.Bind(Hull);
        FollowEyes();
    }
    protected override void Update()
    {
        if(WorldTimeStop.Frozen)return;
        if(Angel!=null&&!GameRules.playerDisabled){Angel.Step(Time.deltaTime*LevelRun.EnemyTempoScale);FollowEyes();}
        base.Update();
    }
    void FollowEyes(){for(int i=0;i<5;i++)firingCue.FollowPort(i,Angel.Emitter(i));}
    Vector3 Eye(int index)=>Angel.Emitter(index).position;
    protected override void MoveBoss(float dt)
    {
        station=new Vector3(0,Camera.UpLimit-3.1f,1);
        if(!openingComplete){base.MoveBoss(dt*2);return;}
        if(!anchored)transform.position=Vector3.MoveTowards(transform.position,station+new Vector3(Mathf.Sin(motionAge*.4f)*1.1f,Mathf.Cos(motionAge*.6f)*.35f),dt*1.2f);
    }
    PulseLaser Beam(int port,float angle,float width,float hold,float charge=1.4f)
        =>HeavenArmory.Lance(Angel.Emitter(port),Vector3.zero,CombatVisuals.Rotate(Vector2.down,angle),width,hold,charge);
    protected override void OnPhaseEntered(int phase){anchored=false;}
    protected override IEnumerator Cards()
    {
        while(true)
        {
            if(CurrentPhase==1)
            {
                // Gates of Dawn: open outward from a safe central aisle, then force a side change.
                anchored=true;Beam(1,-8,1.25f,2.6f);Beam(2,8,1.25f,2.6f);
                yield return Every(3.8f,.47f,n=>Feathers(n,6,2.5f,false));
                anchored=false;yield return Wait(Rest(1.0f));
                // The First Trumpet: a broad centre lance with a refracted ceiling canopy.
                anchored=true;Beam(0,0,2.0f,2.7f,1.65f);
                yield return Every(4.3f,.55f,n=>Canopy(n,7));
            }
            else if(CurrentPhase==2)
            {
                // Spear of the Firmament: sequential banks, never an instantaneous full-width wall.
                anchored=true;Beam(1,-16,1.5f,2.4f);Beam(2,16,1.5f,2.4f);
                yield return Wait(Rest(1.8f));
                Beam(0,0,1.75f,2.1f,1.5f);
                yield return Every(3.9f,.36f,n=>Feathers(n,8,2.6f,true));
                anchored=false;yield return Wait(Rest(1.1f));
                // Judgment Pendulum: the entire beam swings after a long, stationary charge.
                anchored=true;var beam=Beam(0,-24,1.45f,4.5f,1.7f);
                if(beam!=null){beam.sweep=12;beam.reversalArc=48;}
                yield return Every(6.2f,.64f,n=>Canopy(n,8));
            }
            else
            {
                // Last Trumpet: a wide centre blast between outward wing lances.
                anchored=true;Beam(0,0,2.45f,3.1f,1.8f);Beam(1,-38,1.0f,2.8f,1.8f);Beam(2,38,1.0f,2.8f,1.8f);
                yield return Every(5.0f,.44f,n=>Canopy(n,10));
                anchored=false;yield return Wait(Rest(1.2f));
                // Broken Covenant: opposing scythes of light fold inward, then leave a dark interval.
                anchored=true;
                for(int s=-1;s<=1;s+=2){var beam=Beam(s<0?1:2,s*27,1.25f,3.8f,1.65f);if(beam!=null)beam.sweep=-s*7;}
                yield return Every(5.4f,.29f,n=>Feathers(n,10,2.8f,true));
            }
            anchored=false;yield return Wait(Rest(1.4f));
        }
    }
    void Feathers(int beat,int count,float speed,bool returnFire)
    {
        int n=Dense(count);
        for(int s=-1;s<=1;s+=2)for(int i=0;i<n;i++)
        {
            float a=s*(30+i*7+Mathf.Sin(beat*.6f)*15);
            var shot=Fire<NeedleRound>(Eye(s<0?3:4),CombatVisuals.Rotate(Vector2.down,a),speed,.18f).Ramp(1.5f,.8f,.9f);
            if(returnFire&&i%3==0)shot.Bounce(1);
        }
    }
    void Canopy(int beat,int count)
    {
        int n=Dense(count);
        for(int s=-1;s<=1;s+=2)for(int i=0;i<n;i++)
            Fire<NeedleRound>(Eye(s<0?3:4),CombatVisuals.Rotate(Vector2.up,s*(15+i*8+beat%2*4)),2.6f,.19f).Bounce(1).FadeAt(8);
    }
}
