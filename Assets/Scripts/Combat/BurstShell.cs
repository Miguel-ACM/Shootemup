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
        if(ignition==null)ignition=PSXImpactEffects.Create(transform,"Shell ignition sparks",32);
        for(int i=0;i<4;i++)
        {
            float angle=Random.value*Mathf.PI*2;
            var direction=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
            var particle=new ParticleSystem.EmitParams();
            particle.position=transform.position+direction*transform.localScale.x*.72f+Vector3.back*.12f;
            particle.velocity=direction*Random.Range(.35f,1.1f);
            particle.startColor=Color.Lerp(new Color(1,.42f,.08f),new Color(1,.85f,.3f),charge);
            particle.startSize3D=Vector3.one*Random.Range(.065f,.105f);
            particle.startLifetime=Random.Range(.18f,.30f);ignition.Emit(particle,1);
        }
    }
    protected override void Tick(float dt)
    {
        transform.position+=(Vector3)heading*(DistanceAt(age)-DistanceAt(Mathf.Max(0,age-dt)));
        float stop=cruise+brake;
        float charge=Mathf.Clamp01((age-stop)/.8f);
        visual.color=Color.white;
        if(age>=stop && age>=nextSpark){Spark(charge);nextSpark=age+Mathf.Lerp(.10f,.035f,charge);}
        if(age>=stop+.8f){Ring(12,3.6f,.14f,Vector2.SignedAngle(Vector2.down,heading));Release();}
    }
}
