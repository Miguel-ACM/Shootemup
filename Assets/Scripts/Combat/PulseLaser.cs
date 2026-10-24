using UnityEngine;

public class PulseLaser : CombatRound
{
    BeamRibbon ribbon;
    LaserSound sound;
    Transform emitter;
    Vector3 muzzle;
    bool attached;
    bool startedSound;
    public float warning=.9f, activeTime=.65f, length=22, width=.25f, sweep;
    public bool sideShots;
    float nextSide,clockRate=1;
    protected override float ClockRate=>clockRate;
    const float Cooldown=.22f;
    public float Charge=>Mathf.Clamp01(age/Mathf.Max(.01f,warning));
    public bool IsFiring=>Active;
    public bool IsAttachedTo(Transform source)=>attached && emitter==source && isActiveAndEnabled;
    public float Power=>Active ? Mathf.Min(Mathf.Clamp01((age-warning)/.07f),Mathf.Lerp(.4f,1,Mathf.Clamp01((warning+activeTime-age)/.09f))) :
        age>=warning+activeTime ? Mathf.Pow(Mathf.Clamp01(1-(age-warning-activeTime)/Cooldown),2)*.4f : 0;
    protected override void Initialize()
    {
        warning=.9f;activeTime=.65f;length=22;width=.25f;sweep=0;sideShots=false;nextSide=0;
        emitter=null;attached=false;startedSound=false;clockRate=1;transform.localScale=Vector3.one;
        visual.enabled=false;GetComponent<Collider2D>().enabled=false;
        if(ribbon==null){ribbon=gameObject.AddComponent<BeamRibbon>();ribbon.Build();}
        if(sound==null)sound=gameObject.AddComponent<LaserSound>();
        ribbon.Draw(transform.position,heading,length,width,0,0);lifetime=12;
    }
    public PulseLaser Bind(Transform source,Vector3 localMuzzle)
    {
        emitter=source;muzzle=localMuzzle;attached=source!=null;
        if(attached)
        {
            transform.position=source.TransformPoint(localMuzzle);
            // The beam is part of its emitter's phrase, including charge, sweep and release.
            clockRate=Mathf.Max(.01f,LevelRun.EnemyTempoScale*(source.GetComponentInParent<FormationFlight>()!=null ? PatternMover.FlightTempo*GameRules.enemySpeed : 1));
        }
        return this;
    }
    protected virtual bool Active=>age>=warning && age<warning+activeTime;
    protected override void Tick(float dt)
    {
        if(!startedSound){startedSound=true;sound.Charge(warning/clockRate);}
        if(attached)
        {
            if(emitter==null || !emitter.gameObject.activeInHierarchy)
            {attached=false;age=Mathf.Max(age,warning+activeTime);}
            else transform.position=emitter.TransformPoint(muzzle);
        }
        if(age>warning+activeTime+Cooldown){Release();return;}
        if(Active)heading=CombatVisuals.Rotate(heading,sweep*dt);
        float power=Power;
        sound.Drive(power,Active);
        ribbon.Draw(transform.position,heading,length,width,Charge,power,Mathf.Max(0,age-warning-activeTime)/Cooldown);
        var player=GameObject.FindGameObjectWithTag("Player");
        if(Active && power>.15f && player!=null)
        {
            Vector2 delta=player.transform.position-transform.position;
            float along=Vector2.Dot(delta,heading),across=Mathf.Abs(delta.x*heading.y-delta.y*heading.x);
            if(along>=0 && along<=length && across<width*power*.5f+.16f)
                player.SendMessage("ReceiveHazardDamage",damage,SendMessageOptions.DontRequireReceiver);
        }
        if(Active && sideShots && age>=nextSide)
        {
            nextSide=age+.45f;var side=new Vector2(-heading.y,heading.x);
            for(int i=1;i<=3;i++)for(int sign=-1;sign<=1;sign+=2)
                Launch<SimpleRound>(transform.position+(Vector3)heading*(i*2.3f),side*sign,2.4f,.12f);
        }
    }
    public override void Vanish(){Release();}
}
