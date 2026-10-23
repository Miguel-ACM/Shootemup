using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class CityDuoBoss : DistrictBoss
{
    public bool heavy;
    public CityDuoBoss partner;
    bool enraged;
    float combinedMaximum;
    public override float DisplayFraction=>(Mathf.Max(0,health)+(partner!=null ? Mathf.Max(0,partner.health) : 0))/Mathf.Max(1,combinedMaximum);
    protected override void Configure(){hull=12;hullScale=.8f;health=1500;attackInterval=1.1f;}
    protected override void Start()
    {
        if(heavy)
        {
            hull=14;hullScale=1.15f;size=hullScale;Hull.GetComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.CombatHull(14);Hull.localScale=Vector3.one*hullScale;
            GetComponent<CircleCollider2D>().radius=hullScale*.8f;GetComponent<EnemyThrusters>().sockets=IndustrialMeshFactory.CombatEngineSockets(14);attackInterval=1.7f;
        }
        base.Start();
        if(partner==null && !heavy)
        {
            partner=(CityDuoBoss)Spawn(typeof(CityDuoBoss),new Vector3(2,Camera.UpLimit+2,1));
            partner.heavy=true;partner.health=2100*GameRules.enemyHealth*(1+Mathf.Min(8,LevelRun.Stage-1)*.22f);partner.partner=this;
        }
        combinedMaximum=health+(partner!=null ? partner.health : 0);
        if(bar!=null)bar.SetMax(combinedMaximum);
    }
    protected override void MoveBoss(float dt)
    {
        float pace=heavy ? .6f : 1.6f;
        station.x=Mathf.Sin(age*pace+(heavy ? Mathf.PI : 0))*(heavy ? 2.1f : 3.2f);
        station.y=Camera.UpLimit-(heavy ? 2 : 3.5f);
        transform.position=Vector3.MoveTowards(transform.position,station,dt*(heavy ? 2 : 5)*(enraged ? 1.4f : 1));
    }
    protected override float PressureInterval=>enraged ? .3f : .65f;
    protected override void PressureAttack()
    {
        if(heavy)Radial<SimpleRound>(enraged ? 24 : 16,3.5f,.11f,age*28);
        else Fan<NeedleRound>(enraged ? 5 : 3,12,6.5f);
    }
    protected override void Attack()
    {
        if(heavy)
        {
            Fan<BurstShell>(enraged ? 5 : 3,22,3.6f,.4f);
            if(volley%2==0)Fan<GuidedMissile>(2,35,4);
        }
        else
        {
            Fan<NeedleRound>(enraged ? 5 : 3,8,7);
            if(volley%2==0)Fan<GuidedMissile>(2,25,5.5f);
        }
    }
    protected override void UpdateBar(){if(bar!=null)bar.Set(Mathf.Max(0,health)+(partner!=null ? Mathf.Max(0,partner.health) : 0));}
    protected override void Die()
    {
        if(partner!=null && partner.health>0)
        {
            partner.health+=Mathf.Max(0,partner.initialHealth-partner.health)*.5f;
            partner.enraged=true;partner.attackInterval*=.6f;partner.partner=null;
            partner.NotifyBossPhaseChange(2);
            // The first kill gives neither pickups nor an encounter-completion event.
            isBoss=false;lifeDropChance=manaDropChance=moneyDropChance=0;scoreOnKill=0;bar=null;
        }
        base.Die();
    }
}
