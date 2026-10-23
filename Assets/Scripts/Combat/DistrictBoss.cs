using UnityEngine;
public abstract class DistrictBoss : DistrictEnemy
{
    protected float initialHealth;
    protected bool phaseTwo;
    protected ResourceBar bar;
    protected Vector3 station;
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
    }
    protected override void Update()
    {
        if(GameRules.playerDisabled)return;
        age+=Time.deltaTime*LevelRun.EnemyTempoScale;
        if(!phaseTwo && HealthFraction<=.5f){phaseTwo=true;NotifyBossPhaseChange(2);OnPhaseTwo();}
        MoveBoss(Time.deltaTime*LevelRun.EnemyTempoScale);
        if(transform.position.y<Camera.UpLimit-1 && age>=nextAttack)
        {
            volley++;Attack();nextAttack=age+attackInterval*(phaseTwo ? .72f : 1)/Mathf.Max(.5f,GameRules.enemyShootSpeed);
        }
        if(transform.position.y<Camera.UpLimit-1 && age>=nextPressure)
        {
            PressureAttack();nextPressure=age+PressureInterval/Mathf.Max(.5f,GameRules.enemyShootSpeed);
        }
        UpdateBar();
    }
    protected virtual void MoveBoss(float dt){transform.position=Vector3.MoveTowards(transform.position,station,dt*2);}
    protected virtual float PressureInterval=>phaseTwo ? .55f : .85f;
    protected virtual void PressureAttack(){}
    protected virtual void OnPhaseTwo(){}
    protected virtual void UpdateBar(){if(bar!=null)bar.Set(health);}
    protected override void Die(){if(bar!=null)bar.SetVisible(false);base.Die();}
    protected void Radial<T>(int count,float speed,float radius,float offset=0) where T:CombatRound
    {
        for(int i=0;i<count;i++)CombatRound.Launch<T>(transform.position,CombatVisuals.Rotate(Vector2.down,i*360f/count+offset),speed,radius);
    }
}
