using UnityEngine;

public abstract class DistrictMiniboss : DistrictEnemy
{
    float maximum,clock,retreatClock,engagedAt=-1;
    public bool Engaged {get;private set;}
    public bool Retreating {get;private set;}
    public float Remaining {get;private set;}=20;
    public float HealthFraction=>health/Mathf.Max(1,maximum);
    public override bool CanBeTargeted=>!Retreating && health>0;
    protected bool Enraged=>health<maximum*.5f;
    protected override void Start()
    {
        base.Start();maximum=health;scoreOnKill=400;
        if(movement!=null){movement.enabled=false;Destroy(movement);movement=null;}
    }
    protected override void Update()
    {
        if(GameRules.playerDisabled)return;
        float dt=Time.deltaTime*LevelRun.EnemyTempoScale;age+=dt;clock+=dt*MovementScale;
        if(Retreating)
        {
            retreatClock+=dt;transform.position+=Vector3.up*dt*(5+retreatClock*8);
            if(transform.position.y>Camera.UpLimit+4)Depart();return;
        }
        var target=Position(clock);
        transform.position=Vector3.MoveTowards(transform.position,target,dt*(Engaged ? 8 : 6));
        if(transform.position.y<Camera.UpLimit-1.6f)Engaged=true;
        if(Engaged)
        {
            if(engagedAt<0){engagedAt=Time.time;nextAttack=Mathf.Max(nextAttack,age+.35f);}
            float left=Mathf.Max(0,20-(Time.time-engagedAt));SetRemaining(left);
            if(left<=0){Retreat();return;}
        }
        firingCue?.Warn((nextAttack-age)/LevelRun.EnemyTempoScale);
        if(Engaged && age>=nextAttack){volley++;Attack();nextAttack=age+attackInterval*(Enraged ? .78f : 1)/Mathf.Max(.5f,GameRules.enemyShootSpeed);}
    }
    protected virtual Vector3 Position(float t)
    {
        float x=0,y=Camera.UpLimit-2.8f;
        switch(LevelRun.Current)
        {
            case LevelId.Fields:x=Mathf.Sin(t*.75f)*2.0f;y+=Mathf.Sin(t*.4f)*.35f;break;
            case LevelId.Snow:x=Mathf.Sin(t*.9f)*1.6f;y+=Mathf.Cos(t*.9f)*.7f;break;
            case LevelId.City:x=Mathf.Sin(t*1.4f)*2.8f;y+=Mathf.Sin(t*2.8f)*.7f;break;
            case LevelId.Tunnel:x=Mathf.PingPong(t*2,5.6f)-2.8f;y+=Mathf.Sin(t*1.3f)*.65f;break;
            case LevelId.Thunder:x=Mathf.Sin(t*1.1f)*2.6f;y+=Mathf.Sin(t*.7f)*.8f;break;
            case LevelId.Desert:x=Mathf.Sin(t*.85f)*2.3f;y-=1.0f+Mathf.Sin(t*1.7f)*1.0f;break;
            case LevelId.Candyland:x=Mathf.Sin(t*1.85f)*2.8f;y-=.7f+Mathf.Sin(t*2.7f)*1.3f;break;
            case LevelId.Space:x=Mathf.Sin(t*1.1f)*2.5f;y-=.4f+Mathf.Cos(t*1.1f)*1.2f;break;
            case LevelId.Underwater:x=Mathf.Sin(t*.8f)*2.3f;y-=.7f+Mathf.Sin(t*1.6f)*.8f;break;
            case LevelId.Hell:x=Mathf.Sin(t*1.2f)*2.5f;y-=.5f+Mathf.Cos(t*2.4f)*.8f;break;
        }
        return new Vector3(x,y,1);
    }
    public void SetRemaining(float remaining)
    {
        Remaining=Mathf.Max(0,remaining);
    }
    public void Retreat()
    {
        Retreating=true;
        foreach(var round in CombatRound.Live)if(round is PulseLaser beam && beam.IsAttachedTo(Hull))beam.Cease();
    }
    public override void damage(float amount){if(!Retreating)base.damage(amount);}
}
