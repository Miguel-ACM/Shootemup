using System.Collections;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SeraphJudicatorBoss : ArmoredHullBoss
{
    static readonly Vector2[] outline={new(0,-.82f),new(-.6f,-.6f),new(-.91f,0),new(-.6f,.6f),new(0,.82f),new(.6f,.6f),new(.91f,0),new(.6f,-.6f)};
    protected override Vector2[] Outline=>outline;
    public HeavenAngelVisual Angel { get; private set; }
    public int CardIndex { get; private set; }
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
        if(!anchored)transform.position=Vector3.MoveTowards(transform.position,station+new Vector3(Mathf.Sin(motionAge*.4f)*1.35f,Mathf.Cos(motionAge*.6f)*.35f),dt*1.2f);
    }
    void Pray(bool value){anchored=value;Angel.Pray(value);}
    PulseLaser Beam(int port,float angle,float width,float hold,float charge=1.4f)
        =>HeavenArmory.Lance(Angel.Emitter(port),Vector3.zero,CombatVisuals.Rotate(Vector2.down,angle),width,hold,charge);
    protected override void OnPhaseEntered(int phase){Pray(false);}
    protected override IEnumerator Cards()
    {
        while(true)
        {
            CardIndex=0;Pray(true);
            yield return Charge(Angel.Emitter(0),Vector3.zero,.65f);
            if(CurrentPhase==1)
            {
                // Dawn Liturgy: a golden nave between two lances, feather-vaults above it.
                Beam(1,-14,.92f,2.7f);Beam(2,14,.92f,2.7f);
                yield return Every(4.4f,.28f,n=>Vault(n,13,29,false));
            }
            else if(CurrentPhase==2)
            {
                // Sevenfold Procession: the entire lace opens and closes around a moving aisle.
                Beam(1,-22,1.05f,3.0f);Beam(2,22,1.05f,3.0f);
                yield return Every(4.7f,.24f,n=>Vault(n,17,38,true));
            }
            else
            {
                // Last Trumpet: a central column framed by symmetrical upward lancets.
                Beam(0,0,2.1f,3.1f,1.7f);
                Beam(1,-47,.85f,2.8f,1.7f);Beam(2,47,.85f,2.8f,1.7f);
                yield return Every(5.2f,.22f,n=>Vault(n,19,52,true));
            }
            Pray(false);yield return Wait(Rest(1.25f));
            CardIndex=1;Pray(true);
            yield return Charge(Angel.Emitter(0),Vector3.zero,.65f);
            if(CurrentPhase==1)
            {
                // Cathedral of Eyes: fixed, reflected arches over a slow central lance.
                Beam(0,0,1.7f,2.8f,1.6f);
                yield return Every(4.8f,.66f,n=>Window(n,19,2.15f));
            }
            else if(CurrentPhase==2)
            {
                // Judgment Pendulum: a single deliberate sweep and pearled arch ribs.
                var beam=Beam(0,-22,1.12f,3.6f,1.65f);
                if(beam!=null){beam.sweep=10;beam.reversalArc=44;}
                yield return Every(5.6f,.56f,n=>Window(n,23,2.2f));
            }
            else
            {
                // Covenant: two opposed slow scythes; the same windows now have an inner rib.
                for(int s=-1;s<=1;s+=2)
                {
                    var beam=Beam(s<0?1:2,s*30,1.0f,3.5f,1.7f);
                    if(beam!=null)beam.sweep=-s*6;
                }
                yield return Every(5.6f,.48f,n=>Window(n,27,2.3f));
            }
            Pray(false);yield return Wait(Rest(1.45f));
        }
    }
    static float Across(int i,int count)=>count<=1?0:i/(count-1f)*2-1;
    void Vault(int beat,int count,float opening,bool pearls)
    {
        int n=Dense(count);
        float breath=Mathf.Sin(beat*.18f)*12;
        for(int s=-1;s<=1;s+=2)for(int i=0;i<n;i++)
        {
            float u=Across(i,n),angle=s*(opening+breath+u*17);
            // Speed curvature makes a pointed arch in space rather than a straight fan.
            float speed=2.15f+.7f*(1-u*u);
            Fire<NeedleRound>(Eye(s<0?3:4),CombatVisuals.Rotate(Vector2.up,angle),speed,.17f).Bounce(1).FadeAt(8);
            if(pearls&&beat%4==0&&i%3==0)
                Fire<SimpleRound>(Eye(s<0?1:2),CombatVisuals.Rotate(Vector2.down,s*(48+u*23)),1.55f,.22f).FadeAt(7.8f);
        }
    }
    void Window(int beat,int count,float speed)
    {
        int n=Dense(count);
        for(int s=-1;s<=1;s+=2)for(int i=0;i<n;i++)
        {
            float u=Across(i,n);
            float a=s*(46+u*31+(beat%2==0?6:-6));
            Fire<NeedleRound>(Eye(s<0?3:4),CombatVisuals.Rotate(Vector2.up,a),speed+.85f*(1-u*u),.18f)
                .Bounce(1).Ramp(1.25f,1.4f,2).FadeAt(8);
            if(CurrentPhase==3&&i%3==0)
                Fire<SimpleRound>(Eye(s<0?1:2),CombatVisuals.Rotate(Vector2.down,s*(54+u*26)),1.6f+.3f*u*u,.22f).FadeAt(8);
        }
    }
}
