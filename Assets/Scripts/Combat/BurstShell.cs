using UnityEngine;
public sealed class BurstShell : CombatRound
{
    ParticleSystem ignition;
    float nextSpark;
    float cruise,brake;
    public float TravelDistance {get;private set;}
    protected override void Initialize()
    {
        visual.color=Color.white;nextSpark=0;if(ignition!=null){ignition.Clear();ignition.Play();}
        SetTravelDistance(3.8f);
    }
    public BurstShell SetTravelDistance(float distance)
    {
        TravelDistance=Mathf.Max(.1f,distance);
        float brakingDistance=Mathf.Min(TravelDistance*.35f,1.5f);
        float velocity=Mathf.Max(.1f,speed);
        cruise=(TravelDistance-brakingDistance)/velocity;brake=2*brakingDistance/velocity;
        lifetime=Mathf.Max(9,cruise+brake+3);
        return this;
    }
    float DistanceAt(float t)
    {
        float coast=Mathf.Min(t,cruise),slow=Mathf.Clamp(t-cruise,0,brake);
        return speed*(coast+slow-slow*slow/(2*brake));
    }
    void Spark(float charge)
    {
        if(ignition==null)
        {
            ignition=PSXImpactEffects.Create(transform,"Shell ignition sparks",40);
            ignition.GetComponent<ParticleSystemRenderer>().sortingOrder=14;
        }
        float shellRadius=transform.localScale.x*.5f;
        float radius=Mathf.Lerp(shellRadius*2.3f+.2f,shellRadius*1.1f+.025f,Mathf.SmoothStep(0,1,charge));
        for(int i=0;i<4;i++)
        {
            float angle=Random.value*Mathf.PI*2;
            var direction=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
            var particle=new ParticleSystem.EmitParams();
            float life=Mathf.Lerp(.25f,.14f,charge)*Random.Range(.85f,1.1f);
            particle.position=transform.position+direction*radius+Vector3.back*.12f;
            particle.velocity=-direction*(radius-shellRadius*.72f)/life;
            particle.startColor=Color.Lerp(Accent,Color.white,charge*.30f);
            particle.startSize3D=Vector3.one*Random.Range(.055f,.085f);
            particle.startLifetime=life;ignition.Emit(particle,1);
        }
    }
    protected override void Tick(float dt)
    {
        transform.position+=(Vector3)heading*(DistanceAt(age)-DistanceAt(Mathf.Max(0,age-dt)));
        float stop=cruise+brake;
        float charge=Mathf.Clamp01((age-stop)/.8f);
        visual.color=Color.white;
        if(age>=stop && age>=nextSpark){Spark(charge);nextSpark=age+Mathf.Lerp(.10f,.035f,charge);}
        if(age>=stop+.8f && Ring(12,3.6f,.14f,Vector2.SignedAngle(Vector2.down,heading)))Release();
    }
}
