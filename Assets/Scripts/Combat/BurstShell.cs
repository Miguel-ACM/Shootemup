using UnityEngine;
public sealed class BurstShell : CombatRound
{
    ParticleSystem ignition;
    float nextSpark;
    protected override void Initialize(){visual.color=Color.white;nextSpark=0;if(ignition!=null)ignition.Clear();}
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
        transform.position+=(Vector3)heading*speed*Mathf.Clamp01(1-age/1.25f)*dt;
        float charge=Mathf.Clamp01((age-1.25f)/.8f);
        visual.color=Color.white;
        if(age>=1.25f && age>=nextSpark){Spark(charge);nextSpark=age+Mathf.Lerp(.10f,.035f,charge);}
        if(age>=2.05f){Ring(12,3.6f,.14f,Vector2.SignedAngle(Vector2.down,heading));Release();}
    }
}
