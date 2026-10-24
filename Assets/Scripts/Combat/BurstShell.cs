using UnityEngine;
public sealed class BurstShell : CombatRound
{
    ParticleSystem ignition;
    float nextSpark;
    static int flights;
    float cruise,brake;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetFlights(){flights=0;}
    protected override void Initialize()
    {
        visual.color=Color.white;nextSpark=0;if(ignition!=null)ignition.Clear();
        // Every third shell carries its payload deep into the arena.
        bool longFlight=flights++%3==2;cruise=longFlight ? 1.8f : .65f;brake=longFlight ? .85f : .6f;
    }
    float DistanceAt(float t)
    {
        float coast=Mathf.Min(t,cruise),slow=Mathf.Clamp(t-cruise,0,brake);
        return speed*(coast+slow-slow*slow/(2*brake));
    }
    void Spark(float charge)
    {
        if(ignition==null)ignition=PSXImpactEffects.Create(transform,"Shell ignition sparks",32);
        for(int i=0;i<3;i++)
        {
            float angle=Random.value*Mathf.PI*2;
            var direction=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
            var particle=new ParticleSystem.EmitParams();
            particle.position=transform.position+direction*transform.localScale.x*.4f+Vector3.back*.12f;
            particle.velocity=direction*Random.Range(.35f,1.1f);
            particle.startColor=Color.Lerp(new Color(1,.42f,.08f),new Color(1,.85f,.3f),charge);
            particle.startSize3D=Vector3.one*Random.Range(.045f,.075f);
            particle.startLifetime=Random.Range(.12f,.24f);ignition.Emit(particle,1);
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
