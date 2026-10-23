using UnityEngine;
public sealed class SuspendedRound : CombatRound
{
    protected override void Initialize(){visual.color=new Color(.45f,.9f,1);}
    protected override void Tick(float dt)
    {
        float factor=age<.8f ? 1-age/.8f : age<1.7f ? 0 : Mathf.Min(1.5f,(age-1.7f)*2);
        transform.position+=(Vector3)heading*speed*factor*dt;Orient();
        visual.color=Color.Lerp(new Color(.4f,.7f,1),Color.white,age<1.7f ? Mathf.PingPong(age*3,1) : .6f);
    }
}
