using UnityEngine;
public abstract class DistrictBoss : DistrictEnemy
{
    protected float initialHealth;
    protected bool phaseTwo;
    protected ResourceBar bar;
    protected Vector3 station;
    protected float motionAge;
    float nextPressure;
    public virtual bool UsesHealthBar=>true;
    public virtual float DisplayFraction=>HealthFraction;
    public float HealthFraction=>health/Mathf.Max(1,initialHealth);
    protected override void Start()
    {
        base.Start();initialHealth=health;isBoss=true;scoreOnKill=1600;
        lifeDropChance=manaDropChance=moneyDropChance=1;minmaxLife=new[]{4,6};minmaxMana=new[]{6,9};minmaxMoney=new[]{10,15};
        var ui=GameObject.Find("UI/ItemsRight/BossHealth");if(ui!=null && UsesHealthBar){bar=ui.GetComponent<ResourceBar>();bar.SetMax(health);bar.SetVisible(true);}
        station=new Vector3(0,Camera.UpLimit-2.3f,1);
        nextPressure=nextAttack;
    }
    protected override void Update()
    {
        if(GameRules.playerDisabled)return;
        age+=Time.deltaTime*LevelRun.EnemyTempoScale;
        firingCue?.Warn((nextAttack-age)/LevelRun.EnemyTempoScale);
        if(!phaseTwo && PhaseTwoReady){phaseTwo=true;NotifyBossPhaseChange(2);OnPhaseTwo();}
        float motionDelta=Time.deltaTime*LevelRun.EnemyTempoScale*MovementScale;motionAge+=motionDelta;
        MoveBoss(motionDelta);
        if(!enteredFight && transform.position.y<Camera.UpLimit-1){enteredFight=true;nextAttack=Mathf.Max(nextAttack,age+.35f);nextPressure=Mathf.Max(nextPressure,age+.35f);}
        if(transform.position.y<Camera.UpLimit-1 && age>=nextAttack)
        {
            volley++;Attack();nextAttack=age+attackInterval*AttackPace/Mathf.Max(.5f,GameRules.enemyShootSpeed);
        }
        if(transform.position.y<Camera.UpLimit-1 && age>=nextPressure)
        {
            PressureAttack();nextPressure=age+PressureInterval/Mathf.Max(.5f,GameRules.enemyShootSpeed);
        }
        UpdateBar();
    }
    protected virtual void MoveBoss(float dt){transform.position=Vector3.MoveTowards(transform.position,station,dt*2);}
    protected virtual bool PhaseTwoReady=>HealthFraction<=.5f;
    protected virtual float AttackPace=>phaseTwo ? .72f : 1;
    protected virtual float PressureInterval=>phaseTwo ? .55f : .85f;
    protected virtual void PressureAttack(){}
    protected virtual void OnPhaseTwo(){}
    protected virtual void UpdateBar(){if(bar!=null)bar.Set(health);}
    protected override void Die(){if(bar!=null)bar.SetVisible(false);base.Die();}
    // A continuous scalloped front: symmetric speeds bow each fan into a petal.
    // Geometry and shot counts stay deterministic; nothing is added per projectile.
    protected void PetalFan<T>(Vector3 port,Vector2 axis,int count,float spacing,float speed,float radius,float curvature=.12f) where T:CombatRound
    {
        for(int i=0;i<count;i++)
        {
            float lane=i-(count-1)*.5f;
            float t=count<=1 ? 0 : lane/((count-1)*.5f);
            Fire<T>(port,CombatVisuals.Rotate(axis,lane*spacing),speed*(1+curvature*(1-t*t)),radius);
        }
    }
    protected void Radial<T>(int count,float speed,float radius,float offset=0) where T:CombatRound
    {
        for(int i=0;i<count;i++)Fire<T>(transform.position,CombatVisuals.Rotate(Vector2.down,i*360f/count+offset),speed,radius);
    }
}
