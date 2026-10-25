using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class CityDuoBoss : DistrictBoss
{
    public bool heavy;
    public CityDuoBoss partner;
    bool enraged;
    float combinedMaximum;
    Vector2 attackAxis=Vector2.down;
    public override float DisplayFraction=>(Mathf.Max(0,health)+(partner!=null ? Mathf.Max(0,partner.health) : 0))/Mathf.Max(1,combinedMaximum);
    protected override void Configure(){hull=12;hullScale=.8f;health=1000;attackInterval=.48f;shellTravelDistance=5.8f;}
    protected override void Start()
    {
        if(heavy)
        {
            hull=14;hullScale=1.15f;size=hullScale;Hull.GetComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.CombatHull(14);Hull.localScale=Vector3.one*hullScale;
            GetComponent<CircleCollider2D>().radius=hullScale*.8f;GetComponent<EnemyThrusters>().sockets=IndustrialMeshFactory.CombatEngineSockets(14);
        }
        base.Start();
        if(partner==null && !heavy)
        {
            partner=(CityDuoBoss)Spawn(typeof(CityDuoBoss),new Vector3(2,Camera.UpLimit+2,1));
            partner.heavy=true;partner.health=1400*GameRules.enemyHealth*LevelRun.EnemyHealthScale;partner.partner=this;
            ShareEncounterWith(partner);
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
    // Both ships keep one tempo until a partner dies; independent half-health
    // acceleration used to scramble the duo's alternating releases.
    protected override bool PhaseTwoReady=>false;
    protected override float AttackPace=>enraged ? .64f : 1;
    protected override void PressureAttack(){}
    protected override void Attack()
    {
        int beat=(volley-1)%12;
        if(beat==0 || beat==1 || beat==8)attackAxis=Aim;
        if(heavy && (beat==0 || beat==2 || beat==4))
        {
            int count=enraged ? 5 : 4;
            for(int i=0;i<count;i++)Fire<BurstShell>(Hull.TransformPoint(new Vector3(0,-.7f,-.3f)),
                CombatVisuals.Rotate(attackAxis,(i-(count-1)*.5f)*18),4.4f,.4f);
        }
        else if(heavy && (beat==6 || beat==7))
        {
            int count=enraged ? 19 : 15;
            PetalFan<SimpleRound>(transform.position,CombatVisuals.Rotate(Vector2.down,beat==6 ? -2.25f : 2.25f),count,9,4.7f,.12f,-.12f);
        }
        else if(!heavy && (beat==1 || beat==3 || beat==5))
        {
            int count=enraged ? 15 : 11;
            PetalFan<NeedleRound>(transform.position,CombatVisuals.Rotate(attackAxis,(beat-3)*3),count,6,7.6f,.13f,.15f);
        }
        else if(!heavy && (beat==8 || beat==9))Fan<GuidedMissile>(enraged ? 5 : 3,17,5.5f);
        // A lone survivor fills its missing partner's accents without continuous noise.
        else if(enraged && (beat==2 || beat==4 || beat==6))Fan<NeedleRound>(9,9,7);
    }
    protected override void UpdateBar(){if(bar!=null)bar.Set(Mathf.Max(0,health)+(partner!=null ? Mathf.Max(0,partner.health) : 0));}
    protected override void Die()
    {
        if(partner!=null && partner.health>0)
        {
            partner.health+=Mathf.Max(0,partner.initialHealth-partner.health)*.5f;
            partner.enraged=true;partner.phaseTwo=true;partner.volley=0;partner.nextAttack=partner.age+.8f;partner.partner=null;
            partner.NotifyBossPhaseChange(2);
            // The first kill gives neither pickups nor an encounter-completion event.
            isBoss=false;lifeDropChance=manaDropChance=moneyDropChance=0;scoreOnKill=0;bar=null;
        }
        base.Die();
    }
}
